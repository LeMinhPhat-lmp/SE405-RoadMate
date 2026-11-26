part of '../../main.dart';

/// RM/36 – demo tools so the safety flows can be shown without falling off
/// a bike. Crash and idle simulations go through the real detection code.
class DebugSimulatorPage extends StatefulWidget {
  const DebugSimulatorPage({
    required this.api,
    required this.runtime,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime runtime;

  @override
  State<DebugSimulatorPage> createState() => _DebugSimulatorPageState();
}

class _DebugSimulatorPageState extends State<DebugSimulatorPage> {
  double speed = 45;

  TripRuntime get runtime => widget.runtime;

  void _snack(String text) =>
      ScaffoldMessenger.of(context).showSnackBar(SnackBar(content: Text(text)));

  bool _requireTrip() {
    if (runtime.isTracking) return true;
    _snack('Hãy bắt đầu một chuyến đi trước khi giả lập.');
    return false;
  }

  Future<void> _serverDryRun() async {
    final id = runtime.activeTripId;
    if (!_requireTrip() || id == null) return;
    try {
      final point = runtime.latestPoint;
      await widget.api.simulateAlert(
        id,
        type: 'crash',
        lat: point?.latitude,
        lng: point?.longitude,
      );
      runtime.log('server simulate-alert (dry-run) created');
      _snack('Đã tạo cảnh báo giả lập phía server (không gửi SMS thật).');
    } on Object catch (exception) {
      _snack('$exception');
    }
    setState(() {});
  }

  Future<void> _lowBattery() async {
    final id = runtime.activeTripId;
    if (!_requireTrip() || id == null) return;
    await NotificationService.show(
      1004,
      'Pin yếu · 12%',
      'Bật tiết kiệm pin. RoadMate đã gửi vị trí cuối cho người thân.',
    );
    final point = runtime.latestPoint ?? defaultMapCenter;
    try {
      await widget.api.createAlert(
        id,
        clientId: 'lowBattery-${DateTime.now().millisecondsSinceEpoch}',
        type: 'lowBattery',
        lat: point.latitude,
        lng: point.longitude,
        battery: 12,
      );
      runtime.log('simulate low battery 12% → alert sent');
    } on Object catch (exception) {
      runtime.log('low battery alert failed: $exception');
    }
    setState(() {});
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: runtime,
    builder: (context, _) {
      final simulating = runtime.simulatedSpeedKmh != null;
      return Scaffold(
        appBar: AppBar(
          title: const Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Text('Debug Simulator'),
              Text(
                'Chỉ dùng để demo',
                style: TextStyle(fontSize: 13, color: RoadMateColors.muted),
              ),
            ],
          ),
        ),
        body: ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Card(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  children: [
                    Row(
                      children: [
                        const Expanded(
                          child: Text(
                            'Giả lập tốc độ GPS',
                            style: TextStyle(fontWeight: FontWeight.w700),
                          ),
                        ),
                        Text('${speed.round()} km/h'),
                      ],
                    ),
                    Slider(
                      value: speed,
                      max: 120,
                      divisions: 24,
                      onChanged: (value) {
                        setState(() => speed = value);
                        if (simulating) runtime.setSimulatedSpeed(value);
                      },
                    ),
                    SwitchListTile(
                      contentPadding: EdgeInsets.zero,
                      title: const Text('Dùng tốc độ giả lập'),
                      subtitle: Text(
                        runtime.isTracking
                            ? 'Chuyến đang chạy'
                            : 'Chưa có chuyến đang chạy',
                      ),
                      value: simulating,
                      onChanged: (value) =>
                          runtime.setSimulatedSpeed(value ? speed : null),
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 8),
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 10,
              crossAxisSpacing: 10,
              childAspectRatio: 1.45,
              children: [
                _SimulatorTile(
                  icon: Icons.car_crash_outlined,
                  color: RoadMateColors.danger,
                  title: 'Giả lập va chạm',
                  subtitle: '4.2g + dừng đột ngột',
                  onTap: () {
                    if (_requireTrip()) runtime.simulateCrash();
                  },
                ),
                _SimulatorTile(
                  icon: Icons.timer_outlined,
                  color: RoadMateColors.amber,
                  title: 'Giả lập dừng lâu',
                  subtitle: 'Bỏ qua ${runtime.idleDetector.idleMinutes} phút',
                  onTap: () {
                    if (_requireTrip()) runtime.simulateIdle();
                  },
                ),
                _SimulatorTile(
                  icon: Icons.signal_cellular_off,
                  color: RoadMateColors.sky,
                  title: runtime.simulatedOffline
                      ? 'Có mạng lại'
                      : 'Giả lập mất sóng',
                  subtitle: runtime.simulatedOffline
                      ? '${runtime.pendingPoints} điểm chờ đồng bộ'
                      : 'Ngắt đồng bộ 10 phút',
                  onTap: () {
                    if (!_requireTrip()) return;
                    runtime.simulatedOffline
                        ? runtime.clearSimulatedOffline()
                        : runtime.simulateOffline(const Duration(minutes: 10));
                  },
                ),
                _SimulatorTile(
                  icon: Icons.battery_alert,
                  color: RoadMateColors.violet,
                  title: 'Giả lập pin yếu',
                  subtitle: 'Đặt pin = 12%',
                  onTap: _lowBattery,
                ),
                _SimulatorTile(
                  icon: Icons.cloud_outlined,
                  color: RoadMateColors.primaryDeep,
                  title: 'Cảnh báo server',
                  subtitle: 'simulate-alert · dry-run',
                  onTap: _serverDryRun,
                ),
                _SimulatorTile(
                  icon: Icons.emergency_outlined,
                  color: RoadMateColors.coral,
                  title: 'Màn SOS',
                  subtitle: 'Giữ 3 giây',
                  onTap: () =>
                      Navigator.of(context).push(SosPage.route(runtime)),
                ),
              ],
            ),
            const SectionLabel('Log'),
            Card(
              color: RoadMateColors.ink,
              child: Padding(
                padding: const EdgeInsets.all(14),
                child: runtime.logs.isEmpty
                    ? const Text(
                        'Chưa có sự kiện.',
                        style: TextStyle(color: Colors.white70),
                      )
                    : Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          for (final line in runtime.logs.take(30))
                            Text(
                              line,
                              style: const TextStyle(
                                color: Colors.white,
                                fontFamily: 'monospace',
                                fontSize: 12,
                              ),
                            ),
                        ],
                      ),
              ),
            ),
          ],
        ),
      );
    },
  );
}

class _SimulatorTile extends StatelessWidget {
  const _SimulatorTile({
    required this.icon,
    required this.color,
    required this.title,
    required this.subtitle,
    required this.onTap,
  });
  final IconData icon;
  final Color color;
  final String title;
  final String subtitle;
  final VoidCallback onTap;

  @override
  Widget build(BuildContext context) => Material(
    color: Theme.of(context).colorScheme.surface,
    borderRadius: BorderRadius.circular(18),
    child: InkWell(
      borderRadius: BorderRadius.circular(18),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Icon(icon, color: color, size: 28),
            const Spacer(),
            Text(title, style: const TextStyle(fontWeight: FontWeight.w800)),
            Text(
              subtitle,
              style: const TextStyle(color: RoadMateColors.muted, fontSize: 12),
            ),
          ],
        ),
      ),
    ),
  );
}
