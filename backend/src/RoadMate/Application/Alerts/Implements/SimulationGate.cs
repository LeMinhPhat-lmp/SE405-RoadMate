using Microsoft.Extensions.Options;
using RoadMate.Application.Alerts.Interfaces;
using RoadMate.Domain.Alerts;
using RoadMate.Domain.ValueObjects;

namespace RoadMate.Application.Alerts.Implements;

public sealed class SimulationGate(IOptions<DebugOptions> debug) : ISimulationGate
{
  public bool AllowsSend(Alert alert, string phone) => alert.Simulation switch
  {
    null => true,
    SimulationMode.DryRun => false,
    _ => debug.Value.AllowedPhones.Select(PhoneNumber.Normalize).Contains(phone),
  };
}
