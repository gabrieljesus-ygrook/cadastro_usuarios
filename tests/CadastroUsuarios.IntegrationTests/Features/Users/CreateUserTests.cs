using System.Net;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
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

public sealed class CreateUserTests(ApiFactory factory) : IClassFixture<ApiFactory>
{
    private const string MasterLogin = "master";
    private const string MasterPassword = "master123";
    private const string ValidLogin = "new.user_1";
    private const string ValidPassword = "userpass123";

    [Fact]
    public async Task PostUsers_WithValidMasterAndRequest_ReturnsCreatedWithoutCredentials()
    {
        using var client = await CreateMasterClientAsync();

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "New User",
            login = ValidLogin,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.NotNull(response.Headers.Location);

        using var body = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
        var properties = body.RootElement.EnumerateObject().Select(property => property.Name).ToArray();
        Assert.Contains("id", properties);
        Assert.Contains("name", properties);
        Assert.Contains("login", properties);
        Assert.DoesNotContain("password", properties);
        Assert.DoesNotContain("passwordHash", properties);
    }

    [Fact]
    public async Task PostUsers_WithDuplicateLogin_ReturnsConflict()
    {
        await factory.ResetDatabaseAsync();
        await CreateMasterAsync();
        await CreateUserAsync("Existing User", ValidLogin, ValidPassword);
        using var client = await LoginAsMasterAsync();

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "Duplicate User",
            login = ValidLogin,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abcdefghijklmnopqrstuvwxyz12345")]
    [InlineData("invalid-login")]
    [InlineData(".starts-with-dot")]
    [InlineData("ends-with-dot.")]
    [InlineData("two..dots")]
    public async Task PostUsers_WithInvalidLogin_ReturnsBadRequest(string login)
    {
        using var client = await CreateMasterClientAsync();

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "New User",
            login,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithoutRequiredName_ReturnsBadRequest()
    {
        using var client = await CreateMasterClientAsync();

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = (string?)null,
            login = ValidLogin,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithPasswordShorterThanEightCharacters_ReturnsBadRequest()
    {
        using var client = await CreateMasterClientAsync();

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "New User",
            login = ValidLogin,
            password = "short7"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithoutAuthentication_ReturnsUnauthorized()
    {
        using var client = CreateClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "New User",
            login = ValidLogin,
            password = ValidPassword
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task PostUsers_WithAuthenticatedAccountWithoutMasterRole_ReturnsForbidden()
    {
        await factory.ResetDatabaseAsync();
        using var client = CreateNonMasterClient();
        await AddAntiforgeryTokenAsync(client);

        using var response = await client.PostAsJsonAsync("/api/users", new
        {
            name = "New User",
            login = ValidLogin,
            password = ValidPassword
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

    private async Task CreateUserAsync(string name, string login, string password)
    {
        using var scope = factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var result = await userManager.CreateAsync(new ApplicationUser
        {
            Name = name,
            UserName = login
        }, password);

        Assert.True(result.Succeeded, string.Join(", ", result.Errors.Select(error => error.Description)));
    }

    private sealed record AntiforgeryResponse(string RequestToken);

    private sealed class NonMasterAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder)
        : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
    {
        public const string SchemeName = "NonMasterTest";

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            var identity = new ClaimsIdentity(
                [new Claim(ClaimTypes.NameIdentifier, "non-master-test-user")],
                Scheme.Name);
            var principal = new ClaimsPrincipal(identity);
            var ticket = new AuthenticationTicket(principal, Scheme.Name);

            return Task.FromResult(AuthenticateResult.Success(ticket));
        }
    }
}
