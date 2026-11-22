using RoadMate.Application.Shared;

namespace RoadMate.Application.Tracking.DTOs.Responses;

public record PublicAlertResponse(string Type, string Status, double Lat, double Lng, DateTimeOffset CreatedAt);
