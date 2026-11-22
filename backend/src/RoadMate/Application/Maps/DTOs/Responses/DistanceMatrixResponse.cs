using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Responses;

public record DistanceMatrixResponse(List<List<MatrixElementResponse>> Rows);
