using RoadMate.Application.Groups;

namespace RoadMate.Application.Tracking.DTOs.Requests;

/// <param name="MessagesSince">UTC mark of the last group message the client has; the response then carries newer ones.</param>
public record HeartbeatRequest(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk, DateTime? MessagesSince = null);
