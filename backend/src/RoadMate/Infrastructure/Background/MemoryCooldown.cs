using Microsoft.Extensions.Caching.Memory;
using RoadMate.Application.Abstractions.Messaging;

namespace RoadMate.Infrastructure.Background;

/// <summary>Single-instance "do not repeat" memory (resets on restart).</summary>
public sealed class MemoryCooldown(IMemoryCache cache) : ICooldown
{
  public bool TryAcquire(string key, TimeSpan duration)
  {
    if (cache.TryGetValue(key, out _)) return false;
    cache.Set(key, true, duration);
    return true;
  }
}
