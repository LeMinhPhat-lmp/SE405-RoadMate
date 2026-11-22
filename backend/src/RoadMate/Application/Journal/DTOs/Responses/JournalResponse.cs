using RoadMate.Application.Trips;
using RoadMate.Application.Trips.DTOs.Responses;

namespace RoadMate.Application.Journal.DTOs.Responses;

public record JournalResponse(Guid TripId, string Title, string Status, TripStatsResponse? Stats, List<WaypointResponse> Stops, List<JournalDayResponse> Days);
