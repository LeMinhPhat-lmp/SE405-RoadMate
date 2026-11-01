namespace RoadMate.Dtos.Journal;

public record MemoryRequest(string Type, string? MediaUrl, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTime? TakenAt);
public record MemoryDto(Guid Id, string Type, string? MediaUrl, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTime TakenAt);
public record JournalDayDto(DateOnly Date, List<MemoryDto> Entries);
public record JournalDto(Guid TripId, string Title, string Status, TripStatsDto? Stats, List<WaypointDto> Stops, List<JournalDayDto> Days);
