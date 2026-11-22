using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application.Handlers;

/// <summary>Keep the group's shared map current from every accepted position (set-based write, no group aggregate load).</summary>
public sealed class LocationReportedUpdatesGroupMember(IGroupMemberLocationWriter writer) : IDomainEventHandler<LocationReported>
{
  public Task HandleAsync(LocationReported e, CancellationToken ct) =>
    e.GroupId is { } gid ? writer.UpdateAsync(gid, e.OwnerId, e.Location, e.Battery, e.OccurredAt, ct) : Task.CompletedTask;
}
