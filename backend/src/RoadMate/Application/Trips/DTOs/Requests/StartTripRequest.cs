namespace RoadMate.Application.Trips.DTOs.Requests;

public record StartTripRequest(DateTime? ExpectedReturnTime, List<Guid>? AlertContactIds, bool? LiveShare);
