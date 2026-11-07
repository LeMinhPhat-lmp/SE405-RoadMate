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

  /// <summary>"+84901234567" -> "+84******567" – dùng khi ghi log để không lộ số điện thoại.</summary>
  public static string Mask(string? phone) =>
    string.IsNullOrEmpty(phone) ? "" : phone.Length <= 6 ? new string('*', phone.Length) : phone[..3] + new string('*', phone.Length - 6) + phone[^3..];
}
