namespace RoadMate.Application.Common;

/// <summary>Typed keyset-paginated slice returned by repositories and read models.</summary>
public sealed record CursorPage<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);
