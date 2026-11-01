using System.Text.Json;

namespace RoadMate.Common;

public static class JsonElementExtensions
{
  /// <summary>Giá trị chuỗi của property; null nếu không có hoặc không phải chuỗi.</summary>
  public static string? GetStringOrNull(this JsonElement e, string name) =>
    e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
