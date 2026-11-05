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
        const Text(
          'Your trips',
          style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold),
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
                text: 'No trips yet. Plan one from Home.',
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
  String? trackingUrl;

  Future<void> action() async {
    setState(() => loading = true);
    try {
      final id = widget.trip['id'] as String;
      if (widget.trip['status'] == 'active') {
        await widget.runtime.stopTracking();
        await widget.api.finishTrip(id);
      } else {
        await widget.api.startTrip(id);
        try {
          await widget.runtime.startTracking(id);
        } catch (exception) {
          await widget.api.finishTrip(id);
          rethrow;
        }
        final link = await widget.api.createTrackingLink(id);
        trackingUrl =
            link['url']?.toString() ?? link['trackingUrl']?.toString();
      }
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

  @override
  Widget build(BuildContext context) {
    final active = widget.trip['status'] == 'active';
    return Card(
      child: ListTile(
        leading: CircleAvatar(
          child: Icon(active ? Icons.navigation : Icons.route),
        ),
        title: Text(widget.trip['title'] as String? ?? 'Untitled trip'),
        subtitle: Text(
          '${widget.trip['originName']} → ${widget.trip['destName']}\n${widget.trip['status']}',
        ),
        isThreeLine: true,
        trailing: FilledButton(
          onPressed: loading ? null : action,
          child: Text(active ? 'Finish' : 'Start'),
        ),
        onTap: active && trackingUrl != null
            ? () => SharePlus.instance.share(
                ShareParams(text: 'Follow my RoadMate trip: $trackingUrl'),
              )
            : null,
      ),
    );
  }
}
