using System.Net.Http.Json;
using System.Text.Json;

namespace RoadMate.Infrastructure.Implements;

/// <summary>
/// eSMS.vn. CHƯA KIỂM CHỨNG với tài khoản thật: cần đối chiếu SmsType/Brandname theo gói đăng ký
/// (xem docs/SETUP_AND_LIMITATIONS.md mục SMS).
/// </summary>
public class ESmsSender(HttpClient http, IOptions<SmsOptions> options) : ISmsSender
{
  public async Task<Result<string>> SendAsync(string phone, string message, CancellationToken ct = default)
  {
    var o = options.Value.ESms;
    var local = phone.StartsWith("+84") ? "0" + phone[3..] : phone;
    var body = new
    {
      ApiKey = o.ApiKey, SecretKey = o.SecretKey, Phone = local, Content = message,
      SmsType = o.SmsType, Brandname = o.Brandname, IsUnicode = "0", Sandbox = "0"
    };
    try
    {
      using var res = await http.PostAsJsonAsync("https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/", body, ct);
      var json = await res.Content.ReadFromJsonAsync<JsonElement>(cancellationToken: ct);
      var code = json.TryGetProperty("CodeResult", out var c) ? c.GetString() : null;
      if (code == "100") return json.TryGetProperty("SMSID", out var id) ? id.GetString() ?? "" : "";
      var err = json.TryGetProperty("ErrorMessage", out var e) ? e.GetString() : "unknown";
      return Error.External("SMS_FAILED", $"eSMS code={code}: {err}");
    }
    catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
    {
      return Error.External("SMS_FAILED", ex.Message);
    }
  }
}
