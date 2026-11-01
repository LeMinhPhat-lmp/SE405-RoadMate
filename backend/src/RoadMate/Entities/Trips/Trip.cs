namespace RoadMate.Entities.Trips;

public class Trip : FullAuditedEntity
{
  public Guid OwnerId { get; set; }
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

  // ---------- Hành vi domain (quy tắc đổi trạng thái nằm ở đây, service chỉ điều phối)

  /// <summary>Draft -> Active. Nếu bật LiveShare thì giữ token cũ (hoặc tạo mới) và bỏ hạn.</summary>
  public void Start(DateTime now)
  {
    Status = TripStatus.Active;
    StartTime = now;
    if (!LiveShare) return;
    TrackingToken ??= TokenGenerator.NewTrackingToken();
    TrackingExpiresAt = null;
  }

  /// <summary>Active -> Finished: chốt giờ kết thúc, bỏ nghỉ chân, vô hiệu link theo dõi.</summary>
  public void Finish(DateTime now)
  {
    Status = TripStatus.Finished;
    EndTime = now;
    RestUntil = null;
    if (TrackingToken is not null) TrackingExpiresAt = now;
  }

  /// <summary>Tạo token mới (link cũ mất hiệu lực) và bật chia sẻ.</summary>
  public void RotateTrackingToken()
  {
    TrackingToken = TokenGenerator.NewTrackingToken();
    TrackingExpiresAt = null;
    LiveShare = true;
  }

  public void DisableTracking()
  {
    TrackingToken = null;
    TrackingExpiresAt = null;
    LiveShare = false;
  }

  /// <summary>Đồng bộ danh sách người nhận cảnh báo của chuyến với <paramref name="contactIds"/>.</summary>
  public void SetRecipients(IReadOnlyCollection<Guid> contactIds)
  {
    foreach (var r in Recipients.Where(r => !contactIds.Contains(r.ContactId)).ToList()) Recipients.Remove(r);
    foreach (var id in contactIds.Where(id => Recipients.All(r => r.ContactId != id)))
      Recipients.Add(new TripAlertRecipient { TripId = Id, ContactId = id });
  }

  /// <summary>Ghi đè vị trí mới nhất (tạo bản ghi TripLive nếu chưa có).</summary>
  public void UpdateLive(double lat, double lng, double? speed, int? battery, bool networkOk, DateTime now)
  {
    var live = Live ??= new TripLive { TripId = Id };
    live.Lat = lat; live.Lng = lng; live.Speed = speed; live.Battery = battery ?? live.Battery;
    live.NetworkOk = networkOk; live.UpdatedAt = now;
  }
}
