using RoadMate.Domain.Common;

namespace RoadMate.Domain.ValueObjects;

/// <summary>Client-generated idempotency key: trimmed, 1–100 chars.</summary>
public readonly record struct ClientRequestId
{
  public const int MaxLength = 100;
  private ClientRequestId(string value) => Value = value;
  public string Value { get; }

  public static Result<ClientRequestId> Create(string? raw)
  {
    var t = raw?.Trim();
    return string.IsNullOrEmpty(t) || t.Length > MaxLength ? Errors.InvalidClientId : new ClientRequestId(t);
  }

  public override string ToString() => Value;
}
