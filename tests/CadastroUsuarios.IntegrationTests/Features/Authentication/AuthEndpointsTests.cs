using System.Net;
using System.Net.Http.Json;
using CadastroUsuarios.Api.Infrastructure.Identity;
using CadastroUsuarios.Api.Infrastructure.Persistence;
using CadastroUsuarios.IntegrationTests.Infrastructure;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;

namespace CadastroUsuarios.IntegrationTests.Features.Authentication;

public sealed class AuthEndpointsTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string MasterLogin = "master";
    private const string MasterPassword = "master123";
    private const string RegularLogin = "regular";
    private const string RegularPassword = "regular123";

    [Fact]
    public async Task GetAntiforgery_ReturnsRequestTokenAndCookie()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/auth/antiforgery");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var payload = await response.Content.ReadFromJsonAsync<AntiforgeryResponse>();
        Assert.False(string.IsNullOrWhiteSpace(payload?.RequestToken));
        Assert.Contains(response.Headers, header =>
            header.Key.Equals("Set-Cookie", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task PostLogin_WithValidMasterCredentials_EstablishesSession()
    {
        await factory.ResetDatabaseAsync();
        await CreateMasterAsync();

        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login = MasterLogin,
            password = MasterPassword
        });

        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);

        using var sessionResponse = await client.GetAsync("/api/auth/session");
        var session = await sessionResponse.Content.ReadFromJsonAsync<SessionResponse>();

        Assert.Equal(HttpStatusCode.OK, sessionResponse.StatusCode);
        Assert.NotNull(session);
        Assert.Equal("Master", session.Name);
        Assert.Equal(MasterLogin, session.Login);
        Assert.False(string.IsNullOrWhiteSpace(session.Id));
    }

    [Fact]
    public async Task PostLogin_WithInvalidCredentials_ReturnsUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        await CreateMasterAsync();

        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login = MasterLogin,
            password = "incorrect-password"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLogin_WithRegularUserCredentials_ReturnsUnauthorized()
    {
        await factory.ResetDatabaseAsync();
        await CreateRegularUserAsync();

        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login = RegularLogin,
            password = RegularPassword
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetSession_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/auth/session");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostLogout_WithAuthenticatedMasterAndAntiforgeryToken_EndsSession()
    {
        await factory.ResetDatabaseAsync();
        await CreateMasterAsync();

        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);
        using var loginResponse = await client.PostAsJsonAsync("/api/auth/login", new
        {
            login = MasterLogin,
            password = MasterPassword
        });
        Assert.Equal(HttpStatusCode.NoContent, loginResponse.StatusCode);

        using var logoutResponse = await client.PostAsync("/api/auth/logout", content: null);

        Assert.Equal(HttpStatusCode.NoContent, logoutResponse.StatusCode);

        using var sessionResponse = await client.GetAsync("/api/auth/session");
        Assert.Equal(HttpStatusCode.Unauthorized, sessionResponse.StatusCode);
    }

    private HttpClient CreateClient() => factory.CreateClient(new()
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

    private async Task CreateRegularUserAsync()
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var user = new ApplicationUser
        {
            Name = "Regular User",
            UserName = RegularLogin
        };

        var result = await userManager.CreateAsync(user, RegularPassword);
        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
    }

    private sealed record AntiforgeryResponse(string RequestToken);

    private sealed record SessionResponse(string Id, string Name, string Login);
}
