namespace RoadMate.Common;

public static class ResultExtensions
{
  public static int ToStatusCode(this ErrorType type) => type switch
  {
    ErrorType.Validation => StatusCodes.Status400BadRequest,
    ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
    ErrorType.Forbidden => StatusCodes.Status403Forbidden,
    ErrorType.NotFound => StatusCodes.Status404NotFound,
    ErrorType.Conflict => StatusCodes.Status409Conflict,
    ErrorType.TooManyRequests => StatusCodes.Status429TooManyRequests,
    ErrorType.External => StatusCodes.Status502BadGateway,
    ErrorType.NotConfigured => StatusCodes.Status503ServiceUnavailable,
    _ => StatusCodes.Status500InternalServerError
  };

  /// <summary>Lỗi nghiệp vụ -> ProblemDetails (Title = mã lỗi, Detail = thông báo tiếng Việt, kèm traceId/correlationId).</summary>
  public static ObjectResult ToProblem(this Error e, HttpContext http)
  {
    var pd = ProblemFactory.Create(http, e.Type.ToStatusCode(), e.Code, e.Message);
    return new ObjectResult(pd) { StatusCode = pd.Status };
  }
}
