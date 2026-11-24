using RoadMate.Application.Shared;

namespace RoadMate.Application.Tracking.DTOs.Responses;

public record PublicTrackResponse(string OwnerName, string Title, string Status, LocationResponse Destination, PublicLiveResponse? Live, List<PublicAlertResponse> OpenAlerts);
