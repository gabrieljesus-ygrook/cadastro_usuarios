using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Encodings.Web;
using CadastroUsuarios.Api.Infrastructure.Identity;
using CadastroUsuarios.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CadastroUsuarios.IntegrationTests.Features.Users;

public sealed class DeleteUserTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string MasterRoleName = "Master";
    private const string MasterLogin = "master";
    private const string MasterPassword = "master123";
    private const string UserPassword = "userpass123";

    [Fact]
    public async Task DeleteUser_WithCommonUser_ReturnsNoContentAndRemovesUser()
    {
        var (client, _) = await CreateMasterClientAsync();
        using (client)
        {
            var userId = await CreateUserAsync("Example User", "example.user");

            using var response = await client.DeleteAsync($"/api/users/{userId}");

            Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

            using var scope = factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            Assert.Null(await userManager.FindByIdAsync(userId));
        }
    }

    [Fact]
    public async Task DeleteUser_WithMissingId_ReturnsNotFound()
    {
        var (client, _) = await CreateMasterClientAsync();
        using (client)
        using (var response = await client.DeleteAsync("/api/users/missing-user-id"))
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }

    [Fact]
    public async Task DeleteUser_WithoutAuthentication_ReturnsUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.DeleteAsync("/api/users/user-id");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WithAuthenticatedAccountWithoutMasterRole_ReturnsForbidden()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateNonMasterClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.DeleteAsync("/api/users/user-id");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteUser_WhenItIsTheLastMaster_ReturnsConflictAndPreservesMaster()
    {
        var (client, masterId) = await CreateMasterClientAsync();
        using (client)
        using (var response = await client.DeleteAsync($"/api/users/{masterId}"))
        {
            Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        }

        Assert.Equal(1, await GetMasterCountAsync());
    }

    [Fact]
    public async Task DeleteUser_WithTwoMastersDeletedConcurrently_PreservesAtLeastOneMaster()
    {
        var (client, firstMasterId) = await CreateMasterClientAsync();
        using (client)
        {
            var secondMasterId = await CreateMasterUserAsync("second.master");
            var deleteFirstMaster = client.DeleteAsync($"/api/users/{firstMasterId}");
            var deleteSecondMaster = client.DeleteAsync($"/api/users/{secondMasterId}");

            using var firstResponse = await deleteFirstMaster;
            using var secondResponse = await deleteSecondMaster;

            var statuses = new[] { firstResponse.StatusCode, secondResponse.StatusCode };
            Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.NoContent));
            Assert.Equal(1, statuses.Count(status => status == HttpStatusCode.Conflict));
        }

        Assert.Equal(1, await GetMasterCountAsync());
    }

    private async Task<(HttpClient Client, string MasterId)> CreateMasterClientAsync()
    {
        await factory.ResetDatabaseAsync();
        var masterId = await CreateMasterUserAsync(MasterLogin);
        var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login = MasterLogin,
            password = MasterPassword
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        return (client, masterId);
    }

    private HttpClient CreateClient() => factory.CreateClient(new()
    {
        AllowAutoRedirect = false,
        HandleCookies = true
    });

    private HttpClient CreateNonMasterClient() => factory.WithWebHostBuilder(builder =>
        builder.ConfigureTestServices(services =>
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = NonMasterAuthenticationHandler.SchemeName;
                options.DefaultChallengeScheme = NonMasterAuthenticationHandler.SchemeName;
                options.DefaultForbidScheme = NonMasterAuthenticationHandler.SchemeName;
            }).AddScheme<AuthenticationSchemeOptions, NonMasterAuthenticationHandler>(
                NonMasterAuthenticationHandler.SchemeName,
                _ => { });
        })).CreateClient(new()
        {
            AllowAutoRedirect = false,
            HandleCookies = true
        });

    private static async Task AddAntiforgeryTokenAsync(HttpClient client)
    {
        using var response = await client.GetAsync("/api/auth/antiforgery");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var payload = await response.Content.ReadFromJsonAsync<AntiforgeryResponse>();
        Assert.False(string.IsNullOrWhiteSpace(payload?.RequestToken));
        client.DefaultRequestHeaders.Add("RequestVerificationToken", payload!.RequestToken);
    }

    private async Task<string> CreateMasterUserAsync(string login)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        if (!await roleManager.RoleExistsAsync(MasterRoleName))
        {
            var roleResult = await roleManager.CreateAsync(new IdentityRole(MasterRoleName));
            Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Description)));
        }

        var user = new ApplicationUser
        {
            Name = login,
            UserName = login
        };
        var userResult = await userManager.CreateAsync(user, MasterPassword);
        Assert.True(userResult.Succeeded, string.Join(", ", userResult.Errors.Select(error => error.Description)));

        var assignmentResult = await userManager.AddToRoleAsync(user, MasterRoleName);
        Assert.True(assignmentResult.Succeeded, string.Join(", ", assignmentResult.Errors.Select(error => error.Description)));

        return user.Id;
    }

    private async Task<string> CreateUserAsync(string name, string login)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Name = name,
            UserName = login
        };

        var result = await userManager.CreateAsync(user, UserPassword);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));

        return user.Id;
    }

    private async Task<int> GetMasterCountAsync()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        return (await userManager.GetUsersInRoleAsync(MasterRoleName)).Count;
    }

    private sealed record AntiforgeryResponse(string RequestToken);

    private sealed class NonMasterAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "NonMasterDeleteTest";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "non-master-delete-test-user")],
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
