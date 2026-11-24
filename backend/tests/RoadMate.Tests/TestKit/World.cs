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

  private static List<T> Slice<T>(IEnumerable<T> source, int? skip, int? take) where T : Entity
  {
    IEnumerable<T> q = source.OrderBy(e => e.Id);
    if (skip is > 0) q = q.Skip(skip.Value);
    if (take is > 0) q = q.Take(take.Value);
    return q.ToList();
  }

  private sealed class FakeUsers(World w) : IUserRepository
  {
    private IEnumerable<User> Active => w.Users.Where(u => !u.IsDeleted);
    public Task<User?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(u => u.Id == id));
    public Task<List<User>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(Active, skip, take));
    public Task AddAsync(User u, CancellationToken ct = default) { w.Users.Add(u); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<User> entities, CancellationToken ct = default) { w.Users.AddRange(entities); return Task.CompletedTask; }
    public void Update(User entity) { }
    public void Remove(User entity) => w.Users.Remove(entity);
    public void RemoveRange(IEnumerable<User> entities) { foreach (var e in entities.ToList()) w.Users.Remove(e); }
    public void Delete(User u) => u.SoftDelete(Guid.Empty, w.Clock.Now);
    public void DeleteRange(IEnumerable<User> entities) { foreach (var e in entities) e.SoftDelete(Guid.Empty, w.Clock.Now); }
    public Task<User?> GetWithContactsAsync(Guid id, CancellationToken ct = default) => GetByIdAsync(id, true, ct);
    public Task<User?> GetByFirebaseUidAsync(string uid, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(u => u.FirebaseUid == uid));
    public Task<Guid?> GetIdByFirebaseUidAsync(string uid, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(u => u.FirebaseUid == uid)?.Id);
    public Task<Guid?> FindIdByVerifiedPhoneAsync(string phone, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(u => u.Phone == phone)?.Id);
    public Task<IReadOnlyDictionary<Guid, string>> GetFcmTokensAsync(IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyDictionary<Guid, string>>(Active.Where(u => ids.Contains(u.Id) && !string.IsNullOrEmpty(u.FcmToken)).ToDictionary(u => u.Id, u => u.FcmToken!));
  }

  private sealed class FakeContacts(World w) : IEmergencyContactRepository
  {
    private IEnumerable<EmergencyContact> All => w.Users.SelectMany(u => u.Contacts).Where(c => !c.IsDeleted);
    public Task<EmergencyContact?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(c => c.Id == id));
    public Task<List<EmergencyContact>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(All, skip, take));
    public Task AddAsync(EmergencyContact entity, CancellationToken ct = default) => Task.CompletedTask;
    public Task AddRangeAsync(IEnumerable<EmergencyContact> entities, CancellationToken ct = default) => Task.CompletedTask;
    public void Update(EmergencyContact entity) { }
    public void Remove(EmergencyContact entity) { }
    public void RemoveRange(IEnumerable<EmergencyContact> entities) { }
    public void Delete(EmergencyContact entity) => entity.SoftDelete(Guid.Empty, w.Clock.Now);
    public void DeleteRange(IEnumerable<EmergencyContact> entities) { foreach (var e in entities) e.SoftDelete(Guid.Empty, w.Clock.Now); }
    public Task<IReadOnlyList<EmergencyContact>> ListForUserAsync(Guid userId, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<EmergencyContact>>(All.Where(c => c.UserId == userId).OrderBy(c => c.Priority).ThenBy(c => c.CreatedAt).ThenBy(c => c.Id).ToList());
    public Task<EmergencyContact?> GetOwnedAsync(Guid userId, Guid id, CancellationToken ct = default) => Task.FromResult(All.FirstOrDefault(c => c.UserId == userId && c.Id == id));
    public Task<IReadOnlyList<Guid>> FilterOwnedIdsAsync(Guid userId, IReadOnlyCollection<Guid> ids, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Guid>>(All.Where(c => c.UserId == userId && ids.Contains(c.Id)).Select(c => c.Id).ToList());
    public Task<IReadOnlyList<AlertRecipient>> GetAlertRecipientsAsync(Guid tripId, Guid ownerId, CancellationToken ct = default)
    {
      var trip = w.Trips.First(t => t.Id == tripId);
      var mine = All.Where(c => c.UserId == ownerId).OrderBy(c => c.Priority).ThenBy(c => c.Id).ToList();
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
    private IEnumerable<Trip> Active => w.Trips.Where(t => !t.IsDeleted);
    public Task<Trip?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(t => t.Id == id));
    public Task<List<Trip>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(Active, skip, take));
    public Task AddAsync(Trip t, CancellationToken ct = default) { w.Trips.Add(t); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<Trip> entities, CancellationToken ct = default) { w.Trips.AddRange(entities); return Task.CompletedTask; }
    public void Update(Trip entity) { }
    public void Remove(Trip entity) => w.Trips.Remove(entity);
    public void RemoveRange(IEnumerable<Trip> entities) { foreach (var e in entities.ToList()) w.Trips.Remove(e); }
    public void Delete(Trip t) => t.SoftDelete(Guid.Empty, w.Clock.Now);
    public void DeleteRange(IEnumerable<Trip> entities) { foreach (var e in entities) e.SoftDelete(Guid.Empty, w.Clock.Now); }
    public Task<Trip?> GetOwnedAsync(Guid id, Guid owner, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(t => t.Id == id && t.OwnerId == owner));
    public Task<Trip?> GetOwnedWithDetailsAsync(Guid id, Guid owner, CancellationToken ct = default) => GetOwnedAsync(id, owner, ct);
    public Task<IReadOnlyList<Trip>> ListByOwnerAsync(Guid owner, TripStatus? status, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Trip>>(Active.Where(t => t.OwnerId == owner && (status is null || t.Status == status))
        .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).ToList());
    public Task<CursorPage<Trip>> ListByOwnerPageAsync(Guid owner, TripStatus? status, CursorPosition? after, int limit, CancellationToken ct = default)
    {
      var take = CursorPagination.NormalizeLimit(limit);
      var q = Active.Where(t => t.OwnerId == owner && (status is null || t.Status == status));
      if (after is not null) q = q.Where(t => t.CreatedAt < after.SortAt || (t.CreatedAt == after.SortAt && t.Id.CompareTo(after.Id) < 0));
      var rows = q.OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id).Take(take + 1).ToList();
      return Task.FromResult(CursorPagination.ToPage(rows, take, t => new CursorPosition(t.CreatedAt, t.Id)));
    }
    public Task<bool> HasActiveAsync(Guid owner, CancellationToken ct = default) => Task.FromResult(Active.Any(t => t.OwnerId == owner && t.Status == TripStatus.Active));
    public Task<IReadOnlyList<Trip>> ListActiveForMonitoringAsync(CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Trip>>(Active.Where(t => t.Status == TripStatus.Active).ToList());
    public Task<IReadOnlyList<Trip>> ListInGroupAsync(Guid user, Guid group, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Trip>>(Active.Where(t => t.OwnerId == user && t.GroupId == group).ToList());
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
    public Task<Alert?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(w.Alerts.FirstOrDefault(a => a.Id == id));
    public Task<List<Alert>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(w.Alerts, skip, take));
    public Task AddAsync(Alert a, CancellationToken ct = default) { w.Alerts.Add(a); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<Alert> entities, CancellationToken ct = default) { w.Alerts.AddRange(entities); return Task.CompletedTask; }
    public void Update(Alert entity) { }
    public void Remove(Alert entity) => w.Alerts.Remove(entity);
    public void RemoveRange(IEnumerable<Alert> entities) { foreach (var e in entities.ToList()) w.Alerts.Remove(e); }
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
    public Task<AlertAck?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(w.Acks.FirstOrDefault(a => a.Id == id));
    public Task<List<AlertAck>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(w.Acks, skip, take));
    public Task AddAsync(AlertAck entity, CancellationToken ct = default) { w.Acks.Add(entity); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<AlertAck> acks, CancellationToken ct = default) { w.Acks.AddRange(acks); return Task.CompletedTask; }
    public void Update(AlertAck entity) { }
    public void Remove(AlertAck entity) => w.Acks.Remove(entity);
    public void RemoveRange(IEnumerable<AlertAck> entities) { foreach (var e in entities.ToList()) w.Acks.Remove(e); }
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
    public Task<NotificationDelivery?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(w.Deliveries.FirstOrDefault(d => d.Id == id));
    public Task<List<NotificationDelivery>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(w.Deliveries, skip, take));
    public Task AddAsync(NotificationDelivery entity, CancellationToken ct = default) { w.Deliveries.Add(entity); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<NotificationDelivery> d, CancellationToken ct = default) { w.Deliveries.AddRange(d); return Task.CompletedTask; }
    public void Update(NotificationDelivery entity) { }
    public void Remove(NotificationDelivery entity) => w.Deliveries.Remove(entity);
    public void RemoveRange(IEnumerable<NotificationDelivery> entities) { foreach (var e in entities.ToList()) w.Deliveries.Remove(e); }
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
      Task.FromResult<IReadOnlyList<AlertResponse>>(w.Alerts.Where(a => a.TripId == trip).OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id)
        .Select(a => a.ToResponse(w.Deliveries.Where(d => d.AlertId == a.Id), w.Acks.Where(k => k.AlertId == a.Id))).ToList());
    public Task<CursorPage<AlertResponse>> ListForTripPageAsync(Guid trip, CursorPosition? after, int limit, CancellationToken ct = default)
    {
      var take = CursorPagination.NormalizeLimit(limit);
      var q = w.Alerts.Where(a => a.TripId == trip);
      if (after is not null) q = q.Where(a => a.CreatedAt < after.SortAt || (a.CreatedAt == after.SortAt && a.Id.CompareTo(after.Id) < 0));
      var rows = q.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).Take(take + 1).ToList();
      var page = CursorPagination.ToPage(rows, take, a => new CursorPosition(a.CreatedAt, a.Id));
      return Task.FromResult(new CursorPage<AlertResponse>(
        page.Items.Select(a => a.ToResponse(w.Deliveries.Where(d => d.AlertId == a.Id), w.Acks.Where(k => k.AlertId == a.Id))).ToList(),
        page.NextCursor, page.HasMore));
    }
  }

  private sealed class FakeMemories(World w) : IMemoryRepository
  {
    private IEnumerable<Memory> Active => w.Memories.Where(m => !m.IsDeleted);
    public Task<Memory?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(m => m.Id == id));
    public Task<List<Memory>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(Active, skip, take));
    public Task AddAsync(Memory m, CancellationToken ct = default) { w.Memories.Add(m); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<Memory> entities, CancellationToken ct = default) { w.Memories.AddRange(entities); return Task.CompletedTask; }
    public void Update(Memory entity) { }
    public void Remove(Memory entity) => w.Memories.Remove(entity);
    public void RemoveRange(IEnumerable<Memory> entities) { foreach (var e in entities.ToList()) w.Memories.Remove(e); }
    public void Delete(Memory m) => m.SoftDelete(Guid.Empty, w.Clock.Now);
    public void DeleteRange(IEnumerable<Memory> entities) { foreach (var e in entities) e.SoftDelete(Guid.Empty, w.Clock.Now); }
    public Task<Memory?> GetOwnedAsync(Guid id, Guid owner, CancellationToken ct = default) =>
      Task.FromResult(Active.FirstOrDefault(m => m.Id == id && w.Trips.Any(t => t.Id == m.TripId && t.OwnerId == owner && !t.IsDeleted)));
    public Task<IReadOnlyList<Memory>> ListByTripAsync(Guid trip, CancellationToken ct = default) =>
      Task.FromResult<IReadOnlyList<Memory>>(Active.Where(m => m.TripId == trip).OrderBy(m => m.TakenAt).ThenBy(m => m.Id).ToList());
    public Task<CursorPage<Memory>> ListByTripPageAsync(Guid trip, CursorPosition? after, int limit, CancellationToken ct = default)
    {
      var take = CursorPagination.NormalizeLimit(limit);
      var q = Active.Where(m => m.TripId == trip);
      if (after is not null) q = q.Where(m => m.TakenAt > after.SortAt || (m.TakenAt == after.SortAt && m.Id.CompareTo(after.Id) > 0));
      var rows = q.OrderBy(m => m.TakenAt).ThenBy(m => m.Id).Take(take + 1).ToList();
      return Task.FromResult(CursorPagination.ToPage(rows, take, m => new CursorPosition(m.TakenAt, m.Id)));
    }
  }

  private sealed class FakeGroups(World w) : IGroupRepository
  {
    private IEnumerable<Group> Active => w.Groups.Where(g => !g.IsDeleted);
    public Task<Group?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(g => g.Id == id));
    public Task<List<Group>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(Active, skip, take));
    public Task AddAsync(Group g, CancellationToken ct = default) { w.Groups.Add(g); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<Group> entities, CancellationToken ct = default) { w.Groups.AddRange(entities); return Task.CompletedTask; }
    public void Update(Group entity) { }
    public void Remove(Group entity) => w.Groups.Remove(entity);
    public void RemoveRange(IEnumerable<Group> entities) { foreach (var e in entities.ToList()) w.Groups.Remove(e); }
    public void Delete(Group g) => g.SoftDelete(Guid.Empty, w.Clock.Now);
    public void DeleteRange(IEnumerable<Group> entities) { foreach (var e in entities) e.SoftDelete(Guid.Empty, w.Clock.Now); }
    public Task<Group?> GetWithMembersAsync(Guid id, CancellationToken ct = default) => GetByIdAsync(id, true, ct);
    public Task<Group?> GetByCodeAsync(string code, CancellationToken ct = default) => Task.FromResult(Active.FirstOrDefault(g => g.Code == code));
    public Task<bool> CodeExistsAsync(string code, CancellationToken ct = default) => Task.FromResult(Active.Any(g => g.Code == code));
    public Task<IReadOnlyList<Group>> ListRidingAsync(CancellationToken ct = default) => Task.FromResult<IReadOnlyList<Group>>(Active.Where(g => g.Status == GroupStatus.Riding).ToList());
  }

  private sealed class FakeGroupLoc(World w) : IGroupMemberLocationWriter
  {
    public Task UpdateAsync(Guid g, Guid u, GeoPoint at, int? b, DateTimeOffset now, CancellationToken ct = default) { w.GroupLocations.Add((g, u, at)); return Task.CompletedTask; }
  }

  private sealed class FakeGroupMessages(World w) : IGroupMessageRepository
  {
    public Task<GroupMessage?> GetByIdAsync(Guid id, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(w.Messages.FirstOrDefault(m => m.Id == id));
    public Task<List<GroupMessage>> ListEntitiesAsync(int? skip, int? take, bool trackChanges = false, CancellationToken ct = default) => Task.FromResult(Slice(w.Messages, skip, take));
    public Task AddAsync(GroupMessage m, CancellationToken ct = default) { w.Messages.Add(m); return Task.CompletedTask; }
    public Task AddRangeAsync(IEnumerable<GroupMessage> entities, CancellationToken ct = default) { w.Messages.AddRange(entities); return Task.CompletedTask; }
    public void Update(GroupMessage entity) { }
    public void Remove(GroupMessage entity) => w.Messages.Remove(entity);
    public void RemoveRange(IEnumerable<GroupMessage> entities) { foreach (var e in entities.ToList()) w.Messages.Remove(e); }
    public Task<int> PurgeOlderThanAsync(DateTimeOffset cutoff, CancellationToken ct = default) => Task.FromResult(0);
  }

  private sealed class FakeGroupRead(World w) : IGroupReadModel
  {
    public Task<IReadOnlyList<GroupListItemResponse>> ListForUserAsync(Guid u, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GroupListItemResponse>>([]);
    public Task<GroupResponse?> GetAsync(Guid id, Guid u, CancellationToken ct = default) => Task.FromResult(w.Groups.FirstOrDefault(g => g.Id == id && g.IsMember(u))?.ToResponse(10));
    public Task<GroupSnapshotResponse?> GetSnapshotAsync(Guid id, Guid u, DateTimeOffset? since, int max, CancellationToken ct = default) => Task.FromResult<GroupSnapshotResponse?>(null);
    public Task<IReadOnlyList<GroupMessageResponse>> ListMessagesAsync(Guid id, DateTimeOffset? since, int max, CancellationToken ct = default) => Task.FromResult<IReadOnlyList<GroupMessageResponse>>([]);
    public Task<CursorPage<GroupMessageResponse>> ListMessagesPageAsync(Guid id, CursorPosition? after, int limit, CancellationToken ct = default)
    {
      var take = CursorPagination.NormalizeLimit(limit);
      var q = w.Messages.Where(m => m.GroupId == id);
      if (after is not null) q = q.Where(m => m.CreatedAt > after.SortAt || (m.CreatedAt == after.SortAt && m.Id.CompareTo(after.Id) > 0));
      var rows = q.OrderBy(m => m.CreatedAt).ThenBy(m => m.Id).Take(take + 1).ToList();
      var page = CursorPagination.ToPage(rows, take, m => new CursorPosition(m.CreatedAt, m.Id));
      return Task.FromResult(new CursorPage<GroupMessageResponse>(
        page.Items.Select(m => new GroupMessageResponse(m.Id, m.SenderId, w.Users.FirstOrDefault(u => u.Id == m.SenderId)?.Name ?? "", m.QuickType ?? "", m.Text ?? "", m.CreatedAt)).ToList(),
        page.NextCursor, page.HasMore));
    }
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
