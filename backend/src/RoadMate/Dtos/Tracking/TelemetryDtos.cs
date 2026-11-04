namespace RoadMate.Dtos.Tracking;

public record PointInput(double Lat, double Lng, double? Speed, double? Altitude, int? Battery, DateTime RecordedAt);
public record AddPointsRequest(List<PointInput> Points);
/// <param name="MessagesSince">Mốc (UTC) tin nhắn nhóm client đã có; gửi kèm để nhận tin mới trong <c>HeartbeatResponse.Group.NewMessages</c>.</param>
public record HeartbeatRequest(double Lat, double Lng, double? Speed, int? Battery, bool NetworkOk, DateTime? MessagesSince = null);
/// <param name="Group">Vị trí các thành viên + tin mới của nhóm; null nếu chuyến chưa gắn nhóm.</param>
public record HeartbeatResponse(GroupSnapshotDto? Group);
