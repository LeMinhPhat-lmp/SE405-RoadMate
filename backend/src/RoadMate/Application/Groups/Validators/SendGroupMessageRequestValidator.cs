using FluentValidation;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Groups;

namespace RoadMate.Application.Groups.Validators;

public sealed class SendGroupMessageRequestValidator : AbstractValidator<SendGroupMessageRequest>
{
  public SendGroupMessageRequestValidator()
  {
    RuleFor(x => x.QuickType)
      .Must(QuickMessages.IsValid)
      .WithErrorCode("INVALID_QUICK_TYPE")
      .WithMessage($"quickType phải là một trong: {string.Join(", ", QuickMessages.All)}.");
  }
}
