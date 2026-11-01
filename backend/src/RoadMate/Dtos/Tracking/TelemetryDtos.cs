namespace RoadMate.Dtos.Tracking;

public record PointInput(double Lat, double Lng, double? Speed, double? Altitude, int? Battery, DateTime RecordedAt);
public record AddPointsRequest(List<PointInput> Points);
public record HeartbeatRequest(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk);
