namespace RoadMate.Dtos.Journal;

/// <param name="MediaPath">Đường dẫn object trong Storage (lấy từ upload-url); bắt buộc với photo/video.</param>
public record MemoryRequest(string Type, string? MediaPath, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTime? TakenAt);

/// <param name="MediaUrl">URL đọc đã ký, có hạn (MediaUrlExpiresAt); null nếu là ghi chú hoặc Storage chưa cấu hình. Gọi lại danh sách/GET media-url để lấy URL mới.</param>
public record MemoryDto(Guid Id, string Type, string? MediaPath, string? MediaUrl, DateTime? MediaUrlExpiresAt, string? Text, double? Lat, double? Lng,
  string? Mood, string? PlaceName, int? Rating, DateTime TakenAt);
public record JournalDayDto(DateOnly Date, List<MemoryDto> Entries);
public record JournalDto(Guid TripId, string Title, string Status, TripStatsDto? Stats, List<WaypointDto> Stops, List<JournalDayDto> Days);
