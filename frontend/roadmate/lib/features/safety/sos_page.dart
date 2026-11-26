part of '../../main.dart';

/// RM/14 – manual SOS: hold 3 seconds, then the alert goes to contacts, the
/// siren starts and the emergency dial buttons are shown.
class SosPage extends StatefulWidget {
  const SosPage({required this.runtime, super.key});
  final TripRuntime runtime;

  static Route<void> route(TripRuntime runtime) => MaterialPageRoute(
    fullscreenDialog: true,
    builder: (_) => SosPage(runtime: runtime),
  );

  @override
  State<SosPage> createState() => _SosPageState();
}

class _SosPageState extends State<SosPage> {
  double progress = 0;
  bool sending = false;

  Future<void> _trigger() async {
    setState(() => sending = true);
    final navigator = Navigator.of(context);
    unawaited(SafetyAlarm.start());
    try {
      await widget.runtime.raiseAlert('sos');
      unawaited(
        navigator.pushReplacement(AlertActivePage.route(widget.runtime)),
      );
    } on Object catch (exception) {
      unawaited(SafetyAlarm.stop());
      if (!mounted) return;
      setState(() => sending = false);
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('$exception')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final recipients = widget.runtime.recipients;
    final held = (progress * 3).floor();
    return Scaffold(
      appBar: AppBar(title: const Text('Khẩn cấp')),
      body: ListView(
        padding: const EdgeInsets.all(20),
        children: [
          const SizedBox(height: 12),
          Center(
            child: SosHoldButton(
              size: 200,
              onProgress: (value) => setState(() => progress = value),
              onTriggered: _trigger,
            ),
          ),
          const SizedBox(height: 20),
          Text(
            sending
                ? 'Đang gửi SOS…'
                : progress > 0
                ? 'Đang giữ… $held/3 giây'
                : 'Nhấn giữ 3 giây để gửi SOS',
            textAlign: TextAlign.center,
            style: const TextStyle(fontSize: 18, fontWeight: FontWeight.w800),
          ),
          const SizedBox(height: 6),
          const Text(
            'Thả tay để huỷ. Sẽ gửi vị trí, bật còi và mở bàn phím gọi.',
            textAlign: TextAlign.center,
            style: TextStyle(color: RoadMateColors.muted),
          ),
          if (!widget.runtime.isTracking)
            const ErrorBanner(
              'Chưa có chuyến đi đang chạy: SOS chỉ gửi được khi đang đi. '
              'Bạn vẫn có thể gọi khẩn cấp bên dưới.',
            ),
          const SectionLabel('Sẽ thông báo cho'),
          Card(
            child: ListTile(
              leading: const Icon(Icons.people_outline),
              title: Text(
                recipients.isEmpty
                    ? 'Chưa có liên hệ khẩn cấp'
                    : recipients.map((contact) => contact['name']).join(', '),
              ),
              subtitle: const Text('qua SMS + Push'),
            ),
          ),
          const SectionLabel('Gọi nhanh'),
          EmergencyDialRow(numbers: emergencyNumbers.take(3).toList()),
        ],
      ),
    );
  }
}
