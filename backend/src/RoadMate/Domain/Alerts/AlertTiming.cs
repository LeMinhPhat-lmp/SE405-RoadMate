namespace RoadMate.Domain.Alerts;

/// <summary>Repeat intervals (replaces passing AlertOptions into the domain).</summary>
public sealed record AlertTiming(TimeSpan Fast, TimeSpan Slow);
