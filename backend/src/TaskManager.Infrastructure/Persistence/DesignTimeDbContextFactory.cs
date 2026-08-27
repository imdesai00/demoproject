using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace TaskManager.Infrastructure.Persistence;

/// <summary>
/// Used only by `dotnet ef migrations add` at design time; the connection string here
/// is never actually connected to (migrations are generated as C#, not executed against it).
/// </summary>
public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseNpgsql("Host=localhost;Database=taskmanager;Username=taskmanager;Password=design_time_only");
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
