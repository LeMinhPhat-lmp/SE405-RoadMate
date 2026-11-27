part of '../../main.dart';

/// RM/19 timeline by day + RM/20 photo map for one trip.
class JournalDetailPage extends StatefulWidget {
  const JournalDetailPage({
    required this.api,
    required this.tripId,
    this.runtime,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime? runtime;
  final String tripId;

  @override
  State<JournalDetailPage> createState() => _JournalDetailPageState();
}

class _JournalDetailPageState extends State<JournalDetailPage> {
  Map<String, dynamic>? journal;
  Map<String, dynamic>? trip;
  String? error;
  bool loading = true;
  int dayIndex = 0;
  bool showMap = false;

  @override
  void initState() {
    super.initState();
    unawaited(_load());
  }

  Future<void> _load() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final results = await Future.wait([
        widget.api.getJournal(widget.tripId),
        widget.api.getTrip(widget.tripId),
      ]);
      if (!mounted) return;
      setState(() {
        journal = results[0];
        trip = results[1];
        final days = (journal?['days'] as List?) ?? const [];
        if (dayIndex >= days.length) dayIndex = 0;
      });
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  List<Map<String, dynamic>> get _allEntries =>
      ((journal?['days'] as List?) ?? const [])
          .expand((day) => (day as Map)['entries'] as List? ?? const [])
          .map((entry) => Map<String, dynamic>.from(entry as Map))
          .toList();

  Future<void> _delete(Map<String, dynamic> entry) async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xoá kỷ niệm này?'),
        actions: [
          TextButton(
            onPressed: () => Navigator.pop(context, false),
            child: const Text('Huỷ'),
          ),
          FilledButton(
            onPressed: () => Navigator.pop(context, true),
            child: const Text('Xoá'),
          ),
        ],
      ),
    );
    if (confirm != true) return;
    try {
      await widget.api.deleteMemory(entry['id'] as String);
      await _load();
    } on Object catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$exception')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    final stats = journal?['stats'] as Map?;
    final days = ((journal?['days'] as List?) ?? const [])
        .map((day) => Map<String, dynamic>.from(day as Map))
        .toList();
    final active = journal?['status'] == 'active';
    return Scaffold(
      appBar: AppBar(
        title: Text(journal?['title'] as String? ?? 'Nhật ký'),
        actions: [
          IconButton(
            tooltip: 'Tổng kết & chia sẻ',
            icon: const Icon(Icons.ios_share),
            onPressed: () => Navigator.push(
              context,
              MaterialPageRoute(
                builder: (_) =>
                    TripSummaryPage(api: widget.api, tripId: widget.tripId),
              ),
            ),
          ),
        ],
      ),
      floatingActionButton: active
          ? FloatingActionButton.extended(
              onPressed: () async {
                final saved = await MemoryCapture.photo(
                  context,
                  widget.api,
                  widget.tripId,
                  widget.runtime,
                );
                if (saved) await _load();
              },
              icon: const Icon(Icons.photo_camera),
              label: const Text('Chụp ảnh'),
            )
          : null,
      body: loading && journal == null
          ? const Center(child: CircularProgressIndicator())
          : error != null && journal == null
          ? ListView(
              padding: const EdgeInsets.all(20),
              children: [ErrorBanner(error!)],
            )
          : RefreshIndicator(
              onRefresh: _load,
              child: ListView(
                padding: const EdgeInsets.all(20),
                children: [
                  Wrap(
                    spacing: 16,
                    children: [
                      Text('🛣 ${formatKm(asDouble(stats?['distanceKm']))}'),
                      Text('⏱ ${formatMinutes(stats?['movingMin'] as num?)}'),
                      Text(
                        '📷 ${_allEntries.where((e) => e['type'] == 'photo').length} ảnh',
                      ),
                      Text(
                        '📍 ${(journal?['stops'] as List?)?.length ?? 0} điểm dừng',
                      ),
                    ],
                  ),
                  const SizedBox(height: 12),
                  SegmentedButton<bool>(
                    segments: const [
                      ButtonSegment(value: false, label: Text('Timeline')),
                      ButtonSegment(value: true, label: Text('Bản đồ ảnh')),
                    ],
                    selected: {showMap},
                    onSelectionChanged: (value) =>
                        setState(() => showMap = value.first),
                  ),
                  const SizedBox(height: 12),
                  if (showMap)
                    _MemoryMap(trip: trip, entries: _allEntries)
                  else if (days.isEmpty)
                    EmptyState(
                      text: active
                          ? 'Chụp ảnh hoặc ghi chú để bắt đầu nhật ký.'
                          : 'Chuyến này chưa có kỷ niệm nào.',
                    )
                  else ...[
                    SingleChildScrollView(
                      scrollDirection: Axis.horizontal,
                      child: Row(
                        children: [
                          for (var i = 0; i < days.length; i++)
                            Padding(
                              padding: const EdgeInsets.only(right: 8),
                              child: ChoiceChip(
                                selected: dayIndex == i,
                                label: Text(
                                  'Ngày ${i + 1} · ${_dayLabel(days[i]['date'])}',
                                ),
                                onSelected: (_) => setState(() => dayIndex = i),
                              ),
                            ),
                        ],
                      ),
                    ),
                    const SizedBox(height: 12),
                    for (final entry
                        in ((days[dayIndex]['entries'] as List?) ?? const []))
                      _TimelineEntry(
                        entry: Map<String, dynamic>.from(entry as Map),
                        onLongPress: () =>
                            _delete(Map<String, dynamic>.from(entry)),
                      ),
                  ],
                  if (active) ...[
                    const SizedBox(height: 8),
                    OutlinedButton.icon(
                      onPressed: () async {
                        final saved = await MemoryCapture.note(
                          context,
                          widget.api,
                          widget.tripId,
                          widget.runtime,
                        );
                        if (saved) await _load();
                      },
                      icon: const Icon(Icons.edit_note),
                      label: const Text('Thêm ghi chú'),
                    ),
                  ],
                ],
              ),
            ),
    );
  }

  static String _dayLabel(Object? date) {
    final parsed = DateTime.tryParse(date.toString());
    return parsed == null ? '$date' : '${parsed.day}/${parsed.month}';
  }
}

class _TimelineEntry extends StatelessWidget {
  const _TimelineEntry({required this.entry, required this.onLongPress});
  final Map<String, dynamic> entry;
  final VoidCallback onLongPress;

  @override
  Widget build(BuildContext context) {
    final takenAt = parseTime(entry['takenAt']);
    final mediaUrl = entry['mediaUrl'] as String?;
    final mood = moodLabels[entry['mood']];
    return IntrinsicHeight(
      child: Row(
        crossAxisAlignment: CrossAxisAlignment.stretch,
        children: [
          SizedBox(
            width: 52,
            child: Column(
              children: [
                Text(
                  takenAt == null ? '' : formatClock(takenAt),
                  style: const TextStyle(fontWeight: FontWeight.w700),
                ),
                const SizedBox(height: 4),
                Container(
                  width: 10,
                  height: 10,
                  decoration: const BoxDecoration(
                    color: RoadMateColors.primary,
                    shape: BoxShape.circle,
                  ),
                ),
                Expanded(
                  child: Container(width: 2, color: RoadMateColors.primarySoft),
                ),
              ],
            ),
          ),
          Expanded(
            child: GestureDetector(
              onLongPress: onLongPress,
              child: Card(
                clipBehavior: Clip.antiAlias,
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    if (mediaUrl != null)
                      AspectRatio(
                        aspectRatio: 4 / 3,
                        child: Image.network(
                          mediaUrl,
                          fit: BoxFit.cover,
                          errorBuilder: (_, _, _) => const ColoredBox(
                            color: RoadMateColors.primarySoft,
                            child: Center(child: Icon(Icons.broken_image)),
                          ),
                        ),
                      )
                    else if (entry['type'] == 'photo')
                      const ListTile(
                        leading: Icon(Icons.photo_outlined),
                        title: Text('Ảnh (Storage chưa cấu hình URL đọc)'),
                      ),
                    Padding(
                      padding: const EdgeInsets.all(12),
                      child: Column(
                        crossAxisAlignment: CrossAxisAlignment.start,
                        children: [
                          if (entry['placeName'] != null)
                            Text(
                              entry['placeName'].toString(),
                              style: const TextStyle(
                                fontWeight: FontWeight.w800,
                              ),
                            ),
                          if (entry['text'] != null)
                            Text(entry['text'].toString()),
                          if (mood != null || entry['rating'] != null)
                            Padding(
                              padding: const EdgeInsets.only(top: 6),
                              child: Wrap(
                                spacing: 6,
                                children: [
                                  if (mood != null) Chip(label: Text(mood)),
                                  if (entry['rating'] != null)
                                    Chip(label: Text('★${entry['rating']}')),
                                ],
                              ),
                            ),
                        ],
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
  }
}

class _MemoryMap extends StatelessWidget {
  const _MemoryMap({required this.trip, required this.entries});
  final Map<String, dynamic>? trip;
  final List<Map<String, dynamic>> entries;

  @override
  Widget build(BuildContext context) {
    final located = entries
        .where((entry) => entry['lat'] != null && entry['lng'] != null)
        .toList();
    final route = decodePolyline(trip?['plannedPolyline'] as String?);
    final stops = <LatLng>[
      if (trip != null)
        LatLng(
          asDouble((trip!['origin'] as Map)['lat'])!,
          asDouble((trip!['origin'] as Map)['lng'])!,
        ),
      if (trip != null)
        LatLng(
          asDouble((trip!['destination'] as Map)['lat'])!,
          asDouble((trip!['destination'] as Map)['lng'])!,
        ),
    ];
    return SizedBox(
      height: 420,
      child: ClipRRect(
        borderRadius: BorderRadius.circular(18),
        child: RoadMap(
          route: route,
          stops: stops,
          extraMarkers: [
            for (final entry in located)
              Marker(
                point: LatLng(asDouble(entry['lat'])!, asDouble(entry['lng'])!),
                width: 48,
                height: 48,
                child: GestureDetector(
                  onTap: () => showModalBottomSheet<void>(
                    context: context,
                    builder: (_) => Padding(
                      padding: const EdgeInsets.all(16),
                      child: _TimelineEntry(entry: entry, onLongPress: () {}),
                    ),
                  ),
                  child: Container(
                    decoration: BoxDecoration(
                      color: Colors.white,
                      borderRadius: BorderRadius.circular(12),
                      border: Border.all(color: Colors.white, width: 3),
                      boxShadow: const [
                        BoxShadow(blurRadius: 6, color: Colors.black26),
                      ],
                    ),
                    clipBehavior: Clip.antiAlias,
                    child: entry['mediaUrl'] != null
                        ? Image.network(
                            entry['mediaUrl'] as String,
                            fit: BoxFit.cover,
                            errorBuilder: (_, _, _) => const Icon(Icons.photo),
                          )
                        : const Icon(
                            Icons.sticky_note_2_outlined,
                            color: RoadMateColors.violet,
                          ),
                  ),
                ),
              ),
          ],
        ),
      ),
    );
  }
}
