using Microsoft.EntityFrameworkCore;
using TaskManager.Application.Common.Interfaces;
using TaskManager.Domain.Entities;
using TaskManager.Infrastructure.Persistence;

namespace TaskManager.Infrastructure.Repositories;

public class ProjectRepository : IProjectRepository
{
    private readonly ApplicationDbContext _context;

    public ProjectRepository(ApplicationDbContext context)
    {
        _context = context;
    }

    public Task<List<Project>> GetAllForUserAsync(Guid userId, CancellationToken ct = default) =>
        _context.Projects
            .Include(p => p.Tasks)
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.UpdatedAt)
            .ToListAsync(ct);

    public Task<Project?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _context.Projects.FirstOrDefaultAsync(p => p.Id == id, ct);

    public Task<Project?> GetByIdWithTasksAsync(Guid id, CancellationToken ct = default) =>
        _context.Projects.Include(p => p.Tasks).FirstOrDefaultAsync(p => p.Id == id, ct);

    public async Task AddAsync(Project project, CancellationToken ct = default) =>
        await _context.Projects.AddAsync(project, ct);

    public void Remove(Project project) => _context.Projects.Remove(project);

    public Task SaveChangesAsync(CancellationToken ct = default) => _context.SaveChangesAsync(ct);
}
