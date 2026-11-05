using System.ComponentModel.DataAnnotations;

namespace CadastroUsuarios.Api.Features.Users;

public sealed record CreateUserRequest
{
    [Required]
    public required string Name { get; init; }

    [Required]
    [StringLength(30, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)*$")]
    public required string Login { get; init; }

    [Required]
    [MinLength(8)]
    public required string Password { get; init; }
}
