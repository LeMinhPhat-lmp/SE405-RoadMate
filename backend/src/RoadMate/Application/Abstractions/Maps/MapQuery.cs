using System.Globalization;
using RoadMate.Application.Common;
using RoadMate.Application.Shared;

namespace RoadMate.Application.Abstractions.Maps;

/// <summary>Builder query string cho nhà cung cấp bản đồ: bỏ qua giá trị rỗng, luôn URL-encode, số theo invariant culture.</summary>
public sealed class MapQuery
{
  private readonly List<KeyValuePair<string, string>> _items = [];

  public MapQuery Add(string name, string? value)
  {
    if (!string.IsNullOrWhiteSpace(value)) _items.Add(new(name, value));
    return this;
  }

  public MapQuery Add(string name, int value) => Add(name, value.ToString(CultureInfo.InvariantCulture));
  public MapQuery Add(string name, bool value) => Add(name, value ? "true" : "false");

  /// <summary>Thêm khi điều kiện đúng (tham số tuỳ chọn chỉ gửi khi cần).</summary>
  public MapQuery AddIf(bool condition, string name, string value) => condition ? Add(name, value) : this;

  /// <summary>Toạ độ dạng "lat,lng".</summary>
  public MapQuery AddLatLng(string name, double lat, double lng) => Add(name, $"{lat.ToCoordinate()},{lng.ToCoordinate()}");

  /// <summary>Danh sách toạ độ "lat,lng" ngăn cách bằng <paramref name="separator"/> (Distance Matrix dùng '|').</summary>
  public MapQuery AddLatLngList(string name, IEnumerable<LatLng> points, char separator) =>
    Add(name, string.Join(separator, points.Select(p => $"{p.Lat.ToCoordinate()},{p.Lng.ToCoordinate()}")));

  public string Build() => string.Join("&", _items.Select(i => $"{Uri.EscapeDataString(i.Key)}={Uri.EscapeDataString(i.Value)}"));
}
