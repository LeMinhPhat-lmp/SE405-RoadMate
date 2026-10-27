namespace RoadMate.Infrastructure.Auth;

public static class UserClaims
{
  public const string FirebaseUid = "user_id";   // claim của Firebase ID token
  public const string InternalId = "rm_uid";     // claim do UserContextClaimsTransformation thêm: Id (Guid) trong DB
}
