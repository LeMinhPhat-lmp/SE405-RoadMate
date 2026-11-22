using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Events;

namespace RoadMate.Application.Handlers;

/// <summary>A member left/was removed: their trips stop referencing the group.</summary>
public sealed class MemberRemovedDetachesTrips(ITripRepository trips) : IDomainEventHandler<MemberRemovedFromGroup>
{
  public async Task HandleAsync(MemberRemovedFromGroup e, CancellationToken ct)
  {
    foreach (var t in await trips.ListInGroupAsync(e.UserId, e.GroupId, ct)) t.ForceDetachGroup();
  }
}
