namespace RoadMate.Common;

public static class TrackingUrl
{
  public static string? Build(string baseUrl, string? token) =>
    string.IsNullOrEmpty(token) || string.IsNullOrWhiteSpace(baseUrl) ? null : $"{baseUrl.TrimEnd('/')}/{token}";
}
