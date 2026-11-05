part of '../../main.dart';

class JournalPage extends StatefulWidget {
  const JournalPage({required this.api, required this.tripId, super.key});
  final RoadMateApi api;
  final String? tripId;

  @override
  State<JournalPage> createState() => _JournalPageState();
}

class _JournalPageState extends State<JournalPage> {
  final note = TextEditingController();
  late Future<Map<String, dynamic>> journal;
  bool uploading = false;

  @override
  void initState() {
    super.initState();
    journal = _loadJournal();
  }

  @override
  void didUpdateWidget(covariant JournalPage oldWidget) {
    super.didUpdateWidget(oldWidget);
    if (oldWidget.tripId != widget.tripId) {
      setState(() => journal = _loadJournal());
    }
  }

  Future<Map<String, dynamic>> _loadJournal() {
    final tripId = widget.tripId;
    if (tripId == null) return Future.value(<String, dynamic>{});
    return widget.api.getJournal(tripId);
  }

  Future<void> saveNote() async {
    if (widget.tripId == null || note.text.trim().isEmpty) return;
    try {
      await widget.api.addMemory(widget.tripId!, note.text.trim());
      note.clear();
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Journal note saved')));
        setState(() => journal = _loadJournal());
      }
    } catch (e) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$e')));
      }
    }
  }

  @override
  Widget build(BuildContext context) {
    if (widget.tripId == null) {
      return const EmptyState(text: 'Create a trip to build its journal.');
    }
    return ListView(
      padding: const EdgeInsets.all(20),
      children: [
        const Text(
          'Trip journal',
          style: TextStyle(fontSize: 28, fontWeight: FontWeight.bold),
        ),
        const SizedBox(height: 16),
        TextField(
          controller: note,
          maxLines: 4,
          decoration: const InputDecoration(
            labelText: 'Quick note',
            border: OutlineInputBorder(),
          ),
        ),
        const SizedBox(height: 10),
        FilledButton.icon(
          onPressed: saveNote,
          icon: const Icon(Icons.save_outlined),
          label: const Text('Save note'),
        ),
        const SizedBox(height: 8),
        OutlinedButton.icon(
          onPressed: uploading ? null : addPhoto,
          icon: const Icon(Icons.camera_alt_outlined),
          label: Text(uploading ? 'Uploading...' : 'Add photo memory'),
        ),
        const SizedBox(height: 20),
        FutureBuilder<Map<String, dynamic>>(
          future: journal,
          builder: (context, snapshot) {
            if (snapshot.connectionState == ConnectionState.waiting) {
              return const CircularProgressIndicator();
            }
            if (snapshot.hasError) {
              return ErrorBanner(snapshot.error.toString());
            }
            final days = (snapshot.data?['days'] as List? ?? []);
            if (days.isEmpty) {
              return const EmptyState(text: 'Your memories will appear here.');
            }
            return Column(
              children: days
                  .map(
                    (day) => Card(
                      child: ListTile(
                        title: Text(day['date'].toString()),
                        subtitle: Text(
                          '${(day['entries'] as List? ?? []).length} memories',
                        ),
                      ),
                    ),
                  )
                  .toList(),
            );
          },
        ),
      ],
    );
  }

  Future<void> addPhoto() async {
    final tripId = widget.tripId;
    if (tripId == null) return;
    final image = await ImagePicker().pickImage(source: ImageSource.camera);
    if (image == null) return;
    setState(() => uploading = true);
    try {
      final position = await _currentPosition();
      final ref = FirebaseStorage.instance.ref(
        'users/${FirebaseAuth.instance.currentUser!.uid}/trips/$tripId/${DateTime.now().millisecondsSinceEpoch}.jpg',
      );
      await ref.putFile(File(image.path));
      final url = await ref.getDownloadURL();
      await widget.api.addMemoryMedia(
        tripId,
        mediaUrl: url,
        lat: position.latitude,
        lng: position.longitude,
      );
      if (mounted) {
        setState(() => journal = _loadJournal());
        ScaffoldMessenger.of(context)
            .showSnackBar(const SnackBar(content: Text('Photo memory saved')));
      }
    } catch (exception) {
      if (mounted) {
        ScaffoldMessenger.of(context)
            .showSnackBar(SnackBar(content: Text('$exception')));
      }
    } finally {
      if (mounted) setState(() => uploading = false);
    }
  }

  Future<Position> _currentPosition() async {
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      throw Exception('Location permission is required for photo memories.');
    }
    return Geolocator.getCurrentPosition();
  }

  @override
  void dispose() {
    note.dispose();
    super.dispose();
  }
}
