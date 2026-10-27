namespace RoadMate.Services.Interfaces;

public interface IMapService
{
  Task<Result<DirectionsResponse>> DirectionsAsync(DirectionsRequest req, CancellationToken ct);
  Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, CancellationToken ct);
  Task<Result<PlaceDetail>> PlaceDetailAsync(string placeId, CancellationToken ct);
  Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct);
}
