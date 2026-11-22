using RoadMate.Application.Groups;

namespace RoadMate.Application.Tracking.DTOs.Requests;

public record PointRequest(double Lat, double Lng, double? Speed, double? Altitude, int? Battery, DateTime RecordedAt);
