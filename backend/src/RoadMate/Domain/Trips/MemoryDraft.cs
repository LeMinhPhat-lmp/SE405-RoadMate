namespace RoadMate.Domain.Trips;

public sealed record MemoryDraft(MemoryType Type, string? MediaPath, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTimeOffset? TakenAt);
