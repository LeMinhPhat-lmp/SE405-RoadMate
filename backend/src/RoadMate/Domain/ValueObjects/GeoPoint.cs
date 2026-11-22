using RoadMate.Domain.Common;

namespace RoadMate.Domain.ValueObjects;

/// <summary>Validated WGS84 coordinate (finite, lat ∈ [−90,90], lng ∈ [−180,180]).</summary>
public readonly record struct GeoPoint
{
  private GeoPoint(double lat, double lng) { Lat = lat; Lng = lng; }
  public double Lat { get; }
  public double Lng { get; }

  public static Result<GeoPoint> Create(double lat, double lng) =>
    double.IsFinite(lat) && double.IsFinite(lng) && Geo.ValidLatLng(lat, lng) ? new GeoPoint(lat, lng) : Errors.InvalidLocation;

  /// <summary>For values already validated/persisted (entities rebuilding a point from stored columns).</summary>
  internal static GeoPoint FromTrusted(double lat, double lng) => new(lat, lng);

  public double DistanceMetersTo(GeoPoint other) => Geo.HaversineMeters(Lat, Lng, other.Lat, other.Lng);
}
