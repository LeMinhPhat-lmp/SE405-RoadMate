using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

public record TripRouteResponse(Guid TripId, bool Rerouted, DirectionsResponse Route);
