using System.Text.RegularExpressions;
using RoadMate.Domain.Common;

namespace RoadMate.Domain.ValueObjects;

/// <summary>Vietnamese phone number normalized to <c>+84</c> + 9–10 digits.</summary>
public readonly partial record struct PhoneNumber
{
  [GeneratedRegex(@"^\+84\d{9,10}$")] private static partial Regex VnPhone();

  private PhoneNumber(string value) => Value = value;
  public string Value { get; }

  /// <summary>Accepts <c>0…</c>, <c>84…</c>, <c>+84…</c>; ignores spaces, dots, dashes (any non-digit except +).</summary>
  public static Result<PhoneNumber> ParseVietnam(string? raw)
  {
    var n = Normalize(raw);
    return n is null ? Errors.InvalidPhone : new PhoneNumber(n);
  }

  /// <summary>Legacy-compatible normalization; null when invalid.</summary>
  public static string? Normalize(string? raw)
  {
    if (string.IsNullOrWhiteSpace(raw)) return null;
    var s = new string(raw.Where(ch => char.IsDigit(ch) || ch == '+').ToArray());
    if (s.StartsWith("+84", StringComparison.Ordinal)) { }
    else if (s.StartsWith("84", StringComparison.Ordinal)) s = "+" + s;
    else if (s.StartsWith('0')) s = "+84" + s[1..];
    else return null;
    return VnPhone().IsMatch(s) ? s : null;
  }

  /// <summary><c>0xxxxxxxxx</c> form (what eSMS expects).</summary>
  public string ToLocal() => "0" + Value[3..];

  public string Masked() => Mask(Value);

  /// <summary>"+84901234567" → "+84******567" (log-safe).</summary>
  public static string Mask(string? phone) =>
    string.IsNullOrEmpty(phone) ? "" : phone.Length <= 6 ? new string('*', phone.Length) : phone[..3] + new string('*', phone.Length - 6) + phone[^3..];

  public override string ToString() => Value;
}
