namespace RoadMate.Application.Abstractions.Identity;

/// <summary>Actor for code that runs outside an HTTP request.</summary>
public sealed class SystemActor : ICurrentUser
{
    public Guid? UserId => null;
    public string? FirebaseUid => null;
}
