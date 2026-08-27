using FluentAssertions;
using Microsoft.Extensions.Options;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.Common.Options;
using TaskManager.Application.DTOs.Auth;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.UnitTests.Services;

public class AuthServiceTests
{
    private readonly Mock<IUserRepository> _userRepository = new();
    private readonly Mock<IRefreshTokenRepository> _refreshTokenRepository = new();
    private readonly Mock<ITokenService> _tokenService = new();
    private readonly AuthService _sut;

    public AuthServiceTests()
    {
        var jwtSettings = Options.Create(new JwtSettings { RefreshTokenDays = 7 });

        _tokenService
            .Setup(t => t.GenerateAccessToken(It.IsAny<User>(), out It.Ref<DateTime>.IsAny))
            .Returns(new GenerateAccessTokenDelegate((User user, out DateTime expiresAt) =>
            {
                expiresAt = DateTime.UtcNow.AddMinutes(15);
                return "fake-access-token";
            }));

        _tokenService.Setup(t => t.GenerateRefreshToken()).Returns("fake-refresh-token");
        _tokenService.Setup(t => t.HashRefreshToken(It.IsAny<string>())).Returns<string>(s => $"hashed-{s}");

        _sut = new AuthService(_userRepository.Object, _refreshTokenRepository.Object, _tokenService.Object, jwtSettings);
    }

    private delegate string GenerateAccessTokenDelegate(User user, out DateTime expiresAt);

    [Fact]
    public async Task RegisterAsync_WithNewEmail_CreatesUserAndReturnsTokens()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("newuser@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var request = new RegisterRequest("NewUser@example.com", "Password123!", "New User");

        var result = await _sut.RegisterAsync(request);

        result.AccessToken.Should().Be("fake-access-token");
        result.RefreshToken.Should().Be("fake-refresh-token");
        result.User.Email.Should().Be("newuser@example.com");
        _userRepository.Verify(r => r.AddAsync(It.Is<User>(u => u.Email == "newuser@example.com"), It.IsAny<CancellationToken>()), Times.Once);
        _userRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RegisterAsync_WithExistingEmail_ThrowsValidationException()
    {
        _userRepository.Setup(r => r.EmailExistsAsync("taken@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        var request = new RegisterRequest("taken@example.com", "Password123!", "Someone");

        var act = () => _sut.RegisterAsync(request);

        await act.Should().ThrowAsync<ValidationAppException>();
        _userRepository.Verify(r => r.AddAsync(It.IsAny<User>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task LoginAsync_WithCorrectPassword_ReturnsTokens()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!"),
            DisplayName = "User"
        };

        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var result = await _sut.LoginAsync(new LoginRequest("user@example.com", "CorrectPassword1!"));

        result.User.Id.Should().Be(user.Id);
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsAuthenticationException()
    {
        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@example.com",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("CorrectPassword1!"),
            DisplayName = "User"
        };

        _userRepository.Setup(r => r.GetByEmailAsync("user@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync(user);

        var act = () => _sut.LoginAsync(new LoginRequest("user@example.com", "WrongPassword"));

        await act.Should().ThrowAsync<Application.Common.Exceptions.AuthenticationException>();
    }

    [Fact]
    public async Task LoginAsync_WithUnknownEmail_ThrowsAuthenticationException()
    {
        _userRepository.Setup(r => r.GetByEmailAsync("nobody@example.com", It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        var act = () => _sut.LoginAsync(new LoginRequest("nobody@example.com", "whatever"));

        await act.Should().ThrowAsync<Application.Common.Exceptions.AuthenticationException>();
    }
}
