namespace RoadMate.Infrastructure.Notifications;

public class ESmsOptions
{
  public string ApiKey { get; set; } = "";
  public string SecretKey { get; set; } = "";
  public string SmsType { get; set; } = "8";
  public string Brandname { get; set; } = "";
}
