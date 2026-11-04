using CadastroUsuarios.Api.Infrastructure.Identity;
using CadastroUsuarios.Api.Infrastructure.Persistence;
using CadastroUsuarios.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CadastroUsuarios.IntegrationTests.Infrastructure;

public sealed class IdentityConfigurationTests
{
    [Fact]
    public void RegistersSqlServerAndIdentityWithEightCharacterMinimum()
    {
        const string connectionStringKey = "ConnectionStrings__TestDatabase";
        var previousConnectionString = Environment.GetEnvironmentVariable(connectionStringKey);
        Environment.SetEnvironmentVariable(connectionStringKey, "Server=localhost;Database=CadastroUsuarios_Tests;Trusted_Connection=True;TrustServerCertificate=True");

        try
        {
            using var factory = new ApiFactory();
            using var scope = factory.Services.CreateScope();

            var database = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var identityOptions = scope.ServiceProvider.GetRequiredService<IOptions<IdentityOptions>>().Value;
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            Assert.Equal("Microsoft.EntityFrameworkCore.SqlServer", database.Database.ProviderName);
            Assert.Equal(8, identityOptions.Password.RequiredLength);
            Assert.NotNull(userManager);
        }
        finally
        {
            Environment.SetEnvironmentVariable(connectionStringKey, previousConnectionString);
        }
    }
}
