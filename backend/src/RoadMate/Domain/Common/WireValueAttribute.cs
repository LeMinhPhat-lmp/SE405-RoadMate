namespace RoadMate.Domain.Common;

/// <summary>Legacy string used on the wire/DB for an enum member (e.g. <c>OffRoute</c> ⇄ <c>"offroute"</c>).</summary>
[AttributeUsage(AttributeTargets.Field)]
public sealed class WireValueAttribute(string value) : Attribute
{
  public string Value { get; } = value;
}
