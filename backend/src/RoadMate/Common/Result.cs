namespace RoadMate.Common;

public enum ErrorType { Failure, Validation, Unauthorized, Forbidden, NotFound, Conflict, TooManyRequests, External, NotConfigured }

public sealed record Error(string Code, string Message, ErrorType Type = ErrorType.Failure)
{
  public static readonly Error None = new(string.Empty, string.Empty);
  public static Error Failure(string code, string msg) => new(code, msg, ErrorType.Failure);
  public static Error Validation(string code, string msg) => new(code, msg, ErrorType.Validation);
  public static Error NotFound(string code, string msg) => new(code, msg, ErrorType.NotFound);
  public static Error Conflict(string code, string msg) => new(code, msg, ErrorType.Conflict);
  public static Error Forbidden(string code, string msg) => new(code, msg, ErrorType.Forbidden);
  public static Error External(string code, string msg) => new(code, msg, ErrorType.External);
  public static Error NotConfigured(string code, string msg) => new(code, msg, ErrorType.NotConfigured);   // thiếu cấu hình -> HTTP 503
}

/// <summary>Result pattern: hợp đồng giữa Controller và Service (không ném exception cho lỗi nghiệp vụ).</summary>
public class Result
{
  protected Result(bool ok, Error error) { IsSuccess = ok; Error = error; }
  public bool IsSuccess { get; }
  public bool IsFailure => !IsSuccess;
  public Error Error { get; }

  public static Result Success() => new(true, Error.None);
  public static Result Failure(Error e) => new(false, e);
  public static Result<T> Success<T>(T value) => new(value, true, Error.None);
  public static Result<T> Failure<T>(Error e) => new(default, false, e);

  public static implicit operator Result(Error e) => Failure(e);
}

public sealed class Result<T> : Result
{
  private readonly T? _value;
  internal Result(T? value, bool ok, Error e) : base(ok, e) => _value = value;
  public T Value => IsSuccess ? _value! : throw new InvalidOperationException("Result thất bại, không có Value.");

  public static implicit operator Result<T>(T value) => Success(value);
  public static implicit operator Result<T>(Error e) => Failure<T>(e);
}
