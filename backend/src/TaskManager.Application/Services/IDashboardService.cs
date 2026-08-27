using TaskManager.Application.DTOs.Dashboard;

namespace TaskManager.Application.Services;

public interface IDashboardService
{
    Task<DashboardSummaryDto> GetSummaryAsync(Guid userId, CancellationToken ct = default);
}
