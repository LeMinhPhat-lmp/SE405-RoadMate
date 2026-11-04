namespace RoadMate.Entities.Alerts;

/// <summary>
/// Token xác nhận RIÊNG cho từng (cảnh báo, liên hệ). Người thân mở link kèm token để báo "đã liên lạc được":
/// chỉ dừng SMS lặp cho chính liên hệ đó, cảnh báo vẫn mở tới khi chủ chuyến đóng.
/// </summary>
public class AlertAck : BaseEntity                         // unique (AlertId, ContactId) và unique Token
{
  public Guid AlertId { get; set; }
  public Guid ContactId { get; set; }
  public string Token { get; set; } = default!;
  public DateTime? AckedAt { get; set; }
  public Alert? Alert { get; set; }
  public EmergencyContact? Contact { get; set; }
  public bool IsAcked => AckedAt is not null;
}
