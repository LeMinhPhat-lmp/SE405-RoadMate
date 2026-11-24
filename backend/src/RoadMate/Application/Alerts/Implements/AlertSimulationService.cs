using Microsoft.Extensions.Options;
using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Alerts.DTOs.Requests;
using RoadMate.Application.Alerts.DTOs.Responses;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Application.Alerts.Mappers;
using RoadMate.Application.Trips;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Implements;

/// <summary>Demo helper. dryRun (default) sends nothing; dryRun=false sends really only to Debug:AllowedPhones.</summary>
public class AlertSimulationService(ITripRepository trips, IAlertRaiser raiser, IOptions<DebugOptions> debug, TimeProvider clock) : IAlertSimulationService
{
  public static readonly Error Disabled = Error.NotFound("SIMULATOR_DISABLED", "Trình giả lập đang tắt.");

  public async Task<Result<AlertResponse>> SimulateAsync(Guid userId, SimulateAlertRequest req, CancellationToken ct)
  {
    if (!debug.Value.EnableSimulator) return Disabled;
    var type = AlertType.Crash;
    if (req.Type is not null && !EnumWire.TryParse(req.Type, out type))
      return Error.Validation("INVALID_TYPE", $"type phải thuộc: {EnumWire.JoinWire<AlertType>()}.");
    var at = GeoPoint.Create(req.Lat ?? 10.7769, req.Lng ?? 106.7009);   // default: Ho Chi Minh City square
    if (at.IsFailure) return at.Error;
    var trip = await trips.GetOwnedActiveAsync(req.TripId, userId, false, ct);
    if (trip.IsFailure) return trip.Error;

    var mode = req.DryRun ?? true ? SimulationMode.DryRun : SimulationMode.Live;
    var cmd = new RaiseAlertCommand(req.TripId, userId, ClientRequestId.Create($"sim-{Guid.NewGuid():N}").Value, type, at.Value, 80, mode);
    var raised = await raiser.RaiseAsync(cmd, clock.GetUtcNow(), ct);
    return raised.IsFailure ? raised.Error : raised.Value.Alert.ToDto(raised.Value.Plan.Deliveries, raised.Value.Plan.Acks);
  }
}
