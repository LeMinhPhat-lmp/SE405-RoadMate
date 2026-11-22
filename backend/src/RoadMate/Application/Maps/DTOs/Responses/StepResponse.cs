using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

/// <param name="Lat">Vị trí bắt đầu bước (điểm cần rẽ) – app dùng để phát giọng nói khi đến gần.</param>
public record StepResponse(string Instruction, string? Maneuver, int DistanceM, int DurationS, double? Lat = null, double? Lng = null);
