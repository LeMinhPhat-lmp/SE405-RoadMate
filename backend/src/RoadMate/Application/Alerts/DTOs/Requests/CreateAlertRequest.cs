namespace RoadMate.Application.Alerts.DTOs.Requests;

public record CreateAlertRequest(string ClientId, string Type, double Lat, double Lng, int? Battery);