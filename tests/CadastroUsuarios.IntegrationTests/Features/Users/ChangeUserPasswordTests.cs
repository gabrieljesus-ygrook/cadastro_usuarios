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

public sealed class ChangeUserPasswordTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string MasterLogin = "master";
    private const string MasterPassword = "master123";
    private const string UserPassword = "userpass123";
    private const string NewPassword = "newpass123";

    [Fact]
    public async Task PutUserPassword_WithValidPassword_ReturnsNoContentAndChangesPassword()
    {
        using var client = await CreateMasterClientAsync();
        var userId = await CreateUserAsync("Example User", "example.user");

        using var response = await client.PutAsJsonAsync($"/api/users/{userId}/password", new
        {
            newPassword = NewPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Empty(await response.Content.ReadAsByteArrayAsync());

        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.FindByIdAsync(userId);
        Assert.NotNull(user);
        Assert.True(await userManager.CheckPasswordAsync(user, NewPassword));
        Assert.False(await userManager.CheckPasswordAsync(user, UserPassword));
    }

    [Fact]
    public async Task PutUserPassword_WithPasswordShorterThanEightCharacters_ReturnsBadRequest()
    {
        using var client = await CreateMasterClientAsync();
        var userId = await CreateUserAsync("Example User", "example.user");

        using var response = await client.PutAsJsonAsync($"/api/users/{userId}/password", new
        {
            newPassword = "short7"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PutUserPassword_WithMissingId_ReturnsNotFound()
    {
        using var client = await CreateMasterClientAsync();

        using var response = await client.PutAsJsonAsync("/api/users/missing-user-id/password", new
        {
            newPassword = NewPassword
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PutUserPassword_WithoutAuthentication_ReturnsUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PutAsJsonAsync("/api/users/user-id/password", new
        {
            newPassword = NewPassword
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PutUserPassword_WithAuthenticatedAccountWithoutMasterRole_ReturnsForbidden()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateNonMasterClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PutAsJsonAsync("/api/users/user-id/password", new
        {
            newPassword = NewPassword
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private async Task<HttpClient> CreateMasterClientAsync()
    {
        await factory.ResetDatabaseAsync();
        await CreateMasterAsync();
        return await LoginAsMasterAsync();
    }

    private async Task<HttpClient> LoginAsMasterAsync()
    {
        var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login = MasterLogin,
            password = MasterPassword
        });
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        return client;
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

    private async Task CreateMasterAsync()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        var roleResult = await roleManager.CreateAsync(new IdentityRole("Master"));
        Assert.True(roleResult.Succeeded, string.Join(", ", roleResult.Errors.Select(error => error.Description)));

        var user = new ApplicationUser
        {
            Name = "Master",
            UserName = MasterLogin
        };
        var userResult = await userManager.CreateAsync(user, MasterPassword);
        Assert.True(userResult.Succeeded, string.Join(", ", userResult.Errors.Select(error => error.Description)));

        var assignmentResult = await userManager.AddToRoleAsync(user, "Master");
        Assert.True(assignmentResult.Succeeded, string.Join(", ", assignmentResult.Errors.Select(error => error.Description)));
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

    private sealed record AntiforgeryResponse(string RequestToken);

    private sealed class NonMasterAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "NonMasterPasswordTest";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "non-master-password-test-user")],
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
