using System.Text.Json;

namespace RoadMate.Common;

/// <summary>Đọc JSON của nhà cung cấp theo kiểu "an toàn": thiếu field / sai kiểu thì trả null thay vì ném exception.</summary>
public static class JsonElementExtensions
{
  /// <summary>Property con (object/array/…); null nếu e không phải object, không có property hoặc giá trị là null.</summary>
  public static JsonElement? GetOrNull(this JsonElement e, string name) =>
    e.ValueKind == JsonValueKind.Object && e.TryGetProperty(name, out var v) && v.ValueKind is not (JsonValueKind.Null or JsonValueKind.Undefined)
      ? v : null;

  /// <summary>Đi qua chuỗi property lồng nhau (vd: "geometry", "location"); null nếu đứt giữa chừng.</summary>
  public static JsonElement? GetPath(this JsonElement e, params string[] path)
  {
    JsonElement cur = e;
    foreach (var name in path)
    {
      var next = cur.GetOrNull(name);
      if (next is null) return null;
      cur = next.Value;
    }
    return cur;
  }

  /// <summary>Giá trị chuỗi của property; null nếu không có hoặc không phải chuỗi.</summary>
  public static string? GetStringOrNull(this JsonElement e, string name) =>
    e.GetOrNull(name) is { ValueKind: JsonValueKind.String } v ? v.GetString() : null;

  public static double? GetDoubleOrNull(this JsonElement e, string name) =>
    e.GetOrNull(name) is { ValueKind: JsonValueKind.Number } v && v.TryGetDouble(out var d) ? d : null;

  public static bool GetBoolOrDefault(this JsonElement e, string name, bool fallback = false) =>
    e.GetOrNull(name) is { } v && v.ValueKind is JsonValueKind.True or JsonValueKind.False ? v.GetBoolean() : fallback;

  /// <summary>Mảng chuỗi; rỗng nếu không có.</summary>
  public static List<string> GetStringArray(this JsonElement e, string name)
  {
    var list = new List<string>();
    if (e.GetOrNull(name) is { ValueKind: JsonValueKind.Array } arr)
      foreach (var item in arr.EnumerateArray())
        if (item.ValueKind == JsonValueKind.String && item.GetString() is { } s) list.Add(s);
    return list;
  }

  /// <summary>Các phần tử của mảng property; rỗng nếu không có / không phải mảng.</summary>
  public static IEnumerable<JsonElement> GetArray(this JsonElement e, string name) =>
    e.GetOrNull(name) is { ValueKind: JsonValueKind.Array } arr ? arr.EnumerateArray() : Enumerable.Empty<JsonElement>();
}
