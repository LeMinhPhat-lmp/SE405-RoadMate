namespace RoadMate.Common;

public static class ResultExtensions
{
  public static ObjectResult ToProblem(this Error e)
  {
    var status = e.Type switch
    {
      ErrorType.Validation => 400,
      ErrorType.Unauthorized => 401,
      ErrorType.Forbidden => 403,
      ErrorType.NotFound => 404,
      ErrorType.Conflict => 409,
      ErrorType.TooManyRequests => 429,
      ErrorType.External => 502,
      ErrorType.NotConfigured => 503,
      _ => 500
    };
    var pd = new ProblemDetails { Status = status, Title = e.Code, Detail = e.Message };
    return new ObjectResult(pd) { StatusCode = status };
  }
}
