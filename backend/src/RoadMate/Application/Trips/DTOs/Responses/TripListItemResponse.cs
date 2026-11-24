using RoadMate.Application.Shared;

namespace RoadMate.Application.Trips.DTOs.Responses;

public record TripListItemResponse(Guid Id, string Title, string Status, string OriginName, string DestName, double DistanceKm,
  DateTime? StartTime, DateTime? EndTime, DateTimeOffset CreatedAt);
