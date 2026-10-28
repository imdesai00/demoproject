using System.Net;
using System.Net.Http.Headers;
using Microsoft.Extensions.Logging;

namespace Task.Api.Clients;

public interface IProjectServiceClient
{
    /// <summary>
    /// True only if Project.Api confirms the caller (via their own forwarded bearer token) owns
    /// this project. Used once, at task creation — later reads/writes check the denormalized
    /// user_id on the task row directly instead of calling Project.Api again.
    /// </summary>
    Task<bool> UserOwnsProjectAsync(Guid projectId, string bearerToken, CancellationToken ct);
}

public class ProjectServiceClient : IProjectServiceClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ProjectServiceClient> _logger;

    public ProjectServiceClient(HttpClient httpClient, ILogger<ProjectServiceClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<bool> UserOwnsProjectAsync(Guid projectId, string bearerToken, CancellationToken ct)
    {
        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Get, $"/api/projects/{projectId}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", bearerToken);

            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(5));

            using var response = await _httpClient.SendAsync(request, cts.Token);

            // Project.Api itself enforces ownership on this endpoint (403/404 if not theirs/doesn't
            // exist) — a 200 here IS the ownership proof, nothing further to check.
            return response.StatusCode == HttpStatusCode.OK;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to verify project ownership with Project.Api for project {ProjectId}", projectId);
            return false;
        }
    }
}
