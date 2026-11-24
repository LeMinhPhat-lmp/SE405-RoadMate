namespace RoadMate.Application.Alerts.DTOs.Requests;

public record AlertLocationRequest(double Lat, double Lng, int? Battery);