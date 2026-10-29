using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging;
using Project.Api.Models.DTOs;

namespace Project.Api.Clients;

public interface ITaskServiceClient
{
    /// <summary>
    /// Per-project task status counts for the given projects, as seen by the caller whose
    /// bearer token is forwarded (Task.Api applies its own ownership filtering — this never
    /// bypasses auth). On any failure (timeout, Task.Api down) returns an empty dictionary
    /// rather than throwing, so the dashboard degrades to zero counts instead of failing outright.
    /// </summary>
    Task<Dictionary<Guid, TaskStatusCountsDto>> GetStatusCountsAsync(
        IReadOnlyCollection<Guid> projectIds, string bearerToken, CancellationToken ct);
}

internal record StatusCountsRequest(List<Guid> ProjectIds);
internal record StatusCountsEntry(Guid ProjectId, int Todo, int InProgress, int Done);

public class TaskServiceClient : ITaskServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<TaskServiceClient> _logger;

    public TaskServiceClient(HttpClient httpClient, ILogger<TaskServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<Dictionary<Guid, TaskStatusCountsDto>> GetStatusCountsAsync(
        IReadOnlyCollection<Guid> projectIds, string bearerToken, CancellationToken ct)
    {
        if (projectIds.Count == 0)
        {
            return [];
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, "/api/internal/tasks/status-counts")
            {
                Content = JsonContent.Create(new StatusCountsRequest([.. projectIds]))
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _httpClient.SendAsync(request, cts.Token);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Task.Api status-counts call returned {Status}", response.StatusCode);
                return [];
            }

            var entries = await response.Content.ReadFromJsonAsync<List<StatusCountsEntry>>(cancellationToken: cts.Token) ?? [];
            return entries.ToDictionary(e => e.ProjectId, e => new TaskStatusCountsDto(e.Todo, e.InProgress, e.Done));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch task status counts from Task.Api");
            return [];
        }
    }
}
