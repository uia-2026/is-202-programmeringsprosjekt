namespace Heimevernet.Services.Interfaces;

public interface ICurrentUser
{
    int? UserId { get; }
    string ActorName { get; }
}
