using Common.Data;
using FluentAssertions;
using Moq;
using Task.Api.Clients;
using Task.Api.Models.DTOs;
using Task.Api.Repositories;
using Task.Api.Services;
using Xunit;

namespace Task.Tests.Services;

public class TaskServiceTests
{
    private readonly Mock<ITaskRepository> _taskRepository = new();
    private readonly Mock<IProjectServiceClient> _projectServiceClient = new();
    private readonly TaskService _sut;
    private readonly ActorContext _actor = new(Guid.NewGuid(), "member");

    public TaskServiceTests()
    {
        _sut = new TaskService(_taskRepository.Object, _projectServiceClient.Object);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_WhenUserDoesNotOwnProject_ReturnsForbiddenWithoutCallingRepository()
    {
        _projectServiceClient.Setup(c => c.UserOwnsProjectAsync(It.IsAny<Guid>(), "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        var result = await _sut.CreateAsync(_actor, new CreateTaskRequest(Guid.NewGuid(), "Title", null, "Medium", null), "token", CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Code.Should().Be("FORBIDDEN");
        _taskRepository.Verify(r => r.CreateAsync(It.IsAny<ActorContext>(), It.IsAny<Guid>(), It.IsAny<string>(), It.IsAny<string?>(), It.IsAny<string>(), It.IsAny<DateOnly?>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async System.Threading.Tasks.Task CreateAsync_WhenUserOwnsProject_CreatesTask()
    {
        var projectId = Guid.NewGuid();
        _projectServiceClient.Setup(c => c.UserOwnsProjectAsync(projectId, "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _taskRepository.Setup(r => r.CreateAsync(_actor, projectId, "Title", null, "Medium", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", "Created", System.Text.Json.JsonSerializer.SerializeToElement(new
            {
                id = Guid.NewGuid(), project_id = projectId, title = "Title", description = (string?)null,
                status = "Todo", priority = "Medium", due_date = (DateOnly?)null,
                created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow
            })));

        var result = await _sut.CreateAsync(_actor, new CreateTaskRequest(projectId, "Title", null, "Medium", null), "token", CancellationToken.None);

        result.Ok.Should().BeTrue();
        result.Data!.Value.GetProperty("title").GetString().Should().Be("Title");
    }

    [Fact]
    public async System.Threading.Tasks.Task UpdateStatusAsync_WhenForbidden_PassesThroughResult()
    {
        _taskRepository.Setup(r => r.UpdateStatusAsync(_actor, It.IsAny<Guid>(), "Done", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(false, "FORBIDDEN", "Not your task", null));

        var result = await _sut.UpdateStatusAsync(_actor, Guid.NewGuid(), new UpdateTaskStatusRequest("Done"), CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Code.Should().Be("FORBIDDEN");
    }
}
