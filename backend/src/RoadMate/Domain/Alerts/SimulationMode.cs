using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public enum SimulationMode
{
  [WireValue("dryRun")] DryRun,
  [WireValue("live")] Live
}
