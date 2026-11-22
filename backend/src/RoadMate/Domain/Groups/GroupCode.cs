using System.Security.Cryptography;

namespace RoadMate.Domain.Groups;

/// <summary>6-char invite code without easily confused characters (I, O, 0, 1).</summary>
public static class GroupCode
{
  public const int Length = 6;
  private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

  public static string Generate() => string.Create(Length, 0, static (span, _) =>
  {
    for (var i = 0; i < span.Length; i++) span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
  });

  /// <summary>Trim + uppercase; null when the format is wrong.</summary>
  public static string? Normalize(string? raw)
  {
    var s = (raw ?? "").Trim().ToUpperInvariant();
    return s.Length == Length && s.All(Alphabet.Contains) ? s : null;
  }
}
