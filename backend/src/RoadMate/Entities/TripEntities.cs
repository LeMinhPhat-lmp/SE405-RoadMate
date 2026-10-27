using RoadMate.Entities.Abstractions;

namespace RoadMate.Entities;

public static class TripStatus { public const string Draft = "draft", Active = "active", Finished = "finished"; }
public static class MemoryType { public const string Photo = "photo", Note = "note", Video = "video"; public static readonly string[] All = { Photo, Note, Video }; }

public class Trip : BaseEntity
{
  public string OwnerId { get; set; } = default!;
  public Guid? GroupId { get; set; }
  public string Title { get; set; } = "";
  public string Status { get; set; } = TripStatus.Draft;
  public string OriginName { get; set; } = "";
  public double OriginLat { get; set; }
  public double OriginLng { get; set; }
  public string DestName { get; set; } = "";
  public double DestLat { get; set; }
  public double DestLng { get; set; }
  public string? PlannedPolyline { get; set; }
  public double DistanceKm { get; set; }
  public int EtaMinutes { get; set; }
  public bool AvoidHighway { get; set; }
  public bool AvoidToll { get; set; }
  public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
  public DateTime? StartTime { get; set; }
  public DateTime? EndTime { get; set; }
  public DateTime? ExpectedReturnTime { get; set; }
  public int? IdleMinutes { get; set; }
  public string? CrashSensitivity { get; set; }
  public bool LiveShare { get; set; }
  public string? TrackingToken { get; set; }
  public DateTime? TrackingExpiresAt { get; set; }
  public DateTime? RestUntil { get; set; }

  public User? Owner { get; set; }
  public Group? Group { get; set; }
  public ICollection<TripWaypoint> Waypoints { get; set; } = new List<TripWaypoint>();
  public ICollection<TripAlertRecipient> Recipients { get; set; } = new List<TripAlertRecipient>();
  public ICollection<TripPoint> Points { get; set; } = new List<TripPoint>();
  public ICollection<Memory> Memories { get; set; } = new List<Memory>();
  public ICollection<Alert> Alerts { get; set; } = new List<Alert>();
  public TripLive? Live { get; set; }
  public TripStats? Stats { get; set; }
}

public class TripWaypoint
{
  public Guid Id { get; set; }
  public Guid TripId { get; set; }
  public int Seq { get; set; }
  public string Name { get; set; } = "";
  public double Lat { get; set; }
  public double Lng { get; set; }
  public int? PlannedStayMin { get; set; }
  public Trip? Trip { get; set; }
}

public class TripAlertRecipient
{
  public Guid TripId { get; set; }
  public Guid ContactId { get; set; }
  public Trip? Trip { get; set; }
  public EmergencyContact? Contact { get; set; }
}

public class TripPoint : BaseEntity
{
  public Guid TripId { get; set; }
  public double Lat { get; set; }
  public double Lng { get; set; }
  public double? Speed { get; set; }      // km/h
  public double? Altitude { get; set; }
  public int? Battery { get; set; }
  public DateTime RecordedAt { get; set; }
  public Trip? Trip { get; set; }
}

public class TripLive
{
  public Guid TripId { get; set; }
  public double Lat { get; set; }
  public double Lng { get; set; }
  public double? Speed { get; set; }
  public int? Battery { get; set; }
  public bool NetworkOk { get; set; } = true;
  public DateTime UpdatedAt { get; set; }
  public Trip? Trip { get; set; }
}

public class TripStats
{
  public Guid TripId { get; set; }
  public double DistanceKm { get; set; }
  public int MovingMin { get; set; }
  public int RestMin { get; set; }
  public int StopCount { get; set; }
  public double MaxSpeed { get; set; }
  public double AvgSpeed { get; set; }
  public double MaxAltitude { get; set; }
  public int AlertCount { get; set; }
  public Trip? Trip { get; set; }
}

public class Memory : BaseEntity
{
  public Guid TripId { get; set; }
  public string Type { get; set; } = MemoryType.Note;
  public string? MediaUrl { get; set; }         // URL Firebase Storage (client tự upload)
  public string? Text { get; set; }
  public double? Lat { get; set; }
  public double? Lng { get; set; }
  public string? Mood { get; set; }
  public string? PlaceName { get; set; }
  public int? Rating { get; set; }
  public DateTime TakenAt { get; set; }
  public Trip? Trip { get; set; }
}
