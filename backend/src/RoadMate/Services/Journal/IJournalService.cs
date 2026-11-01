namespace RoadMate.Services.Journal;

public interface IJournalService
{
  /// <summary>Timeline theo ngày (theo múi giờ <paramref name="tzOffsetHours"/>) gồm điểm dừng, kỷ niệm và thống kê.</summary>
  Task<Result<JournalDto>> GetJournalAsync(Guid userId, Guid tripId, int tzOffsetHours, CancellationToken ct);
}
