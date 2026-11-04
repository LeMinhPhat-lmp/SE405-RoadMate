using System.Globalization;

namespace RoadMate.Infrastructure.Maps;

/// <summary>Builder query string cho Goong: bỏ qua giá trị rỗng, luôn URL-encode, số theo invariant culture.</summary>
public sealed class GoongQuery
{
  private readonly List<KeyValuePair<string, string>> _items = [];

  public GoongQuery Add(string name, string? value)
  {
    if (!string.IsNullOrWhiteSpace(value)) _items.Add(new(name, value));
    return this;
  }

  public GoongQuery Add(string name, int value) => Add(name, value.ToString(CultureInfo.InvariantCulture));
  public GoongQuery Add(string name, bool value) => Add(name, value ? "true" : "false");

  /// <summary>Thêm khi điều kiện đúng (tham số tuỳ chọn chỉ gửi khi cần).</summary>
  public GoongQuery AddIf(bool condition, string name, string value) => condition ? Add(name, value) : this;

  /// <summary>Toạ độ dạng "lat,lng".</summary>
  public GoongQuery AddLatLng(string name, double lat, double lng) => Add(name, $"{lat.ToCoordinate()},{lng.ToCoordinate()}");

  /// <summary>Danh sách toạ độ "lat,lng" ngăn cách bằng <paramref name="separator"/> (Distance Matrix dùng '|').</summary>
  public GoongQuery AddLatLngList(string name, IEnumerable<LatLng> points, char separator) =>
    Add(name, string.Join(separator, points.Select(p => $"{p.Lat.ToCoordinate()},{p.Lng.ToCoordinate()}")));

  public string Build() => string.Join("&", _items.Select(i => $"{Uri.EscapeDataString(i.Key)}={Uri.EscapeDataString(i.Value)}"));
}
