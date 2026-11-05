using CadastroUsuarios.Api.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;

namespace CadastroUsuarios.Api.Features.Users;

public static class UserEndpoints
{
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
            if (creationResult.Errors.Any(error => error.Code == "DuplicateUserName"))
            {
                return Results.Conflict();
            }

            var validationErrors = creationResult.Errors
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

        var response = new UserResponse(user.Id, user.Name, login);
        return Results.Created($"/api/users/{user.Id}", response);
    }
}
