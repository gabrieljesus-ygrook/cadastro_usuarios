namespace CadastroUsuarios.Api.Features.Users;

public sealed record UserPageResponse(
    IReadOnlyList<UserResponse> Items,
    int Page,
    int PageSize,
    int TotalCount);
