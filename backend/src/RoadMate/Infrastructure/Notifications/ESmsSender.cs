using System.Net.Http.Json;
using System.Text.Json;

namespace RoadMate.Infrastructure.Notifications;

/// <summary>
/// eSMS.vn. CHƯA KIỂM CHỨNG với tài khoản thật: mã thành công đang giả định là CodeResult = "100"
/// (xem docs/V3_NOTES.md). Không bao giờ log ApiKey/SecretKey hay nội dung tin; số điện thoại luôn được che.
/// </summary>
public class ESmsSender(HttpClient http, IOptions<SmsOptions> options, ILogger<ESmsSender> log) : ISmsSender
{
  public async Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default)
  {
    var o = options.Value.ESms;
    var local = phone.StartsWith("+84") ? "0" + phone[3..] : phone;
    var body = new
    {
      ApiKey = o.ApiKey, SecretKey = o.SecretKey, Phone = local, Content = message,
      SmsType = o.SmsType, Brandname = o.Brandname, IsUnicode = "0", Sandbox = o.Sandbox ? "1" : "0"
    };

    using var timeout = CancellationTokenSource.CreateLinkedTokenSource(ct);
    timeout.CancelAfter(TimeSpan.FromSeconds(Math.Clamp(o.TimeoutSeconds, 1, 60)));
    try
    {
      using var res = await http.PostAsJsonAsync(o.Endpoint, body, timeout.Token);
      var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: timeout.Token);
      var code = json.GetStringOrNull("CodeResult");
      if (code == "100")
      {
        var smsId = json.GetStringOrNull("SMSID") ?? "";
        log.LogInformation("eSMS gửi thành công tới {Phone} (SMSID {SmsId})", PhoneNumber.Mask(phone), smsId);
        return smsId;
      }
      var err = json.GetStringOrNull("ErrorMessage") ?? "unknown";
      log.LogWarning("eSMS từ chối tin tới {Phone}: HTTP {Status}, code {Code}, {Error}", PhoneNumber.Mask(phone), (int)res.StatusCode, code, err);
      return Error.External("SMS_FAILED", $"eSMS code={code}: {err}");
    }
    catch (OperationCanceledException) when (!ct.IsCancellationRequested)
    {
      log.LogWarning("eSMS timeout khi gửi tới {Phone}", PhoneNumber.Mask(phone));
      return Error.External("SMS_TIMEOUT", "eSMS phản hồi quá chậm.");
    }
    catch (Exception ex) when (ex is HttpRequestException or JsonException)
    {
      log.LogWarning("eSMS lỗi khi gửi tới {Phone}: {Type}", PhoneNumber.Mask(phone), ex.GetType().Name);
      return Error.External("SMS_FAILED", "Không gửi được SMS (lỗi kết nối hoặc phản hồi không hợp lệ).");
    }
  }
}
