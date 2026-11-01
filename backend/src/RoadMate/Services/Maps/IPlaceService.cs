namespace RoadMate.Services.Maps;

public interface IPlaceService
{
  Task<Result<List<PlacePrediction>>> AutocompleteAsync(string q, double? lat, double? lng, CancellationToken ct);
  Task<Result<PlaceDetail>> DetailAsync(string placeId, CancellationToken ct);
  Task<Result<ReverseResult>> ReverseAsync(double lat, double lng, CancellationToken ct);
}
