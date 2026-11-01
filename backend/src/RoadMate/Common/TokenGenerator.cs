using System.Security.Cryptography;

namespace RoadMate.Common;

public static class TokenGenerator
{
  public static string NewTrackingToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}
