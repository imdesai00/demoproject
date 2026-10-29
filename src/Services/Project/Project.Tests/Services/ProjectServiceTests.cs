using System.Text.Json;
using Common.Data;
using FluentAssertions;
using Moq;
using Project.Api.Clients;
using Project.Api.Models.DTOs;
using Project.Api.Repositories;
using Project.Api.Services;
using Xunit;

namespace Project.Tests.Services;

public class ProjectServiceTests
{
    private readonly Mock<IProjectRepository> _projectRepository = new();
    private readonly Mock<ITaskServiceClient> _taskServiceClient = new();
    private readonly ProjectService _sut;
    private readonly ActorContext _actor = new(Guid.NewGuid(), "member");

    public ProjectServiceTests()
    {
        _sut = new ProjectService(_projectRepository.Object, _taskServiceClient.Object);
    }

    private static JsonElement ToJson(object value) => JsonSerializer.SerializeToElement(value);

    [Fact]
    public async Task GetAllAsync_MergesTaskCountsFromTaskServiceClient()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.Setup(r => r.GetAllAsync(_actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", null, ToJson(new[]
            {
                new { id = projectId, name = "P1", description = (string?)null, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            })));

        _taskServiceClient.Setup(c => c.GetStatusCountsAsync(It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(projectId)), "token", It.IsAny<CancellationToken>()))
            .ReturnsAsync(new Dictionary<Guid, TaskStatusCountsDto> { [projectId] = new(2, 1, 3) });

        var result = await _sut.GetAllAsync(_actor, "token", CancellationToken.None);

        result.Ok.Should().BeTrue();
        var projects = JsonSerializer.Deserialize<List<ProjectDto>>(result.Data!.Value.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        projects.Should().ContainSingle();
        projects[0].TaskCounts.Should().BeEquivalentTo(new TaskStatusCountsDto(2, 1, 3));
    }

    [Fact]
    public async Task GetAllAsync_WhenTaskClientFails_DefaultsToZeroCounts()
    {
        var projectId = Guid.NewGuid();
        _projectRepository.Setup(r => r.GetAllAsync(_actor, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(true, "OK", null, ToJson(new[]
            {
                new { id = projectId, name = "P1", description = (string?)null, created_at = DateTime.UtcNow, updated_at = DateTime.UtcNow }
            })));

        _taskServiceClient.Setup(c => c.GetStatusCountsAsync(It.IsAny<IReadOnlyCollection<Guid>>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var result = await _sut.GetAllAsync(_actor, "token", CancellationToken.None);

        var projects = JsonSerializer.Deserialize<List<ProjectDto>>(result.Data!.Value.GetRawText(), new JsonSerializerOptions(JsonSerializerDefaults.Web))!;
        projects[0].TaskCounts.Should().BeEquivalentTo(new TaskStatusCountsDto(0, 0, 0));
    }

    [Fact]
    public async Task DeleteAsync_WhenForbidden_PassesThroughResult()
    {
        _projectRepository.Setup(r => r.DeleteAsync(_actor, It.IsAny<Guid>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new FnResult(false, "FORBIDDEN", "Not your project", null));

        var result = await _sut.DeleteAsync(_actor, Guid.NewGuid(), CancellationToken.None);

        result.Ok.Should().BeFalse();
        result.Code.Should().Be("FORBIDDEN");
    }
}
