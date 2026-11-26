part of '../../main.dart';

class TripsPage extends StatefulWidget {
  const TripsPage({required this.api, required this.runtime, super.key});
  final RoadMateApi api;
  final TripRuntime runtime;

  @override
  State<TripsPage> createState() => _TripsPageState();
}

class _TripsPageState extends State<TripsPage> {
  late Future<List<Map<String, dynamic>>> trips;

  @override
  void initState() {
    super.initState();
    trips = widget.api.getTrips();
  }

  void reload() => setState(() => trips = widget.api.getTrips());

  @override
  Widget build(BuildContext context) => RefreshIndicator(
    onRefresh: () async => reload(),
    child: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        Row(
          children: [
            const Expanded(
              child: Text(
                'Chuyến đi của bạn',
                style: TextStyle(fontSize: 28, fontWeight: FontWeight.w800),
              ),
            ),
            IconButton.filled(
              tooltip: 'Lập chuyến mới',
              onPressed: () async {
                await Navigator.push<String>(
                  context,
                  MaterialPageRoute(
                    builder: (_) => TripPlannerPage(
                      api: widget.api,
                      runtime: widget.runtime,
                    ),
                  ),
                );
                reload();
              },
              icon: const Icon(Icons.add),
            ),
          ],
        ),
        const SizedBox(height: 16),
        FutureBuilder<List<Map<String, dynamic>>>(
          future: trips,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const Center(child: CircularProgressIndicator());
            }
            if (snapshot.hasError) {
              return ErrorBanner(snapshot.error.toString());
            }
            final values = snapshot.data ?? [];
            if (values.isEmpty) {
              return const EmptyState(
                text: 'Chưa có chuyến nào. Lập chuyến mới từ Trang chủ.',
              );
            }
            return Column(
              children: values
                  .map(
                    (trip) => TripCard(
                      api: widget.api,
                      runtime: widget.runtime,
                      trip: trip,
                      onChanged: reload,
                    ),
                  )
                  .toList(),
            );
          },
        ),
      ],
    ),
  );
}

class TripCard extends StatefulWidget {
  const TripCard({
    required this.api,
    required this.runtime,
    required this.trip,
    required this.onChanged,
    super.key,
  });
  final RoadMateApi api;
  final TripRuntime runtime;
  final Map<String, dynamic> trip;
  final VoidCallback onChanged;

  @override
  State<TripCard> createState() => _TripCardState();
}

class _TripCardState extends State<TripCard> {
  bool loading = false;

  String get id => widget.trip['id'] as String;
  String get status => widget.trip['status'] as String? ?? 'draft';

  static String statusLabel(String status) => switch (status) {
    'active' => 'Đang đi',
    'finished' => 'Đã kết thúc',
    _ => 'Bản nháp',
  };

  Future<void> _run(Future<void> Function() action) async {
    setState(() => loading = true);
    try {
      await action();
      widget.onChanged();
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> _start() => _run(() async {
    if (widget.runtime.isTracking && widget.runtime.activeTripId != id) {
      throw Exception('Hãy kết thúc chuyến đang chạy trước.');
    }
    await startTripAndNavigate(context, widget.api, widget.runtime, id);
  });

  Future<void> _resume() async {
    // Active on the server but not tracked here (e.g. app reinstalled).
    if (widget.runtime.activeTripId != id) {
      await _run(() => widget.runtime.startTracking(id));
    }
    if (mounted) {
      await Navigator.of(context)
          .push(NavigationPage.route(widget.api, widget.runtime));
    }
  }

  Future<void> _edit() async {
    final full = await widget.api.getTrip(id);
    if (!mounted) return;
    await Navigator.push<String>(
      context,
      MaterialPageRoute(
        builder: (_) => TripPlannerPage(
          api: widget.api,
          runtime: widget.runtime,
          trip: full,
        ),
      ),
    );
    widget.onChanged();
  }

  Future<void> _delete() => _run(() async {
    final confirm = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Xoá chuyến nháp?'),
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
    if (confirm == true) await widget.api.deleteTrip(id);
  });

  void _summary() => Navigator.push(
    context,
    MaterialPageRoute(
      builder: (_) => TripSummaryPage(api: widget.api, tripId: id),
    ),
  );

  @override
  Widget build(BuildContext context) {
    final active = status == 'active';
    final start = parseTime(widget.trip['startTime']);
    return Card(
      child: Padding(
        padding: const EdgeInsets.all(14),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            Row(
              children: [
                CircleAvatar(
                  backgroundColor: active
                      ? RoadMateColors.primary
                      : RoadMateColors.primarySoft,
                  child: Icon(
                    active ? Icons.navigation : Icons.route,
                    color: active ? Colors.white : RoadMateColors.primaryDeep,
                  ),
                ),
                const SizedBox(width: 12),
                Expanded(
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(
                        widget.trip['title'] as String? ?? 'Chuyến đi',
                        style: const TextStyle(
                          fontWeight: FontWeight.w800,
                          fontSize: 16,
                        ),
                      ),
                      Text(
                        '${widget.trip['originName']} → ${widget.trip['destName']}',
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                      ),
                      Text(
                        '${statusLabel(status)} · ${formatKm(asDouble(widget.trip['distanceKm']))}'
                        '${start == null ? '' : ' · ${formatDate(start)}'}',
                        style: const TextStyle(color: RoadMateColors.muted),
                      ),
                    ],
                  ),
                ),
              ],
            ),
            const SizedBox(height: 10),
            Row(
              children: [
                if (status == 'draft') ...[
                  Expanded(
                    child: FilledButton(
                      onPressed: loading ? null : _start,
                      child: Text(loading ? 'Đang bắt đầu…' : 'Bắt đầu'),
                    ),
                  ),
                  const SizedBox(width: 8),
                  OutlinedButton(
                    onPressed: loading ? null : _edit,
                    child: const Text('Sửa'),
                  ),
                  IconButton(
                    tooltip: 'Xoá',
                    onPressed: loading ? null : _delete,
                    icon: const Icon(Icons.delete_outline),
                  ),
                ],
                if (active)
                  Expanded(
                    child: FilledButton.icon(
                      onPressed: loading ? null : _resume,
                      icon: const Icon(Icons.navigation),
                      label: const Text('Mở dẫn đường'),
                    ),
                  ),
                if (status == 'finished')
                  Expanded(
                    child: OutlinedButton.icon(
                      onPressed: _summary,
                      icon: const Icon(Icons.insights),
                      label: const Text('Xem tổng kết'),
                    ),
                  ),
              ],
            ),
          ],
        ),
      ),
    );
  }
}
