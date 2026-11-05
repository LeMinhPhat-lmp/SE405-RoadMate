part of '../../main.dart';

class HomePage extends StatefulWidget {
  const HomePage({required this.api, required this.onTripCreated, super.key});
  final RoadMateApi api;
  final ValueChanged<String> onTripCreated;

  @override
  State<HomePage> createState() => _HomePageState();
}

class _HomePageState extends State<HomePage> {
  bool loading = false;
  String? error;

  Future<void> createTrip() async {
    setState(() {
      loading = true;
      error = null;
    });
    try {
      final trip = await widget.api.createTrip(
        title: 'Weekend getaway',
        origin: 'Ho Chi Minh City',
        destination: 'Binh Duong',
      );
      widget.onTripCreated(trip['id'] as String);
      if (mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Trip created on the RoadMate API')),
        );
      }
    } catch (e) {
      if (mounted) setState(() => error = e.toString());
    } finally {
      if (mounted) setState(() => loading = false);
    }
  }

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      const Text(
        'RoadMate',
        style: TextStyle(fontSize: 32, fontWeight: FontWeight.bold),
      ),
      const Text(
        'Ride farther. Get home safer.',
        style: TextStyle(color: Colors.black54),
      ),
      const SizedBox(height: 24),
      Card(
        color: Theme.of(context).colorScheme.primary,
        child: Padding(
          padding: const EdgeInsets.all(20),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              const Icon(Icons.shield_outlined, color: Colors.white, size: 38),
              const SizedBox(height: 14),
              const Text(
                'Ready for your next ride?',
                style: TextStyle(
                  color: Colors.white,
                  fontSize: 22,
                  fontWeight: FontWeight.bold,
                ),
              ),
              const SizedBox(height: 6),
              const Text(
                'Plan a route and keep your emergency contacts close.',
                style: TextStyle(color: Colors.white70),
              ),
              const SizedBox(height: 18),
              FilledButton.tonal(
                onPressed: loading
                    ? null
                    : () async {
                        final id = await Navigator.push<String>(
                          context,
                          MaterialPageRoute(
                            builder: (_) => TripPlannerPage(api: widget.api),
                          ),
                        );
                        if (id != null) widget.onTripCreated(id);
                      },
                child: const Text('Plan a trip'),
              ),
            ],
          ),
        ),
      ),
      if (error != null) ErrorBanner(error!),
      const SizedBox(height: 16),
      Row(
        children: [
          Expanded(
            child: StatCard(
              label: 'Safety',
              value: 'Protected',
              icon: Icons.verified_user_outlined,
            ),
          ),
          const SizedBox(width: 12),
          Expanded(
            child: StatCard(
              label: 'Firebase',
              value: FirebaseBridge.available ? 'Connected' : 'Not configured',
              icon: Icons.cloud_outlined,
            ),
          ),
        ],
      ),
      const SizedBox(height: 20),
      const Text(
        'Demo checklist',
        style: TextStyle(fontSize: 18, fontWeight: FontWeight.bold),
      ),
      const ChecklistItem(text: 'Create a trip through the .NET API'),
      const ChecklistItem(text: 'Trigger SOS or crash countdown'),
      const ChecklistItem(text: 'Add a journal note'),
    ],
  );
}
