using RoadMate.Application.Shared;

namespace RoadMate.Application.Tracking.DTOs.Responses;

public record PublicLiveResponse(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk, DateTime UpdatedAt);
