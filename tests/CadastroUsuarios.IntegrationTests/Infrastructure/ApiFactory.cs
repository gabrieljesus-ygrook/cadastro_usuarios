using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using CadastroUsuarios.Api.Infrastructure.Persistence;
using Xunit;

namespace CadastroUsuarios.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly string _testDatabaseConnectionString = GetTestDatabaseConnectionString();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:TestDatabase"] = _testDatabaseConnectionString
            });
        });
    }

    async Task IAsyncLifetime.InitializeAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await database.Database.MigrateAsync();
        await ClearDataAsync(database);
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await base.DisposeAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        await using var scope = Services.CreateAsyncScope();
        var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        await ClearDataAsync(database);
    }

    private static async Task ClearDataAsync(ApplicationDbContext database)
    {
        await database.UserTokens.ExecuteDeleteAsync();
        await database.UserLogins.ExecuteDeleteAsync();
        await database.UserClaims.ExecuteDeleteAsync();
        await database.UserRoles.ExecuteDeleteAsync();
        await database.RoleClaims.ExecuteDeleteAsync();
        await database.Users.ExecuteDeleteAsync();
        await database.Roles.ExecuteDeleteAsync();
    }

    private static string GetTestDatabaseConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__TestDatabase") is { Length: > 0 } connectionString
            ? connectionString
            : throw new InvalidOperationException(
                "Set ConnectionStrings__TestDatabase to a dedicated SQL Server test database before running integration tests.");
}
