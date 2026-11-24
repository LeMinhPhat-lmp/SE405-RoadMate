using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

public record LegResponse(double DistanceKm, int DurationMin, string? StartAddress, string? EndAddress);
