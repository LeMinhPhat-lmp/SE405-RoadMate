namespace RoadMate.Common;

public static class CorrelationKeys
{
  public const string HeaderName = "X-Correlation-Id";
  /// <summary>Khoá trong HttpContext.Items chứa mã tương quan của request.</summary>
  public const string ItemKey = "CorrelationId";
}
