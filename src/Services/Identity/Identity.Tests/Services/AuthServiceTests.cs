using System.Text.Json;
using Common.Data;
using Common.Security;
using FluentAssertions;
using Identity.Api.Models.DTOs;
using Identity.Api.Repositories;
using Identity.Api.Services;
using Moq;
using Xunit;

namespace Identity.Tests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<IAccessTokenIssuer> _tokenIssuer = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        _tokenIssuer
            .Setup(t => t.IssueAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), out It.Ref<DateTime>.IsAny))
            .Returns(new IssueAccessTokenDelegate((Guid id, string email, string name, out DateTime expiresAt) =>
            {
                expiresAt = DateTime.UtcNow.AddMinutes(15);
                return "fake-access-token";
            }));

        _refreshTokenRepository
            .Setup(r => r.CreateAsync(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<DateTime>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", null, null));

        _sut = new AuthService(_userRepository.Object, _refreshTokenRepository.Object, _tokenIssuer.Object,
            new JwtSettings { RefreshTokenDays = 7 });
    }

    private delegate string IssueAccessTokenDelegate(Guid userId, string email, string displayName, out DateTime expiresAt);

    private static JsonElement ToJson(object value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task RegisterAsync_WhenRepositorySucceeds_IssuesTokens()
    {
        _userRepository.Setup(r => r.RegisterAsync("new@example.com", It.IsAny<string>(), "New User", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", "Created", ToJson(new { id = Guid.NewGuid(), email = "new@example.com", display_name = "New User" })));

        var result = await _sut.RegisterAsync(new RegisterRequest("New@example.com", "Password123!", "New User"), CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.Data!.Value.GetProperty("accessToken").GetString().Should().Be("fake-access-token");
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailTaken_PassesThroughConflict()
    {
        _userRepository.Setup(r => r.RegisterAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(false, "CONFLICT", "An account with this email already exists", null));

        var result = await _sut.RegisterAsync(new RegisterRequest("taken@example.com", "Password123!", "User"), CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Code.Should().Be("CONFLICT");
        _tokenIssuer.Verify(t => t.IssueAccessToken(It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string>(), out It.Ref<DateTime>.IsAny), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WhenUserNotFound_ReturnsGenericUnauthenticated()
    {
        _userRepository.Setup(r => r.FindForLoginAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", null, null));

        var result = await _sut.LoginAsync(new LoginRequest("nobody@example.com", "whatever"), CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Code.Should().Be("UNAUTHENTICATED");
        result.Message.Should().Be("Invalid email or password");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordWrong_ReturnsGenericUnauthenticated()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!");
        _userRepository.Setup(r => r.FindForLoginAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", null, ToJson(new { id = Guid.NewGuid(), email = "user@example.com", password_hash = hash, display_name = "User" })));

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "WrongPassword"), CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Code.Should().Be("UNAUTHENTICATED");
    }

    [Fact]
    public async Task LoginAsync_WhenPasswordCorrect_IssuesTokens()
    {
        var hash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!");
        var userId = Guid.NewGuid();
        _userRepository.Setup(r => r.FindForLoginAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", null, ToJson(new { id = userId, email = "user@example.com", password_hash = hash, display_name = "User" })));

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "CorrectPassword1!"), CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.Data!.Value.GetProperty("user").GetProperty("id").GetGuid().Should().Be(userId);
    }
}
