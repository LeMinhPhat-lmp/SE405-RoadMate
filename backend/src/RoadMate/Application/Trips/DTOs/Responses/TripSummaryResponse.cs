namespace RoadMate.Application.Trips.DTOs.Responses;

public record TripSummaryResponse(TripResponse Trip, TripStatsResponse Stats);
