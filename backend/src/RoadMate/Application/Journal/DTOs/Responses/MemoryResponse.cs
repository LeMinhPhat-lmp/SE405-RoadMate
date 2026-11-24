using RoadMate.Application.Trips;

namespace RoadMate.Application.Journal.DTOs.Responses;

/// <param name="MediaUrl">URL đọc đã ký, có hạn (MediaUrlExpiresAt); null nếu là ghi chú hoặc Storage chưa cấu hình. Gọi lại danh sách/GET media-url để lấy URL mới.</param>
public record MemoryResponse(Guid Id, string Type, string? MediaPath, string? MediaUrl, DateTime? MediaUrlExpiresAt, string? Text, double? Lat, double? Lng,
  string? Mood, string? PlaceName, int? Rating, DateTime TakenAt);
