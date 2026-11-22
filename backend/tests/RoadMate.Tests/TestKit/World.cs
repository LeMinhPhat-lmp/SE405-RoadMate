using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using RoadMate.Application;
using RoadMate.Application.Abstractions.Messaging;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Abstractions.Storage;
using RoadMate.Application.Alerts;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Alerts.Mappers;
using RoadMate.Application.Common;
using RoadMate.Application.Groups;
using RoadMate.Application.Groups.DTOs.Requests;
using RoadMate.Application.Groups.DTOs.Responses;
using RoadMate.Application.Groups.Mappers;
using RoadMate.Application.Maps;
using RoadMate.Application.Maps.DTOs.Responses;
using RoadMate.Application.Maps.Interfaces;
using RoadMate.Application.Monitoring;
using RoadMate.Application.Tracking;
using RoadMate.Application.Trips.DTOs.Requests;
using RoadMate.Application.Trips.DTOs.Responses;
using RoadMate.Application.Trips.Mappers;
using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Tests.TestKit;

/// <summary>
/// In-memory implementation of every persistence/messaging PORT plus the REAL Application services and domain-event handlers
/// (wired through the real <c>AddApplication()</c>). Lets Application tests exercise whole flows (e.g. FinishTrip → handlers close alerts →
/// safe notices planned) without a database. Persistence semantics that need SQL (SKIP LOCKED, unique indexes) are covered by integration tests.
/// </summary>
public sealed class World
{
  public readonly TestClock Clock = new();
  public readonly List<User> Users = [];
  public readonly List<Trip> Trips = [];
  public readonly List<Alert> Alerts = [];
  public readonly List<AlertAck> Acks = [];
  public readonly List<NotificationDelivery> Deliveries = [];
  public readonly List<Group> Groups = [];
  public readonly List<GroupMessage> Messages = [];
  public readonly List<Memory> Memories = [];
  public readonly FakeStorage Storage = new();
  public readonly List<TripPointSample> Points = [];
  public readonly Dictionary<Guid, TripLiveUpdate> Live = [];
  public readonly List<(Guid Group, Guid User, GeoPoint At)> GroupLocations = [];
  public readonly RecordingChannel Sms = new(DeliveryChannel.Sms), PushCh = new(DeliveryChannel.Push);
  public readonly RecordingPush Push = new();
  public int SaveCount;
  public bool ThrowUniqueOnNextSave;
  public Func<Alert, bool>? RepeatFailsFor;
  public ServiceProvider Provider { get; }
  public IServiceScope Scope { get; }

  public World(Action<AlertOptions>? alertOptions = null, Action<DebugOptions>? debug = null)
  {
    var s = new ServiceCollection();
    s.AddLogging();
    s.AddApplication();
    s.AddSingleton<TimeProvider>(Clock);
    s.AddSingleton(this);
    s.AddSingleton(Options.Create(new AlertOptions { MaxRepeats = 3 }.Apply(alertOptions)));
    s.AddSingleton(Options.Create(new DebugOptions { EnableSimulator = true }.Apply(debug)));
    s.AddSingleton(Options.Create(new TrackingOptions { BaseUrl = "https://roadmate.test/track", AckUrl = "https://roadmate.test/ack", ExpireAfterFinishMinutes = 120 }));
    s.AddSingleton(Options.Create(new SafetyOptions()));
    s.AddSingleton(Options.Create(new GroupOptions()));
    s.AddScoped<IUnitOfWork>(sp => new FakeUow(this, sp));
    s.AddScoped<IUserRepository>(_ => new FakeUsers(this));
    s.AddScoped<IEmergencyContactRepository>(_ => new FakeContacts(this));
    s.AddScoped<ITripRepository>(_ => new FakeTrips(this));
    s.AddScoped<ITripTelemetryWriter>(_ => new FakeTelemetry(this));
    s.AddScoped<ITripPointReader>(_ => new FakeTelemetry(this));
    s.AddScoped<IAlertRepository>(_ => new FakeAlerts(this));
    s.AddScoped<IAlertAckRepository>(_ => new FakeAcks(this));
    s.AddScoped<INotificationDeliveryRepository>(_ => new FakeDeliveries(this));
    s.AddScoped<IAlertReadModel>(_ => new FakeAlertRead(this));
    s.AddScoped<IGroupRepository>(_ => new FakeGroups(this));
    s.AddScoped<IGroupMemberLocationWriter>(_ => new FakeGroupLoc(this));
    s.AddScoped<IGroupMessageRepository>(_ => new FakeGroupMessages(this));
    s.AddScoped<IGroupReadModel>(_ => new FakeGroupRead(this));
    s.AddSingleton<INotificationChannel>(Sms);
    s.AddSingleton<INotificationChannel>(PushCh);
    s.AddSingleton<IPushNotifier>(Push);
    s.AddSingleton<IMediaStorage>(Storage);
    s.AddSingleton(Options.Create(new StorageOptions()));
    s.AddScoped<IMemoryRepository>(_ => new FakeMemories(this));
    s.AddSingleton<IGeocodingService, NoGeocoder>();
    s.AddSingleton<IIsolatedExecutor, ScopedExecutor>();
    Provider = s.BuildServiceProvider();
    Scope = Provider.CreateScope();
  }

  public T Get<T>() where T : notnull => Scope.ServiceProvider.GetRequiredService<T>();

  // ---- seed helpers
  public User AddUser(string name = "Nguyễn Văn An", string? fcm = null, string? phone = null)
  {
    var u = User.Create("uid-" + Guid.NewGuid().ToString("N")[..6], "u@x.vn", name);
    u.ApplyAuthSync(null, fcm, phone);
    Users.Add(u);
    return u;
  }

  public EmergencyContact AddContact(User owner, string phone = "0912345678", Guid? linkedUser = null)
  {
    var c = owner.AddContact("Mẹ", Make.Phone(phone), "mẹ", null, linkedUser).Value;
    return c;
  }

  public Trip AddActiveTrip(User owner, params EmergencyContact[] recipients)
  {
    var t = Make.ActiveTrip(owner.Id, now: Clock.Now);
    if (recipients.Length > 0) t.SetRecipients(recipients.Select(r => r.Id).ToList());
    Trips.Add(t);
    return t;
  }

  public IEnumerable<IHasDomainEvents> AllEntities() =>
    Users.Cast<IHasDomainEvents>().Concat(Users.SelectMany(u => u.Contacts)).Concat(Trips).Concat(Alerts).Concat(Groups).Concat(Memories).Concat(Deliveries).Concat(Acks);

  // ---- fakes -------------------------------------------------------------------------------------------------

  private sealed class FakeUow(World w, IServiceProvider sp) : IUnitOfWork
  {
    public async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
      if (w.ThrowUniqueOnNextSave) { w.ThrowUniqueOnNextSave = false; throw new UniqueViolationException("test"); }
      for (var round = 0; round < 5; round++)
      {
        var pending = w.AllEntities().Where(e => e.DomainEvents.Count > 0).ToList();
        if (pending.Count == 0) break;
        var events = pending.SelectMany(e => e.DomainEvents).ToList();
        foreach (var e in pending) e.ClearDomainEvents();
        foreach (var ev in events)
        {
          var handlerType = typeof(IDomainEventHandler<>).MakeGenericType(ev.GetType());
          foreach (var h in sp.GetServices(handlerType))
            await (Task)handlerType.GetMethod("HandleAsync")!.Invoke(h, [ev, ct])!;
        }
      }
      w.SaveCount++;
      return 1;
    }
    public Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default) => action(ct);
    public void DiscardChanges() { }
  }

  private sealed class FakeUsers(World w) : IUserRepository
  {
    public Task<User?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(w.Users.FirstOrDefault(u => u.Id == id));
    public Task AddAsync(User u, CancellationToken ct = default) { w.Users.Add(u); return Task.CompletedTask; }
    public void Delete(User u) => u.SoftDelete(Guid.Empty, w.Clock.Now);
    public Task<User?> GetWithContactsAsync(Guid id, CancellationToken ct = default) => GetByIdAsync(id, ct);
    public Task<User?> GetByFirebaseUidAsync(string uid, CancellationToken ct = default) => Task.FromResult(w.Users.FirstOrDefault(u => u.FirebaseUid == uid));
    public Task<Guid?> GetIdByFirebaseUidAsync(string uid, CancellationToken ct = default) => Task.FromResult(w.Users.FirstOrDefault(u => u.FirebaseUid == uid)?.Id);
    public Task<Guid?> FindIdByVerifiedPhoneAsync(string phone, CancellationToken ct = default) => Task.FromResult(w.Users.FirstOrDefault(u => u.Phone == phone)?.Id);
    public Task<IReadOnlyDictionary<Guid, string>> GetFcmTokensAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyDictionary<Guid, string>>(w.Users.Where(u => ids.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmToken)).ToDictionary(u => u.Id, u => u.FcmToken!));
  }

  private sealed class FakeContacts(World w) : IEmergencyContactRepository
  {
    private IEnumerable<EmergencyContact> All => w.Users.SelectMany(u => u.Contacts).Where(c => !c.IsDeleted);
    public Task<IReadOnlyList<EmergencyContact>> ListForUserAsync(Guid userId, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<EmergencyContact>>(All.Where(c => c.UserId == userId).OrderBy(c => c.Priority).ToList());
    public Task<EmergencyContact?> GetOwnedAsync(Guid userId, Guid id, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(c => c.UserId == userId && c.Id == id));
    public Task<IReadOnlyList<Guid>> FilterOwnedIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Guid>>(All.Where(c => c.UserId == userId && ids.Contains(c.Id)).Select(c => c.Id).ToList());
    public Task<IReadOnlyList<AlertRecipient>> GetAlertRecipientsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default)
    {
      var trip = w.Trips.First(t => t.Id == tripId);
      var mine = All.Where(c => c.UserId == ownerId).OrderBy(c => c.Priority).ToList();
      var chosen = mine.Where(c => trip.Recipients.Any(r => r.ContactId == c.Id)).ToList();
      var list = (chosen.Count > 0 ? chosen : mine).Select(c => new AlertRecipient(c.Id, c.Name, c.Phone, c.LinkedUserId,
        c.LinkedUserId is { } l ? w.Users.FirstOrDefault(u => u.Id == l)?.FcmToken : null)).ToList();
      return Task.FromResult<IReadOnlyList<AlertRecipient>>(list);
    }
    public Task<int> LinkByPhoneAsync(string phone, Guid userId, CancellationToken ct = default)
    {
      var n = 0; foreach (var c in All.Where(c => c.Phone == phone && c.LinkedUserId is null && c.UserId != userId)) { c.LinkTo(userId); n++; }
      return Task.FromResult(n);
    }
  }

  private sealed class FakeTrips(World w) : ITripRepository
  {
    public Task<Trip?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(w.Trips.FirstOrDefault(t => t.Id == id && !t.IsDeleted));
    public Task AddAsync(Trip t, CancellationToken ct = default) { w.Trips.Add(t); return Task.CompletedTask; }
    public void Delete(Trip t) => t.SoftDelete(Guid.Empty, w.Clock.Now);
    public Task<Trip?> GetOwnedAsync(Guid id, Guid owner, CancellationToken ct = default) => Task.FromResult(w.Trips.FirstOrDefault(t => t.Id == id && t.OwnerId == owner && !t.IsDeleted));
    public Task<Trip?> GetOwnedWithDetailsAsync(Guid id, Guid owner, CancellationToken ct = default) => GetOwnedAsync(id, owner, ct);
    public Task<IReadOnlyList<Trip>> ListByOwnerAsync(Guid owner, TripStatus? status, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Trip>>(w.Trips.Where(t => t.OwnerId == owner && !t.IsDeleted && (status is null || t.Status == status)).ToList());
    public Task<bool> HasActiveAsync(Guid owner, CancellationToken ct = default) => Task.FromResult(w.Trips.Any(t => t.OwnerId == owner && t.Status == TripStatus.Active && !t.IsDeleted));
    public Task<IReadOnlyList<Trip>> ListActiveForMonitoringAsync(CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Trip>>(w.Trips.Where(t => t.Status == TripStatus.Active && !t.IsDeleted).ToList());
    public Task<IReadOnlyList<Trip>> ListInGroupAsync(Guid user, Guid group, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Trip>>(w.Trips.Where(t => t.OwnerId == user && t.GroupId == group).ToList());
  }

  private sealed class FakeTelemetry(World w) : ITripTelemetryWriter, ITripPointReader
  {
    public Task<int> AppendPointsAsync(Guid tripId, IReadOnlyList<TripPointSample> s, CancellationToken ct = default) { w.Points.AddRange(s); return Task.FromResult(s.Count); }
    public Task<bool> UpsertLiveIfNewerAsync(TripLiveUpdate u, CancellationToken ct = default)
    {
      if (w.Live.TryGetValue(u.TripId, out var cur) && u.ReportedAt < cur.ReportedAt) return Task.FromResult(false);
      w.Live[u.TripId] = u;
      LoadLiveIntoTrip(w.Trips.FirstOrDefault(t => t.Id == u.TripId), u);
      return Task.FromResult(true);
    }
    /// <summary>What EF would load into <c>trip.Live</c> after the SQL upsert (no domain event: the service raises it).</summary>
    private static void LoadLiveIntoTrip(Trip? trip, TripLiveUpdate u)
    {
      if (trip is null) return;
      var flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
      var live = trip.Live ?? (TripLive)Activator.CreateInstance(typeof(TripLive), true)!;
      typeof(TripLive).GetMethod("Apply", flags)!.Invoke(live, [u.Location.Lat, u.Location.Lng, u.Speed, u.Battery, u.NetworkOk, u.ReportedAt, u.ReceivedAt]);
      typeof(Trip).GetProperty("Live")!.SetValue(trip, live);
    }

    public Task<IReadOnlyList<TripPointSample>> ListAsync(Guid tripId, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TripPointSample>>(w.Points.OrderBy(p => p.RecordedAt).ToList());
    public Task<IReadOnlyList<TripPointSample>> LastAsync(Guid tripId, int take, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<TripPointSample>>(w.Points.OrderByDescending(p => p.RecordedAt).Take(take).ToList());
  }

  private sealed class FakeAlerts(World w) : IAlertRepository
  {
    public Task<Alert?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(w.Alerts.FirstOrDefault(a => a.Id == id));
    public Task AddAsync(Alert a, CancellationToken ct = default) { w.Alerts.Add(a); return Task.CompletedTask; }
    public Task<Alert?> GetOwnedAsync(Guid id, Guid user, CancellationToken ct = default) => Task.FromResult(w.Alerts.FirstOrDefault(a => a.Id == id && a.UserId == user));
    public Task<Alert?> GetByClientIdAsync(string c, CancellationToken ct = default) => Task.FromResult(w.Alerts.FirstOrDefault(a => a.ClientId == c));
    public Task<IReadOnlyList<Alert>> ListOpenByTripAsync(Guid trip, AlertType? type, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Alert>>(w.Alerts.Where(a => a.TripId == trip && a.IsOpen && (type is null || a.Type == type)).ToList());
    public Task<bool> HasOpenAsync(Guid trip, AlertType type, CancellationToken ct = default) => Task.FromResult(w.Alerts.Any(a => a.TripId == trip && a.Type == type && a.IsOpen));
    public Task<bool> ExistsSinceAsync(Guid trip, AlertType type, DateTimeOffset since, CancellationToken ct = default) => Task.FromResult(w.Alerts.Any(a => a.TripId == trip && a.Type == type && a.CreatedAt >= since));
    public Task<IReadOnlyList<Guid>> ClaimDueForRepeatAsync(string worker, DateTimeOffset now, TimeSpan lease, int take, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Guid>>(w.Alerts.Where(a => a.IsOpen && a.NextRepeatAt <= now).Take(take).Select(a => a.Id).ToList());
    public Task<int> CountForTripAsync(Guid trip, CancellationToken ct = default) => Task.FromResult(w.Alerts.Count(a => a.TripId == trip));
  }

  private sealed class FakeAcks(World w) : IAlertAckRepository
  {
    public Task AddRangeAsync(IEnumerable<AlertAck> acks, CancellationToken ct = default) { w.Acks.AddRange(acks); return Task.CompletedTask; }
    public Task<IReadOnlyList<AlertAck>> ListByAlertAsync(Guid alert, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<AlertAck>>(w.Acks.Where(a => a.AlertId == alert).ToList());
    public Task<AlertAck?> GetByTokenAsync(string token, CancellationToken ct = default)
    {
      var ack = w.Acks.FirstOrDefault(a => a.Token == token);
      if (ack is null) return Task.FromResult<AlertAck?>(null);
      var alert = w.Alerts.First(a => a.Id == ack.AlertId);
      typeof(AlertAck).GetProperty("Alert")!.SetValue(ack, alert);                         // navigation normally loaded by EF Include
      typeof(Alert).GetProperty("User")!.SetValue(alert, w.Users.First(u => u.Id == alert.UserId));
      return Task.FromResult<AlertAck?>(ack);
    }
  }

  private sealed class FakeDeliveries(World w) : INotificationDeliveryRepository
  {
    public Task AddRangeAsync(IEnumerable<NotificationDelivery> d, CancellationToken ct = default) { w.Deliveries.AddRange(d); return Task.CompletedTask; }
    public Task<bool> ExistsAsync(Guid alert, NotificationKind kind, int seq, CancellationToken ct = default) => Task.FromResult(w.Deliveries.Any(d => d.AlertId == alert && d.Kind == kind && d.Sequence == seq));
    public Task<IReadOnlyList<NotificationDelivery>> ClaimDueAsync(string worker, DateTimeOffset now, TimeSpan lease, int take, CancellationToken ct = default)
    {
      var claimed = new List<NotificationDelivery>();
      foreach (var d in w.Deliveries.Where(d => (d.Status == DeliveryStatus.Queued && d.NextAttemptAt <= now) || (d.Status == DeliveryStatus.Processing && d.LockedUntil < now)).Take(take))
        if (d.Claim(worker, now, lease).IsSuccess) claimed.Add(d);
      return Task.FromResult<IReadOnlyList<NotificationDelivery>>(claimed);
    }
    public Task<DeliveryContext?> LoadContextAsync(NotificationDelivery d, CancellationToken ct = default)
    {
      var alert = w.Alerts.FirstOrDefault(a => a.Id == d.AlertId); if (alert is null) return Task.FromResult<DeliveryContext?>(null);
      var owner = w.Users.First(u => u.Id == alert.UserId);
      var contact = w.Users.SelectMany(u => u.Contacts).FirstOrDefault(c => c.Id == d.ContactId);
      var token = w.Acks.FirstOrDefault(a => a.AlertId == alert.Id && a.ContactId == d.ContactId)?.Token;
      var push = contact?.LinkedUserId is { } l ? w.Users.FirstOrDefault(u => u.Id == l)?.FcmToken : null;
      return Task.FromResult<DeliveryContext?>(new DeliveryContext(alert, owner, d.ContactId, contact?.Phone, token, push));
    }
    public Task<IReadOnlyList<NotificationDelivery>> ListByAlertAsync(Guid alert, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<NotificationDelivery>>(w.Deliveries.Where(d => d.AlertId == alert).ToList());
  }

  private sealed class FakeAlertRead(World w) : IAlertReadModel
  {
    public Task<IReadOnlyList<AlertResponse>> ListForTripAsync(Guid trip, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<AlertResponse>>(w.Alerts.Where(a => a.TripId == trip).Select(a => a.ToResponse(w.Deliveries.Where(d => d.AlertId == a.Id), w.Acks.Where(k => k.AlertId == a.Id))).ToList());
  }

  private sealed class FakeMemories(World w) : IMemoryRepository
  {
    public Task<Memory?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(w.Memories.FirstOrDefault(m => m.Id == id && !m.IsDeleted));
    public Task AddAsync(Memory m, CancellationToken ct = default) { w.Memories.Add(m); return Task.CompletedTask; }
    public void Delete(Memory m) => m.SoftDelete(Guid.Empty, w.Clock.Now);
    public Task<Memory?> GetOwnedAsync(Guid id, Guid owner, CancellationToken ct = default) =>
      Task.FromResult(w.Memories.FirstOrDefault(m => m.Id == id && !m.IsDeleted && w.Trips.Any(t => t.Id == m.TripId && t.OwnerId == owner)));
    public Task<IReadOnlyList<Memory>> ListByTripAsync(Guid trip, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Memory>>(w.Memories.Where(m => m.TripId == trip && !m.IsDeleted).OrderBy(m => m.TakenAt).ToList());
  }

  private sealed class FakeGroups(World w) : IGroupRepository
  {
    public Task<Group?> GetByIdAsync(Guid id, CancellationToken ct = default) => Task.FromResult(w.Groups.FirstOrDefault(g => g.Id == id));
    public Task AddAsync(Group g, CancellationToken ct = default) { w.Groups.Add(g); return Task.CompletedTask; }
    public void Delete(Group g) => g.SoftDelete(Guid.Empty, w.Clock.Now);
    public Task<Group?> GetWithMembersAsync(Guid id, CancellationToken ct = default) => GetByIdAsync(id, ct);
    public Task<Group?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult(w.Groups.FirstOrDefault(g => g.Code == code));
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) => Task.FromResult(w.Groups.Any(g => g.Code == code));
    public Task<IReadOnlyList<Group>> ListRidingAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Group>>(w.Groups.Where(g => g.Status == GroupStatus.Riding).ToList());
  }

  private sealed class FakeGroupLoc(World w) : IGroupMemberLocationWriter
  {
    public Task UpdateAsync(Guid g, Guid u, GeoPoint at, int? b, DateTimeOffset now, CancellationToken ct = default) { w.GroupLocations.Add((g, u, at)); return Task.CompletedTask; }
  }

  private sealed class FakeGroupMessages(World w) : IGroupMessageRepository
  {
    public Task AddAsync(GroupMessage m, CancellationToken ct = default) { w.Messages.Add(m); return Task.CompletedTask; }
    public Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) => Task.FromResult(0);
  }

  private sealed class FakeGroupRead(World w) : IGroupReadModel
  {
    public Task<IReadOnlyList<GroupListItemResponse>> ListForUserAsync(Guid u, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GroupListItemResponse>>([]);
    public Task<GroupResponse?> GetAsync(Guid id, Guid u, CancellationToken ct = default) => Task.FromResult(w.Groups.FirstOrDefault(g => g.Id == id && g.IsMember(u))?.ToResponse(10));
    public Task<GroupSnapshotResponse?> GetSnapshotAsync(Guid id, Guid u, DateTimeOffset? since, int max, CancellationToken ct = default) => Task.FromResult<GroupSnapshotResponse?>(null);
    public Task<IReadOnlyList<GroupMessageResponse>> ListMessagesAsync(Guid id, DateTimeOffset? since, int max, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GroupMessageResponse>>([]);
  }

  private sealed class NoGeocoder : IGeocodingService
  {
    private static readonly Error Off = Error.NotConfigured("MAP_NOT_CONFIGURED", "x");
    public Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct) => Task.FromResult<Result<ReverseResult>>(Off);
    public Task<Result<List<GeocodeResult>>> ForwardAsync(string address, int? limit, CancellationToken ct) => Task.FromResult<Result<List<GeocodeResult>>>(Off);
    public Task<Result<StreetResult>> StreetAsync(double lat, double lng, CancellationToken ct) => Task.FromResult<Result<StreetResult>>(Off);
  }

  private sealed class ScopedExecutor(IServiceProvider root, World w) : IIsolatedExecutor
  {
    public async Task RunAsync<T>(Func<T, Task> work, CancellationToken ct) where T : notnull
    {
      using var scope = w.Provider.CreateScope();
      await work(scope.ServiceProvider.GetRequiredService<T>());
    }
  }
}

public static class OptionsExt
{
  public static T Apply<T>(this T o, Action<T>? a) { a?.Invoke(o); return o; }
}

/// <summary>Records sends; can be told to fail.</summary>
public sealed class RecordingChannel(DeliveryChannel channel) : INotificationChannel
{
  public DeliveryChannel Channel { get; } = channel;
  public readonly List<NotificationMessage> Sent = [];
  public bool Fail;
  public Task<Result<string>> SendAsync(NotificationMessage m, CancellationToken ct)
  {
    if (Fail) return Task.FromResult<Result<string>>(Error.External("SMS_FAILED", "boom"));
    Sent.Add(m); return Task.FromResult<Result<string>>($"id-{Sent.Count}");
  }
}

public sealed class RecordingPush : IPushNotifier
{
  public readonly List<(IReadOnlyCollection<Guid> Users, PushContent Content)> Calls = [];
  public Task<int> NotifyUsersAsync(IReadOnlyCollection<Guid> users, PushContent c, CancellationToken ct = default) { Calls.Add((users, c)); return Task.FromResult(users.Count); }
  public Task<Result<string>> NotifyTokenAsync(string t, PushContent c, CancellationToken ct = default) => Task.FromResult<Result<string>>("ok");
}

/// <summary>Storage stub: signs nothing, pretends every uploaded object exists unless told otherwise.</summary>
public sealed class FakeStorage : IMediaStorage
{
  public StoredObject? Existing = new(1024, "image/jpeg");
  public Result<SignedUpload> CreateUploadUrl(string path, string contentType, long max) =>
    new SignedUpload("https://storage.test/up/" + path, new Dictionary<string, string> { ["Content-Type"] = contentType }, DateTime.UtcNow.AddMinutes(15));
  public Result<SignedUrl> CreateDownloadUrl(string path) => new SignedUrl("https://storage.test/dl/" + path, DateTime.UtcNow.AddHours(1));
  public Task<Result<StoredObject>> GetObjectAsync(string path, CancellationToken ct) =>
    Task.FromResult<Result<StoredObject>>(Existing is null ? Error.NotFound("MEDIA_OBJECT_NOT_FOUND", "x") : Existing);
}
