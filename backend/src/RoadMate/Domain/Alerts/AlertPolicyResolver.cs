using RoadMate.Domain.Common;

namespace RoadMate.Domain.Alerts;

public sealed class AlertPolicyResolver : IAlertPolicyResolver
{
  private readonly Dictionary<AlertType, IAlertTypePolicy> _map;

  public AlertPolicyResolver(IEnumerable<IAlertTypePolicy> policies)
  {
    var list = policies.ToList();
    var dup = list.GroupBy(p => p.Type).FirstOrDefault(g => g.Count() > 1);
    if (dup is not null) throw new InvalidOperationException($"Có nhiều hơn một policy cho loại cảnh báo '{dup.Key}'.");
    _map = list.ToDictionary(p => p.Type);
    var missing = EnumWire.Values<AlertType>().FirstOrDefault(t => !_map.ContainsKey(t));
    if (_map.Count != EnumWire.Values<AlertType>().Count) throw new InvalidOperationException($"Thiếu policy cho loại cảnh báo '{missing}'.");
  }

  public IAlertTypePolicy For(AlertType type) => _map[type];

  public static AlertPolicyResolver CreateDefault() => new(DefaultPolicies());

  public static IEnumerable<IAlertTypePolicy> DefaultPolicies() =>
    [new CrashPolicy(), new SosPolicy(), new IdlePolicy(), new LostSignalPolicy(), new LateReturnPolicy(), new LowBatteryPolicy(), new OffRoutePolicy()];
}
