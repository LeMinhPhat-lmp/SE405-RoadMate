using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Domain.Common;
using RoadMate.Domain.Trips;

namespace RoadMate.Application.Journal.Interfaces;

public interface IMediaReferenceChecker
{
  /// <summary>mediaPath must belong to this user + trip; with Storage:VerifyOnCreate the object must exist, have the right type and size. Skipped for notes or an unchanged path.</summary>
  Task<Result> CheckAsync(Guid userId, Guid tripId, MemoryType type, string? mediaPath, string? currentPath, CancellationToken ct);
}
