using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

/// <param name="Status">OK | NOT_FOUND | ZERO_RESULTS (theo Goong). DistanceM/DurationS chỉ có khi OK.</param>
public record MatrixElementResponse(string Status, int? DistanceM, int? DurationS);
