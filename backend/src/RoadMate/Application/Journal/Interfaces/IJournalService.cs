using RoadMate.Application.Journal.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Journal.Interfaces;

public interface IJournalService
{
  /// <summary>Timeline theo ngày (theo múi giờ <paramref name="tzOffsetHours"/>) gồm điểm dừng, kỷ niệm và thống kê.</summary>
  Task<Result<JournalResponse>> GetJournalAsync(Guid userId, Guid tripId, int tzOffsetHours, CancellationToken ct);
}
