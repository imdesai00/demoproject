using FluentAssertions;
using Moq;
using TaskManager.Application.Common.Exceptions;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Application.DTOs.Tasks;
using TaskManager.Application.Services;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;
using Xunit;

namespace TaskManager.UnitTests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly TaskService _sut;

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepository.Object, _projectRepository.Object);
    }

    [Fact]
    public async Task CreateAsync_WhenUserOwnsProject_CreatesTask()
    {
        var userId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), UserId = userId, Name = "Project" };

        _projectRepository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var request = new CreateTaskRequest("Task title", "desc", TaskPriority.High, null);

        var result = await _sut.CreateAsync(userId, project.Id, request);

        result.Title.Should().Be("Task title");
        result.Status.Should().Be(ProjectTaskStatus.Todo);
        _taskRepository.Verify(r => r.AddAsync(It.Is<ProjectTask>(t => t.ProjectId == project.Id), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WhenUserDoesNotOwnProject_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), UserId = ownerId, Name = "Project" };

        _projectRepository.Setup(r => r.GetByIdAsync(project.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(project);

        var act = () => _sut.CreateAsync(otherUserId, project.Id, new CreateTaskRequest("Title", null, TaskPriority.Low, null));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }

    [Fact]
    public async Task UpdateStatusAsync_UpdatesTaskStatus()
    {
        var userId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), UserId = userId, Name = "Project" };
        var task = new ProjectTask
        {
            Id = Guid.NewGuid(),
            ProjectId = project.Id,
            Title = "Task",
            Status = ProjectTaskStatus.Todo,
            Project = project
        };

        _taskRepository.Setup(r => r.GetByIdWithProjectAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var result = await _sut.UpdateStatusAsync(userId, project.Id, task.Id, new UpdateTaskStatusRequest(ProjectTaskStatus.Done));

        result.Status.Should().Be(ProjectTaskStatus.Done);
        _taskRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateStatusAsync_WhenTaskBelongsToAnotherUsersProject_ThrowsForbidden()
    {
        var ownerId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();
        var project = new Project { Id = Guid.NewGuid(), UserId = ownerId, Name = "Project" };
        var task = new ProjectTask { Id = Guid.NewGuid(), ProjectId = project.Id, Title = "Task", Project = project };

        _taskRepository.Setup(r => r.GetByIdWithProjectAsync(task.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(task);

        var act = () => _sut.UpdateStatusAsync(otherUserId, project.Id, task.Id, new UpdateTaskStatusRequest(ProjectTaskStatus.Done));

        await act.Should().ThrowAsync<ForbiddenAccessException>();
    }
}
