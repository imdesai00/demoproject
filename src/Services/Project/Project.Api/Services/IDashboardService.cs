using Common.Data;
using Project.Api.Models.DTOs;

namespace Project.Api.Services;

public interface IDashboardService
{
    Task<FnResult> GetSummaryAsync(ActorContext actor, string bearerToken, CancellationToken ct);
}
