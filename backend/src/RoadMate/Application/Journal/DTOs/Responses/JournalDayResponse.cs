using RoadMate.Application.Trips;

namespace RoadMate.Application.Journal.DTOs.Responses;

public record JournalDayResponse(DateOnly Date, List<MemoryResponse> Entries);
