using RoadMate.Domain.Common;

namespace RoadMate.Application.Abstractions.Storage;

public record StoredObject(long SizeBytes, string? ContentType);
