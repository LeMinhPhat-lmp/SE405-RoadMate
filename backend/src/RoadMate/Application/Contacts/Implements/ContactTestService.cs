using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Contacts.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Contacts.Implements;

/// <summary>Immediate test SMS (+push when linked) in Live mode. Not stored in the outbox: the caller wants the result now.</summary>
public class ContactTestService(IUserRepository users, IEmergencyContactRepository contacts, IEnumerable<INotificationChannel> channels) : IContactTestService
{
  public async Task<Result<List<DeliveryResponse>>> TestAsync(Guid userId, Guid contactId, CancellationToken ct)
  {
    var c = await contacts.GetOwnedAsync(userId, contactId, ct);
    if (c is null) return Errors.ContactNotFound;
    var user = await users.GetByIdAsync(userId, ct);
    if (user is null) return Errors.UserNotSynced;

    var body = $"[RoadMate] Tin nhắn thử: {user.Name} đã thêm bạn làm người liên hệ khẩn cấp.";
    var data = new Dictionary<string, string> { ["type"] = "test" };
    var result = new List<DeliveryResponse>();

    var sms = channels.FirstOrDefault(x => x.Channel == DeliveryChannel.Sms);
    if (sms is not null) result.Add(await SendAsync(sms, c.Id, c.Phone, body, data, ct));

    var push = channels.FirstOrDefault(x => x.Channel == DeliveryChannel.Push);
    if (push is not null && c.LinkedUserId is { } linkedId)
    {
      var tokens = await users.GetFcmTokensAsync([linkedId], ct);
      if (tokens.TryGetValue(linkedId, out var token)) result.Add(await SendAsync(push, c.Id, token, body, data, ct));
    }
    return result;
  }

  private static async Task<DeliveryResponse> SendAsync(INotificationChannel ch, Guid contactId, string recipient, string body, IReadOnlyDictionary<string, string> data, CancellationToken ct)
  {
    var r = await ch.SendAsync(new NotificationMessage(recipient, "RoadMate: tin nhắn thử", body, data), ct);
    return new DeliveryResponse(contactId, EnumWire.ToWire(ch.Channel), r.IsSuccess ? "sent" : "failed", r.IsFailure ? r.Error.Message : null);
  }
}
