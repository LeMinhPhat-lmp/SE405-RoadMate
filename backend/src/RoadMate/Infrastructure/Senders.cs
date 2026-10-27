using System.Net.Http.Json;
using FirebaseAdmin.Messaging;

namespace RoadMate.Infrastructure;

public interface ISmsSender { Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default); }
public interface IPushSender { Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default); }

/// <summary>Mặc định (dev): chỉ ghi log, không gửi thật.</summary>
public class LogSmsSender : ISmsSender
{
  private readonly ILogger<LogSmsSender> _log;
  public LogSmsSender(ILogger<LogSmsSender> log) => _log = log;
  public Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default)
  {
    _log.LogWarning("[SMS-LOG] to {Phone}: {Message}", phone, message);
    return Task.FromResult(Result.Success("log-" + Guid.NewGuid().ToString("N")[..8]));
  }
}

/// <summary>eSMS.vn - cần kiểm tra lại SmsType/Brandname theo gói bạn đăng ký trong tài liệu eSMS.</summary>
public class ESmsSender : ISmsSender
{
  private readonly HttpClient _http; private readonly SmsOptions _o;
  public ESmsSender(HttpClient http, IOptions<SmsOptions> o) { _http = http; _o = o.Value; }
  public async Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default)
  {
    var local = phone.StartsWith("+84") ? "0" + phone[3..] : phone;
    var body = new
    {
      ApiKey = _o.ESms.ApiKey,
      SecretKey = _o.ESms.SecretKey,
      Phone = local,
      Content = message,
      SmsType = _o.ESms.SmsType,
      Brandname = _o.ESms.Brandname,
      IsUnicode = "0",
      Sandbox = "0"
    };
    try
    {
      using var res = await _http.PostAsJsonAsync("https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/", body, ct);
      var json = await res.Content.ReadFromJsonAsync<System.Text.Json.JsonElement>(cancellationToken: ct);
      var code = json.TryGetProperty("CodeResult", out var c) ? c.GetString() : null;
      if (code == "100") return json.TryGetProperty("SMSID", out var id) ? id.GetString() ?? "" : "";
      var err = json.TryGetProperty("ErrorMessage", out var e) ? e.GetString() : "unknown";
      return Error.External("SMS_FAILED", $"eSMS code={code}: {err}");
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
    {
      return Error.External("SMS_FAILED", ex.Message);
    }
  }
}

public class LogPushSender : IPushSender
{
  private readonly ILogger<LogPushSender> _log;
  public LogPushSender(ILogger<LogPushSender> log) => _log = log;
  public Task<Result<string>> SendAsync(string fcmToken, string title, string body, IDictionary<string, string>? data = null, CancellationToken ct = default)
  {
    _log.LogWarning("[PUSH-LOG] {Title}: {Body}", title, body);
    return Task.FromResult(Result.Success("log-push"));
  }
}

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
