namespace RoadMate.Application.Maps;

/// <summary>Provider-neutral map behaviour (bound to the same "Goong" configuration section as the adapter's own <c>GoongOptions</c>).</summary>
public class MapOptions
{
  /// <summary>true: ask the provider for the pre-merger administrative units too.</summary>
  public bool IncludeDeprecatedAdministrativeUnit { get; set; }
  /// <summary>Provider vehicle value for motorbikes (Directions V2 lists "bike", Distance Matrix also "motorcycle").</summary>
  public string MotorbikeVehicle { get; set; } = "bike";
  public string CarVehicle { get; set; } = "car";
  /// <summary>Max parallel provider requests (multi-leg routes / place details).</summary>
  public int MaxParallelRequests { get; set; } = 4;
  public int AutocompleteCacheSeconds { get; set; } = 120;
  public int DetailCacheMinutes { get; set; } = 60;
  public int ReverseCacheMinutes { get; set; } = 10;
  public int NearbyCacheMinutes { get; set; } = 5;
}
