using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Domain.Common;

namespace RoadMate.Application.Groups.Validators;

public sealed class SendGroupMessageRequestValidator : IRequestValidator<SendGroupMessageRequest>
{
  public Result Validate(SendGroupMessageRequest r, DateTimeOffset now) =>
    RoadMate.Domain.Groups.QuickMessages.IsValid(r.QuickType)
      ? Result.Success()
      : Error.Validation("INVALID_QUICK_TYPE", $"quickType phải là một trong: {string.Join(", ", RoadMate.Domain.Groups.QuickMessages.All)}.");
}
