namespace RoadMate.Application.Shared;

/// <summary>Standard cursor-paginated response envelope for long feature lists.</summary>
public sealed record CursorPageResponse<T>(IReadOnlyList<T> Items, string? NextCursor, bool HasMore);
