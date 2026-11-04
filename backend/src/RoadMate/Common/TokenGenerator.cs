using System.Security.Cryptography;

namespace RoadMate.Common;

public static class TokenGenerator
{
  /// <summary>Token ngẫu nhiên 32 ký tự hex (128 bit) cho link theo dõi / link xác nhận.</summary>
  public static string NewToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}
