namespace RoadMate.Common;

public static class Geo
{
  public static double HaversineMeters(double lat1, double lng1, double lat2, double lng2)
  {
    const double R = 6371000;
    double dLat = double.DegreesToRadians(lat2 - lat1), dLng = double.DegreesToRadians(lng2 - lng1);
    double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
      Math.Cos(double.DegreesToRadians(lat1)) * Math.Cos(double.DegreesToRadians(lat2)) * Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
    return 2 * R * Math.Asin(Math.Sqrt(a));
  }

  public static bool ValidLatLng(double lat, double lng) => lat is >= -90 and <= 90 && lng is >= -180 and <= 180;
}
