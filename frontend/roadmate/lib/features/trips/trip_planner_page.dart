part of '../../main.dart';

/// RM/07 – origin, reorderable stops, destination, route options and a map
/// preview. "Tiếp tục" opens the per-trip safety step (RM/10). Pass [trip]
/// to edit an existing draft. Pops with the saved trip id.
class TripPlannerPage extends StatefulWidget {
  const TripPlannerPage({
    required this.api,
    required this.runtime,
    this.trip,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime runtime;
  final Map<String, dynamic>? trip;

  @override
  State<TripPlannerPage> createState() => _TripPlannerPageState();
}

class _TripPlannerPageState extends State<TripPlannerPage> {
  Map<String, dynamic>? origin;
  Map<String, dynamic>? destination;
  final List<Map<String, dynamic>> waypoints = [];
  bool avoidHighway = false;
  bool avoidToll = false;
  bool loading = false;
  bool locating = false;
  Map<String, dynamic>? route;
  String? error;
  final mapController = MapController();

  @override
  void initState() {
    super.initState();
    final trip = widget.trip;
    if (trip != null) {
      origin = Map<String, dynamic>.from(trip['origin'] as Map);
      destination = Map<String, dynamic>.from(trip['destination'] as Map);
      waypoints.addAll(
        ((trip['waypoints'] as List?) ?? const []).map(
          (item) => Map<String, dynamic>.from(item as Map),
        ),
      );
      avoidHighway = trip['avoidHighway'] == true;
      avoidToll = trip['avoidToll'] == true;
      unawaited(calculate());
    } else {
      unawaited(_useCurrentLocationAsOrigin());
    }
  }

  Future<void> _useCurrentLocationAsOrigin() async {
    setState(() => locating = true);
    try {
      final position = await currentPositionWithPermission();
      var name = 'Vị trí hiện tại';
      try {
        final reverse = await widget.api.reverseGeocode(
          position.latitude,
          position.longitude,
        );
        final address = reverse['name'] ?? reverse['address'];
        if (address != null) name = 'Vị trí hiện tại · $address';
      } on Object {
        // Keep the generic name.
      }
      if (mounted && origin == null) {
        setState(
          () => origin = {
            'name': name,
            'lat': position.latitude,
            'lng': position.longitude,
          },
        );
      }
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => locating = false);
    }
  }

  List<LatLng> get _stopPoints => [
    if (origin != null) _point(origin!),
    ...waypoints.map(_point),
    if (destination != null) _point(destination!),
  ];

  static LatLng _point(Map<String, dynamic> place) =>
      LatLng(asDouble(place['lat'])!, asDouble(place['lng'])!);

  Future<void> calculate() async {
    if (origin == null || destination == null) return;
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final result = await widget.api.getDirections(
        originLat: asDouble(origin!['lat'])!,
        originLng: asDouble(origin!['lng'])!,
        destinationLat: asDouble(destination!['lat'])!,
        destinationLng: asDouble(destination!['lng'])!,
        waypoints: waypoints
            .map(
              (w) => {'lat': asDouble(w['lat'])!, 'lng': asDouble(w['lng'])!},
            )
            .toList(),
        avoidHighway: avoidHighway,
        avoidToll: avoidToll,
      );
      if (!mounted) return;
      setState(() => route = result);
      final points = decodePolyline(result['polyline'] as String?);
      if (points.length >= 2) {
        mapController.fitCamera(
          CameraFit.coordinates(
            coordinates: points,
            padding: const EdgeInsets.all(40),
          ),
        );
      }
    } on Object catch (exception) {
      if (mounted) {
        setState(() {
          route = null;
          error = '$exception';
        });
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _pick(
    String title,
    void Function(Map<String, dynamic>) apply,
  ) async {
    final place = await PlaceSearchPage.pick(context, widget.api, title);
    if (place == null || place['lat'] == null) return;
    setState(() => apply(place));
    await calculate();
  }

  Future<void> _addStop() => _pick('Thêm điểm dừng', (place) {
    if (waypoints.length >= 8) return;
    waypoints.add({...place, 'plannedStayMin': 30});
  });

  void _removeStop(int index) {
    setState(() => waypoints.removeAt(index));
    unawaited(calculate());
  }

  Future<void> _continue() async {
    if (origin == null || destination == null) {
      setState(() => error = 'Chọn điểm đi và điểm đến trước.');
      return;
    }
    if (route == null) await calculate();
    if (route == null || !mounted) return;
    final saved = await Navigator.push<String>(
      context,
      MaterialPageRoute(
        builder: (_) => TripSafetyPage(
          api: widget.api,
          runtime: widget.runtime,
          existing: widget.trip,
          draft: TripDraft(
            origin: origin!,
            destination: destination!,
            waypoints: waypoints
                .map(
                  (w) => {
                    'name': w['name'],
                    'lat': asDouble(w['lat']),
                    'lng': asDouble(w['lng']),
                    'plannedStayMin': w['plannedStayMin'],
                  },
                )
                .toList(),
            route: route!,
            avoidHighway: avoidHighway,
            avoidToll: avoidToll,
          ),
        ),
      ),
    );
    if (saved != null && mounted) Navigator.pop(context, saved);
  }

  Widget _stopTile({
    Key? key,
    required String badge,
    required Color color,
    required String label,
    required Map<String, dynamic>? place,
    required VoidCallback onTap,
    Widget? trailing,
  }) => ListTile(
    key: key,
    dense: true,
    contentPadding: const EdgeInsets.symmetric(horizontal: 4),
    onTap: onTap,
    leading: SizedBox(
      width: 28,
      height: 28,
      child: MapPin(label: badge, color: color),
    ),
    title: Text(
      label,
      style: const TextStyle(color: RoadMateColors.muted, fontSize: 12),
    ),
    subtitle: Text(
      place?['name']?.toString() ?? 'Chạm để chọn',
      maxLines: 1,
      overflow: TextOverflow.ellipsis,
      style: TextStyle(
        fontSize: 15,
        fontWeight: FontWeight.w700,
        color: place == null ? RoadMateColors.muted : null,
      ),
    ),
    trailing: trailing,
  );

  @override
  Widget build(BuildContext context) {
    final polyline = decodePolyline(route?['polyline'] as String?);
    final warnings = (route?['warnings'] as List?) ?? const [];
    return Scaffold(
      appBar: AppBar(
        title: Text(widget.trip == null ? 'Lập chuyến đi' : 'Sửa chuyến đi'),
      ),
      body: Column(
        children: [
          Card(
            margin: const EdgeInsets.fromLTRB(16, 0, 16, 8),
            child: Padding(
              padding: const EdgeInsets.symmetric(vertical: 6),
              child: Column(
                children: [
                  _stopTile(
                    badge: 'A',
                    color: RoadMateColors.primaryDeep,
                    label: locating ? 'Điểm đi · đang lấy vị trí…' : 'Điểm đi',
                    place: origin,
                    onTap: () => _pick('Điểm đi', (place) => origin = place),
                  ),
                  ReorderableListView(
                    shrinkWrap: true,
                    physics: const NeverScrollableScrollPhysics(),
                    buildDefaultDragHandles: false,
                    onReorderItem: (from, to) {
                      setState(() {
                        final item = waypoints.removeAt(from);
                        waypoints.insert(to, item);
                      });
                      unawaited(calculate());
                    },
                    children: [
                      for (var i = 0; i < waypoints.length; i++)
                        _stopTile(
                          key: ValueKey('stop-$i-${waypoints[i]['name']}'),
                          badge: '${i + 1}',
                          color: RoadMateColors.violet,
                          label: 'Điểm dừng ${i + 1}',
                          place: waypoints[i],
                          onTap: () => _pick(
                            'Điểm dừng ${i + 1}',
                            (place) => waypoints[i] = {
                              ...place,
                              'plannedStayMin': waypoints[i]['plannedStayMin'],
                            },
                          ),
                          trailing: Row(
                            mainAxisSize: MainAxisSize.min,
                            children: [
                              IconButton(
                                tooltip: 'Xoá điểm dừng',
                                icon: const Icon(Icons.close, size: 18),
                                onPressed: () => _removeStop(i),
                              ),
                              ReorderableDragStartListener(
                                index: i,
                                child: const Icon(Icons.drag_handle),
                              ),
                            ],
                          ),
                        ),
                    ],
                  ),
                  _stopTile(
                    badge: 'B',
                    color: RoadMateColors.coral,
                    label: 'Điểm đến',
                    place: destination,
                    onTap: () =>
                        _pick('Điểm đến', (place) => destination = place),
                  ),
                  Align(
                    alignment: Alignment.centerLeft,
                    child: TextButton.icon(
                      onPressed: waypoints.length >= 8 ? null : _addStop,
                      icon: const Icon(Icons.add_circle_outline),
                      label: const Text(
                        'Thêm điểm dừng (kéo thả để đổi thứ tự)',
                      ),
                    ),
                  ),
                ],
              ),
            ),
          ),
          Expanded(
            child: Padding(
              padding: const EdgeInsets.symmetric(horizontal: 16),
              child: ClipRRect(
                borderRadius: BorderRadius.circular(18),
                child: RoadMap(
                  controller: mapController,
                  route: polyline,
                  stops: _stopPoints,
                ),
              ),
            ),
          ),
          SafeArea(
            top: false,
            child: Padding(
              padding: const EdgeInsets.fromLTRB(16, 10, 16, 12),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const Text(
                    'Tuỳ chọn tuyến',
                    style: TextStyle(fontWeight: FontWeight.w800, fontSize: 16),
                  ),
                  const SizedBox(height: 6),
                  Wrap(
                    spacing: 8,
                    children: [
                      FilterChip(
                        label: const Text('Tránh cao tốc'),
                        selected: avoidHighway,
                        onSelected: (value) {
                          setState(() => avoidHighway = value);
                          unawaited(calculate());
                        },
                      ),
                      FilterChip(
                        label: const Text('Tránh trạm thu phí'),
                        selected: avoidToll,
                        onSelected: (value) {
                          setState(() => avoidToll = value);
                          unawaited(calculate());
                        },
                      ),
                    ],
                  ),
                  for (final warning in warnings)
                    Padding(
                      padding: const EdgeInsets.only(top: 4),
                      child: Text(
                        '⚠ ${(warning as Map)['message']}',
                        style: const TextStyle(color: RoadMateColors.amber),
                      ),
                    ),
                  if (error != null) ErrorBanner(error!),
                  const SizedBox(height: 8),
                  Row(
                    children: [
                      Expanded(
                        child: loading
                            ? const Text('Đang tính lộ trình…')
                            : route == null
                            ? const Text(
                                'Chọn điểm đi và điểm đến',
                                style: TextStyle(color: RoadMateColors.muted),
                              )
                            : Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                children: [
                                  Text(
                                    '${formatKm(asDouble(route!['distanceKm']))} · ${formatMinutes(route!['etaMinutes'] as num?)}',
                                    style: const TextStyle(
                                      fontSize: 17,
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                  Text(
                                    '${waypoints.length} điểm dừng · xe máy',
                                    style: const TextStyle(
                                      color: RoadMateColors.muted,
                                    ),
                                  ),
                                ],
                              ),
                      ),
                      FilledButton(
                        onPressed: loading ? null : _continue,
                        child: const Text('Tiếp tục'),
                      ),
                    ],
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
