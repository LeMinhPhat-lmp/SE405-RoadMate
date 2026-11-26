part of '../../main.dart';

/// RM/15 – "Bạn đã dừng 15 phút, mọi thứ ổn chứ?". No answer within
/// [responseTime]: siren + vibration, then after [escalationTime] the idle
/// alert is sent to contacts (doc 3.4b).
class IdleCheckPage extends StatefulWidget {
  const IdleCheckPage({
    required this.runtime,
    required this.stoppedFor,
    this.responseTime = const Duration(minutes: 3),
    this.escalationTime = const Duration(seconds: 30),
    super.key,
  });
  final TripRuntime runtime;
  final Duration stoppedFor;
  final Duration responseTime;
  final Duration escalationTime;

  static Route<void> route(TripRuntime runtime, Duration stoppedFor) =>
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (_) => IdleCheckPage(runtime: runtime, stoppedFor: stoppedFor),
      );

  @override
  State<IdleCheckPage> createState() => _IdleCheckPageState();
}

class _IdleCheckPageState extends State<IdleCheckPage> {
  final opened = DateTime.now();
  Timer? timer;
  bool escalated = false;
  bool sending = false;

  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(seconds: 1), (_) => _tick());
  }

  Duration get elapsed => DateTime.now().difference(opened);

  void _tick() {
    if (!mounted) return;
    if (!escalated && elapsed >= widget.responseTime) {
      escalated = true;
      unawaited(SafetyAlarm.start());
      widget.runtime.log('idle: no answer → siren');
    }
    if (elapsed >= widget.responseTime + widget.escalationTime) {
      timer?.cancel();
      unawaited(_raise('idle'));
    }
    setState(() {});
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  Future<void> _ok() async {
    timer?.cancel();
    await widget.runtime.acknowledgeIdle();
    if (mounted) Navigator.pop(context);
  }

  Future<void> _rest(int minutes) async {
    timer?.cancel();
    final navigator = Navigator.of(context);
    await widget.runtime.startRest(minutes);
    unawaited(
      navigator.pushReplacement(
        RestModePage.route(widget.runtime.api, widget.runtime),
      ),
    );
  }

  Future<void> _raise(String type) async {
    if (sending) return;
    timer?.cancel();
    setState(() => sending = true);
    final navigator = Navigator.of(context);
    try {
      await widget.runtime.raiseAlert(type);
      unawaited(
        navigator.pushReplacement(AlertActivePage.route(widget.runtime)),
      );
    } on Object catch (exception) {
      if (!mounted) return;
      setState(() => sending = false);
      ScaffoldMessenger.of(context)
          .showSnackBar(SnackBar(content: Text('$exception')));
    }
  }

  @override
  Widget build(BuildContext context) {
    final remaining = widget.responseTime - elapsed;
    final progress = (elapsed.inSeconds / widget.responseTime.inSeconds).clamp(
      0.0,
      1.0,
    );
    return PopScope(
      canPop: false,
      child: Scaffold(
        backgroundColor: Colors.black54,
        body: Center(
          child: SingleChildScrollView(
            padding: const EdgeInsets.all(20),
            child: Card(
              child: Padding(
                padding: const EdgeInsets.all(24),
                child: Column(
                  mainAxisSize: MainAxisSize.min,
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    const CircleAvatar(
                      radius: 36,
                      backgroundColor: Color(0xfffff1dc),
                      child: Icon(
                        Icons.schedule,
                        size: 36,
                        color: RoadMateColors.amber,
                      ),
                    ),
                    const SizedBox(height: 16),
                    Text(
                      'Bạn đã dừng ${widget.stoppedFor.inMinutes} phút, mọi thứ ổn chứ?',
                      textAlign: TextAlign.center,
                      style: const TextStyle(
                        fontSize: 22,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 8),
                    Text(
                      escalated
                          ? 'Không có phản hồi. Sắp gửi vị trí cho người thân…'
                          : 'Không phản hồi trong ${formatCountdown(remaining)}, máy sẽ rung, '
                                'phát âm lớn rồi gửi vị trí cho người thân.',
                      textAlign: TextAlign.center,
                      style: TextStyle(
                        color: escalated
                            ? RoadMateColors.danger
                            : RoadMateColors.muted,
                      ),
                    ),
                    const SizedBox(height: 16),
                    LinearProgressIndicator(
                      value: progress,
                      minHeight: 6,
                      borderRadius: BorderRadius.circular(6),
                      color: escalated
                          ? RoadMateColors.danger
                          : RoadMateColors.amber,
                    ),
                    const SizedBox(height: 16),
                    FilledButton.icon(
                      onPressed: sending ? null : _ok,
                      icon: const Icon(Icons.check),
                      label: const Text('Tôi ổn, đi tiếp'),
                    ),
                    const SizedBox(height: 14),
                    const Text(
                      'Hoặc nghỉ chân trong:',
                      textAlign: TextAlign.center,
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        for (final (minutes, label) in const [
                          (30, '30 phút'),
                          (60, '1 giờ'),
                          (120, '2 giờ'),
                        ])
                          Expanded(
                            child: Padding(
                              padding: const EdgeInsets.symmetric(
                                horizontal: 4,
                              ),
                              child: OutlinedButton(
                                style: OutlinedButton.styleFrom(
                                  minimumSize: const Size(0, 40),
                                ),
                                onPressed: sending
                                    ? null
                                    : () => _rest(minutes),
                                child: Text(label),
                              ),
                            ),
                          ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    TextButton.icon(
                      style: TextButton.styleFrom(
                        foregroundColor: RoadMateColors.danger,
                      ),
                      onPressed: sending ? null : () => _raise('sos'),
                      icon: const Icon(Icons.warning_amber),
                      label: const Text('Tôi cần giúp'),
                    ),
                  ],
                ),
              ),
            ),
          ),
        ),
      ),
    );
  }
}
