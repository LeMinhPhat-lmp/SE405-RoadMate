namespace RoadMate.Entities;

public static class AlertType
{
  public const string Crash = "crash", Idle = "idle", Sos = "sos", OffRoute = "offroute",
    LowBattery = "lowBattery", LostSignal = "lostSignal", LateReturn = "lateReturn";
  public static readonly string[] All = { Crash, Idle, Sos, OffRoute, LowBattery, LostSignal, LateReturn };
}
public static class AlertStatus
{
  public const string Pending = "pending", Sent = "sent", Cancelled = "cancelled", Resolved = "resolved";
}
public static class DeliveryChannel { public const string Sms = "sms", Push = "push", Email = "email"; }
public static class DeliveryStatus { public const string Queued = "queued", Sent = "sent", Failed = "failed"; }

/// <summary>Có audit nhưng KHÔNG soft delete: cảnh báo là bằng chứng, không được ẩn/xoá.</summary>
public class Alert : AuditableEntity
{
  public Guid TripId { get; set; }
  public Guid UserId { get; set; }
  public string ClientId { get; set; } = default!;         // idempotency
  public string Type { get; set; } = AlertType.Crash;
  public string Status { get; set; } = AlertStatus.Pending;
  public double Lat { get; set; }
  public double Lng { get; set; }
  public int? Battery { get; set; }
  public int RepeatCount { get; set; }
  public DateTime? NextRepeatAt { get; set; }
  public DateTime? ResolvedAt { get; set; }
  public string? ResolvedBy { get; set; }                  // user|contact|system

  public Trip? Trip { get; set; }
  public User? User { get; set; }
  public ICollection<AlertDelivery> Deliveries { get; set; } = new List<AlertDelivery>();
  public bool IsOpen => Status is AlertStatus.Pending or AlertStatus.Sent;
}

public class AlertDelivery : BaseEntity
{
  public Guid AlertId { get; set; }
  public Guid? ContactId { get; set; }
  public string Channel { get; set; } = DeliveryChannel.Sms;
  public string Status { get; set; } = DeliveryStatus.Queued;
  public string? ProviderMessageId { get; set; }
  public string? Error { get; set; }
  public DateTime SentAt { get; set; } = DateTime.UtcNow;
  public Alert? Alert { get; set; }
  public EmergencyContact? Contact { get; set; }
}
