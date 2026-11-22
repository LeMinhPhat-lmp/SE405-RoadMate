using System.Reflection;

namespace RoadMate.Domain.Common;

/// <summary>Enum ⇄ legacy string conversion (write exact, parse case-insensitive). Values cached per enum type.</summary>
public static class EnumWire
{
  private static class Cache<T> where T : struct, Enum
  {
    public static readonly T[] Values = Enum.GetValues<T>();
    public static readonly Dictionary<T, string> ToWireMap = Values.ToDictionary(v => v, Wire);
    public static readonly Dictionary<string, T> FromWireMap = Values.ToDictionary(Wire, v => v, StringComparer.OrdinalIgnoreCase);

    private static string Wire(T v) =>
      typeof(T).GetField(v.ToString(), BindingFlags.Public | BindingFlags.Static)!.GetCustomAttribute<WireValueAttribute>()?.Value
      ?? v.ToString().ToLowerInvariant();
  }

  public static string ToWire<T>(T value) where T : struct, Enum => Cache<T>.ToWireMap[value];

  public static bool TryParse<T>(string? text, out T value) where T : struct, Enum
  {
    if (text is not null && Cache<T>.FromWireMap.TryGetValue(text, out value)) return true;
    value = default;
    return false;
  }

  public static IReadOnlyList<T> Values<T>() where T : struct, Enum => Cache<T>.Values;

  /// <summary>All wire values joined, for error messages ("crash, idle, sos, …").</summary>
  public static string JoinWire<T>(string separator = ", ") where T : struct, Enum =>
    string.Join(separator, Cache<T>.Values.Select(ToWire));
}
