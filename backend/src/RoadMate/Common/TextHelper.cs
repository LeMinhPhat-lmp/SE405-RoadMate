using System.Text.RegularExpressions;

namespace RoadMate.Common;

public static class TextHelper
{
  public static string StripDiacritics(string s)
  {
    var n = s.Normalize(NormalizationForm.FormD);
    var sb = new StringBuilder();
    foreach (var ch in n)
      if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark) sb.Append(ch);
    return sb.ToString().Replace('đ', 'd').Replace('Đ', 'D').Normalize(NormalizationForm.FormC);
  }

  /// <summary>Bỏ thẻ HTML (vd: html_instructions của Goong).</summary>
  public static string StripHtmlTags(string s) => Regex.Replace(s, "<.*?>", "");
}
