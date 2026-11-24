namespace RoadMate.Application.Common;

/// <summary>Optimistic-concurrency token mismatch (another writer changed the row).</summary>
public class ConcurrencyConflictException(Exception? inner = null) : Exception("Dữ liệu vừa được thay đổi bởi người khác.", inner);
