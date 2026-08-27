using FluentAssertions;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.DTOs.Projects;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using Xunit;

namespace TaskManager.UnitTests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly ProjectService _sut;

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_projectRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_CreatesProjectOwnedByCurrentUser()
    {
        var userId = Guid.NewGuid();
        var request = new CreateProjectRequest("New Project", "A description");

        var result = await _sut.CreateAsync(userId, request);

        result.Name.Should().Be("New Project");
        _projectRepository.Verify(r => r.AddAsync(It.Is<Project>(p => p.UserId == userId && p.Name == "New Project"), It.IsAny<CancellationToken>()), Times.Once);
        _projectRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_WhenProjectBelongsToAnotherUser_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), UserId = ownerId, Name = "Owner's Project" };

        _projectRepository.Setup(r => r.GetByIdWithTasksAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var act = () => _sut.GetByIdAsync(otherUserId, project.Id);

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task GetByIdAsync_WhenProjectDoesNotExist_ThrowsNotFound()
    {
        var userId = Guid.NewGuid();
        var projectId = Guid.NewGuid();

        _projectRepository.Setup(r => r.GetByIdWithTasksAsync(projectId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Project?)null);

        var act = () => _sut.GetByIdAsync(userId, projectId);

        await act.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_WhenOwnedByUser_RemovesProject()
    {
        var userId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), UserId = userId, Name = "Mine" };

        _projectRepository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        await _sut.DeleteAsync(userId, project.Id);

        _projectRepository.Verify(r => r.Remove(project), Times.Once);
        _projectRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
