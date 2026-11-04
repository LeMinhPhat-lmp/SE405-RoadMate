using Microsoft.Extensions.Caching.Memory;

namespace RoadMate.Services.Maps;

public static class MapCacheExtensions
{
  /// <summary>Cache kết quả THÀNH CÔNG của Goong (tiết kiệm hạn mức); lỗi không bao giờ được cache.</summary>
  public static async Task<Result<T>> GetOrAddAsync<T>(this IMemoryCache cache, string key, TimeSpan ttl, Func<Task<Result<T>>> factory)
  {
    if (ttl > TimeSpan.Zero && cache.TryGetValue(key, out T? hit) && hit is not null) return hit;
    var result = await factory();
    if (result.IsSuccess && ttl > TimeSpan.Zero) cache.Set(key, result.Value, ttl);
    return result;
  }
}
