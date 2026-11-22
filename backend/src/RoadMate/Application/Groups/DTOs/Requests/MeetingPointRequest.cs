namespace RoadMate.Application.Groups.DTOs.Requests;

public record MeetingPointRequest(string? Name, double Lat, double Lng, DateTime? DepartAt);
