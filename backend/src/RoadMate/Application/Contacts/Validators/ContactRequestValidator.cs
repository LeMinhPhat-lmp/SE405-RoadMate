using FluentValidation;
using RoadMate.Application.Common;
using RoadMate.Application.Contacts.DTOs.Requests;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Contacts.Validators;

public sealed class ContactRequestValidator : AbstractValidator<ContactRequest>
{
  public ContactRequestValidator()
  {
    RuleFor(x => x.Name)
      .NotEmpty()
      .WithError(Errors.InvalidName);

    RuleFor(x => x.Phone)
      .Must(p => PhoneNumber.ParseVietnam(p).IsSuccess)
      .WithError(Errors.InvalidPhone);
  }
}
