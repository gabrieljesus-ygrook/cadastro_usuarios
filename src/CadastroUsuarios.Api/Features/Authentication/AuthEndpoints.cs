using System.Security.Claims;
using CadastroUsuarios.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Antiforgery;
using Microsoft.AspNetCore.Identity;

namespace CadastroUsuarios.Api.Features.Authentication;

public static class AuthEndpoints
{
    private const string MasterRoleName = "Master";

    public static RouteGroupBuilder MapAuthEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/auth");

        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));
        group.MapGet("/antiforgery", GetAntiforgery)
            .AllowAnonymous();
        group.MapGet("/session", GetSessionAsync);
        group.MapPost("/logout", LogoutAsync)
            .WithMetadata(new RequireAntiforgeryTokenAttribute(true));

        return group;
    }

    private static IResult GetAntiforgery(HttpContext httpContext, IAntiforgery antiforgery)
    {
        var tokens = antiforgery.GetAndStoreTokens(httpContext);
        return Results.Ok(new AntiforgeryResponse(tokens.RequestToken!));
    }

    private static async Task<IResult> GetSessionAsync(
        ClaimsPrincipal principal,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.GetUserAsync(principal);
        if (user?.UserName is not { } login)
        {
            return Results.Unauthorized();
        }

        return Results.Ok(new SessionResponse(user.Id, user.Name, login));
    }

    private static async Task<IResult> LogoutAsync(SignInManager<ApplicationUser> signInManager)
    {
        await signInManager.SignOutAsync();
        return Results.NoContent();
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        UserManager<ApplicationUser> userManager,
        SignInManager<ApplicationUser> signInManager)
    {
        var user = await userManager.FindByNameAsync(request.Login);
        if (user is null)
        {
            return Results.Unauthorized();
        }

        var passwordResult = await signInManager.CheckPasswordSignInAsync(
            user,
            request.Password,
            lockoutOnFailure: true);

        if (!passwordResult.Succeeded || !await userManager.IsInRoleAsync(user, MasterRoleName))
        {
            return Results.Unauthorized();
        }

        await signInManager.SignInAsync(user, isPersistent: false);

        return Results.NoContent();
    }

    private sealed record AntiforgeryResponse(string RequestToken);

    private sealed record SessionResponse(string Id, string Name, string Login);
}
