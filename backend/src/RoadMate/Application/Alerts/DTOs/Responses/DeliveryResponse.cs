namespace RoadMate.Application.Alerts.DTOs.Responses;

public record DeliveryResponse(Guid? ContactId, string Channel, string Status, string? Error);
