using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Storage;

public record SignedUrl(string Url, DateTime ExpiresAt);
