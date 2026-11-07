namespace RoadMate.Services.Tracking;

public class TrackingOptions
{
  /// <summary>Gốc URL trang theo dõi của người thân; link = BaseUrl/{token}.</summary>
  public string BaseUrl { get; set; } = "https://roadmate.app/track";
  /// <summary>Gốc URL trang xác nhận của người thân; link = AckUrl/{token riêng của liên hệ}. Để trống = SMS không kèm link xác nhận.</summary>
  public string AckUrl { get; set; } = "";
  /// <summary>Link theo dõi còn dùng được bao lâu sau khi chuyến kết thúc (để người thân thấy "đã về an toàn").</summary>
  public int ExpireAfterFinishMinutes { get; set; } = 120;
}
