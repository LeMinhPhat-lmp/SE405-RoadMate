namespace RoadMate.Common;

/// <summary>Encoded polyline (precision 5, chuẩn Google/Goong).</summary>
public static class PolylineCodec
{
  public static List<(double Lat, double Lng)> Decode(string s)
  {
    var list = new List<(double Lat, double Lng)>();
    int i = 0, lat = 0, lng = 0;
    while (i < s.Length) { lat += ReadValue(s, ref i); lng += ReadValue(s, ref i); list.Add((lat / 1e5, lng / 1e5)); }
    return list;
  }

  public static string Encode(IEnumerable<(double Lat, double Lng)> pts)
  {
    var sb = new StringBuilder(); int pLat = 0, pLng = 0;
    foreach (var p in pts)
    {
      int la = (int)Math.Round(p.Lat * 1e5), lo = (int)Math.Round(p.Lng * 1e5);
      WriteValue(la - pLat, sb); WriteValue(lo - pLng, sb); pLat = la; pLng = lo;
    }
    return sb.ToString();
  }

  /// <summary>Đọc một giá trị (delta) có dấu từ vị trí <paramref name="i"/>, đẩy <paramref name="i"/> qua phần đã đọc.</summary>
  public static int ReadValue(string s, ref int i)
  {
    int shift = 0, result = 0, b;
    do { b = s[i++] - 63; result |= (b & 0x1f) << shift; shift += 5; } while (b >= 0x20);
    return (result & 1) != 0 ? ~(result >> 1) : result >> 1;
  }

  /// <summary>Ghi một giá trị (delta) có dấu vào <paramref name="sb"/>.</summary>
  public static void WriteValue(int v, StringBuilder sb)
  {
    v = v < 0 ? ~(v << 1) : v << 1;
    while (v >= 0x20) { sb.Append((char)((0x20 | (v & 0x1f)) + 63)); v >>= 5; }
    sb.Append((char)(v + 63));
  }
}
