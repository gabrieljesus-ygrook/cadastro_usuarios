using CadastroUsuarios.Api.Infrastructure.Identity;
using CadastroUsuarios.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CadastroUsuarios.IntegrationTests.Features.Authentication;

public sealed class MasterProvisioningTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string MasterName = "Master Admin";
    private const string MasterLogin = "admin.master";
    private const string MasterPassword = "master123";

    [Fact]
    public async Task ProvisionAsync_CreatesMasterUserAndRoleFromConfiguration()
    {
        await factory.ResetDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var provisioner = CreateProvisioner(userManager, roleManager);

        await provisioner.ProvisionAsync(CancellationToken.None);

        var master = await userManager.FindByNameAsync(MasterLogin);
        Assert.NotNull(master);
        Assert.Equal(MasterName, master.Name);
        Assert.True(await userManager.CheckPasswordAsync(master, MasterPassword));
        Assert.True(await userManager.IsInRoleAsync(master, "Master"));
    }

    [Fact]
    public async Task ProvisionAsync_WhenCalledTwice_DoesNotCreateDuplicateMasterOrRole()
    {
        await factory.ResetDatabaseAsync();
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var provisioner = CreateProvisioner(userManager, roleManager);

        await provisioner.ProvisionAsync(CancellationToken.None);
        await provisioner.ProvisionAsync(CancellationToken.None);

        Assert.Equal(1, await userManager.Users.CountAsync());
        Assert.Equal(1, await roleManager.Roles.CountAsync());

        var master = await userManager.FindByNameAsync(MasterLogin);
        Assert.NotNull(master);
        Assert.True(await userManager.IsInRoleAsync(master, "Master"));
        Assert.True(await userManager.CheckPasswordAsync(master, MasterPassword));
    }

    private static MasterUserProvisioner CreateProvisioner(
        UserManager<ApplicationUser> userManager,
        RoleManager<IdentityRole> roleManager)
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Master:Name"] = MasterName,
                ["Master:Login"] = MasterLogin,
                ["Master:Password"] = MasterPassword
            })
            .Build();

        return new MasterUserProvisioner(userManager, roleManager, configuration);
    }
}
