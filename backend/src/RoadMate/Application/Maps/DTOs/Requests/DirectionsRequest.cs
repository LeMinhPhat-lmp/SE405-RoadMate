using RoadMate.Application.Shared;

namespace RoadMate.Application.Maps.DTOs.Requests;

// ---------- Routing
/// <param name="Vehicle">"motorbike" (mặc định) | "car". Chấp nhận thêm "motorcycle"/"bike" như đồng nghĩa của motorbike.</param>
public record DirectionsRequest(LatLng Origin, LatLng Destination, List<LatLng>? Waypoints, bool AvoidHighway, bool AvoidToll, string? Vehicle);
