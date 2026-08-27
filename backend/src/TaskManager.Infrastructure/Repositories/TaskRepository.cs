using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class TaskRepository : ITaskRepository
{
    private readonly ApplicationDbContext _context;

    public TaskRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<ProjectTask>> GetAllForProjectAsync(Guid projectId, CancellationToken ct = default) =>
        _context.ProjectTasks
            .Where(t => t.ProjectId == projectId)
            .OrderBy(t => t.CreatedAt)
            .ToListAsync(ct);

    public Task<ProjectTask?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.ProjectTasks.FirstOrDefaultAsync(t => t.Id == id, ct);

    public Task<ProjectTask?> GetByIdWithProjectAsync(Guid id, CancellationToken ct = default) =>
        _context.ProjectTasks.Include(t => t.Project).FirstOrDefaultAsync(t => t.Id == id, ct);

    public async Task AddAsync(ProjectTask task, CancellationToken ct = default) =>
        await _context.ProjectTasks.AddAsync(task, ct);

    public void Remove(ProjectTask task) => _context.ProjectTasks.Remove(task);

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
