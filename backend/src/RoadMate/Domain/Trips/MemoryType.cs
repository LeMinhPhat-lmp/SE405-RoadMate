using RoadMate.Domain.Common;

namespace RoadMate.Domain.Trips;

public enum MemoryType { [WireValue("photo")] Photo, [WireValue("note")] Note, [WireValue("video")] Video }
