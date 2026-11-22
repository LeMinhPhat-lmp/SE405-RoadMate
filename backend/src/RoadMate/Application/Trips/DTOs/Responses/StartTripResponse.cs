namespace RoadMate.Application.Trips.DTOs.Responses;

public record StartTripResponse(TripResponse Trip, string? TrackingUrl, List<string> Warnings);
