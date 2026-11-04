using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;

namespace CadastroUsuarios.IntegrationTests.Infrastructure;

public sealed class ApiFactory : WebApplicationFactory<Program>
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

    private static string GetTestDatabaseConnectionString() =>
        Environment.GetEnvironmentVariable("ConnectionStrings__TestDatabase") is { Length: > 0 } connectionString
            ? connectionString
            : throw new InvalidOperationException(
                "Set ConnectionStrings__TestDatabase to a dedicated SQL Server test database before running integration tests.");
}
