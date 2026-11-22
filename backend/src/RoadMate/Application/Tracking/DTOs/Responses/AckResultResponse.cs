using RoadMate.Application.Shared;

namespace RoadMate.Application.Tracking.DTOs.Responses;

public record AckResultResponse(string OwnerName, string AlertType, string AlertStatus, DateTime AckedAt);
