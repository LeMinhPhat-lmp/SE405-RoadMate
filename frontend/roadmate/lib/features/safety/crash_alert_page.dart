part of '../../main.dart';

/// RM/12 – full-screen red countdown after a suspected crash. "Tôi ổn"
/// cancels (logged as a false alarm); "Cần giúp ngay" or no answer within
/// [countdown] sends the alert to emergency contacts.
class CrashAlertPage extends StatefulWidget {
  const CrashAlertPage({
    required this.runtime,
    required this.crash,
    this.countdown = const Duration(seconds: 30),
    super.key,
  });
  final TripRuntime runtime;
  final CrashEvent crash;
  final Duration countdown;

  static Route<void> route(TripRuntime runtime, CrashEvent crash) =>
      MaterialPageRoute(
        fullscreenDialog: true,
        builder: (_) => CrashAlertPage(runtime: runtime, crash: crash),
      );

  @override
  State<CrashAlertPage> createState() => _CrashAlertPageState();
}

class _CrashAlertPageState extends State<CrashAlertPage> {
  late int seconds = widget.countdown.inSeconds;
  Timer? timer;
  bool sending = false;

  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (!mounted) return;
      if (seconds <= 1) {
        timer?.cancel();
        setState(() => seconds = 0);
        unawaited(_needHelp());
      } else {
        setState(() => seconds--);
      }
    });
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  Future<void> _imOk() async {
    timer?.cancel();
    await widget.runtime.dismissCrash();
    if (mounted) Navigator.pop(context);
  }

  Future<void> _needHelp() async {
    if (sending) return;
    timer?.cancel();
    setState(() => sending = true);
    final navigator = Navigator.of(context);
    try {
      await widget.runtime.raiseAlert('crash');
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
    final names = widget.runtime.recipients
        .take(2)
        .map((contact) => contact['name'])
        .join(' và ');
    return PopScope(
      canPop: false,
      child: Scaffold(
        backgroundColor: RoadMateColors.danger,
        body: SafeArea(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 28, vertical: 16),
            child: Column(
              children: [
                Align(
                  alignment: Alignment.centerLeft,
                  child: Chip(
                    backgroundColor: Colors.white24,
                    side: BorderSide.none,
                    avatar: const Icon(
                      Icons.volume_up,
                      color: Colors.white,
                      size: 16,
                    ),
                    label: const Text(
                      'Rung + còi đang bật',
                      style: TextStyle(color: Colors.white),
                    ),
                  ),
                ),
                const Spacer(),
                SizedBox(
                  width: 220,
                  height: 220,
                  child: Stack(
                    fit: StackFit.expand,
                    children: [
                      CircularProgressIndicator(
                        value: seconds / widget.countdown.inSeconds,
                        strokeWidth: 10,
                        color: Colors.white,
                        backgroundColor: Colors.white24,
                      ),
                      Center(
                        child: Column(
                          mainAxisSize: MainAxisSize.min,
                          children: [
                            Text(
                              '$seconds',
                              style: const TextStyle(
                                color: Colors.white,
                                fontSize: 72,
                                fontWeight: FontWeight.w900,
                              ),
                            ),
                            const Text(
                              'giây',
                              style: TextStyle(color: Colors.white70),
                            ),
                          ],
                        ),
                      ),
                    ],
                  ),
                ),
                const SizedBox(height: 28),
                const Text(
                  'Bạn có ổn không?',
                  style: TextStyle(
                    color: Colors.white,
                    fontSize: 30,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                const SizedBox(height: 10),
                Text(
                  'Phát hiện va chạm mạnh (${widget.crash.peakG.toStringAsFixed(1)}g). '
                  'Nếu không phản hồi, RoadMate sẽ gửi vị trí cho '
                  '${names.isEmpty ? 'người thân' : names}.',
                  textAlign: TextAlign.center,
                  style: const TextStyle(color: Colors.white, fontSize: 15),
                ),
                const Spacer(),
                SizedBox(
                  width: double.infinity,
                  height: 64,
                  child: FilledButton.icon(
                    style: FilledButton.styleFrom(
                      backgroundColor: Colors.white,
                      foregroundColor: RoadMateColors.danger,
                    ),
                    onPressed: sending ? null : _imOk,
                    icon: const Icon(Icons.check),
                    label: const Text('Tôi ổn', style: TextStyle(fontSize: 18)),
                  ),
                ),
                const SizedBox(height: 12),
                SizedBox(
                  width: double.infinity,
                  height: 60,
                  child: OutlinedButton.icon(
                    style: OutlinedButton.styleFrom(
                      foregroundColor: Colors.white,
                      side: const BorderSide(color: Colors.white, width: 2),
                    ),
                    onPressed: sending ? null : _needHelp,
                    icon: const Icon(Icons.phone),
                    label: Text(sending ? 'Đang gửi…' : 'Cần giúp ngay'),
                  ),
                ),
              ],
            ),
          ),
        ),
      ),
    );
  }
}
