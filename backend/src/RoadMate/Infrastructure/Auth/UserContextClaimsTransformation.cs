using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Caching.Memory;

namespace RoadMate.Infrastructure.Auth;

/// <summary>Đổi Firebase UID (string) thành Id nội bộ (Guid) và gắn vào claim rm_uid; cache 10 phút.</summary>
public class UserContextClaimsTransformation(IUserRepository users, IMemoryCache cache) : IClaimsTransformation
{
  public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal)
  {
    if (principal.Identity?.IsAuthenticated != true || principal.HasClaim(c => c.Type == UserClaims.InternalId)) return principal;
    var firebaseUid = principal.FindFirstValue(UserClaims.FirebaseUid) ?? principal.FindFirstValue("sub");
    if (string.IsNullOrEmpty(firebaseUid)) return principal;

    var key = $"rm:uid:{firebaseUid}";
    if (!cache.TryGetValue(key, out Guid id))
    {
      var found = await users.GetUserIdByFirebaseUidAsync(firebaseUid);
      if (found is null) return principal;               // chưa sync: không cache kết quả rỗng
      id = found.Value;
      cache.Set(key, id, TimeSpan.FromMinutes(10));
    }
    var identity = new ClaimsIdentity();
    identity.AddClaim(new Claim(UserClaims.InternalId, id.ToString()));
    principal.AddIdentity(identity);
    return principal;
  }
}
