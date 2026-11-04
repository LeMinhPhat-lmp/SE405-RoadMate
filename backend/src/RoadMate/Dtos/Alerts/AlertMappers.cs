namespace RoadMate.Dtos.Alerts;

public static class AlertMappers
{
  public static DeliveryDto ToDto(this AlertDelivery d) => new(d.ContactId, d.Channel, d.Status, d.Error);

  public static AlertDto ToDto(this Alert a, bool withDeliveries = true) => new(a.Id, a.TripId, a.Type, a.Status, a.Lat, a.Lng, a.Battery,
    a.RepeatCount, a.CreatedAt, a.ResolvedAt, a.ResolvedBy,
    withDeliveries ? a.Deliveries.Select(d => d.ToDto()).ToList() : null,
    a.Acks.Select(k => new AlertAckDto(k.ContactId, k.AckedAt)).ToList(), a.SimulationMode);

  /// <summary>Cảnh báo mới ở trạng thái Pending, hẹn lần lặp đầu tiên lúc <paramref name="nextRepeatAt"/> (null = không lặp).</summary>
  public static Alert ToEntity(this CreateAlertRequest r, Guid tripId, Guid userId, DateTime? nextRepeatAt, string? simulationMode = null) => new()
  {
    Id = Guid.NewGuid(),
    TripId = tripId,
    UserId = userId,
    ClientId = r.ClientId,
    Type = r.Type,
    Status = AlertStatus.Pending,
    Lat = r.Lat,
    Lng = r.Lng,
    Battery = r.Battery,
    NextRepeatAt = nextRepeatAt,
    SimulationMode = simulationMode
  };
}
