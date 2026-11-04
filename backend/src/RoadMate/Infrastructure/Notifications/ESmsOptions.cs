namespace RoadMate.Infrastructure.Notifications;

public class ESmsOptions
{
  public string ApiKey { get; set; } = "";
  public string SecretKey { get; set; } = "";
  /// <summary>Loại tin theo gói eSMS đã đăng ký (cần đối chiếu tài khoản thật).</summary>
  public string SmsType { get; set; } = "8";
  /// <summary>Để trống nếu dùng đầu số cố định/ngẫu nhiên không cần đăng ký brandname.</summary>
  public string Brandname { get; set; } = "";
  public string Endpoint { get; set; } = "https://rest.esms.vn/MainService.svc/json/SendMultipleMessage_V4_post_json/";
  /// <summary>true: gửi Sandbox=1 (eSMS xử lý thử, không gửi/không trừ tiền) – cần xác minh gói của bạn có hỗ trợ.</summary>
  public bool Sandbox { get; set; }
  public int TimeoutSeconds { get; set; } = 15;
}
