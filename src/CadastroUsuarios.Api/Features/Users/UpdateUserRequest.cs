using System.ComponentModel.DataAnnotations;

namespace CadastroUsuarios.Api.Features.Users;

public sealed record UpdateUserRequest : IValidatableObject
{
    [MinLength(1)]
    public string? Name { get; init; }

    [StringLength(30, MinimumLength = 1)]
    [RegularExpression(@"^[A-Za-z0-9_]+(?:\.[A-Za-z0-9_]+)*$")]
    public string? Login { get; init; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        if (Name is null && Login is null)
        {
            yield return new ValidationResult(
                "At least one of name or login must be provided.",
                [nameof(Name), nameof(Login)]);
        }
    }
}
