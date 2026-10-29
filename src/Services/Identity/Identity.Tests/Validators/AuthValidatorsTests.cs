using FluentAssertions;
using Identity.Api.Models.DTOs;
using Identity.Api.Validators;
using Xunit;

namespace Identity.Tests.Validators;

public class AuthValidatorsTests
{
    [Fact]
    public void RegisterRequestValidator_RejectsShortPassword()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("user@example.com", "short", "User"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterRequestValidator_RejectsInvalidEmail()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("not-an-email", "Password123!", "User"));
        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void RegisterRequestValidator_AcceptsValidInput()
    {
        var result = new RegisterRequestValidator().Validate(new RegisterRequest("user@example.com", "Password123!", "User"));
        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void LoginRequestValidator_RequiresPassword()
    {
        var result = new LoginRequestValidator().Validate(new LoginRequest("user@example.com", ""));
        result.IsValid.Should().BeFalse();
    }
}
