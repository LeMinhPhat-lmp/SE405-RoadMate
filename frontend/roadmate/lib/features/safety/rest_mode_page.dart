part of '../../main.dart';

/// RM/16 – rest mode: idle alerts and crash detection are paused until the
/// timer ends; the rider can extend it or continue the trip.
class RestModePage extends StatefulWidget {
  const RestModePage({required this.api, required this.runtime, super.key});
  final RoadMateApi api;
  final TripRuntime runtime;

  static Route<void> route(RoadMateApi api, TripRuntime runtime) =>
      MaterialPageRoute(
        builder: (_) => RestModePage(api: api, runtime: runtime),
      );

  @override
  State<RestModePage> createState() => _RestModePageState();
}

class _RestModePageState extends State<RestModePage> {
  Timer? timer;
  late final DateTime startedAt = DateTime.now();

  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      if (!widget.runtime.resting) {
        timer?.cancel();
        Navigator.pop(context);
        return;
      }
      setState(() {});
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  Future<void> _continue() async {
    await widget.runtime.endRest();
    if (mounted) Navigator.pop(context);
  }

  @override
  Widget build(BuildContext context) {
    final until = widget.runtime.restUntil;
    final remaining = until == null
        ? Duration.zero
        : until.difference(DateTime.now());
    final total = until == null ? 1 : until.difference(startedAt).inSeconds;
    final tripId = widget.runtime.activeTripId;
    return Scaffold(
      appBar: AppBar(
        actions: const [
          Padding(
            padding: EdgeInsets.only(right: 12),
            child: Chip(
              avatar: Icon(Icons.notifications_paused, size: 16),
              label: Text('Cảnh báo dừng lâu: tạm tắt'),
            ),
          ),
        ],
      ),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const SizedBox(height: 12),
          Center(
            child: SizedBox(
              width: 240,
              height: 240,
              child: Stack(
                fit: StackFit.expand,
                children: [
                  CircularProgressIndicator(
                    value: total <= 0
                        ? 0
                        : (remaining.inSeconds / total).clamp(0.0, 1.0),
                    strokeWidth: 12,
                    color: RoadMateColors.primary,
                    backgroundColor: RoadMateColors.primarySoft,
                  ),
                  Center(
                    child: Column(
                      mainAxisSize: MainAxisSize.min,
                      children: [
                        const Text(
                          'Đang nghỉ chân',
                          style: TextStyle(color: RoadMateColors.muted),
                        ),
                        Text(
                          formatCountdown(remaining),
                          style: const TextStyle(
                            fontSize: 48,
                            fontWeight: FontWeight.w900,
                          ),
                        ),
                        Text(
                          'còn lại · đến ${until == null ? '--' : formatClock(until)}',
                          style: const TextStyle(color: RoadMateColors.muted),
                        ),
                      ],
                    ),
                  ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 24),
          Row(
            children: [
              for (final (minutes, label) in const [
                (15, '+15 phút'),
                (30, '+30 phút'),
                (60, '+1 giờ'),
              ])
                Expanded(
                  child: Padding(
                    padding: const EdgeInsets.symmetric(horizontal: 4),
                    child: OutlinedButton(
                      onPressed: () => widget.runtime.extendRest(minutes),
                      child: Text(label),
                    ),
                  ),
                ),
            ],
          ),
          const SectionLabel('Trong lúc nghỉ'),
          Card(
            child: Column(
              children: [
                ListTile(
                  leading: const Icon(Icons.photo_camera_outlined),
                  title: const Text('Chụp ảnh lưu vào nhật ký'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: tripId == null
                      ? null
                      : () => MemoryCapture.photo(
                          context,
                          widget.api,
                          tripId,
                          widget.runtime,
                        ),
                ),
                ListTile(
                  leading: const Icon(Icons.edit_note),
                  title: const Text('Ghi chú nhanh'),
                  trailing: const Icon(Icons.chevron_right),
                  onTap: tripId == null
                      ? null
                      : () => MemoryCapture.note(
                          context,
                          widget.api,
                          tripId,
                          widget.runtime,
                        ),
                ),
              ],
            ),
          ),
        ],
      ),
      bottomNavigationBar: SafeArea(
        child: Padding(
          padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
          child: FilledButton.icon(
            onPressed: _continue,
            icon: const Icon(Icons.navigation),
            label: const Text('Tiếp tục hành trình'),
          ),
        ),
      ),
    );
  }
}
