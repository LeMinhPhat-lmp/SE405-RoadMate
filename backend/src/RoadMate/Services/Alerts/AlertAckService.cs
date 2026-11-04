namespace RoadMate.Services.Alerts;

public class AlertAckService(IUnitOfWork uow, TimeProvider clock, ILogger<AlertAckService> log) : IAlertAckService
{
  public async Task<Result<AckResultDto>> AckAsync(string token, CancellationToken ct)
  {
    var ack = await uow.AlertAckRepository.GetByTokenAsync(token, ct);
    if (ack?.Alert is null) return Errors.AckInvalid;

    if (!ack.IsAcked)
    {
      ack.AckedAt = clock.UtcNow();
      await uow.SaveChangesAsync(ct);
      log.LogInformation("Người thân (contact {ContactId}) đã xác nhận cảnh báo {AlertId}", ack.ContactId, ack.AlertId);
    }
    return new AckResultDto(NameFormatter.Abbreviate(ack.Alert.User?.Name), ack.Alert.Type, ack.Alert.Status, ack.AckedAt!.Value);
  }
}
