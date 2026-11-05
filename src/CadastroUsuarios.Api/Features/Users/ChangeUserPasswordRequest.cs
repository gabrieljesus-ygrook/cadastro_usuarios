using System.ComponentModel.DataAnnotations;

namespace CadastroUsuarios.Api.Features.Users;

public sealed record ChangeUserPasswordRequest
{
    [Required]
    [MinLength(8)]
    public required string NewPassword { get; init; }
}
