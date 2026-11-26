part of '../../main.dart';

/// Route chosen on the planner, waiting for the safety step.
class TripDraft {
  const TripDraft({
    required this.origin,
    required this.destination,
    required this.waypoints,
    required this.route,
    required this.avoidHighway,
    required this.avoidToll,
  });
  final Map<String, dynamic> origin;
  final Map<String, dynamic> destination;
  final List<Map<String, dynamic>> waypoints;
  final Map<String, dynamic> route;
  final bool avoidHighway;
  final bool avoidToll;

  String get defaultTitle =>
      '${_shortName(origin['name'])} → ${_shortName(destination['name'])}';

  static String _shortName(Object? name) {
    final text = (name ?? '').toString().replaceFirst('Vị trí hiện tại · ', '');
    return text.split(',').first.trim();
  }
}

/// RM/10 – alert recipients, expected return, live sharing, crash and idle
/// thresholds. "Lưu nháp" pops with the trip id; "Bắt đầu ngay" starts the
/// trip and opens navigation.
class TripSafetyPage extends StatefulWidget {
  const TripSafetyPage({
    required this.api,
    required this.runtime,
    required this.draft,
    this.existing,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime runtime;
  final TripDraft draft;
  final Map<String, dynamic>? existing;

  @override
  State<TripSafetyPage> createState() => _TripSafetyPageState();
}

class _TripSafetyPageState extends State<TripSafetyPage> {
  late final title = TextEditingController(
    text: widget.existing?['title'] as String? ?? widget.draft.defaultTitle,
  );
  List<Map<String, dynamic>> contacts = [];
  final Set<String> selected = {};
  DateTime? expectedReturn;
  bool liveShare = true;
  String crashSensitivity = 'medium';
  int idleMinutes = 15;
  bool loading = true;
  bool saving = false;
  String? error;

  @override
  void initState() {
    super.initState();
    final existing = widget.existing;
    if (existing != null) {
      expectedReturn = parseTime(existing['expectedReturnTime']);
      liveShare = existing['liveShare'] != false;
      selected.addAll(
        ((existing['alertContactIds'] as List?) ?? const []).map(
          (id) => id.toString(),
        ),
      );
    }
    unawaited(_load());
  }

  Future<void> _load() async {
    try {
      final results = await Future.wait([
        widget.api.getContacts(),
        widget.api.getMe(),
      ]);
      final list = results[0] as List<Map<String, dynamic>>;
      final me = results[1] as Map<String, dynamic>;
      list.sort(
        (a, b) => ((a['priority'] as num?) ?? 9).compareTo(
          (b['priority'] as num?) ?? 9,
        ),
      );
      if (!mounted) return;
      setState(() {
        contacts = list;
        if (selected.isEmpty) {
          selected.addAll(list.map((contact) => contact['id'] as String));
        }
        crashSensitivity =
            widget.existing?['crashSensitivity'] as String? ??
            me['crashSensitivity'] as String? ??
            'medium';
        idleMinutes =
            (widget.existing?['idleMinutes'] as num?)?.toInt() ??
            (me['idleMinutes'] as num?)?.toInt() ??
            15;
      });
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _pickReturnTime() async {
    final now = DateTime.now();
    final initial = expectedReturn ?? now.add(const Duration(hours: 8));
    final date = await showDatePicker(
      context: context,
      initialDate: initial,
      firstDate: now,
      lastDate: now.add(const Duration(days: 30)),
    );
    if (date == null || !mounted) return;
    final time = await showTimePicker(
      context: context,
      initialTime: TimeOfDay.fromDateTime(initial),
    );
    if (time == null) return;
    setState(
      () => expectedReturn = DateTime(
        date.year,
        date.month,
        date.day,
        time.hour,
        time.minute,
      ),
    );
  }

  Future<Map<String, dynamic>> _save() async {
    final draft = widget.draft;
    final name = title.text.trim().isEmpty
        ? draft.defaultTitle
        : title.text.trim();
    final args = (
      title: name,
      origin: draft.origin,
      destination: draft.destination,
      waypoints: draft.waypoints,
      plannedPolyline: draft.route['polyline'] as String?,
      distanceKm: asDouble(draft.route['distanceKm']),
      etaMinutes: (draft.route['etaMinutes'] as num?)?.toInt(),
    );
    final existingId = widget.existing?['id'] as String?;
    if (existingId != null) {
      return widget.api.updateTrip(
        existingId,
        title: args.title,
        origin: _location(args.origin),
        destination: _location(args.destination),
        waypoints: args.waypoints,
        plannedPolyline: args.plannedPolyline,
        distanceKm: args.distanceKm,
        etaMinutes: args.etaMinutes,
        avoidHighway: draft.avoidHighway,
        avoidToll: draft.avoidToll,
        expectedReturnTime: expectedReturn,
        idleMinutes: idleMinutes,
        crashSensitivity: crashSensitivity,
        liveShare: liveShare,
        alertContactIds: selected.toList(),
      );
    }
    return widget.api.createTrip(
      title: args.title,
      origin: _location(args.origin),
      destination: _location(args.destination),
      waypoints: args.waypoints,
      plannedPolyline: args.plannedPolyline,
      distanceKm: args.distanceKm,
      etaMinutes: args.etaMinutes,
      avoidHighway: draft.avoidHighway,
      avoidToll: draft.avoidToll,
      expectedReturnTime: expectedReturn,
      idleMinutes: idleMinutes,
      crashSensitivity: crashSensitivity,
      liveShare: liveShare,
      alertContactIds: selected.toList(),
    );
  }

  static Map<String, dynamic> _location(Map<String, dynamic> place) => {
    'name': place['name'],
    'lat': asDouble(place['lat']),
    'lng': asDouble(place['lng']),
  };

  Future<void> _saveDraft() async {
    setState(() {
      saving = true;
      error = null;
    });
    try {
      final trip = await _save();
      if (mounted) Navigator.pop(context, trip['id'] as String);
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  Future<void> _startNow() async {
    if (contacts.isEmpty) {
      final proceed = await showDialog<bool>(
        context: context,
        builder: (context) => AlertDialog(
          title: const Text('Chưa có liên hệ khẩn cấp'),
          content: const Text(
            'Khi gặp sự cố sẽ không có ai nhận cảnh báo. Vẫn bắt đầu?',
          ),
          actions: [
            TextButton(
              onPressed: () => Navigator.pop(context, false),
              child: const Text('Thêm liên hệ'),
            ),
            FilledButton(
              onPressed: () => Navigator.pop(context, true),
              child: const Text('Vẫn bắt đầu'),
            ),
          ],
        ),
      );
      if (proceed != true) return;
    }
    setState(() {
      saving = true;
      error = null;
    });
    try {
      final trip = await _save();
      if (!mounted) return;
      final started = await startTripAndNavigate(
        context,
        widget.api,
        widget.runtime,
        trip['id'] as String,
        expectedReturnTime: expectedReturn,
        alertContactIds: selected.toList(),
        liveShare: liveShare,
      );
      if (!started && mounted) Navigator.pop(context, trip['id'] as String);
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => saving = false);
    }
  }

  @override
  void dispose() {
    title.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      title: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          const Text('An toàn cho chuyến'),
          Text(
            '${formatKm(asDouble(widget.draft.route['distanceKm']))} · ${formatMinutes(widget.draft.route['etaMinutes'] as num?)}',
            style: const TextStyle(fontSize: 13, color: RoadMateColors.muted),
          ),
        ],
      ),
    ),
    body: loading
        ? const Center(child: CircularProgressIndicator())
        : ListView(
            padding: const EdgeInsets.fromLTRB(20, 0, 20, 20),
            children: [
              TextField(
                controller: title,
                decoration: const InputDecoration(labelText: 'Tên chuyến đi'),
              ),
              const SectionLabel('Người nhận cảnh báo'),
              Card(
                child: contacts.isEmpty
                    ? const ListTile(
                        leading: Icon(Icons.info_outline),
                        title: Text('Chưa có liên hệ khẩn cấp'),
                        subtitle: Text('Thêm ở tab Liên hệ để nhận cảnh báo.'),
                      )
                    : Column(
                        children: [
                          for (final contact in contacts)
                            CheckboxListTile(
                              value: selected.contains(contact['id']),
                              onChanged: (value) => setState(() {
                                final id = contact['id'] as String;
                                value == true
                                    ? selected.add(id)
                                    : selected.remove(id);
                              }),
                              secondary: InitialAvatar(
                                contact['name'] as String?,
                              ),
                              title: Text(contact['name'] as String),
                              subtitle: Text(
                                '${contact['phone']} · Ưu tiên ${contact['priority']}',
                              ),
                            ),
                        ],
                      ),
              ),
              const SectionLabel('Theo dõi & cảnh báo'),
              Card(
                child: Column(
                  children: [
                    ListTile(
                      title: const Text('Giờ dự kiến về'),
                      subtitle: const Text(
                        'Quá 60 phút sẽ nhắc bạn, rồi báo người thân',
                      ),
                      trailing: TextButton(
                        onPressed: _pickReturnTime,
                        child: Text(
                          expectedReturn == null
                              ? 'Chọn'
                              : '${formatClock(expectedReturn!)} ${expectedReturn!.day}/${expectedReturn!.month}',
                        ),
                      ),
                    ),
                    SwitchListTile(
                      value: liveShare,
                      onChanged: (value) => setState(() => liveShare = value),
                      title: const Text('Chia sẻ vị trí realtime'),
                      subtitle: const Text('Tạo link theo dõi cho người nhận'),
                    ),
                    ListTile(
                      title: const Text('Phát hiện va chạm'),
                      subtitle: Text(
                        'Độ nhạy: ${crashSensitivityLabel(crashSensitivity)}',
                      ),
                      trailing: DropdownButton<String>(
                        value: crashSensitivity,
                        underline: const SizedBox.shrink(),
                        onChanged: (value) => setState(
                          () => crashSensitivity = value ?? 'medium',
                        ),
                        items: const [
                          DropdownMenuItem(value: 'high', child: Text('Nhạy')),
                          DropdownMenuItem(value: 'medium', child: Text('Vừa')),
                          DropdownMenuItem(value: 'low', child: Text('Ít')),
                        ],
                      ),
                    ),
                    ListTile(
                      title: const Text('Cảnh báo dừng lâu'),
                      subtitle: Text('Sau $idleMinutes phút đứng yên'),
                      trailing: DropdownButton<int>(
                        value: idleMinutes,
                        underline: const SizedBox.shrink(),
                        onChanged: (value) =>
                            setState(() => idleMinutes = value ?? 15),
                        items: {1, 5, 10, 15, 20, 30, 45, 60, idleMinutes}
                            .map(
                              (value) => DropdownMenuItem(
                                value: value,
                                child: Text('$value phút'),
                              ),
                            )
                            .toList(),
                      ),
                    ),
                  ],
                ),
              ),
              if (error != null) ErrorBanner(error!),
            ],
          ),
    bottomNavigationBar: SafeArea(
      child: Padding(
        padding: const EdgeInsets.fromLTRB(20, 8, 20, 12),
        child: Row(
          children: [
            OutlinedButton(
              onPressed: saving || loading ? null : _saveDraft,
              child: const Text('Lưu nháp'),
            ),
            const SizedBox(width: 10),
            Expanded(
              child: FilledButton.icon(
                onPressed: saving || loading ? null : _startNow,
                icon: const Icon(Icons.navigation),
                label: Text(saving ? 'Đang lưu…' : 'Bắt đầu ngay'),
              ),
            ),
          ],
        ),
      ),
    ),
  );
}
