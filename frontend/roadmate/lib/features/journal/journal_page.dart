part of '../../main.dart';

/// RM/18 – journal list: totals, the trip being recorded (quick note/photo)
/// and past trips. Opening a trip shows its timeline and photo map.
class JournalPage extends StatefulWidget {
  const JournalPage({
    required this.api,
    required this.tripId,
    this.runtime,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime? runtime;

  /// Trip currently recorded (or just created).
  final String? tripId;

  @override
  State<JournalPage> createState() => _JournalPageState();
}

class _JournalPageState extends State<JournalPage> {
  late Future<List<Map<String, dynamic>>> trips;

  @override
  void initState() {
    super.initState();
    trips = _load();
  }

  @override
  void didUpdateWidget(covariant JournalPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.tripId != widget.tripId) {
      setState(() => trips = _load());
    }
  }

  Future<List<Map<String, dynamic>>> _load() async {
    final all = await widget.api.getTrips();
    return all.where((trip) => trip['status'] != 'draft').toList();
  }

  void reload() => setState(() => trips = _load());

  void _open(Map<String, dynamic> trip) => Navigator.push(
    context,
    MaterialPageRoute(
      builder: (_) => JournalDetailPage(
        api: widget.api,
        runtime: widget.runtime,
        tripId: trip['id'] as String,
      ),
    ),
  ).then((_) => reload());

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: () async => reload(),
    child: FutureBuilder<List<Map<String, dynamic>>>(
      future: trips,
      builder: (context, snapshot) {
        final values = snapshot.data ?? [];
        final totalKm = values.fold<double>(
          0,
          (sum, trip) => sum + (asDouble(trip['distanceKm']) ?? 0),
        );
        final active = values.where((trip) => trip['status'] == 'active');
        final finished = values.where((trip) => trip['status'] != 'active');
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            const Text(
              'Nhật ký',
              style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
            ),
            const SizedBox(height: 12),
            Row(
              children: [
                Expanded(
                  child: MetricTile(value: '${values.length}', label: 'chuyến'),
                ),
                const SizedBox(width: 8),
                Expanded(
                  child: MetricTile(value: formatKm(totalKm), label: 'đã đi'),
                ),
              ],
            ),
            if (snapshot.connectionState == ConnectionState.waiting)
              const Padding(
                padding: EdgeInsets.all(24),
                child: Center(child: CircularProgressIndicator()),
              )
            else if (snapshot.hasError)
              ErrorBanner(snapshot.error.toString())
            else if (values.isEmpty)
              const EmptyState(
                text: 'Bắt đầu một chuyến đi để RoadMate tự ghi nhật ký.',
              ),
            for (final trip in active) ...[
              const SectionLabel('Đang ghi'),
              _JournalTripCard(
                trip: trip,
                onTap: () => _open(trip),
                actions: Row(
                  children: [
                    Expanded(
                      child: OutlinedButton.icon(
                        onPressed: () => MemoryCapture.note(
                          context,
                          widget.api,
                          trip['id'] as String,
                          widget.runtime,
                        ),
                        icon: const Icon(Icons.edit_note),
                        label: const Text('Ghi chú'),
                      ),
                    ),
                    const SizedBox(width: 8),
                    Expanded(
                      child: FilledButton.icon(
                        onPressed: () => MemoryCapture.photo(
                          context,
                          widget.api,
                          trip['id'] as String,
                          widget.runtime,
                        ),
                        icon: const Icon(Icons.photo_camera_outlined),
                        label: const Text('Chụp ảnh'),
                      ),
                    ),
                  ],
                ),
              ),
            ],
            if (finished.isNotEmpty) const SectionLabel('Chuyến đã đi'),
            for (final trip in finished)
              _JournalTripCard(trip: trip, onTap: () => _open(trip)),
          ],
        );
      },
    ),
  );
}

class _JournalTripCard extends StatelessWidget {
  const _JournalTripCard({
    required this.trip,
    required this.onTap,
    this.actions,
  });
  final Map<String, dynamic> trip;
  final VoidCallback onTap;
  final Widget? actions;

  @override
  Widget build(BuildContext context) {
    final start = parseTime(trip['startTime']);
    final end = parseTime(trip['endTime']);
    final days = start == null || end == null
        ? 1
        : end.toLocal().difference(start.toLocal()).inDays + 1;
    return Card(
      child: InkWell(
        borderRadius: BorderRadius.circular(18),
        onTap: onTap,
        child: Padding(
          padding: const EdgeInsets.all(12),
          child: Column(
            children: [
              Row(
                children: [
                  Container(
                    width: 64,
                    height: 64,
                    decoration: BoxDecoration(
                      color: RoadMateColors.primarySoft,
                      borderRadius: BorderRadius.circular(14),
                    ),
                    child: const Icon(
                      Icons.landscape_outlined,
                      color: RoadMateColors.primaryDeep,
                      size: 32,
                    ),
                  ),
                  const SizedBox(width: 12),
                  Expanded(
                    child: Column(
                      crossAxisAlignment: CrossAxisAlignment.start,
                      children: [
                        Text(
                          trip['title'] as String? ?? 'Chuyến đi',
                          style: const TextStyle(
                            fontWeight: FontWeight.w800,
                            fontSize: 16,
                          ),
                        ),
                        Text(
                          start == null
                              ? 'Chưa bắt đầu'
                              : '${formatDate(start)} · $days ngày',
                          style: const TextStyle(color: RoadMateColors.muted),
                        ),
                        Text(
                          '${formatKm(asDouble(trip['distanceKm']))} · ${trip['originName']} → ${trip['destName']}',
                          maxLines: 1,
                          overflow: TextOverflow.ellipsis,
                          style: const TextStyle(color: RoadMateColors.muted),
                        ),
                      ],
                    ),
                  ),
                  const Icon(Icons.chevron_right),
                ],
              ),
              if (actions != null) ...[const SizedBox(height: 10), actions!],
            ],
          ),
        ),
      ),
    );
  }
}
