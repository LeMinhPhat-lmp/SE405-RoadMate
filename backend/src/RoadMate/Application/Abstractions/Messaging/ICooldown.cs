namespace RoadMate.Application.Abstractions.Messaging;

/// <summary>Short-lived "do not repeat" memory (single instance; resets on restart). False while the key is still cooling down.</summary>
public interface ICooldown
{
  bool TryAcquire(string key, TimeSpan duration);
}
