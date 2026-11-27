part of '../../main.dart';

/// RM/06 – greeting, "Bạn muốn phượt đi đâu?", the running trip, upcoming
/// drafts and recent trips.
class HomePage extends StatefulWidget {
  const HomePage({
    required this.api,
    required this.runtime,
    required this.onTripCreated,
    this.onOpenTab,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime runtime;
  final ValueChanged<String> onTripCreated;
  final ValueChanged<int>? onOpenTab;

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  late Future<(Map<String, dynamic>?, List<Map<String, dynamic>>)> data =
      _load();
  String? startingId;

  Future<(Map<String, dynamic>?, List<Map<String, dynamic>>)> _load() async {
    final trips = await widget.api.getTrips();
    Map<String, dynamic>? me;
    try {
      me = await widget.api.getMe();
    } on Object {
      // Greeting falls back to a generic name.
    }
    return (me, trips);
  }

  void reload() => setState(() => data = _load());

  static String _greeting() {
    final hour = DateTime.now().hour;
    if (hour < 11) return 'Chào buổi sáng';
    if (hour < 14) return 'Chào buổi trưa';
    if (hour < 18) return 'Chào buổi chiều';
    return 'Chào buổi tối';
  }

  Future<void> _plan([Map<String, dynamic>? trip]) async {
    Map<String, dynamic>? full;
    if (trip != null) {
      try {
        full = await widget.api.getTrip(trip['id'] as String);
      } on Object catch (exception) {
        if (mounted) {
          ScaffoldMessenger.of(context)
              .showSnackBar(SnackBar(content: Text('$exception')));
        }
        return;
      }
    }
    if (!mounted) return;
    final id = await Navigator.push<String>(
      context,
      MaterialPageRoute(
        builder: (_) => TripPlannerPage(
          api: widget.api,
          runtime: widget.runtime,
          trip: full,
        ),
      ),
    );
    if (id != null) widget.onTripCreated(id);
    reload();
  }

  Future<void> _start(Map<String, dynamic> trip) async {
    if (widget.runtime.isTracking) {
      ScaffoldMessenger.of(context).showSnackBar(
        const SnackBar(content: Text('Hãy kết thúc chuyến đang chạy trước.')),
      );
      return;
    }
    setState(() => startingId = trip['id'] as String);
    try {
      await startTripAndNavigate(
        context,
        widget.api,
        widget.runtime,
        trip['id'] as String,
      );
      widget.onTripCreated(trip['id'] as String);
    } on Object catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$exception')));
      }
    } finally {
      if (mounted) {
        setState(() => startingId = null);
        reload();
      }
    }
  }

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: () async => reload(),
    child: FutureBuilder<(Map<String, dynamic>?, List<Map<String, dynamic>>)>(
      future: data,
      builder: (context, snapshot) {
        final me = snapshot.data?.$1;
        final trips = snapshot.data?.$2 ?? const <Map<String, dynamic>>[];
        final drafts = trips
            .where((trip) => trip['status'] == 'draft')
            .toList();
        final recent = trips
            .where((trip) => trip['status'] == 'finished')
            .take(6)
            .toList();
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            Row(
              children: [
                InitialAvatar(me?['name'] as String?, radius: 22),
                const SizedBox(width: 10),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        _greeting(),
                        style: const TextStyle(color: RoadMateColors.muted),
                      ),
                      Text(
                        me?['name'] as String? ?? 'RoadMate',
                        style: const TextStyle(
                          fontSize: 20,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                    ],
                  ),
                ),
                IconButton.filledTonal(
                  tooltip: 'SOS',
                  onPressed: () =>
                      Navigator.of(context).push(SosPage.route(widget.runtime)),
                  icon: const Icon(
                    Icons.emergency,
                    color: RoadMateColors.danger,
                  ),
                ),
              ],
            ),
            const SizedBox(height: 16),
            Material(
              color: Theme.of(context).colorScheme.surface,
              borderRadius: BorderRadius.circular(18),
              child: InkWell(
                borderRadius: BorderRadius.circular(18),
                onTap: () => _plan(),
                child: Padding(
                  padding: const EdgeInsets.fromLTRB(16, 10, 10, 10),
                  child: Row(
                    children: [
                      const Icon(Icons.search, color: RoadMateColors.muted),
                      const SizedBox(width: 10),
                      const Expanded(
                        child: Text(
                          'Bạn muốn phượt đi đâu?',
                          style: TextStyle(color: RoadMateColors.muted),
                        ),
                      ),
                      FilledButton(
                        style: FilledButton.styleFrom(
                          minimumSize: const Size(0, 38),
                        ),
                        onPressed: () => _plan(),
                        child: const Text('Chuyến mới'),
                      ),
                    ],
                  ),
                ),
              ),
            ),
            if (widget.runtime.isTracking) ...[
              const SectionLabel('Đang đi'),
              Card(
                color: RoadMateColors.primaryDeep,
                child: ListTile(
                  leading: const Icon(Icons.navigation, color: Colors.white),
                  title: Text(
                    widget.runtime.trip?['title'] as String? ?? 'Chuyến đi',
                    style: const TextStyle(
                      color: Colors.white,
                      fontWeight: FontWeight.w800,
                    ),
                  ),
                  subtitle: Text(
                    'Đã đi ${formatDistance(widget.runtime.distanceMeters)}',
                    style: const TextStyle(color: Colors.white70),
                  ),
                  trailing: const Icon(
                    Icons.chevron_right,
                    color: Colors.white,
                  ),
                  onTap: () => Navigator.of(context)
                      .push(NavigationPage.route(widget.api, widget.runtime)),
                ),
              ),
            ],
            Row(
              children: [
                const Expanded(child: SectionLabel('Chuyến sắp tới')),
                TextButton(
                  onPressed: () => widget.onOpenTab?.call(1),
                  child: const Text('Xem tất cả'),
                ),
              ],
            ),
            if (snapshot.connectionState == ConnectionState.waiting)
              const Center(child: CircularProgressIndicator())
            else if (snapshot.hasError)
              ErrorBanner(snapshot.error.toString())
            else if (drafts.isEmpty)
              const EmptyState(
                text: 'Chưa có chuyến nháp. Lập chuyến mới nhé!',
              ),
            for (final trip in drafts.take(3))
              Card(
                child: Padding(
                  padding: const EdgeInsets.all(16),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      const Text(
                        'BẢN NHÁP',
                        style: TextStyle(
                          fontSize: 11,
                          fontWeight: FontWeight.w800,
                          color: RoadMateColors.amber,
                        ),
                      ),
                      const SizedBox(height: 4),
                      Text(
                        trip['title'] as String? ?? 'Chuyến đi',
                        style: const TextStyle(
                          fontSize: 18,
                          fontWeight: FontWeight.w800,
                        ),
                      ),
                      Text(
                        '${trip['originName']} → ${trip['destName']} · ${formatKm(asDouble(trip['distanceKm']))}',
                        style: const TextStyle(color: RoadMateColors.muted),
                      ),
                      const SizedBox(height: 12),
                      Row(
                        children: [
                          Expanded(
                            child: FilledButton(
                              onPressed: startingId == null
                                  ? () => _start(trip)
                                  : null,
                              child: Text(
                                startingId == trip['id']
                                    ? 'Đang bắt đầu…'
                                    : 'Bắt đầu',
                              ),
                            ),
                          ),
                          const SizedBox(width: 10),
                          OutlinedButton(
                            onPressed: () => _plan(trip),
                            child: const Text('Sửa'),
                          ),
                        ],
                      ),
                    ],
                  ),
                ),
              ),
            if (recent.isNotEmpty) ...[
              Row(
                children: [
                  const Expanded(child: SectionLabel('Chuyến gần đây')),
                  TextButton(
                    onPressed: () => widget.onOpenTab?.call(2),
                    child: const Text('Nhật ký'),
                  ),
                ],
              ),
              SizedBox(
                height: 110,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  children: [
                    for (final trip in recent)
                      SizedBox(
                        width: 180,
                        child: Card(
                          margin: const EdgeInsets.only(right: 10),
                          child: InkWell(
                            borderRadius: BorderRadius.circular(18),
                            onTap: () => Navigator.push(
                              context,
                              MaterialPageRoute(
                                builder: (_) => JournalDetailPage(
                                  api: widget.api,
                                  tripId: trip['id'] as String,
                                ),
                              ),
                            ),
                            child: Padding(
                              padding: const EdgeInsets.all(14),
                              child: Column(
                                crossAxisAlignment: CrossAxisAlignment.start,
                                mainAxisAlignment: MainAxisAlignment.end,
                                children: [
                                  const Icon(
                                    Icons.landscape_outlined,
                                    color: RoadMateColors.primaryDeep,
                                  ),
                                  const Spacer(),
                                  Text(
                                    trip['title'] as String? ?? '',
                                    maxLines: 1,
                                    overflow: TextOverflow.ellipsis,
                                    style: const TextStyle(
                                      fontWeight: FontWeight.w800,
                                    ),
                                  ),
                                  Text(
                                    formatKm(asDouble(trip['distanceKm'])),
                                    style: const TextStyle(
                                      color: RoadMateColors.muted,
                                    ),
                                  ),
                                ],
                              ),
                            ),
                          ),
                        ),
                      ),
                  ],
                ),
              ),
            ],
          ],
        );
      },
    ),
  );
}
