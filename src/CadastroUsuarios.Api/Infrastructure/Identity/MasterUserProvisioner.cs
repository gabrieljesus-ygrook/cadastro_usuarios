using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;

namespace CadastroUsuarios.Api.Infrastructure.Identity;

public sealed class MasterUserProvisioner(
    UserManager<ApplicationUser> userManager,
    RoleManager<IdentityRole> roleManager,
    IConfiguration configuration)
{
    private const string MasterRoleName = "Master";

    public async Task ProvisionAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var name = GetRequiredSetting("Master:Name");
        var login = GetRequiredSetting("Master:Login");
        var password = GetRequiredSetting("Master:Password");

        var user = await userManager.FindByNameAsync(login);
        if (user is not null && !await userManager.CheckPasswordAsync(user, password))
        {
            throw new InvalidOperationException(
                $"The configured Master account '{login}' already exists with a different password.");
        }

        if (!await roleManager.RoleExistsAsync(MasterRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(MasterRoleName));
            EnsureSucceeded(roleResult, $"create the '{MasterRoleName}' role");
        }

        if (user is null)
        {
            user = new ApplicationUser
            {
                Name = name,
                UserName = login
            };

            var userResult = await userManager.CreateAsync(user, password);
            EnsureSucceeded(userResult, "create the configured Master account");
        }

        if (!await userManager.IsInRoleAsync(user, MasterRoleName))
        {
            var assignmentResult = await userManager.AddToRoleAsync(user, MasterRoleName);
            EnsureSucceeded(assignmentResult, $"assign the '{MasterRoleName}' role to the Master account");
        }
    }

    private string GetRequiredSetting(string key) =>
        configuration[key] is { Length: > 0 } value
            ? value
            : throw new InvalidOperationException($"Configure '{key}' using an external configuration source.");

    private static void EnsureSucceeded(IdentityResult result, string operation)
    {
        if (!result.Succeeded)
        {
            var errors = string.Join("; ", result.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Could not {operation}: {errors}");
        }
    }
}
