using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Contacts.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Contacts.Validators;

public sealed class ContactRequestValidator : IRequestValidator<ContactRequest>
{
  public Result Validate(ContactRequest req, DateTimeOffset now)
  {
    if (string.IsNullOrWhiteSpace(req.Name)) return Errors.InvalidName;
    return PhoneNumber.ParseVietnam(req.Phone).IsFailure ? Errors.InvalidPhone : Result.Success();
  }
}
