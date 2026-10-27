namespace RoadMate.Infrastructure.Implements;

public class HttpCurrentUserContext(IHttpContextAccessor accessor) : ICurrentUserContext
{
  public Guid? UserId =>
    Guid.TryParse(accessor.HttpContext?.User.FindFirstValue(UserClaims.InternalId), out var id) ? id : null;
}
