namespace RoadMate.Application.Alerts.DTOs.Responses;

public record AlertAckResponse(Guid ContactId, DateTime? AckedAt);
