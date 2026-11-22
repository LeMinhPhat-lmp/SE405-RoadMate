using RoadMate.Application.Trips;

namespace RoadMate.Application.Journal.DTOs.Requests;

/// <param name="MediaPath">Đường dẫn object trong Storage (lấy từ upload-url); bắt buộc với photo/video.</param>
public record MemoryRequest(string Type, string? MediaPath, string? Text, double? Lat, double? Lng, string? Mood, string? PlaceName, int? Rating, DateTime? TakenAt);
