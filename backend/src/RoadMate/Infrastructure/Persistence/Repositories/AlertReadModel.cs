using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Alerts.Mappers;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class AlertReadModel(AppDbContext db) : IAlertReadModel
{
  public async Task<IReadOnlyList<AlertResponse>> ListForTripAsync(Guid tripId, CancellationToken ct = default)
  {
    var alerts = await db.Alerts.AsNoTracking().Include(a => a.Deliveries).Include(a => a.Acks).AsSplitQuery()
      .Where(a => a.TripId == tripId).OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).ToListAsync(ct);
    return alerts.Select(a => a.ToDto(a.Deliveries.OrderBy(d => d.CreatedAt), a.Acks)).ToList();
  }

  public async Task<CursorPage<AlertResponse>> ListForTripPageAsync(Guid tripId, CursorPosition? after, int limit, CancellationToken ct = default)
  {
    var take = CursorPagination.NormalizeLimit(limit);
    IQueryable<Alert> q = db.Alerts.AsNoTracking()
      .Include(a => a.Deliveries).Include(a => a.Acks).AsSplitQuery()
      .Where(a => a.TripId == tripId);

    if (after is not null)
    {
      var afterAt = after.SortAt;
      var afterId = after.Id;
      q = q.Where(a => a.CreatedAt < afterAt || (a.CreatedAt == afterAt && a.Id.CompareTo(afterId) < 0));
    }

    var rows = await q.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.Id).Take(take + 1).ToListAsync(ct);
    var page = CursorPagination.ToPage(rows, take, a => new CursorPosition(a.CreatedAt, a.Id));
    var items = page.Items.Select(a => a.ToDto(a.Deliveries.OrderBy(d => d.CreatedAt), a.Acks)).ToList();
    return new CursorPage<AlertResponse>(items, page.NextCursor, page.HasMore);
  }
}
