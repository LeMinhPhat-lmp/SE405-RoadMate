using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Common;
using RoadMate.Domain.Events;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Users;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Domain.Trips;

/// <summary>Trip aggregate. State machine Draft → Active → Finished; every rule lives here, services only orchestrate.</summary>
public sealed class Trip : FullAuditedEntity, IAggregateRoot
{
  private readonly List<TripWaypoint> _waypoints = [];
  private readonly List<TripAlertRecipient> _recipients = [];

  private Trip() { }

  public Guid OwnerId { get; private set; }
  public Guid? GroupId { get; private set; }
  public string Title { get; private set; } = "";
  public TripStatus Status { get; private set; }
  public string OriginName { get; private set; } = "";
  public double OriginLat { get; private set; }
  public double OriginLng { get; private set; }
  public string DestName { get; private set; } = "";
  public double DestLat { get; private set; }
  public double DestLng { get; private set; }
  public string? PlannedPolyline { get; private set; }
  public double DistanceKm { get; private set; }
  public int EtaMinutes { get; private set; }
  public bool AvoidHighway { get; private set; }
  public bool AvoidToll { get; private set; }
  public DateTimeOffset? StartTime { get; private set; }
  public DateTimeOffset? EndTime { get; private set; }
  public DateTimeOffset? ExpectedReturnTime { get; private set; }
  public int? IdleMinutes { get; private set; }
  public CrashSensitivity? CrashSensitivity { get; private set; }
  public bool LiveShare { get; private set; }
  public string? TrackingToken { get; private set; }
  public DateTimeOffset? TrackingExpiresAt { get; private set; }
  public DateTimeOffset? RestUntil { get; private set; }
  public DateTimeOffset? LateReturnRemindedAt { get; private set; }
  /// <summary>True while an open lostSignal alert exists, so heartbeats need no query to know whether to close it.</summary>
  public bool LostSignalAlertOpen { get; private set; }

  public User? Owner { get; private set; }
  public Group? Group { get; private set; }
  public TripLive? Live { get; private set; }
  public TripStats? Stats { get; private set; }
  public IReadOnlyCollection<TripWaypoint> Waypoints => _waypoints.AsReadOnly();
  public IReadOnlyCollection<TripAlertRecipient> Recipients => _recipients.AsReadOnly();

  public static Result<Trip> Create(Guid ownerId, TripDetails details, IReadOnlyList<WaypointSpec> waypoints, IReadOnlyCollection<Guid> recipientContactIds)
  {
    var trip = new Trip
    {
      Id = Guid.NewGuid(),
      OwnerId = ownerId,
      Status = TripStatus.Draft
    };
    var dResult = trip.UpdateDetails(details);
    if (!dResult.IsSuccess) return dResult.Error;
    var wResult = trip.ReplaceWaypoints(waypoints);
    if (wResult.IsFailure) return wResult.Error;
    trip.SetRecipients(recipientContactIds);
    return trip;
  }

  public Result UpdateDetails(TripDetails d)
  {
    if (Status > TripStatus.Draft) return Errors.TripNotDraft;
    Title = d.Title.Trim();
    OriginName = d.OriginName;
    OriginLat = d.Origin.Lat;
    OriginLng = d.Origin.Lng;
    DestName = d.DestName;
    DestLat = d.Destination.Lat;
    DestLng = d.Destination.Lng;
    PlannedPolyline = d.PlannedPolyline;
    DistanceKm = d.DistanceKm;
    EtaMinutes = d.EtaMinutes;
    AvoidHighway = d.AvoidHighway;
    AvoidToll = d.AvoidToll;
    ExpectedReturnTime = d.ExpectedReturnTime;
    IdleMinutes = d.IdleMinutes;
    CrashSensitivity = d.CrashSensitivity;
    LiveShare = d.LiveShare;
    return Result.Success();
  }

  public Result ReplaceWaypoints(IReadOnlyList<WaypointSpec> specs)
  {
    if (Status > TripStatus.Draft) return Errors.TripNotDraft;
    if (specs.Count > Errors.MaxWaypoints) return Errors.TooManyWaypoints;
    _waypoints.Clear();
    for (int i = 0; i < specs.Count; i++)
    {
      _waypoints.Add(TripWaypoint.Create(Id, i, specs[i]));
    }
    return Result.Success();
  }

  public void SetRecipients(IReadOnlyCollection<Guid> contactIds)
  {
    _recipients.RemoveAll(r => !contactIds.Contains(r.ContactId));
    foreach (var id in contactIds.Distinct().Where(id => !_recipients.Any(r => r.ContactId == id)))
    {
      _recipients.Add(TripAlertRecipient.Create(Id, id));
    }
  }

  public void SetRoute(string polyline, double? distanceKm, int? etaMinutes)
  {
    PlannedPolyline = polyline;
    if (distanceKm.HasValue) DistanceKm = distanceKm.Value;
    if (etaMinutes.HasValue) EtaMinutes = etaMinutes.Value;
  }

  public Result Start(DateTimeOffset now, DateTimeOffset? expectedReturn = null, bool? liveShare = null)
  {
    if (Status > TripStatus.Draft) return Errors.TripNotDraft;
    if (expectedReturn.HasValue)
    {
      if (expectedReturn.Value <= now) return Errors.InvalidReturnTime;
      ExpectedReturnTime = expectedReturn.Value;
    }
    if (liveShare.HasValue) LiveShare = liveShare.Value;
    Status = TripStatus.Active;
    StartTime = now;
    if (LiveShare)
    {
      if (TrackingToken == null) TrackingToken = TokenGenerator.NewToken();
      TrackingExpiresAt = null;
    }
    return Result.Success();
  }

  public Result Finish(DateTimeOffset now, TimeSpan linkGrace = default, TripStatisticsValues? stats = null)
  {
    if (Status != TripStatus.Active) return Errors.TripNotActive;
    Status = TripStatus.Finished;
    EndTime = now;
    RestUntil = null;
    LateReturnRemindedAt = null;
    if (TrackingToken != null)
    {
      TrackingExpiresAt = now + linkGrace;
    }
    if (stats != null)
    {
      SetStats(stats);
    }
    AddEvent(new TripFinished(Id, OwnerId, now));
    return Result.Success();
  }

  public void SetStats(TripStatisticsValues values)
  {
    if (Stats == null)
    {
      Stats = TripStats.CreateFor(Id);
    }
    Stats.Set(values);
  }

  public Result SetRest(int minutes, DateTimeOffset now)
  {
    if (Status != TripStatus.Active) return Errors.TripNotActive;
    if (minutes < 5 || minutes > 480) return Errors.InvalidRest;
    RestUntil = now.AddMinutes(minutes);
    return Result.Success();
  }

  public Result ClearRest()
  {
    if (Status != TripStatus.Active) return Errors.TripNotActive;
    RestUntil = null;
    return Result.Success();
  }

  public Result ExtendExpectedReturn(DateTimeOffset newTime, DateTimeOffset now)
  {
    if (Status != TripStatus.Active) return Errors.TripNotActive;
    if (newTime <= now) return Errors.InvalidReturnTime;
    ExpectedReturnTime = newTime;
    LateReturnRemindedAt = null;
    AddEvent(new ExpectedReturnExtended(Id, OwnerId, newTime));
    return Result.Success();
  }

  public void MarkLateReturnReminded(DateTimeOffset now)
  {
    LateReturnRemindedAt = now;
  }

  public Result RotateTrackingToken()
  {
    if (Status == TripStatus.Finished) return Errors.TripFinished;
    TrackingToken = TokenGenerator.NewToken();
    TrackingExpiresAt = null;
    LiveShare = true;
    return Result.Success();
  }

  public void DisableTracking() { TrackingToken = null; TrackingExpiresAt = null; LiveShare = false; }

  public Result AttachGroup(Guid groupId)
  {
    if (Status == TripStatus.Finished) return Errors.TripFinished;
    GroupId = groupId;
    return Result.Success();
  }

  public Result DetachGroup()
  {
    if (Status == TripStatus.Finished) return Errors.TripFinished;
    GroupId = null;
    return Result.Success();
  }

  public void ForceDetachGroup() => GroupId = null;

  public bool ApplyLivePosition(GeoPoint at, double? speed, int? battery, bool networkOk, DateTimeOffset reportedAt, DateTimeOffset receivedAt)
  {
    if (Live == null) Live = TripLive.CreateFor(Id);
    if (Live.ReportedAt != default && reportedAt < Live.ReportedAt)
    {
      Live.Refresh(receivedAt);
      return false;
    }
    Live.Apply(at.Lat, at.Lng, speed, battery, networkOk, reportedAt, receivedAt);
    ReportLocation(at, battery, receivedAt);
    return true;
  }

  public void ReportLocation(GeoPoint at, int? battery, DateTimeOffset now) =>
    AddEvent(new LocationReported(Id, OwnerId, GroupId, at, battery, LostSignalAlertOpen, now));

  public void MarkLostSignalRaised(DateTimeOffset now)
  {
    LostSignalAlertOpen = true;
    AddEvent(new LostSignalRaised(Id, now));
  }

  public void MarkSignalRestored() => LostSignalAlertOpen = false;
}
