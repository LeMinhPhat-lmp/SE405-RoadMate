namespace RoadMate.Domain.Common;

/// <summary>Result pattern: the Controller/Service/Domain contract (no exceptions for business errors).</summary>
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
