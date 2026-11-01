namespace RoadMate.Entities.Alerts;

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
  public string? ResolvedBy { get; set; }                  // xem AlertResolver

  public Trip? Trip { get; set; }
  public User? User { get; set; }
  public ICollection<AlertDelivery> Deliveries { get; set; } = new List<AlertDelivery>();
  public bool IsOpen => Status is AlertStatus.Pending or AlertStatus.Sent;

  /// <summary>Đóng cảnh báo (cancelled/resolved) và dừng lặp.</summary>
  public void Close(string status, string resolvedBy, DateTime now)
  {
    Status = status;
    ResolvedAt = now;
    ResolvedBy = resolvedBy;
    NextRepeatAt = null;
  }
}
