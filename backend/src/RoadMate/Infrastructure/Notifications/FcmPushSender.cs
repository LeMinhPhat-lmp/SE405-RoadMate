using FirebaseAdmin.Messaging;

namespace RoadMate.Infrastructure.Notifications;

public class FcmPushSender(ILogger<FcmPushSender> log) : IPushSender
{
  /// <summary>Notification channel khai báo ở app Flutter (độ ưu tiên cao, âm khẩn cấp). Chỉ dùng cho push cảnh báo.</summary>
  public const string EmergencyChannelId = "roadmate_emergency";

  public async Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
  {
    var isAlert = data is not null && data.TryGetValue("type", out var type) && type == "alert";
    try
    {
      var msg = new Message
      {
        Token = fcmToken,
        Notification = new Notification { Title = title, Body = body },
        Data = data is null ? null : new Dictionary<string, string>(data),
        Android = new AndroidConfig
        {
          Priority = Priority.High,
          Notification = isAlert ? new AndroidNotification { ChannelId = EmergencyChannelId } : null
        }
      };
      return await FirebaseMessaging.DefaultInstance.SendAsync(msg, ct);
    }
    catch (FirebaseMessagingException ex)
    {
      log.LogWarning("FCM từ chối push: {ErrorCode}/{MessagingErrorCode}", ex.ErrorCode, ex.MessagingErrorCode);
      return Error.External("PUSH_FAILED", $"FCM: {ex.MessagingErrorCode?.ToString() ?? ex.ErrorCode.ToString()}");
    }
  }
}
