namespace RoadMate.Dtos.Maps;

// ---------- Routing
/// <param name="Vehicle">"motorbike" (mặc định) | "car". Chấp nhận thêm "motorcycle"/"bike" như đồng nghĩa của motorbike.</param>
public record DirectionsRequest(LatLng Origin, LatLng Destination, List<LatLng>? Waypoints, bool AvoidHighway, bool AvoidToll, string? Vehicle);
/// <param name="Lat">Vị trí bắt đầu bước (điểm cần rẽ) – app dùng để phát giọng nói khi đến gần.</param>
public record StepDto(string Instruction, string? Maneuver, int DistanceM, int DurationS, double? Lat = null, double? Lng = null);
public record LegDto(double DistanceKm, int DurationMin, string? StartAddress, string? EndAddress);
public record BoundsDto(LatLng Northeast, LatLng Southwest);
public record RouteWarningDto(string Code, string Message);
public record DirectionsResponse(string Polyline, double DistanceKm, int EtaMinutes, List<StepDto> Steps,
  List<LegDto> Legs, BoundsDto? Bounds, List<RouteWarningDto> Warnings);

public record DistanceMatrixRequest(List<LatLng> Origins, List<LatLng> Destinations, string? Vehicle);
/// <param name="Status">OK | NOT_FOUND | ZERO_RESULTS (theo Goong). DistanceM/DurationS chỉ có khi OK.</param>
public record MatrixElementDto(string Status, int? DistanceM, int? DurationS);
public record DistanceMatrixResponse(List<List<MatrixElementDto>> Rows);

// ---------- Places
public record PlacePrediction(string PlaceId, string Description, string? MainText, string? SecondaryText,
  bool HasChildren = false, List<string>? Types = null, string? Commune = null, string? Province = null,
  double? DistanceMeters = null, string? DeprecatedDescription = null);
public record PlaceDetail(string PlaceId, string Name, string? Address, double Lat, double Lng,
  string? Commune = null, string? Province = null, List<string>? Types = null, string? Url = null, string? DeprecatedAddress = null);
public record NearbyPlace(string PlaceId, string Name, string? Address, double Lat, double Lng, int DistanceM, List<string>? Types = null);

// ---------- Geocoding
public record ReverseResult(string? PlaceId, string Address, string? Name = null, double? Lat = null, double? Lng = null,
  string? Commune = null, string? Province = null, List<string>? Types = null, string? DeprecatedAddress = null);
public record GeocodeResult(string PlaceId, string Name, string Address, double Lat, double Lng,
  string? Commune = null, string? Province = null, List<string>? Types = null, string? DeprecatedAddress = null);
/// <param name="Boundary">Polyline mã hoá ranh giới của đường (nếu Goong có).</param>
public record StreetResult(string Name, string Address, double Lat, double Lng, string? Commune, string? Province, string? PlaceId, string? Boundary);

// ---------- Trip route
/// <param name="From">Có = tính lại từ vị trí hiện tại (chỉ chuyến đang chạy), vd khi lệch tuyến.</param>
/// <param name="PassedWaypoints">Số điểm dừng đã đi qua (bỏ qua khi tính lại). Mặc định 0.</param>
public record RefreshRouteRequest(LatLng? From, int? PassedWaypoints, string? Vehicle);
public record TripRouteResponse(Guid TripId, bool Rerouted, DirectionsResponse Route);
