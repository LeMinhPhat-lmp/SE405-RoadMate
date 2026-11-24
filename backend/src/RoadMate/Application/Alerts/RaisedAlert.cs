using RoadMate.Application.Abstractions.Persistence;
using RoadMate.Application.Common;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.Common;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts;

public sealed record RaisedAlert(Alert Alert, bool Created, PlanResult Plan);
