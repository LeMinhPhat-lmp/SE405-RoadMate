part of '../../main.dart';

/// Starts [tripId] on the server, begins GPS + safety monitoring and opens
/// navigation (doc flow 1). Returns false when the trip could not start.
Future<bool> startTripAndNavigate(
  BuildContext context,
  RoadMateApi api,
  TripRuntime runtime,
  String tripId, {
  DateTime? expectedReturnTime,
  List<String>? alertContactIds,
  bool? liveShare,
}) async {
  final messenger = ScaffoldMessenger.of(context);
  final navigator = Navigator.of(context);
  final result = await api.startTrip(
    tripId,
    expectedReturnTime: expectedReturnTime,
    alertContactIds: alertContactIds,
    liveShare: liveShare,
  );
  final trip = Map<String, dynamic>.from(result['trip'] as Map);
  try {
    await runtime.startTracking(tripId, tripData: trip);
  } on Object catch (exception) {
    // Without GPS the trip cannot be protected: roll back to keep data clean.
    await api.finishTrip(tripId);
    messenger.showSnackBar(SnackBar(content: Text('$exception')));
    return false;
  }
  for (final warning in (result['warnings'] as List?) ?? const []) {
    messenger.showSnackBar(SnackBar(content: Text(warning.toString())));
  }
  navigator.popUntil((route) => route.isFirst);
  unawaited(
    navigator.push(
      NavigationPage.route(
        api,
        runtime,
        trackingUrl: result['trackingUrl'] as String?,
      ),
    ),
  );
  return true;
}

/// RM/11 – live map, next turn (text + voice), drive stats, SOS (hold 3s),
/// rest, photo, note and finish.
class NavigationPage extends StatefulWidget {
  const NavigationPage({
    required this.api,
    required this.runtime,
    this.trackingUrl,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime runtime;
  final String? trackingUrl;

  static Route<void> route(
    RoadMateApi api,
    TripRuntime runtime, {
    String? trackingUrl,
  }) => MaterialPageRoute(
    builder: (_) =>
        NavigationPage(api: api, runtime: runtime, trackingUrl: trackingUrl),
  );

  @override
  State<NavigationPage> createState() => _NavigationPageState();
}

class _NavigationPageState extends State<NavigationPage> {
  final mapController = MapController();
  bool follow = true;
  bool finishing = false;
  bool _mapReady = false;
  late String? trackingUrl =
      widget.trackingUrl ?? widget.runtime.trip?['trackingUrl'] as String?;

  TripRuntime get runtime => widget.runtime;

  @override
  void initState() {
    super.initState();
    unawaited(WakelockPlus.enable());
    runtime.addListener(_onRuntimeChanged);
  }

  @override
  void dispose() {
    runtime.removeListener(_onRuntimeChanged);
    unawaited(WakelockPlus.disable());
    super.dispose();
  }

  void _onRuntimeChanged() {
    final point = runtime.latestPoint;
    if (follow && _mapReady && point != null) {
      final zoom = mapController.camera.zoom < 14
          ? 16.0
          : mapController.camera.zoom;
      mapController.moveAndRotate(point, zoom, -(runtime.headingDegrees ?? 0));
    }
    if (!runtime.isTracking && mounted && !finishing) {
      Navigator.of(context).maybePop();
    }
  }

  Future<void> _triggerSos() async {
    unawaited(SafetyAlarm.start());
    try {
      await runtime.raiseAlert('sos');
    } on Object catch (exception) {
      unawaited(SafetyAlarm.stop());
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$exception')));
      }
      return;
    }
    if (mounted) {
      unawaited(Navigator.of(context).push(AlertActivePage.route(runtime)));
    }
  }

  Future<void> _rest() async {
    final minutes = await showModalBottomSheet<int>(
      context: context,
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const ListTile(
              title: Text(
                'Nghỉ chân trong',
                style: TextStyle(fontWeight: FontWeight.w800),
              ),
              subtitle: Text('Tạm tắt cảnh báo dừng lâu và phát hiện va chạm'),
            ),
            for (final (minutes, label) in const [
              (30, '30 phút'),
              (60, '1 giờ'),
              (120, '2 giờ'),
            ])
              ListTile(
                leading: const Icon(Icons.local_cafe_outlined),
                title: Text(label),
                onTap: () => Navigator.pop(context, minutes),
              ),
          ],
        ),
      ),
    );
    if (minutes == null || !mounted) return;
    await runtime.startRest(minutes);
    if (mounted) {
      unawaited(
        Navigator.of(context).push(RestModePage.route(widget.api, runtime)),
      );
    }
  }

  Future<void> _finish() async {
    final id = runtime.activeTripId;
    if (id == null) return;
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Kết thúc chuyến đi?'),
        content: const Text('RoadMate sẽ dừng ghi lộ trình và tạo tổng kết.'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Đi tiếp'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Kết thúc'),
          ),
        ],
      ),
    );
    if (confirm != true || !mounted) return;
    setState(() => finishing = true);
    final navigator = Navigator.of(context);
    final messenger = ScaffoldMessenger.of(context);
    try {
      await runtime.stopTracking();
      await widget.api.finishTrip(id);
      unawaited(
        navigator.pushReplacement(
          MaterialPageRoute(
            builder: (_) => TripSummaryPage(api: widget.api, tripId: id),
          ),
        ),
      );
    } on Object catch (exception) {
      messenger.showSnackBar(SnackBar(content: Text('$exception')));
      if (mounted) setState(() => finishing = false);
    }
  }

  Future<void> _share() async {
    final id = runtime.activeTripId;
    if (id == null) return;
    try {
      trackingUrl ??=
          (await widget.api.createTrackingLink(id))['url'] as String?;
      if (trackingUrl == null) return;
      await SharePlus.instance.share(
        ShareParams(text: 'Theo dõi chuyến đi RoadMate của mình: $trackingUrl'),
      );
      if (mounted) setState(() {});
    } on Object catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$exception')));
      }
    }
  }

  @override
  Widget build(BuildContext context) => AnimatedBuilder(
    animation: runtime,
    builder: (context, _) {
      final tripId = runtime.activeTripId;
      final step = runtime.nextStep;
      final trip = runtime.trip;
      final stops = <LatLng>[
        if (trip != null) ...[
          LatLng(
            asDouble((trip['origin'] as Map)['lat'])!,
            asDouble((trip['origin'] as Map)['lng'])!,
          ),
          for (final w in (trip['waypoints'] as List? ?? const []))
            LatLng(asDouble((w as Map)['lat'])!, asDouble(w['lng'])!),
          LatLng(
            asDouble((trip['destination'] as Map)['lat'])!,
            asDouble((trip['destination'] as Map)['lng'])!,
          ),
        ],
      ];
      final remaining = runtime.remainingMeters;
      final eta = runtime.eta;
      final planned = asDouble(trip?['distanceKm']);
      final progress = planned == null || planned <= 0 || remaining == null
          ? null
          : (1 - remaining / 1000 / planned).clamp(0.0, 1.0);
      return Scaffold(
        body: Stack(
          children: [
            Positioned.fill(
              child: FlutterMap(
                mapController: mapController,
                options: MapOptions(
                  initialCenter:
                      runtime.latestPoint ??
                      (stops.isEmpty ? defaultMapCenter : stops.first),
                  initialZoom: 16,
                  onMapReady: () => _mapReady = true,
                  onPositionChanged: (_, hasGesture) {
                    if (hasGesture && follow) setState(() => follow = false);
                  },
                ),
                children: [
                  TileLayer(
                    urlTemplate:
                        'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
                    userAgentPackageName: 'com.example.roadmate',
                  ),
                  PolylineLayer(
                    polylines: [
                      if (runtime.routePoints.length >= 2)
                        Polyline(
                          points: runtime.routePoints,
                          strokeWidth: 7,
                          color: RoadMateColors.primary,
                          borderStrokeWidth: 2,
                          borderColor: RoadMateColors.primaryDeep,
                        ),
                      if (runtime.traveled.length >= 2)
                        Polyline(
                          points: runtime.traveled,
                          strokeWidth: 5,
                          color: RoadMateColors.violet,
                        ),
                    ],
                  ),
                  MarkerLayer(
                    markers: [
                      for (var i = 0; i < stops.length; i++)
                        Marker(
                          point: stops[i],
                          width: 28,
                          height: 28,
                          child: MapPin(
                            label: i == 0
                                ? 'A'
                                : i == stops.length - 1
                                ? 'B'
                                : '$i',
                            color: i == stops.length - 1
                                ? RoadMateColors.coral
                                : RoadMateColors.violet,
                          ),
                        ),
                      if (runtime.latestPoint != null)
                        Marker(
                          point: runtime.latestPoint!,
                          width: 46,
                          height: 46,
                          child: const DecoratedBox(
                            decoration: BoxDecoration(
                              color: RoadMateColors.sky,
                              shape: BoxShape.circle,
                              border: Border.fromBorderSide(
                                BorderSide(color: Colors.white, width: 3),
                              ),
                            ),
                            child: Icon(Icons.navigation, color: Colors.white),
                          ),
                        ),
                    ],
                  ),
                ],
              ),
            ),
            SafeArea(
              child: Padding(
                padding: const EdgeInsets.all(16),
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    _TurnCard(
                      step: step,
                      distance: runtime.distanceToNextStep,
                      voiceEnabled: runtime.voiceEnabled,
                      onToggleVoice: () =>
                          runtime.setVoiceEnabled(!runtime.voiceEnabled),
                      onClose: () => Navigator.of(context).maybePop(),
                    ),
                    const SizedBox(height: 8),
                    Row(
                      children: [
                        ActionChip(
                          avatar: const Icon(Icons.share, size: 16),
                          label: Text(
                            trackingUrl == null
                                ? 'Chia sẻ link theo dõi'
                                : 'Đang chia sẻ với ${runtime.recipients.length} người',
                          ),
                          onPressed: _share,
                        ),
                        const Spacer(),
                        if (runtime.resting)
                          const Chip(
                            avatar: Icon(Icons.local_cafe, size: 16),
                            label: Text('Đang nghỉ'),
                          ),
                      ],
                    ),
                    const SizedBox(height: 8),
                    Row(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        if (runtime.simulatedSpeedKmh != null)
                          const Chip(label: Text('Tốc độ giả lập')),
                        const Spacer(),
                        Column(
                          children: [
                            SosHoldButton(onTriggered: _triggerSos),
                            const SizedBox(height: 12),
                            FloatingActionButton.small(
                              heroTag: 'locate',
                              tooltip: 'Về vị trí của tôi',
                              onPressed: () {
                                setState(() => follow = true);
                                _onRuntimeChanged();
                              },
                              child: Icon(
                                follow ? Icons.gps_fixed : Icons.gps_not_fixed,
                              ),
                            ),
                          ],
                        ),
                      ],
                    ),
                  ],
                ),
              ),
            ),
            Align(
              alignment: Alignment.bottomCenter,
              child: Container(
                decoration: const BoxDecoration(
                  color: RoadMateColors.ink,
                  borderRadius: BorderRadius.vertical(top: Radius.circular(28)),
                ),
                padding: const EdgeInsets.fromLTRB(20, 18, 20, 12),
                child: SafeArea(
                  top: false,
                  child: Column(
                    mainAxisSize: MainAxisSize.min,
                    children: [
                      Row(
                        crossAxisAlignment: CrossAxisAlignment.end,
                        children: [
                          Text(
                            runtime.currentSpeedKmh.round().toString(),
                            style: const TextStyle(
                              color: Colors.white,
                              fontSize: 44,
                              fontWeight: FontWeight.w900,
                              height: 1,
                            ),
                          ),
                          const Padding(
                            padding: EdgeInsets.only(left: 6, bottom: 6),
                            child: Text(
                              'km/h',
                              style: TextStyle(color: Colors.white70),
                            ),
                          ),
                          const Spacer(),
                          Column(
                            crossAxisAlignment: CrossAxisAlignment.end,
                            children: [
                              Text(
                                eta == null
                                    ? 'Đang đi'
                                    : 'Đến ${formatClock(eta)}',
                                style: const TextStyle(
                                  color: Colors.white,
                                  fontWeight: FontWeight.w800,
                                  fontSize: 18,
                                ),
                              ),
                              if (remaining != null)
                                Text(
                                  'còn ${formatDistance(remaining)}',
                                  style: const TextStyle(color: Colors.white70),
                                ),
                            ],
                          ),
                        ],
                      ),
                      const SizedBox(height: 12),
                      LinearProgressIndicator(
                        value: progress,
                        minHeight: 8,
                        borderRadius: BorderRadius.circular(8),
                        color: RoadMateColors.primary,
                        backgroundColor: Colors.white12,
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          for (final (value, label) in [
                            (
                              runtime.averageSpeedKmh.round().toString(),
                              'TB km/h',
                            ),
                            (formatDistance(runtime.distanceMeters), 'Đã đi'),
                            (
                              formatMinutes(runtime.movingSeconds / 60),
                              'Di chuyển',
                            ),
                            (formatMinutes(runtime.restSeconds / 60), 'Nghỉ'),
                          ])
                            Expanded(
                              child: Padding(
                                padding: const EdgeInsets.symmetric(
                                  horizontal: 3,
                                ),
                                child: MetricTile(
                                  value: value,
                                  label: label,
                                  inverted: true,
                                ),
                              ),
                            ),
                        ],
                      ),
                      const SizedBox(height: 10),
                      Row(
                        children: [
                          _DriveAction(
                            icon: Icons.local_cafe_outlined,
                            label: 'Nghỉ chân',
                            onTap: tripId == null ? null : _rest,
                          ),
                          _DriveAction(
                            icon: Icons.photo_camera_outlined,
                            label: 'Chụp ảnh',
                            onTap: tripId == null
                                ? null
                                : () => MemoryCapture.photo(
                                    context,
                                    widget.api,
                                    tripId,
                                    runtime,
                                  ),
                          ),
                          _DriveAction(
                            icon: Icons.edit_note,
                            label: 'Ghi chú',
                            onTap: tripId == null
                                ? null
                                : () => MemoryCapture.note(
                                    context,
                                    widget.api,
                                    tripId,
                                    runtime,
                                  ),
                          ),
                          _DriveAction(
                            icon: Icons.flag_outlined,
                            label: 'Kết thúc',
                            onTap: finishing ? null : _finish,
                          ),
                        ],
                      ),
                      if (runtime.lastError != null)
                        Padding(
                          padding: const EdgeInsets.only(top: 6),
                          child: Text(
                            'Đang lưu offline (${runtime.pendingPoints} điểm) · ${runtime.lastError}',
                            maxLines: 1,
                            overflow: TextOverflow.ellipsis,
                            style: const TextStyle(
                              color: RoadMateColors.amber,
                              fontSize: 12,
                            ),
                          ),
                        ),
                    ],
                  ),
                ),
              ),
            ),
          ],
        ),
      );
    },
  );
}

class _TurnCard extends StatelessWidget {
  const _TurnCard({
    required this.step,
    required this.distance,
    required this.voiceEnabled,
    required this.onToggleVoice,
    required this.onClose,
  });
  final Map<String, dynamic>? step;
  final double? distance;
  final bool voiceEnabled;
  final VoidCallback onToggleVoice;
  final VoidCallback onClose;

  static IconData maneuverIcon(String? maneuver) {
    final value = maneuver ?? '';
    if (value.contains('uturn')) return Icons.u_turn_left;
    if (value.contains('roundabout')) return Icons.roundabout_left;
    if (value.contains('left')) return Icons.turn_left;
    if (value.contains('right')) return Icons.turn_right;
    return Icons.straight;
  }

  @override
  Widget build(BuildContext context) => Material(
    color: RoadMateColors.primaryDeep,
    borderRadius: BorderRadius.circular(22),
    child: Padding(
      padding: const EdgeInsets.all(14),
      child: Row(
        children: [
          IconButton(
            tooltip: 'Thu nhỏ',
            onPressed: onClose,
            icon: const Icon(Icons.keyboard_arrow_down, color: Colors.white),
          ),
          Container(
            width: 52,
            height: 52,
            decoration: BoxDecoration(
              color: Colors.white.withValues(alpha: 0.15),
              borderRadius: BorderRadius.circular(14),
            ),
            child: Icon(
              maneuverIcon(step?['maneuver'] as String?),
              color: Colors.white,
              size: 34,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Text(
                  step == null ? 'Đang dẫn đường' : formatDistance(distance),
                  style: const TextStyle(
                    color: Colors.white,
                    fontSize: 22,
                    fontWeight: FontWeight.w900,
                  ),
                ),
                Text(
                  step?['instruction']?.toString() ??
                      'Đi theo tuyến màu xanh trên bản đồ',
                  maxLines: 2,
                  overflow: TextOverflow.ellipsis,
                  style: const TextStyle(color: Colors.white, fontSize: 15),
                ),
              ],
            ),
          ),
          IconButton(
            tooltip: voiceEnabled ? 'Tắt giọng nói' : 'Bật giọng nói',
            onPressed: onToggleVoice,
            icon: Icon(
              voiceEnabled ? Icons.volume_up : Icons.volume_off,
              color: Colors.white,
            ),
          ),
        ],
      ),
    ),
  );
}

class _DriveAction extends StatelessWidget {
  const _DriveAction({required this.icon, required this.label, this.onTap});
  final IconData icon;
  final String label;
  final VoidCallback? onTap;

  @override
  Widget build(BuildContext context) => Expanded(
    child: InkWell(
      borderRadius: BorderRadius.circular(16),
      onTap: onTap,
      child: Padding(
        padding: const EdgeInsets.symmetric(vertical: 8),
        child: Column(
          children: [
            Icon(icon, color: Colors.white, size: 26),
            const SizedBox(height: 4),
            Text(
              label,
              style: const TextStyle(color: Colors.white, fontSize: 12),
            ),
          ],
        ),
      ),
    ),
  );
}
