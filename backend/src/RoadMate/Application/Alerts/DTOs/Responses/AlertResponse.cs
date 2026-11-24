namespace RoadMate.Application.Alerts.DTOs.Responses;

public record AlertResponse(Guid Id, Guid TripId, string Type, string Status, double Lat, double Lng, int? Battery, int RepeatCount,
  DateTimeOffset CreatedAt, DateTime? ResolvedAt, string? ResolvedBy, List<DeliveryResponse>? Deliveries,
  List<AlertAckResponse> Acks, string? SimulationMode);
