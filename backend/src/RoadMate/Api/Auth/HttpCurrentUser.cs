using System.Security.Claims;
using RoadMate.Application.Abstractions.Identity;

namespace RoadMate.Api.Auth;

public class HttpCurrentUser(IHttpContextAccessor accessor) : ICurrentUser
{
  public Guid? UserId => Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(UserClaims.InternalId), out var id) ? id : null;
  public string? FirebaseUid => accessor.HttpContext?.User.FindFirstValue(UserClaims.FirebaseUid) ?? accessor.HttpContext?.User.FindFirstValue("sub");
}
