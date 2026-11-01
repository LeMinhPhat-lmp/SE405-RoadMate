namespace RoadMate.Services.Alerts;

public static class CreateAlertRequestValidator
{
  public static Result Validate(CreateAlertRequest req)
  {
    if (string.IsNullOrWhiteSpace(req.ClientId) || req.ClientId.Length > 100) return Error.Validation("INVALID_CLIENT_ID", "clientId bắt buộc (≤100 ký tự).");
    if (!AlertType.All.Contains(req.Type)) return Error.Validation("INVALID_TYPE", $"type phải thuộc: {string.Join(", ", AlertType.All)}.");
    if (!Geo.ValidLatLng(req.Lat, req.Lng)) return Errors.InvalidLocation;
    return Result.Success();
  }
}
