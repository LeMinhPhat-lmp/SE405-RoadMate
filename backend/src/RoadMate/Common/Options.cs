namespace RoadMate.Common;

public class AlertOptions { public int RepeatMinutes { get; set; } = 2; public int MaxRepeats { get; set; } = 30; }
public class TrackingOptions { public string BaseUrl { get; set; } = "https://roadmate.app/track"; }
public class GoongOptions { public string ApiKey { get; set; } = ""; public string BaseUrl { get; set; } = "https://rsapi.goong.io"; }
public class SmsOptions
{
  public string Provider { get; set; } = "log";
  public bool StripDiacritics { get; set; } = true;
  public ESmsOptions ESms { get; set; } = new();
}
public class ESmsOptions { public string ApiKey { get; set; } = ""; public string SecretKey { get; set; } = ""; public string SmsType { get; set; } = "8"; public string Brandname { get; set; } = ""; }
