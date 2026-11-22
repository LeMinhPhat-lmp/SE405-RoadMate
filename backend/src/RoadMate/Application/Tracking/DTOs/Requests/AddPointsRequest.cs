using RoadMate.Application.Groups;

namespace RoadMate.Application.Tracking.DTOs.Requests;

public record AddPointsRequest(List<PointRequest> Points);
