namespace RoadMate.Application.Alerts.DTOs.Requests;

public record SimulateAlertRequest(Guid TripId, string? Type, double? Lat, double? Lng, bool? DryRun);