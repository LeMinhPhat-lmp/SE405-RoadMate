using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Identity;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Abstractions;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Groups;
using RoadMate.Domain.Trips;
using RoadMate.Domain.Users;
using RoadMate.Infrastructure.Persistence;
using RoadMate.Infrastructure.Persistence.Repositories;
using RoadMate.Tests.TestKit;

namespace RoadMate.Tests.Application;

public class RepositoryArchitectureTests
{
  private sealed class StubCurrentUser(Guid? id) : ICurrentUser
  {
    public Guid? UserId { get; } = id;
    public string? FirebaseUid => null;
  }

  private static AppDbContext CreateContext(Guid? actorId = null, TestClock? clock = null)
  {
    var opts = new DbContextOptionsBuilder<AppDbContext>()
      .UseNpgsql("Host=localhost;Database=roadmate_model_test")
      .Options;
    return new AppDbContext(opts, new StubCurrentUser(actorId), clock ?? new TestClock());
  }

  private static void SetCreatedAt(AuditableEntity entity, DateTimeOffset createdAt) =>
    typeof(AuditableEntity).GetProperty(nameof(AuditableEntity.CreatedAt))!
      .SetValue(entity, createdAt);

  private static void SetId(Entity entity, Guid id) =>
    typeof(Entity).GetProperty(nameof(Entity.Id))!
      .SetValue(entity, id);

  [Fact]
  public void Feature_repository_contracts_inherit_only_applicable_base_contracts()
  {
    Assert.True(typeof(IGenericRepository<User>).IsAssignableFrom(typeof(IUserRepository)));
    Assert.True(typeof(ISoftDeleteRepository<User>).IsAssignableFrom(typeof(IUserRepository)));

    Assert.True(typeof(IGenericRepository<EmergencyContact>).IsAssignableFrom(typeof(IEmergencyContactRepository)));
    Assert.True(typeof(ISoftDeleteRepository<EmergencyContact>).IsAssignableFrom(typeof(IEmergencyContactRepository)));

    Assert.True(typeof(IGenericRepository<Trip>).IsAssignableFrom(typeof(ITripRepository)));
    Assert.True(typeof(ISoftDeleteRepository<Trip>).IsAssignableFrom(typeof(ITripRepository)));

    Assert.True(typeof(IGenericRepository<Memory>).IsAssignableFrom(typeof(IMemoryRepository)));
    Assert.True(typeof(ISoftDeleteRepository<Memory>).IsAssignableFrom(typeof(IMemoryRepository)));

    Assert.True(typeof(IGenericRepository<Group>).IsAssignableFrom(typeof(IGroupRepository)));
    Assert.True(typeof(ISoftDeleteRepository<Group>).IsAssignableFrom(typeof(IGroupRepository)));

    // Non-soft-deletable entities must NOT inherit ISoftDeleteRepository
    Assert.True(typeof(IGenericRepository<Alert>).IsAssignableFrom(typeof(IAlertRepository)));
    Assert.False(typeof(IAlertRepository).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISoftDeleteRepository<>)));

    Assert.True(typeof(IGenericRepository<AlertAck>).IsAssignableFrom(typeof(IAlertAckRepository)));
    Assert.False(typeof(IAlertAckRepository).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISoftDeleteRepository<>)));

    Assert.True(typeof(IGenericRepository<NotificationDelivery>).IsAssignableFrom(typeof(INotificationDeliveryRepository)));
    Assert.False(typeof(INotificationDeliveryRepository).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISoftDeleteRepository<>)));

    Assert.True(typeof(IGenericRepository<GroupMessage>).IsAssignableFrom(typeof(IGroupMessageRepository)));
    Assert.False(typeof(IGroupMessageRepository).GetInterfaces().Any(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(ISoftDeleteRepository<>)));
  }

  [Fact]
  public void SoftDelete_and_Remove_are_distinct_in_ef_core_change_tracker()
  {
    var actor = Guid.NewGuid();
    var clock = new TestClock();
    using var db = CreateContext(actor, clock);
    var repo = new TripRepository(db, new StubCurrentUser(actor), clock);

    var soft = Make.DraftTrip(actor);
    var softBatchA = Make.DraftTrip(actor);
    var softBatchB = Make.DraftTrip(actor);
    var hard = Make.DraftTrip(actor);

    repo.Delete(soft);
    Assert.True(soft.IsDeleted);
    Assert.Equal(actor, soft.DeletedBy);
    Assert.Equal(clock.Now, soft.DeletedAt);
    Assert.Equal(EntityState.Modified, db.Entry(soft).State);

    repo.DeleteRange([softBatchA, softBatchB]);
    Assert.True(softBatchA.IsDeleted && softBatchB.IsDeleted);
    Assert.Equal(EntityState.Modified, db.Entry(softBatchA).State);
    Assert.Equal(EntityState.Modified, db.Entry(softBatchB).State);

    repo.Remove(hard);
    Assert.False(hard.IsDeleted);
    Assert.Equal(EntityState.Deleted, db.Entry(hard).State);
  }

  [Fact]
  public void Child_entity_added_to_tracked_aggregate_is_tracked_as_Added()
  {
    using var db = CreateContext();
    var user = Make.User();
    db.Users.Attach(user);
    Assert.Equal(EntityState.Unchanged, db.Entry(user).State);

    var added = user.AddContact("Mẹ", Make.Phone("0912345678"), "mẹ", null, null).ThrowIfFailed().Value;
    db.ChangeTracker.DetectChanges();

    Assert.Equal(EntityState.Added, db.Entry(added).State);
  }

  [Fact]
  public void Ef_model_has_soft_delete_filters_and_keyset_pagination_indexes()
  {
    using var db = CreateContext();
    var model = db.Model;

    // Global soft-delete query filters on ISoftDeletableEntity types, absent on Alert
    foreach (var t in new[] { typeof(User), typeof(EmergencyContact), typeof(Trip), typeof(Memory), typeof(Group) })
      Assert.NotEmpty(model.FindEntityType(t)!.GetDeclaredQueryFilters());
    Assert.Empty(model.FindEntityType(typeof(Alert))!.GetDeclaredQueryFilters());

    static bool HasIndex(Microsoft.EntityFrameworkCore.Metadata.IEntityType et, params string[] cols) =>
      et.GetIndexes().Any(ix => ix.Properties.Select(p => p.Name).SequenceEqual(cols));

    Assert.True(HasIndex(model.FindEntityType(typeof(Trip))!, nameof(Trip.OwnerId), nameof(Trip.CreatedAt), nameof(Trip.Id)));
    Assert.True(HasIndex(model.FindEntityType(typeof(Trip))!, nameof(Trip.OwnerId), nameof(Trip.Status), nameof(Trip.CreatedAt), nameof(Trip.Id)));
    Assert.True(HasIndex(model.FindEntityType(typeof(Memory))!, nameof(Memory.TripId), nameof(Memory.TakenAt), nameof(Memory.Id)));
    Assert.True(HasIndex(model.FindEntityType(typeof(Alert))!, nameof(Alert.TripId), nameof(Alert.CreatedAt), nameof(Alert.Id)));
    Assert.True(HasIndex(model.FindEntityType(typeof(GroupMessage))!, nameof(GroupMessage.GroupId), nameof(GroupMessage.CreatedAt), nameof(GroupMessage.Id)));

    // Client-generated Guid PKs must use ValueGenerated.Never so child entities added to tracked aggregates are tracked as Added (not Modified)
    foreach (var et in model.GetEntityTypes().Where(t => typeof(Entity).IsAssignableFrom(t.ClrType)))
      Assert.Equal(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, et.FindProperty(nameof(Entity.Id))!.ValueGenerated);

    // Verify Npgsql translates keyset predicate (including Guid.CompareTo tie-breaker) without OFFSET
    var ownerId = Guid.NewGuid();
    var afterAt = Make.Now;
    var afterId = Guid.NewGuid();
    var sql = db.Trips.AsNoTracking()
      .Where(t => t.OwnerId == ownerId && (t.CreatedAt < afterAt || (t.CreatedAt == afterAt && t.Id.CompareTo(afterId) < 0)))
      .OrderByDescending(t => t.CreatedAt).ThenByDescending(t => t.Id)
      .Take(21)
      .ToQueryString();
    Assert.Contains("\"DeletedAt\" IS NULL", sql);
    Assert.Contains("ORDER BY", sql);
    Assert.Contains("LIMIT", sql);
    Assert.DoesNotContain("OFFSET", sql);
  }

  [Fact]
  public void CursorPosition_encode_decode_and_limit_normalization()
  {
    var pos = new CursorPosition(Make.Now, Guid.NewGuid());
    var token = pos.Encode();
    Assert.True(CursorPosition.TryDecode(token, out var decoded));
    Assert.Equal(pos, decoded);

    Assert.True(CursorPosition.TryDecode(null, out var empty));
    Assert.Null(empty);
    Assert.True(CursorPosition.TryDecode("   ", out var blank));
    Assert.Null(blank);

    Assert.False(CursorPosition.TryDecode("not-a-valid-cursor!!!", out _));
    Assert.False(CursorPosition.TryDecode(Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes("bad:guid")), out _));

    Assert.Equal(CursorPagination.DefaultLimit, CursorPagination.NormalizeLimit(null));
    Assert.Equal(1, CursorPagination.NormalizeLimit(0));
    Assert.Equal(1, CursorPagination.NormalizeLimit(-10));
    Assert.Equal(25, CursorPagination.NormalizeLimit(25));
    Assert.Equal(CursorPagination.MaxLimit, CursorPagination.NormalizeLimit(500));
  }

  [Fact]
  public async Task Trips_cursor_pagination_handles_tie_breaks_filters_soft_delete_and_boundaries()
  {
    var w = new World();
    var owner = w.AddUser();
    var other = w.AddUser("Other");
    var svc = w.Get<ITripService>();

    // Empty page
    var empty = (await svc.ListPageAsync(owner.Id, null, null, 10, default)).ThrowIfFailed();
    Assert.Empty(empty.Value.Items);
    Assert.False(empty.Value.HasMore);
    Assert.Null(empty.Value.NextCursor);

    // Invalid cursor rejected
    (await svc.ListPageAsync(owner.Id, null, "!!!bad", 10, default)).ShouldFailWith("INVALID_CURSOR");

    // 5 owner trips: 3 share the exact same CreatedAt (tie-break by Id desc), 1 is soft-deleted, 1 belongs to another user
    var tSame = Make.Now;
    var ids = new[]
    {
      Guid.Parse("00000000-0000-0000-0000-000000000001"),
      Guid.Parse("00000000-0000-0000-0000-000000000002"),
      Guid.Parse("00000000-0000-0000-0000-000000000003"),
      Guid.Parse("00000000-0000-0000-0000-000000000004"),
      Guid.Parse("00000000-0000-0000-0000-000000000005")
    };

    Trip AddTrip(Guid userId, Guid id, DateTimeOffset createdAt, bool active = false, bool deleted = false)
    {
      var t = active ? Make.ActiveTrip(userId) : Make.DraftTrip(userId);
      SetId(t, id);
      SetCreatedAt(t, createdAt);
      if (deleted) t.SoftDelete(userId, Make.Now);
      w.Trips.Add(t);
      return t;
    }

    AddTrip(owner.Id, ids[0], tSame.AddMinutes(-10));
    AddTrip(owner.Id, ids[1], tSame);                      // tie-break 1
    AddTrip(owner.Id, ids[2], tSame);                      // tie-break 2
    AddTrip(owner.Id, ids[3], tSame);                      // tie-break 3 (highest Id at tSame)
    AddTrip(owner.Id, ids[4], tSame.AddMinutes(5), active: true);
    AddTrip(owner.Id, Guid.NewGuid(), tSame, deleted: true);
    AddTrip(other.Id, Guid.NewGuid(), tSame.AddMinutes(10));

    // Walk all 5 non-deleted owner trips with page size 2 (expected desc order: ids[4], ids[3], ids[2], ids[1], ids[0])
    var p1 = (await svc.ListPageAsync(owner.Id, null, null, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[4], ids[3]], p1.Items.Select(x => x.Id));
    Assert.True(p1.HasMore);
    Assert.NotNull(p1.NextCursor);

    var p2 = (await svc.ListPageAsync(owner.Id, null, p1.NextCursor, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[2], ids[1]], p2.Items.Select(x => x.Id));
    Assert.True(p2.HasMore);
    Assert.NotNull(p2.NextCursor);

    var p3 = (await svc.ListPageAsync(owner.Id, null, p2.NextCursor, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[0]], p3.Items.Select(x => x.Id));
    Assert.False(p3.HasMore);
    Assert.Null(p3.NextCursor);

    // Status filter consistent across pages (only draft trips: ids[3], ids[2], ids[1], ids[0])
    var d1 = (await svc.ListPageAsync(owner.Id, "draft", null, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[3], ids[2]], d1.Items.Select(x => x.Id));
    Assert.True(d1.HasMore);

    var d2 = (await svc.ListPageAsync(owner.Id, "draft", d1.NextCursor, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[1], ids[0]], d2.Items.Select(x => x.Id));
    Assert.False(d2.HasMore);
    Assert.Null(d2.NextCursor);

    // Invalid limits clamped: <= 0 becomes 1
    var clamped = (await svc.ListPageAsync(owner.Id, null, null, 0, default)).ThrowIfFailed().Value;
    Assert.Single(clamped.Items);
    Assert.True(clamped.HasMore);

    // Unknown status returns empty page
    var unknown = (await svc.ListPageAsync(owner.Id, "bogus", null, 10, default)).ThrowIfFailed().Value;
    Assert.Empty(unknown.Items);
    Assert.False(unknown.HasMore);
  }

  [Fact]
  public async Task Memories_cursor_pagination_orders_ascending_with_tie_breaks_and_excludes_deleted()
  {
    var w = new World();
    var owner = w.AddUser();
    var trip = w.AddActiveTrip(owner);
    var svc = w.Get<IMemoryService>();

    var t0 = Make.Now;
    var ids = new[]
    {
      Guid.Parse("00000000-0000-0000-0000-000000000010"),
      Guid.Parse("00000000-0000-0000-0000-000000000020"),
      Guid.Parse("00000000-0000-0000-0000-000000000030")
    };

    void AddMem(Guid id, DateTimeOffset takenAt, bool deleted = false)
    {
      var m = Memory.Create(trip.Id, new MemoryDraft(MemoryType.Note, null, "Note", null, null, null, null, null, takenAt), takenAt).Value;
      SetId(m, id);
      if (deleted) m.SoftDelete(owner.Id, t0);
      w.Memories.Add(m);
    }

    AddMem(ids[1], t0);
    AddMem(ids[0], t0);                     // same TakenAt, smaller Id comes first
    AddMem(ids[2], t0.AddMinutes(5));
    AddMem(Guid.NewGuid(), t0, deleted: true);

    var p1 = (await svc.ListPageAsync(owner.Id, trip.Id, null, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[0], ids[1]], p1.Items.Select(x => x.Id));
    Assert.True(p1.HasMore);

    var p2 = (await svc.ListPageAsync(owner.Id, trip.Id, p1.NextCursor, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([ids[2]], p2.Items.Select(x => x.Id));
    Assert.False(p2.HasMore);
    Assert.Null(p2.NextCursor);

    (await svc.ListPageAsync(owner.Id, trip.Id, "bad-cursor", 2, default)).ShouldFailWith("INVALID_CURSOR");
    (await svc.ListPageAsync(Guid.NewGuid(), trip.Id, null, 2, default)).ShouldFailWith("TRIP_NOT_FOUND");
  }

  [Fact]
  public async Task Alerts_and_group_messages_cursor_pagination_handle_tie_breaks_and_authorization()
  {
    var w = new World();
    var owner = w.AddUser();
    var trip = w.AddActiveTrip(owner);
    var alertSvc = w.Get<IAlertService>();

    var t0 = Make.Now;
    var a1 = Make.Alert(AlertType.Sos, trip.Id, owner.Id, clientId: "a1");
    var a2 = Make.Alert(AlertType.Sos, trip.Id, owner.Id, clientId: "a2");
    var a3 = Make.Alert(AlertType.Sos, trip.Id, owner.Id, clientId: "a3");
    SetId(a1, Guid.Parse("00000000-0000-0000-0000-000000000101")); SetCreatedAt(a1, t0);
    SetId(a2, Guid.Parse("00000000-0000-0000-0000-000000000102")); SetCreatedAt(a2, t0);
    SetId(a3, Guid.Parse("00000000-0000-0000-0000-000000000103")); SetCreatedAt(a3, t0.AddMinutes(1));
    w.Alerts.AddRange([a1, a2, a3]);

    var ap1 = (await alertSvc.ListPageAsync(owner.Id, trip.Id, null, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([a3.Id, a2.Id], ap1.Items.Select(x => x.Id));
    Assert.True(ap1.HasMore);
    var ap2 = (await alertSvc.ListPageAsync(owner.Id, trip.Id, ap1.NextCursor, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([a1.Id], ap2.Items.Select(x => x.Id));
    Assert.False(ap2.HasMore);
    (await alertSvc.ListPageAsync(Guid.NewGuid(), trip.Id, null, 2, default)).ShouldFailWith("TRIP_NOT_FOUND");

    // Group messages ascending (CreatedAt, Id)
    var g = Make.Group(owner.Id);
    w.Groups.Add(g);
    var m1 = g.PostQuickMessage(owner.Id, "rest").Value;
    var m2 = g.PostQuickMessage(owner.Id, "police").Value;
    var m3 = g.PostQuickMessage(owner.Id, "need_help").Value;
    SetId(m1, Guid.Parse("00000000-0000-0000-0000-000000000201")); SetCreatedAt(m1, t0);
    SetId(m2, Guid.Parse("00000000-0000-0000-0000-000000000202")); SetCreatedAt(m2, t0);
    SetId(m3, Guid.Parse("00000000-0000-0000-0000-000000000203")); SetCreatedAt(m3, t0.AddSeconds(30));
    w.Messages.AddRange([m2, m1, m3]);

    var msgSvc = w.Get<IGroupMessageService>();
    var mp1 = (await msgSvc.ListMessagesPageAsync(owner.Id, g.Id, null, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([m1.Id, m2.Id], mp1.Items.Select(x => x.Id));
    Assert.True(mp1.HasMore);
    var mp2 = (await msgSvc.ListMessagesPageAsync(owner.Id, g.Id, mp1.NextCursor, 2, default)).ThrowIfFailed().Value;
    Assert.Equal([m3.Id], mp2.Items.Select(x => x.Id));
    Assert.False(mp2.HasMore);
    (await msgSvc.ListMessagesPageAsync(Guid.NewGuid(), g.Id, null, 2, default)).ShouldFailWith("GROUP_NOT_FOUND");
  }
}
