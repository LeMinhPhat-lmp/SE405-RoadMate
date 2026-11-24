using RoadMate.Domain.Common;

namespace RoadMate.Domain.Trips;

public enum TripStatus { [WireValue("draft")] Draft, [WireValue("active")] Active, [WireValue("finished")] Finished }
