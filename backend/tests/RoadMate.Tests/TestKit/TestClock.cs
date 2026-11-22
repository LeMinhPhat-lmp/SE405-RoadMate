using System;

namespace RoadMate.Tests.TestKit;

/// <summary>Deterministic TimeProvider (own implementation: no extra package needed).</summary>
public sealed class TestClock : TimeProvider
{
  private DateTimeOffset _now;
  public TestClock(DateTimeOffset? start = null) => _now = start ?? new DateTimeOffset(2026, 7, 1, 8, 0, 0, TimeSpan.Zero);
  public override DateTimeOffset GetUtcNow() => _now;
  public void Advance(TimeSpan by) => _now += by;
  public void Set(DateTimeOffset to) => _now = to;
  public DateTimeOffset Now => _now;
}
