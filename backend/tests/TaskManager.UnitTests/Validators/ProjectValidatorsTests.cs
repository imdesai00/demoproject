using FluentAssertions;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.Validators;
using Xunit;

namespace TaskManager.UnitTests.Validators;

public class ProjectValidatorsTests
{
    private readonly CreateProjectRequestValidator _sut = new();

    [Fact]
    public void Validate_WithEmptyName_IsInvalid()
    {
        var result = _sut.Validate(new CreateProjectRequest("", "description"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Name");
    }

    [Fact]
    public void Validate_WithNameTooLong_IsInvalid()
    {
        var result = _sut.Validate(new CreateProjectRequest(new string('a', 201), null));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void Validate_WithValidData_IsValid()
    {
        var result = _sut.Validate(new CreateProjectRequest("Valid Name", "Valid description"));

        result.IsValid.Should().BeTrue();
    }
}
