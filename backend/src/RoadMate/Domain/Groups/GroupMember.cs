using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Users;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Groups;

public sealed class GroupMember : Entity
{
  private GroupMember() { }
  public Guid GroupId { get; private set; }
  public Guid UserId { get; private set; }
  public GroupRole Role { get; internal set; }
  public DateTimeOffset JoinedAt { get; private set; }
  public double? LastLat { get; private set; }
  public double? LastLng { get; private set; }
  public int? LastBattery { get; private set; }
  public DateTimeOffset? LocationUpdatedAt { get; private set; }
  /// <summary>When the member first went beyond the straggler distance (persisted, survives restarts).</summary>
  public DateTimeOffset? StragglingSince { get; internal set; }
  public User? User { get; private set; }

  internal static GroupMember Create(Guid groupId, Guid userId, GroupRole role, DateTimeOffset now) =>
    new() { Id = Guid.NewGuid(), GroupId = groupId, UserId = userId, Role = role, JoinedAt = now };

  internal void Report(GeoPoint at, int? battery, DateTimeOffset now)
  {
    LastLat = at.Lat; LastLng = at.Lng; LastBattery = battery; LocationUpdatedAt = now;
  }
}
