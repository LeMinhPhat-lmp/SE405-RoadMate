using System.Text.RegularExpressions;
using System.Text;

namespace RoadMate.Application.Common;

public static class TextHelper
{
  // Explicit table instead of string.Normalize(FormD): Alpine/"invariant globalization" images (the default for our Dockerfile) have no ICU,
  // where Normalize silently returns the text unchanged, so SMS would keep Vietnamese diacritics (Unicode SMS: shorter and costlier).
  private static readonly (string Accented, char Plain)[] Groups =
  [
    ("àáạảãâầấậẩẫăằắặẳẵ", 'a'), ("èéẹẻẽêềếệểễ", 'e'), ("ìíịỉĩ", 'i'), ("òóọỏõôồốộổỗơờớợởỡ", 'o'),
    ("ùúụủũưừứựửữ", 'u'), ("ỳýỵỷỹ", 'y'), ("đ", 'd')
  ];

  private static readonly Dictionary<char, char> Map = BuildMap();

  private static Dictionary<char, char> BuildMap()
  {
    var m = new Dictionary<char, char>();
    foreach (var (accented, plain) in Groups)
      foreach (var c in accented)
      {
        m[c] = plain;
        m[char.ToUpperInvariant(c)] = char.ToUpperInvariant(plain);
      }
    return m;
  }

  /// <summary>"Nguyễn Văn An - Đà Lạt" → "Nguyen Van An - Da Lat". Handles precomposed text and combining marks (U+0300–U+036F).</summary>
  public static string StripDiacritics(string s)
  {
    var sb = new StringBuilder(s.Length);
    foreach (var c in s)
    {
      if (c is >= '\u0300' and <= '\u036F') continue;
      sb.Append(Map.TryGetValue(c, out var plain) ? plain : c);
    }
    return sb.ToString();
  }

  public static string StripHtmlTags(string s) => Regex.Replace(s, "<.*?>", "");
}
