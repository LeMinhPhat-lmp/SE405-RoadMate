namespace RoadMate.Application.Abstractions.Identity;

/// <summary>Who is acting (audit, soft delete). Api implements it from HttpContext; background workers use <c>SystemActor</c>.</summary>
public interface ICurrentUser
{
    Guid? UserId { get; }
    string? FirebaseUid { get; }
}
