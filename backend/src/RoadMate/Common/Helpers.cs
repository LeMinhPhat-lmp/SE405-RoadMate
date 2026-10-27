using System.Security.Cryptography;

namespace RoadMate.Common;

public static class Geo
{
  public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
  {
    const double R = 6371000;
    double dLat = ToRad(lat2 - lat1), dLng = ToRad(lng2 - lng1);
    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
      Math.Cos(ToRad(lat1)) * Math.Cos(ToRad(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return 2 * R * Math.Asin(Math.Sqrt(a));
  }
  private static double ToRad(double d) => d * Math.PI / 180;
  public static bool ValidLatLng(double lat, double lng) => lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
}

/// <summary>Encoded polyline (precision 5, chuẩn Google/Goong).</summary>
public static class PolylineCodec
{
  public static List<(double Lat, double Lng)> Decode(string s)
  {
    var list = new List<(double Lat, double Lng)>();
    int i = 0, lat = 0, lng = 0;
    while (i < s.Length) { lat += Next(s, ref i); lng += Next(s, ref i); list.Add((lat / 1e5, lng / 1e5)); }
    return list;
  }
  private static int Next(string s, ref int i)
  {
    int shift = 0, result = 0, b;
    do { b = s[i++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
    return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
  }
  public static string Encode(IEnumerable<(double Lat, double Lng)> pts)
  {
    var sb = new StringBuilder(); int pLat = 0, pLng = 0;
    foreach (var p in pts)
    {
      int la = (int)Math.Round(p.Lat * 1e5), lo = (int)Math.Round(p.Lng * 1e5);
      Enc(la - pLat, sb); Enc(lo - pLng, sb); pLat = la; pLng = lo;
    }
    return sb.ToString();
  }
  private static void Enc(int v, StringBuilder sb)
  {
    v = v < 0 ? ~(v << 1) : v << 1;
    while (v >= 0x20) { sb.Append((char)((0x20 | (v & 0x1f)) + 63)); v >>= 5; }
    sb.Append((char)(v + 63));
  }
}

public static class DateTimeExtensions
{
  /// <summary>Npgsql (timestamptz) chỉ nhận UTC.</summary>
  public static DateTime ToUtc(this DateTime d) =>
    d.Kind == DateTimeKind.Unspecified ? DateTime.SpecifyKind(d, DateTimeKind.Utc) : d.ToUniversalTime();
  public static DateTime? ToUtc(this DateTime? d) => d?.ToUtc();
}

public static class TokenGenerator
{
  public static string NewTrackingToken() => Convert.ToHexString(RandomNumberGenerator.GetBytes(16)).ToLowerInvariant();
}

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
}
