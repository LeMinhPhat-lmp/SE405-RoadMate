using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Mappers;

public static class AlertMappers
{
  public static DeliveryResponse ToDto(this NotificationDelivery d) => new(d.ContactId, EnumWire.ToWire(d.Channel), EnumWire.ToWire(d.Status), d.Error);
  public static DeliveryResponse ToResponse(this NotificationDelivery d) => d.ToDto();

  /// <summary>Deliveries/acks are passed explicitly (the aggregate's navigation collections may not be loaded).</summary>
  public static AlertResponse ToDto(this Alert a, IEnumerable<NotificationDelivery>? deliveries, IEnumerable<AlertAck> acks) => new(
    a.Id, a.TripId, EnumWire.ToWire(a.Type), EnumWire.ToWire(a.Status), a.Lat, a.Lng, a.Battery, a.RepeatCount, a.CreatedAt,
    a.ResolvedAt.ToWire(), a.ResolvedBy is { } r ? EnumWire.ToWire(r) : null, deliveries?.Select(d => d.ToDto()).ToList(),
    acks.Select(k => new AlertAckResponse(k.ContactId, k.AckedAt.ToWire())).ToList(), a.Simulation is { } s ? EnumWire.ToWire(s) : null);
  public static AlertResponse ToResponse(this Alert a, IEnumerable<NotificationDelivery>? deliveries, IEnumerable<AlertAck> acks) => a.ToDto(deliveries, acks);
}
