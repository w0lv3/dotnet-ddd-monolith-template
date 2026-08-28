namespace Example.Application.Interfaces.Identity;

public interface ICurrentUser
{
    string? UserId { get; }

    string? Email { get; }

    bool IsAuthenticated { get; }
}
