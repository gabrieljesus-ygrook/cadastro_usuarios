using Microsoft.AspNetCore.Identity;

namespace CadastroUsuarios.Api.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser
{
    public required string Name { get; set; }
}
