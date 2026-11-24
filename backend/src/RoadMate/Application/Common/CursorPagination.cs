using RoadMate.Domain.Common;

namespace RoadMate.Application.Common;

/// <summary>Shared keyset pagination limits and page construction helpers.</summary>
public static class CursorPagination
{
  public const int DefaultLimit = 20;
  public const int MaxLimit = 100;

  public static readonly Error InvalidCursor = Error.Validation("INVALID_CURSOR", "Con trỏ phân trang không hợp lệ.");

  public static int NormalizeLimit(int? limit) => Math.Clamp(limit ?? DefaultLimit, 1, MaxLimit);

  public static CursorPage<T> ToPage<T>(IReadOnlyList<T> fetched, int limit, Func<T, CursorPosition> cursorOf)
  {
    var hasMore = fetched.Count > limit;
    var items = hasMore ? fetched.Take(limit).ToList() : fetched;
    var next = hasMore && items.Count > 0 ? cursorOf(items[^1]).Encode() : null;
    return new CursorPage<T>(items, next, hasMore);
  }
}
