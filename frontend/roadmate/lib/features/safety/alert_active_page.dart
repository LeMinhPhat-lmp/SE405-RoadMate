part of '../../main.dart';

/// RM/13 – an emergency alert is active: delivery status per contact, what
/// was sent, quick-dial buttons and "Tôi đã an toàn".
class AlertActivePage extends StatefulWidget {
  const AlertActivePage({required this.runtime, super.key});
  final TripRuntime runtime;

  static Route<void> route(TripRuntime runtime) => MaterialPageRoute(
    fullscreenDialog: true,
    builder: (_) => AlertActivePage(runtime: runtime),
  );

  @override
  State<AlertActivePage> createState() => _AlertActivePageState();
}

class _AlertActivePageState extends State<AlertActivePage> {
  Timer? _clock;
  bool resolving = false;

  SafetyAlertController get alerts => widget.runtime.alerts;

  @override
  void initState() {
    super.initState();
    // Refresh the "next update in" countdown.
    _clock = Timer.periodic(const Duration(seconds: 1), (_) {
      if (mounted) setState(() {});
    });
  }

  @override
  void dispose() {
    _clock?.cancel();
    super.dispose();
  }

  Future<void> _resolve() async {
    setState(() => resolving = true);
    await alerts.resolve();
    if (mounted) Navigator.pop(context);
  }

  String _deliveryText(Map<String, dynamic> contact) {
    final deliveries = ((alerts.alert?['deliveries'] as List?) ?? const [])
        .map((item) => Map<String, dynamic>.from(item as Map))
        .where((delivery) => delivery['contactId'] == contact['id'])
        .toList();
    final acked = ((alerts.alert?['acks'] as List?) ?? const []).any(
      (ack) =>
          (ack as Map)['contactId'] == contact['id'] && ack['ackedAt'] != null,
    );
    if (alerts.queued) return 'Chờ mạng để gửi…';
    if (deliveries.isEmpty) return 'Đang gửi SMS + push';
    final parts = deliveries
        .map(
          (delivery) =>
              '${_channel(delivery['channel'] as String?)} ${_status(delivery['status'] as String?)}',
        )
        .join(' · ');
    return acked ? '$parts · Đã xem' : parts;
  }

  static String _channel(String? channel) => switch (channel) {
    'sms' => 'SMS',
    'email' => 'Email',
    _ => 'Push',
  };

  static String _status(String? status) => switch (status) {
    'sent' => 'đã gửi',
    'failed' => 'lỗi',
    'skipped' => 'bỏ qua',
    _ => 'đang gửi',
  };

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: alerts,
    builder: (context, _) {
      if (!alerts.active) {
        return Scaffold(
          appBar: AppBar(),
          body: const EmptyState(text: 'Không có cảnh báo nào đang hoạt động.'),
        );
      }
      final me = widget.runtime.profile.me;
      final recipients = widget.runtime.recipients;
      final next = alerts.nextUpdateAt;
      final untilNext = next == null
          ? Duration.zero
          : next.difference(DateTime.now());
      return PopScope(
        canPop: true,
        child: Scaffold(
          body: ListView(
            padding: EdgeInsets.zero,
            children: [
              Container(
                color: RoadMateColors.danger,
                padding: EdgeInsets.fromLTRB(
                  20,
                  MediaQuery.paddingOf(context).top + 12,
                  20,
                  24,
                ),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Row(
                      children: [
                        const Icon(Icons.emergency, color: Colors.white),
                        const SizedBox(width: 8),
                        Expanded(
                          child: Text(
                            'Cảnh báo khẩn cấp đang hoạt động · ${alertTypeLabel(alerts.type)}',
                            style: const TextStyle(
                              color: Colors.white,
                              fontWeight: FontWeight.w700,
                            ),
                          ),
                        ),
                        IconButton(
                          tooltip: 'Thu nhỏ',
                          onPressed: () => Navigator.pop(context),
                          icon: const Icon(Icons.close, color: Colors.white),
                        ),
                      ],
                    ),
                    const SizedBox(height: 12),
                    Text(
                      alerts.queued
                          ? 'Chưa gửi được – đang thử lại'
                          : 'Đã gửi vị trí cho người thân',
                      style: const TextStyle(
                        color: Colors.white,
                        fontSize: 24,
                        fontWeight: FontWeight.w900,
                      ),
                    ),
                    const SizedBox(height: 4),
                    Text(
                      alerts.queued
                          ? 'Không có mạng. Hãy gửi SMS thủ công bên dưới.'
                          : 'Tự cập nhật vị trí mỗi 2 phút · lần kế sau ${formatCountdown(untilNext)}',
                      style: const TextStyle(color: Colors.white),
                    ),
                  ],
                ),
              ),
              Padding(
                padding: const EdgeInsets.all(20),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.stretch,
                  children: [
                    Card(
                      child: Column(
                        children: [
                          if (recipients.isEmpty)
                            const ListTile(
                              leading: Icon(Icons.warning_amber),
                              title: Text('Chưa có liên hệ khẩn cấp'),
                              subtitle: Text('Hãy gọi số khẩn cấp bên dưới.'),
                            ),
                          for (final contact in recipients)
                            ListTile(
                              leading: InitialAvatar(
                                contact['name'] as String?,
                              ),
                              title: Text(contact['name'] as String),
                              subtitle: Text(_deliveryText(contact)),
                              trailing: IconButton(
                                tooltip: 'Gọi',
                                icon: const Icon(Icons.phone),
                                onPressed: () =>
                                    openDialer(contact['phone'] as String),
                              ),
                            ),
                        ],
                      ),
                    ),
                    if (alerts.queued && recipients.isNotEmpty)
                      Padding(
                        padding: const EdgeInsets.only(top: 8),
                        child: FilledButton.icon(
                          style: FilledButton.styleFrom(
                            backgroundColor: RoadMateColors.amber,
                            foregroundColor: RoadMateColors.ink,
                          ),
                          onPressed: () => launchUrl(
                            alerts.smsFallbackUri(
                              widget.runtime.profile,
                              recipients,
                            ),
                          ),
                          icon: const Icon(Icons.sms),
                          label: const Text('Gửi SMS thủ công cho người thân'),
                        ),
                      ),
                    const SectionLabel('Thông tin đã gửi'),
                    Card(
                      child: Padding(
                        padding: const EdgeInsets.all(14),
                        child: Column(
                          crossAxisAlignment: CrossAxisAlignment.start,
                          children: [
                            Row(
                              children: [
                                Expanded(
                                  child: MetricTile(
                                    value: me['bloodType']?.toString() ?? '--',
                                    label: 'Nhóm máu',
                                  ),
                                ),
                                Expanded(
                                  child: MetricTile(
                                    value:
                                        me['plateNumber']?.toString() ?? '--',
                                    label: 'Biển số',
                                  ),
                                ),
                                Expanded(
                                  child: MetricTile(
                                    value: alerts.battery == null
                                        ? '--'
                                        : '${alerts.battery}%',
                                    label: 'Pin',
                                  ),
                                ),
                              ],
                            ),
                            if (alerts.lat != null && alerts.lng != null)
                              TextButton.icon(
                                onPressed: () => launchUrl(
                                  Uri.parse(
                                    googleMapsLink(alerts.lat!, alerts.lng!),
                                  ),
                                  mode: LaunchMode.externalApplication,
                                ),
                                icon: const Icon(Icons.place_outlined),
                                label: Text(
                                  googleMapsLink(
                                    alerts.lat!,
                                    alerts.lng!,
                                  ).replaceFirst('https://', ''),
                                ),
                              ),
                          ],
                        ),
                      ),
                    ),
                    const SectionLabel('Gọi khẩn cấp'),
                    const EmergencyDialRow(),
                    const Padding(
                      padding: EdgeInsets.only(top: 8),
                      child: Text(
                        'Chỉ mở trình quay số, không tự động gọi.',
                        textAlign: TextAlign.center,
                        style: TextStyle(color: RoadMateColors.muted),
                      ),
                    ),
                    if (alerts.error != null && !alerts.queued)
                      ErrorBanner(alerts.error!),
                  ],
                ),
              ),
            ],
          ),
          bottomNavigationBar: SafeArea(
            child: Padding(
              padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
              child: FilledButton.icon(
                onPressed: resolving ? null : _resolve,
                icon: const Icon(Icons.check),
                label: const Text('Tôi đã an toàn – dừng cảnh báo'),
              ),
            ),
          ),
        ),
      );
    },
  );
}

class EmergencyDialRow extends StatelessWidget {
  const EmergencyDialRow({this.numbers = emergencyNumbers, super.key});
  final List<(String, String)> numbers;

  @override
  Widget build(BuildContext context) => Row(
    children: [
      for (final (number, label) in numbers)
        Expanded(
          child: Padding(
            padding: const EdgeInsets.symmetric(horizontal: 4),
            child: Material(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(16),
              child: InkWell(
                borderRadius: BorderRadius.circular(16),
                onTap: () => openDialer(number),
                child: Padding(
                  padding: const EdgeInsets.symmetric(vertical: 14),
                  child: Column(
                    children: [
                      Text(
                        number,
                        style: const TextStyle(
                          fontSize: 22,
                          fontWeight: FontWeight.w900,
                          color: RoadMateColors.danger,
                        ),
                      ),
                      Text(
                        label,
                        style: const TextStyle(color: RoadMateColors.muted),
                      ),
                    ],
                  ),
                ),
              ),
            ),
          ),
        ),
    ],
  );
}
