using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Common;
using RoadMate.Application.Tracking.DTOs.Responses;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Alerts.Implements;

/// <summary>A relative says "I reached them": stops repeats for THAT contact only; the alert stays open until the owner (or a system rule) closes it.</summary>
public class AlertAckService(IAlertAckRepository acks, IUnitOfWork uow, TimeProvider clock) : IAlertAckService
{
  public async Task<Result<AckResultResponse>> AckAsync(string token, CancellationToken ct)
  {
    var ack = await acks.GetByTokenAsync(token, ct);
    if (ack?.Alert is null) return Errors.AckInvalid;
    var r = ack.Acknowledge(clock.GetUtcNow(), ack.Alert.IsOpen);
    if (r.IsFailure) return r.Error;
    await uow.SaveChangesAsync(ct);
    return new AckResultResponse(NameFormatter.Abbreviate(ack.Alert.User?.Name), EnumWire.ToWire(ack.Alert.Type), EnumWire.ToWire(ack.Alert.Status), ack.AckedAt.ToWire()!.Value);
  }
}
