part of '../../main.dart';

/// RM/21 – end-of-trip summary: route, stats, photos and sharing.
class TripSummaryPage extends StatefulWidget {
  const TripSummaryPage({required this.api, required this.tripId, super.key});
  final RoadMateApi api;
  final String tripId;

  @override
  State<TripSummaryPage> createState() => _TripSummaryPageState();
}

class _TripSummaryPageState extends State<TripSummaryPage> {
  late Future<(Map<String, dynamic>, List<Map<String, dynamic>>)> data =
      _load();

  Future<(Map<String, dynamic>, List<Map<String, dynamic>>)> _load() async {
    final results = await Future.wait([
      widget.api.getTripSummary(widget.tripId),
      widget.api.getMemories(widget.tripId),
    ]);
    return (
      results[0] as Map<String, dynamic>,
      results[1] as List<Map<String, dynamic>>,
    );
  }

  static String _shareText(Map<String, dynamic> trip, Map stats, int photos) {
    final start = parseTime(trip['startTime']);
    return [
      '🏍 ${trip['title']}',
      if (start != null) '📅 ${formatDate(start)}',
      '🛣 ${formatKm(asDouble(stats['distanceKm']))} · ⏱ ${formatMinutes(stats['movingMin'] as num?)} lái xe',
      '📍 ${stats['stopCount']} điểm dừng · 🚀 tối đa ${(asDouble(stats['maxSpeed']) ?? 0).round()} km/h',
      if (photos > 0) '📷 $photos ảnh kỷ niệm',
      'Ghi lại bằng RoadMate – Đi phượt vui hơn, về nhà an toàn hơn.',
    ].join('\n');
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Tổng kết chuyến')),
    body: FutureBuilder<(Map<String, dynamic>, List<Map<String, dynamic>>)>(
      future: data,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return const Center(child: CircularProgressIndicator());
        }
        if (snapshot.hasError) {
          return ListView(
            padding: const EdgeInsets.all(20),
            children: [
              ErrorBanner(snapshot.error.toString()),
              FilledButton(
                onPressed: () => setState(() => data = _load()),
                child: const Text('Thử lại'),
              ),
            ],
          );
        }
        final (summary, memories) = snapshot.data!;
        final trip = Map<String, dynamic>.from(summary['trip'] as Map);
        final stats = summary['stats'] as Map? ?? const {};
        final photos = memories.where((m) => m['type'] == 'photo').toList();
        final end = parseTime(trip['endTime']);
        final route = decodePolyline(trip['plannedPolyline'] as String?);
        final origin = trip['origin'] as Map;
        final destination = trip['destination'] as Map;
        final tiles = [
          (formatKm(asDouble(stats['distanceKm'])), 'Tổng quãng đường'),
          (formatMinutes(stats['movingMin'] as num?), 'Thời gian lái'),
          ('${stats['stopCount'] ?? 0}', 'Điểm dừng'),
          (
            '${(asDouble(stats['maxSpeed']) ?? 0).round()} km/h',
            'Tốc độ cao nhất',
          ),
          (
            '${(asDouble(stats['maxAltitude']) ?? 0).round()} m',
            'Độ cao lớn nhất',
          ),
          ('${stats['alertCount'] ?? 0}', 'Cảnh báo'),
        ];
        return ListView(
          padding: const EdgeInsets.all(20),
          children: [
            if (end != null)
              Align(
                alignment: Alignment.centerRight,
                child: Chip(label: Text('Đã kết thúc ${formatClock(end)}')),
              ),
            const Text(
              'Hoàn thành chuyến đi!',
              style: TextStyle(fontSize: 26, fontWeight: FontWeight.w900),
            ),
            Text(
              trip['title'] as String? ?? '',
              style: const TextStyle(color: RoadMateColors.muted),
            ),
            const SizedBox(height: 16),
            SizedBox(
              height: 180,
              child: ClipRRect(
                borderRadius: BorderRadius.circular(18),
                child: RoadMap(
                  interactive: false,
                  route: route,
                  stops: [
                    LatLng(asDouble(origin['lat'])!, asDouble(origin['lng'])!),
                    LatLng(
                      asDouble(destination['lat'])!,
                      asDouble(destination['lng'])!,
                    ),
                  ],
                ),
              ),
            ),
            const SizedBox(height: 12),
            GridView.count(
              crossAxisCount: 2,
              shrinkWrap: true,
              physics: const NeverScrollableScrollPhysics(),
              mainAxisSpacing: 8,
              crossAxisSpacing: 8,
              childAspectRatio: 2.6,
              children: [
                for (final (value, label) in tiles)
                  MetricTile(value: value, label: label),
              ],
            ),
            const SectionLabel('Ảnh & ghi chú'),
            if (memories.isEmpty)
              const Text(
                'Chưa có kỷ niệm nào.',
                style: TextStyle(color: RoadMateColors.muted),
              )
            else
              SizedBox(
                height: 72,
                child: ListView(
                  scrollDirection: Axis.horizontal,
                  children: [
                    for (final photo in photos.where(
                      (p) => p['mediaUrl'] != null,
                    ))
                      Padding(
                        padding: const EdgeInsets.only(right: 8),
                        child: ClipRRect(
                          borderRadius: BorderRadius.circular(12),
                          child: Image.network(
                            photo['mediaUrl'] as String,
                            width: 72,
                            height: 72,
                            fit: BoxFit.cover,
                            errorBuilder: (_, _, _) => const SizedBox(
                              width: 72,
                              child: Icon(Icons.photo),
                            ),
                          ),
                        ),
                      ),
                    Center(
                      child: Text(
                        '${memories.length} kỷ niệm',
                        style: const TextStyle(color: RoadMateColors.muted),
                      ),
                    ),
                  ],
                ),
              ),
            const SizedBox(height: 24),
            Row(
              children: [
                OutlinedButton.icon(
                  onPressed: () => SharePlus.instance.share(
                    ShareParams(text: _shareText(trip, stats, photos.length)),
                  ),
                  icon: const Icon(Icons.share),
                  label: const Text('Chia sẻ'),
                ),
                const SizedBox(width: 10),
                Expanded(
                  child: FilledButton.icon(
                    onPressed: () => Navigator.pushReplacement(
                      context,
                      MaterialPageRoute(
                        builder: (_) => JournalDetailPage(
                          api: widget.api,
                          tripId: widget.tripId,
                        ),
                      ),
                    ),
                    icon: const Icon(Icons.book),
                    label: const Text('Xem nhật ký'),
                  ),
                ),
              ],
            ),
          ],
        );
      },
    ),
  );
}
