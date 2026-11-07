part of '../../main.dart';

class TripPlannerPage extends StatefulWidget {
  const TripPlannerPage({required this.api, super.key});
  final RoadMateApi api;

  @override
  State<TripPlannerPage> createState() => _TripPlannerPageState();
}

class _TripPlannerPageState extends State<TripPlannerPage> {
  final title = TextEditingController(text: 'Weekend getaway');
  final origin = TextEditingController(text: 'Ho Chi Minh City');
  final destination = TextEditingController(text: 'Binh Duong');
  bool avoidHighway = false;
  bool avoidToll = false;
  bool loading = false;
  Map<String, dynamic>? route;
  String? error;

  Future<void> calculate() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      route = await widget.api.getDirections(
        originLat: 10.7769,
        originLng: 106.7009,
        destinationLat: 11.0168,
        destinationLng: 106.6297,
        avoidHighway: avoidHighway,
        avoidToll: avoidToll,
      );
    } catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  Future<void> saveTrip() async {
    if (route == null) {
      await calculate();
      if (route == null) return;
    }
    try {
      final trip = await widget.api.createTrip(
        title: title.text.trim().isEmpty ? 'RoadMate trip' : title.text.trim(),
        origin: origin.text.trim(),
        destination: destination.text.trim(),
        plannedPolyline: route!['polyline'] as String?,
        distanceKm: (route!['distanceKm'] as num?)?.toDouble(),
        etaMinutes: route!['etaMinutes'] as int?,
        avoidHighway: avoidHighway,
        avoidToll: avoidToll,
      );
      if (mounted) {
        Navigator.pop(context, trip['id'] as String);
      }
    } catch (exception) {
      if (mounted) setState(() => error = '$exception');
    }
  }

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Plan a trip')),
    body: ListView(
      padding: const EdgeInsets.all(20),
      children: [
        TextField(
          controller: title,
          decoration: const InputDecoration(
            labelText: 'Trip name',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: origin,
          decoration: const InputDecoration(
            labelText: 'Starting point',
            prefixIcon: Icon(Icons.trip_origin),
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: destination,
          decoration: const InputDecoration(
            labelText: 'Destination',
            prefixIcon: Icon(Icons.place_outlined),
            border: OutlineInputBorder(),
          ),
        ),
        SwitchListTile(
          value: avoidHighway,
          onChanged: (value) => setState(() => avoidHighway = value),
          title: const Text('Avoid highways'),
        ),
        SwitchListTile(
          value: avoidToll,
          onChanged: (value) => setState(() => avoidToll = value),
          title: const Text('Avoid toll roads'),
        ),
        if (error != null) ErrorBanner(error!),
        FilledButton.icon(
          onPressed: loading ? null : calculate,
          icon: const Icon(Icons.route),
          label: Text(loading ? 'Calculating...' : 'Calculate route'),
        ),
        if (route != null) ...[
          const SizedBox(height: 18),
          Card(
            child: Padding(
              padding: const EdgeInsets.all(16),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    '${route!['distanceKm']} km · ${route!['etaMinutes']} min',
                    style: const TextStyle(
                      fontSize: 20,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  const SizedBox(height: 12),
                  ...(route!['steps'] as List? ?? [])
                      .take(5)
                      .map(
                        (step) => ListTile(
                          dense: true,
                          leading: const Icon(Icons.turn_right),
                          title: Text((step as Map)['instruction'].toString()),
                          subtitle: Text(
                            '${step['distanceM']} m · ${step['durationS']} sec',
                          ),
                        ),
                      ),
                ],
              ),
            ),
          ),
          const SizedBox(height: 10),
          FilledButton.icon(
            onPressed: saveTrip,
            icon: const Icon(Icons.save_outlined),
            label: const Text('Save trip'),
          ),
        ],
      ],
    ),
  );

  @override
  void dispose() {
    title.dispose();
    origin.dispose();
    destination.dispose();
    super.dispose();
  }
}
