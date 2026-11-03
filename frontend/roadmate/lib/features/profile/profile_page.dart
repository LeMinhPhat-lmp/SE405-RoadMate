part of '../../main.dart';

class ProfilePage extends StatefulWidget {
  const ProfilePage({required this.api, super.key});
  final RoadMateApi api;

  @override
  State<ProfilePage> createState() => _ProfilePageState();
}

class _ProfilePageState extends State<ProfilePage> {
  late Future<Map<String, dynamic>> profile;

  @override
  void initState() {
    super.initState();
    profile = widget.api.getMe();
  }

  @override
  Widget build(BuildContext context) => ListView(
    padding: const EdgeInsets.all(20),
    children: [
      const Text(
        'Profile',
        style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold),
      ),
      const SizedBox(height: 16),
      FutureBuilder<Map<String, dynamic>>(
        future: profile,
        builder: (context, snapshot) {
          if (snapshot.connectionState == ConnectionState.waiting) {
            return const CircularProgressIndicator();
          }
          if (snapshot.hasError) return ErrorBanner(snapshot.error.toString());
          final value = snapshot.data ?? {};
          return Card(
            child: Padding(
              padding: const EdgeInsets.all(18),
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text(
                    value['name'] as String? ?? 'RoadMate rider',
                    style: const TextStyle(
                      fontSize: 22,
                      fontWeight: FontWeight.bold,
                    ),
                  ),
                  Text(value['email'] as String? ?? 'Development account'),
                  const Divider(height: 28),
                  Text('Vehicle: ${value['vehicleType'] ?? 'Not set'}'),
                  Text('Plate: ${value['plateNumber'] ?? 'Not set'}'),
                  Text(
                    'Crash sensitivity: ${value['crashSensitivity'] ?? 'normal'}',
                  ),
                ],
              ),
            ),
          );
        },
      ),
      const SizedBox(height: 18),
      FilledButton.icon(
        onPressed: () => showDialog<void>(
          context: context,
          builder: (_) => CrashDialog(api: widget.api),
        ),
        icon: const Icon(Icons.warning_amber_rounded),
        label: const Text('Demo crash alert'),
      ),
      OutlinedButton.icon(
        onPressed: () => showDialog<void>(
          context: context,
          builder: (_) => SosDialog(api: widget.api),
        ),
        icon: const Icon(Icons.emergency),
        label: const Text('Demo SOS'),
      ),
    ],
  );
}

class CrashDialog extends StatefulWidget {
  const CrashDialog({required this.api, super.key});
  final RoadMateApi api;

  @override
  State<CrashDialog> createState() => _CrashDialogState();
}

class _CrashDialogState extends State<CrashDialog> {
  int seconds = 30;
  Timer? timer;

  @override
  void initState() {
    super.initState();
    timer = Timer.periodic(const Duration(seconds: 1), (_) {
      if (seconds <= 1) {
        timer?.cancel();
        if (mounted) setState(() => seconds = 0);
      } else if (mounted) {
        setState(() => seconds--);
      }
    });
  }

  Future<void> raiseAlert() async {
    try {
      final trips = await widget.api.getTrips();
      final activeTrips = trips
          .where((trip) => trip['status'] == 'active')
          .toList();
      if (activeTrips.isNotEmpty) {
        await widget.api.createAlert(
          activeTrips.first['id'] as String,
          'crash',
        );
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    }
    if (mounted) Navigator.pop(context);
  }

  @override
  void dispose() {
    timer?.cancel();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    icon: const Icon(Icons.warning, color: Colors.red, size: 44),
    title: const Text('Are you okay?'),
    content: Text(
      seconds == 0
          ? 'Alert escalation would start now.'
          : 'Possible crash detected. $seconds seconds remaining.',
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('I am okay'),
      ),
      FilledButton(onPressed: raiseAlert, child: const Text('Need help')),
    ],
  );
}

class SosDialog extends StatelessWidget {
  const SosDialog({required this.api, super.key});
  final RoadMateApi api;

  Future<void> raiseAlert(BuildContext context) async {
    try {
      final trips = await api.getTrips();
      final activeTrips = trips
          .where((trip) => trip['status'] == 'active')
          .toList();
      if (activeTrips.isEmpty) {
        if (context.mounted) {
          ScaffoldMessenger.of(context).showSnackBar(
            const SnackBar(content: Text('Start a trip before sending SOS')),
          );
        }
        return;
      }
      await api.createAlert(activeTrips.first['id'] as String, 'sos');
      if (context.mounted) Navigator.pop(context);
    } catch (e) {
      if (context.mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) => AlertDialog(
    icon: const Icon(Icons.emergency, color: Colors.red, size: 44),
    title: const Text('SOS ready'),
    content: const Text(
      'This creates an SOS alert through the .NET API and escalates to configured contacts.',
    ),
    actions: [
      TextButton(
        onPressed: () => Navigator.pop(context),
        child: const Text('Cancel'),
      ),
      FilledButton(
        onPressed: () => raiseAlert(context),
        child: const Text('Send SOS'),
      ),
    ],
  );
}
