using System.Globalization;
using System.Text;

namespace RoadMate.Application.Common;

/// <summary>Opaque keyset cursor encoding every sort component (<see cref="SortAt"/> and unique tie-breaker <see cref="Id"/>).</summary>
public sealed record CursorPosition(DateTimeOffset SortAt, Guid Id)
{
  public string Encode()
  {
    var raw = $"{SortAt.ToUniversalTime().Ticks.ToString(CultureInfo.InvariantCulture)}:{Id:N}";
    return Convert.ToBase64String(Encoding.UTF8.GetBytes(raw)).TrimEnd('=').Replace('+', '-').Replace('/', '_');
  }

  public static bool TryDecode(string? token, out CursorPosition? cursor)
  {
    if (string.IsNullOrWhiteSpace(token))
    {
      cursor = null;
      return true;
    }

    try
    {
      var s = token.Trim().Replace('-', '+').Replace('_', '/');
      switch (s.Length % 4)
      {
        case 2: s += "=="; break;
        case 3: s += "="; break;
        case 1: cursor = null; return false;
      }

      var decoded = Encoding.UTF8.GetString(Convert.FromBase64String(s));
      var sep = decoded.IndexOf(':');
      if (sep <= 0 || sep >= decoded.Length - 1)
      {
        cursor = null;
        return false;
      }

      if (!long.TryParse(decoded.AsSpan(0, sep), NumberStyles.Integer, CultureInfo.InvariantCulture, out var ticks) ||
          ticks < DateTimeOffset.MinValue.Ticks || ticks > DateTimeOffset.MaxValue.Ticks ||
          !Guid.TryParseExact(decoded.AsSpan(sep + 1), "N", out var id))
      {
        cursor = null;
        return false;
      }

      cursor = new CursorPosition(new DateTimeOffset(ticks, TimeSpan.Zero), id);
      return true;
    }
    catch (FormatException)
    {
      cursor = null;
      return false;
    }
  }
}
