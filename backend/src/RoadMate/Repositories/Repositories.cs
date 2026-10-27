using RoadMate.Data;

namespace RoadMate.Repositories;

public class GenericRepository<T> : IRepository<T> where T : class
{
  protected readonly AppDbContext Db;
  protected readonly DbSet<T> Set;
  public GenericRepository(AppDbContext db) { Db = db; Set = db.Set<T>(); }
  public async Task<T?> GetByIdAsync(object id, CancellationToken ct = default) => await Set.FindAsync(new[] { id }, ct);
  public async Task AddAsync(T entity, CancellationToken ct = default) => await Set.AddAsync(entity, ct);
  public Task AddRangeAsync(IEnumerable<T> entities, CancellationToken ct = default) => Set.AddRangeAsync(entities, ct);
  public void Update(T entity) => Set.Update(entity);
  public void Remove(T entity) => Set.Remove(entity);
}

public class UserRepository : GenericRepository<User>, IUserRepository
{
  public UserRepository(AppDbContext db) : base(db) { }
  public Task<User?> GetByUidAsync(string uid, CancellationToken ct = default) => Set.FirstOrDefaultAsync(u => u.Uid == uid, ct);
  public Task<User?> GetByPhoneAsync(string phone, CancellationToken ct = default) => Set.FirstOrDefaultAsync(u => u.Phone == phone, ct);
}

public class EmergencyContactRepository : GenericRepository<EmergencyContact>, IEmergencyContactRepository
{
  public EmergencyContactRepository(AppDbContext db) : base(db) { }
  public Task<List<EmergencyContact>> ListByUserAsync(string uid, CancellationToken ct = default) =>
      Set.Where(c => c.UserId == uid).OrderBy(c => c.Priority).ToListAsync(ct);
  public Task<int> CountByUserAsync(string uid, CancellationToken ct = default) => Set.CountAsync(c => c.UserId == uid, ct);
  public Task<EmergencyContact?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
      Set.Include(c => c.LinkedUser).FirstOrDefaultAsync(c => c.Id == id && c.UserId == uid, ct);
  public Task<List<EmergencyContact>> GetByIdsAsync(IEnumerable<Guid> ids, string uid, CancellationToken ct = default)
  {
    var list = ids.Distinct().ToList();
    return Set.Where(c => c.UserId == uid && list.Contains(c.Id)).ToListAsync(ct);
  }
  public Task<List<EmergencyContact>> ListByPhoneAsync(string phone, CancellationToken ct = default) =>
      Set.Where(c => c.Phone == phone).ToListAsync(ct);
  public async Task<List<EmergencyContact>> GetAlertRecipientsAsync(Guid tripId, string uid, CancellationToken ct = default)
  {
    var selected = await Db.TripAlertRecipients.Where(r => r.TripId == tripId).Select(r => r.ContactId).ToListAsync(ct);
    var q = Set.Include(c => c.LinkedUser).Where(c => c.UserId == uid);
    if (selected.Count > 0) q = q.Where(c => selected.Contains(c.Id));
    return await q.OrderBy(c => c.Priority).ToListAsync(ct);
  }
}

public class TripRepository : GenericRepository<Trip>, ITripRepository
{
  public TripRepository(AppDbContext db) : base(db) { }
  public Task<Trip?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
      Set.FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == uid, ct);
  public Task<Trip?> GetOwnedWithDetailsAsync(Guid id, string uid, CancellationToken ct = default) =>
      Set.Include(t => t.Waypoints).Include(t => t.Recipients).Include(t => t.Live).Include(t => t.Stats)
         .AsSplitQuery().FirstOrDefaultAsync(t => t.Id == id && t.OwnerId == uid, ct);
  public Task<List<Trip>> ListByOwnerAsync(string uid, string? status, CancellationToken ct = default)
  {
    var q = Set.AsNoTracking().Where(t => t.OwnerId == uid);
    if (!string.IsNullOrEmpty(status)) q = q.Where(t => t.Status == status);
    return q.OrderByDescending(t => t.CreatedAt).ToListAsync(ct);
  }
  public Task<bool> HasActiveAsync(string uid, CancellationToken ct = default) =>
      Set.AnyAsync(t => t.OwnerId == uid && t.Status == TripStatus.Active, ct);
  public Task<Trip?> GetByTrackingTokenAsync(string token, CancellationToken ct = default) =>
      Set.Include(t => t.Owner).Include(t => t.Live).FirstOrDefaultAsync(t => t.TrackingToken == token, ct);
}

public class TripPointRepository : GenericRepository<TripPoint>, ITripPointRepository
{
  public TripPointRepository(AppDbContext db) : base(db) { }
  public Task<List<TripPoint>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.AsNoTracking().Where(p => p.TripId == tripId).OrderBy(p => p.RecordedAt).ToListAsync(ct);
  public async Task<List<TripPoint>> ListRecentAsync(Guid tripId, int take, CancellationToken ct = default)
  {
    var l = await Set.AsNoTracking().Where(p => p.TripId == tripId).OrderByDescending(p => p.RecordedAt).Take(take).ToListAsync(ct);
    l.Reverse(); return l;
  }
}

public class MemoryRepository : GenericRepository<Memory>, IMemoryRepository
{
  public MemoryRepository(AppDbContext db) : base(db) { }
  public Task<List<Memory>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.AsNoTracking().Where(m => m.TripId == tripId).OrderBy(m => m.TakenAt).ToListAsync(ct);
  public Task<Memory?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
      Set.Include(m => m.Trip).FirstOrDefaultAsync(m => m.Id == id && m.Trip!.OwnerId == uid, ct);
}

public class AlertRepository : GenericRepository<Alert>, IAlertRepository
{
  public AlertRepository(AppDbContext db) : base(db) { }
  public Task<Alert?> GetByClientIdAsync(string clientId, CancellationToken ct = default) =>
      Set.Include(a => a.Deliveries).FirstOrDefaultAsync(a => a.ClientId == clientId, ct);
  public Task<Alert?> GetOwnedAsync(Guid id, string uid, CancellationToken ct = default) =>
      Set.Include(a => a.Trip).Include(a => a.User).FirstOrDefaultAsync(a => a.Id == id && a.UserId == uid, ct);
  public Task<List<Alert>> ListByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.AsNoTracking().Include(a => a.Deliveries).Where(a => a.TripId == tripId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
  public Task<List<Alert>> ListOpenByTripAsync(Guid tripId, CancellationToken ct = default) =>
      Set.Where(a => a.TripId == tripId && (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent)).ToListAsync(ct);
  public Task<int> CountByTripAsync(Guid tripId, CancellationToken ct = default) => Set.CountAsync(a => a.TripId == tripId, ct);
  public Task<List<Alert>> ListDueForRepeatAsync(DateTime now, int take, CancellationToken ct = default) =>
    Set.Include(a => a.Trip).Include(a => a.User)
      .Where(a => (a.Status == AlertStatus.Pending || a.Status == AlertStatus.Sent) && a.NextRepeatAt != null && a.NextRepeatAt <= now)
      .OrderBy(a => a.NextRepeatAt).Take(take).ToListAsync(ct);
}

public class AlertDeliveryRepository : GenericRepository<AlertDelivery>, IAlertDeliveryRepository
{
  public AlertDeliveryRepository(AppDbContext db) : base(db) { }
}

public class UnitOfWork : IUnitOfWork
{
  private readonly AppDbContext _db;
  public UnitOfWork(AppDbContext db)
  {
    _db = db;
    Users = new UserRepository(db);
    EmergencyContacts = new EmergencyContactRepository(db);
    Trips = new TripRepository(db);
    TripPoints = new TripPointRepository(db);
    Memories = new MemoryRepository(db);
    Alerts = new AlertRepository(db);
    AlertDeliveries = new AlertDeliveryRepository(db);
  }
  public IUserRepository Users { get; }
  public IEmergencyContactRepository EmergencyContacts { get; }
  public ITripRepository Trips { get; }
  public ITripPointRepository TripPoints { get; }
  public IMemoryRepository Memories { get; }
  public IAlertRepository Alerts { get; }
  public IAlertDeliveryRepository AlertDeliveries { get; }

  public Task<int> SaveChangesAsync(CancellationToken ct = default) => _db.SaveChangesAsync(ct);

  public async Task ExecuteInTransactionAsync(Func<CancellationToken, Task> action, CancellationToken ct = default)
  {
    await using var tx = await _db.Database.BeginTransactionAsync(ct);
    try { await action(ct); await tx.CommitAsync(ct); }
    catch { await tx.RollbackAsync(ct); throw; }
  }
  public void Dispose() { }   // DbContext do DI quản lý
}
