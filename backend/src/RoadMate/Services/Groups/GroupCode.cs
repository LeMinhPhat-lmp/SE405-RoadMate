using System.Security.Cryptography;

namespace RoadMate.Services.Groups;

/// <summary>Mã mời 6 ký tự, bỏ ký tự dễ nhầm (I, O, 0, 1).</summary>
public static class GroupCode
{
  public const int Length = 6;
  private const string Alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZ23456789";

  public static string Generate() => string.Create(Length, 0, static (span, _) =>
  {
    for (var i = 0; i < span.Length; i++) span[i] = Alphabet[RandomNumberGenerator.GetInt32(Alphabet.Length)];
  });

  /// <summary>Chuẩn hoá mã người dùng nhập (cắt khoảng trắng, viết hoa); null nếu sai định dạng.</summary>
  public static string? Normalize(string? raw)
  {
    var s = (raw ?? "").Trim().ToUpperInvariant();
    return s.Length == Length && s.All(Alphabet.Contains) ? s : null;
  }
}
