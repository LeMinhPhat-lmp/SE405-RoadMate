using Microsoft.EntityFrameworkCore;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Alerts.Mappers;
using RoadMate.Domain.Alerts;

namespace RoadMate.Infrastructure.Persistence.Repositories;

public class AlertReadModel(AppDbContext db) : IAlertReadModel
{
  public async Task<IReadOnlyList<AlertResponse>> ListForTripAsync(Guid tripId, CancellationToken ct = default)
  {
    var alerts = await db.Alerts.AsNoTracking().Include(a => a.Deliveries).Include(a => a.Acks).AsSplitQuery()
      .Where(a => a.TripId == tripId).OrderByDescending(a => a.CreatedAt).ToListAsync(ct);
    return alerts.Select(a => a.ToDto(a.Deliveries.OrderBy(d => d.CreatedAt), a.Acks)).ToList();
  }
}
