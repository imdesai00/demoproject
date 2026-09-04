using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskManager.Domain.Entities;
using TaskManager.Domain.Enums;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Idempotent development seed data. Runs on startup after migrations and inserts a single
/// demo account with a handful of projects/tasks so the app has something to show on first run.
/// Does nothing if any user already exists, so it never touches real data.
/// </summary>
public static class DbSeeder
{
    public const string DemoEmail = "demo@taskflow.app";
    public const string DemoPassword = "Demo123!";

    private static readonly Guid DemoUserId = new("11111111-1111-1111-1111-111111111111");

    public static async Task SeedAsync(ApplicationDbContext db, ILogger? logger = null, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var now = DateTime.UtcNow;
        var today = DateOnly.FromDateTime(now);

        var user = new User
        {
            Id = DemoUserId,
            Email = DemoEmail,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(DemoPassword),
            DisplayName = "Demo User",
            CreatedAt = now,
        };

        var websiteRedesign = new Project
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Website Redesign",
            Description = "Refresh the marketing site with a new design system, faster pages, and updated copy.",
            CreatedAt = now.AddDays(-21),
            UpdatedAt = now.AddDays(-2),
            Tasks = new List<ProjectTask>
            {
                Task("Audit current pages and analytics", ProjectTaskStatus.Done, TaskPriority.Medium, today.AddDays(-14), now.AddDays(-20)),
                Task("Define new design tokens (color, type, spacing)", ProjectTaskStatus.Done, TaskPriority.High, today.AddDays(-10), now.AddDays(-16)),
                Task("Build reusable component library", ProjectTaskStatus.InProgress, TaskPriority.High, today.AddDays(4), now.AddDays(-9)),
                Task("Rewrite homepage hero and CTA copy", ProjectTaskStatus.InProgress, TaskPriority.Medium, today.AddDays(6), now.AddDays(-5)),
                Task("Set up visual regression tests", ProjectTaskStatus.Todo, TaskPriority.Low, today.AddDays(12), now.AddDays(-3)),
                Task("Cross-browser QA pass", ProjectTaskStatus.Todo, TaskPriority.Medium, today.AddDays(16), now.AddDays(-2)),
            },
        };

        var mobileLaunch = new Project
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Mobile App Launch",
            Description = "Ship v1.0 of the iOS and Android app to the stores.",
            CreatedAt = now.AddDays(-30),
            UpdatedAt = now.AddDays(-1),
            Tasks = new List<ProjectTask>
            {
                Task("Finalize onboarding flow", ProjectTaskStatus.Done, TaskPriority.High, today.AddDays(-8), now.AddDays(-18)),
                Task("Wire up push notifications", ProjectTaskStatus.InProgress, TaskPriority.Medium, today.AddDays(3), now.AddDays(-7)),
                Task("Prepare App Store / Play Store listings", ProjectTaskStatus.Todo, TaskPriority.Medium, today.AddDays(9), now.AddDays(-4)),
                Task("Beta test with 25 external users", ProjectTaskStatus.Todo, TaskPriority.High, today.AddDays(11), now.AddDays(-4)),
                Task("Submit builds for review", ProjectTaskStatus.Todo, TaskPriority.High, today.AddDays(18), now.AddDays(-1)),
            },
        };

        var q3Marketing = new Project
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "Q3 Marketing Campaign",
            Description = "Content, email, and paid campaign for the summer product push.",
            CreatedAt = now.AddDays(-12),
            UpdatedAt = now.AddDays(-1),
            Tasks = new List<ProjectTask>
            {
                Task("Draft campaign brief and goals", ProjectTaskStatus.Done, TaskPriority.Medium, today.AddDays(-6), now.AddDays(-11)),
                Task("Produce 3 blog posts", ProjectTaskStatus.InProgress, TaskPriority.Medium, today.AddDays(5), now.AddDays(-6)),
                Task("Design email newsletter templates", ProjectTaskStatus.Todo, TaskPriority.Low, today.AddDays(8), now.AddDays(-3)),
                Task("Set up paid social ad sets", ProjectTaskStatus.Todo, TaskPriority.High, today.AddDays(10), now.AddDays(-2)),
            },
        };

        user.Projects = new List<Project> { websiteRedesign, mobileLaunch, q3Marketing };

        db.Users.Add(user);
        await db.SaveChangesAsync(ct);

        logger?.LogInformation(
            "Seeded demo account {Email} with {ProjectCount} projects and {TaskCount} tasks.",
            DemoEmail,
            user.Projects.Count,
            user.Projects.Sum(p => p.Tasks.Count));
    }

    private static ProjectTask Task(
        string title,
        ProjectTaskStatus status,
        TaskPriority priority,
        DateOnly dueDate,
        DateTime createdAt) => new()
    {
        Id = Guid.NewGuid(),
        Title = title,
        Status = status,
        Priority = priority,
        DueDate = dueDate,
        CreatedAt = createdAt,
        UpdatedAt = createdAt,
    };
}
