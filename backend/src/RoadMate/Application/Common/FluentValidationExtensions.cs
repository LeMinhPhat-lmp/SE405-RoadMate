using FluentValidation;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Common;

/// <summary>Maps domain <see cref="Error"/> codes and Vietnamese messages onto FluentValidation rules.</summary>
public static class FluentValidationExtensions
{
  public static IRuleBuilderOptions<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> rule, Error error) =>
    rule.WithErrorCode(error.Code).WithMessage(error.Message);
}
