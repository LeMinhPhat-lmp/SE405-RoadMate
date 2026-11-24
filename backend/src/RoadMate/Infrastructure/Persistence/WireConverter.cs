using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using RoadMate.Domain.Common;

namespace RoadMate.Infrastructure.Persistence;

/// <summary>enum ⇄ legacy wire string (the values the previous version stored, e.g. "lowBattery").</summary>
public sealed class WireConverter<T>() : ValueConverter<T, string>(v => EnumWire.ToWire(v), s => Parse(s)) where T : struct, Enum
{
  private static T Parse(string s) => EnumWire.TryParse<T>(s, out var v) ? v : throw new InvalidOperationException($"Giá trị '{s}' không hợp lệ cho {typeof(T).Name}.");
}
