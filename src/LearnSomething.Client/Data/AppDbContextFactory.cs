using Microsoft.EntityFrameworkCore.Design;

namespace LearnSomething.Client.Data;

// Used only by 'dotnet ef migrations' at design time, to know how to construct AppDbContext.
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args) => new(AppPaths.GetDatabaseFilePath());
}
