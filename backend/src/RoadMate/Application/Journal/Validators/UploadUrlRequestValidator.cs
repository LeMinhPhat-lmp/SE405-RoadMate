using FluentValidation;
using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Journal.DTOs.Requests;

namespace RoadMate.Application.Journal.Validators;

public sealed class UploadUrlRequestValidator : AbstractValidator<UploadUrlRequest>
{
  public UploadUrlRequestValidator(IOptions<StorageOptions> options)
  {
    RuleFor(x => x.Kind)
      .Must(k => MediaPolicy.IsMediaKind(Normalize(k)))
      .WithErrorCode("INVALID_KIND")
      .WithMessage("kind phải là photo|video.");

    When(x => MediaPolicy.IsMediaKind(Normalize(x.Kind)), () =>
    {
      RuleFor(x => x.ContentType)
        .Must((req, ct) => MediaPolicy.TryGetExtension(Normalize(req.Kind), Normalize(ct), out _))
        .WithErrorCode("UNSUPPORTED_CONTENT_TYPE")
        .WithMessage(req => $"contentType '{Normalize(req.ContentType)}' không được hỗ trợ cho {Normalize(req.Kind)}.");

      RuleFor(x => x.SizeBytes)
        .Must((req, size) => size > 0 && size <= MediaPolicy.MaxBytes(Normalize(req.Kind), options.Value))
        .WithErrorCode("FILE_TOO_LARGE")
        .WithMessage(req => $"Dung lượng phải từ 1 đến {MediaPolicy.MaxBytes(Normalize(req.Kind), options.Value)} byte.");
    });
  }

  private static string Normalize(string? value) => (value ?? "").Trim().ToLowerInvariant();
}
