using System.Text.RegularExpressions;

namespace RoadMate.Common;

public static class PhoneNumber
{
  private static readonly Regex VnPhone = new(@"^\+84\d{9,10}$", RegexOptions.Compiled);

  /// <summary>Chuẩn hoá về +84xxxxxxxxx; trả null nếu không hợp lệ.</summary>
  public static string? NormalizeVietnam(string raw)
  {
    var s = new string(raw.Where(ch => char.IsDigit(ch) || ch == '+').ToArray());
    if (s.StartsWith("+84")) { }
    else if (s.StartsWith("84")) s = "+" + s;
    else if (s.StartsWith("0")) s = "+84" + s[1..];
    else return null;
    return VnPhone.IsMatch(s) ? s : null;
  }
}
