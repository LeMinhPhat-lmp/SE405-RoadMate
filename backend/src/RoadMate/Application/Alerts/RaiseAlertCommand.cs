using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts;

public sealed record RaiseAlertCommand(Guid TripId, Guid UserId, ClientRequestId ClientId, AlertType Type, GeoPoint At, int? Battery, SimulationMode? Simulation);
