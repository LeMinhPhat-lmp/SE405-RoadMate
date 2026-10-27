using FirebaseAdmin.Messaging;

namespace RoadMate.Infrastructure.Implements;

public class FcmPushSender : IPushSender
{
  public async Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
  {
    try
    {
      var msg = new Message
      {
        Token = fcmToken,
        Notification = new Notification { Title = title, Body = body },
        Data = data is null ? null : new Dictionary<string, string>(data),
        Android = new AndroidConfig { Priority = Priority.High }
      };
      return await FirebaseMessaging.DefaultInstance.SendAsync(msg, ct);
    }
    catch (FirebaseMessagingException ex) { return Error.External("PUSH_FAILED", ex.Message); }
  }
}
