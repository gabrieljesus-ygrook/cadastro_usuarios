using CadastroUsuarios.Api.Infrastructure.Identity;
using CadastroUsuarios.Api.Infrastructure.Persistence;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace CadastroUsuarios.Api.Features.Users;

public static class UserEndpoints
{
    public static async Task<IResult> GetUsersAsync(
        ApplicationDbContext dbContext,
        int page = 1,
        int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var validationErrors = new Dictionary<string, string[]>();
        if (page < 1)
        {
            validationErrors.Add("page", ["Page must be at least 1."]);
        }

        if (pageSize is < 1 or > 100)
        {
            validationErrors.Add("pageSize", ["Page size must be between 1 and 100."]);
        }

        if (validationErrors.Count > 0)
        {
            return Results.ValidationProblem(validationErrors);
        }

        var usersQuery = dbContext.Users
            .AsNoTracking()
            .Where(user => user.UserName != null)
            .OrderBy(user => user.Name)
            .ThenBy(user => user.Id);

        var totalCount = await usersQuery.CountAsync(cancellationToken);
        var skipCount = (long)(page - 1) * pageSize;
        IReadOnlyList<UserResponse> items = skipCount > int.MaxValue
            ? Array.Empty<UserResponse>()
            : await usersQuery
                .Skip((int)skipCount)
                .Take(pageSize)
                .Select(user => new UserResponse(user.Id, user.Name, user.UserName!))
                .ToListAsync(cancellationToken);

        return Results.Ok(new UserPageResponse(items, page, pageSize, totalCount));
    }

    public static async Task<IResult> GetUserAsync(
        string id,
        ApplicationDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        var user = await dbContext.Users
            .AsNoTracking()
            .Where(user => user.Id == id && user.UserName != null)
            .Select(user => new UserResponse(user.Id, user.Name, user.UserName!))
            .FirstOrDefaultAsync(cancellationToken);

        if (user is null)
        {
            return Results.NotFound();
        }

        return Results.Ok(user);
    }

    public static async Task<IResult> UpdateUserAsync(
        string id,
        UpdateUserRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        if (request.Name is not null)
        {
            user.Name = request.Name;
        }

        if (request.Login is not null)
        {
            user.UserName = request.Login.Trim();
        }

        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            return MapIdentityFailure(updateResult);
        }

        return Results.Ok(new UserResponse(user.Id, user.Name, user.UserName!));
    }

    public static async Task<IResult> ChangePasswordAsync(
        string id,
        ChangeUserPasswordRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            return Results.NotFound();
        }

        var resetToken = await userManager.GeneratePasswordResetTokenAsync(user);
        var resetResult = await userManager.ResetPasswordAsync(user, resetToken, request.NewPassword);
        if (!resetResult.Succeeded)
        {
            return MapIdentityFailure(resetResult);
        }

        return Results.NoContent();
    }

    public static async Task<IResult> CreateAsync(
        CreateUserRequest request,
        UserManager<ApplicationUser> userManager)
    {
        var login = request.Login.Trim();
        var user = new ApplicationUser
        {
            Name = request.Name,
            UserName = login
        };

        var creationResult = await userManager.CreateAsync(user, request.Password);
        if (!creationResult.Succeeded)
        {
            return MapIdentityFailure(creationResult);
        }

        var response = new UserResponse(user.Id, user.Name, login);
        return Results.Created($"/api/users/{user.Id}", response);
    }

    private static IResult MapIdentityFailure(IdentityResult identityResult)
    {
        if (identityResult.Errors.Any(error => error.Code == "DuplicateUserName"))
        {
            return Results.Conflict();
        }

        var validationErrors = identityResult.Errors
            .GroupBy(error => error.Code switch
            {
                "InvalidUserName" => "login",
                _ when error.Code.StartsWith("Password", StringComparison.Ordinal) => "password",
                _ => "request"
            })
            .ToDictionary(
                group => group.Key,
                group => group.Select(error => error.Description).ToArray());

        return Results.ValidationProblem(validationErrors);
    }
}
