using CadastroUsuarios.Api.Features.Authentication;
using CadastroUsuarios.Api.Features.Users;
using CadastroUsuarios.Api.Infrastructure.Identity;
using CadastroUsuarios.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var provisionMasterMode = args.Contains("--provision-master", StringComparer.Ordinal);
var builderArgs = args.Where(argument => argument != "--provision-master").ToArray();
var builder = WebApplication.CreateBuilder(builderArgs);

builder.Services.AddOpenApi();
builder.Services.AddProblemDetails();
builder.Services.AddValidation();
builder.Services.AddAntiforgery(options =>
{
    options.HeaderName = "RequestVerificationToken";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
});

var connectionString = builder.Configuration.GetConnectionString("TestDatabase")
    ?? builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Configure ConnectionStrings:DefaultConnection for the API or ConnectionStrings:TestDatabase for integration tests.");

builder.Services.AddDbContext<ApplicationDbContext>(
    options => options.UseSqlServer(connectionString),
    contextLifetime: ServiceLifetime.Scoped);

builder.Services
    .AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequiredUniqueChars = 0;
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequireUppercase = false;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddScoped<MasterUserProvisioner>();
builder.Services.AddAuthorizationBuilder()
    .AddPolicy("Master", policy => policy.RequireRole("Master"));
builder.Services.ConfigureApplicationCookie(options =>
{
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsProduction()
        ? CookieSecurePolicy.Always
        : CookieSecurePolicy.SameAsRequest;
    options.Events.OnRedirectToLogin = context =>
    {
        context.Response.StatusCode = StatusCodes.Status401Unauthorized;
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        context.Response.StatusCode = StatusCodes.Status403Forbidden;
        return Task.CompletedTask;
    };
});

var app = builder.Build();

if (provisionMasterMode)
{
    await using var scope = app.Services.CreateAsyncScope();
    var provisioner = scope.ServiceProvider.GetRequiredService<MasterUserProvisioner>();
    await provisioner.ProvisionAsync();
    return;
}

// Configure the HTTP request pipeline.
app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapAuthEndpoints()
    .RequireAuthorization("Master");

app.MapGet("/api/users", UserEndpoints.GetUsersAsync)
    .RequireAuthorization("Master");
app.MapGet("/api/users/{id}", UserEndpoints.GetUserAsync)
    .RequireAuthorization("Master");

app.MapPut("/api/users/{id}", UserEndpoints.UpdateUserAsync)
    .RequireAuthorization("Master")
    .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

app.MapPost("/api/users", UserEndpoints.CreateAsync)
    .RequireAuthorization("Master")
    .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

app.Run();

public partial class Program;
