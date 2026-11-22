using Microsoft.Extensions.Options;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Interfaces;

/// <summary>Decides whether a REAL notification may go to a phone for this alert (simulator: dry-run / allow-list).</summary>
public interface ISimulationGate
{
  bool AllowsSend(Alert alert, string phone);
}
