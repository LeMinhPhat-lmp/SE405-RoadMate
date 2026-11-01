namespace RoadMate.Entities.Alerts;

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

  /// <summary>Tạo bản ghi log từ kết quả gửi của SMS/push provider.</summary>
  public static AlertDelivery FromSend(Guid alertId, Guid contactId, string channel, Result<string> sent) => new()
  {
    Id = Guid.NewGuid(),
    AlertId = alertId,
    ContactId = contactId,
    Channel = channel,
    SentAt = DateTime.UtcNow,
    Status = sent.IsSuccess ? DeliveryStatus.Sent : DeliveryStatus.Failed,
    ProviderMessageId = sent.IsSuccess ? sent.Value : null,
    Error = sent.IsSuccess ? null : sent.Error.Message
  };
}
