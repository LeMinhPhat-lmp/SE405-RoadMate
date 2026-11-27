part of '../../main.dart';

/// Quick capture (RM/23): photo or note, automatically tagged with the
/// current position and time.
class MemoryCapture {
  static Future<LatLng?> _point(TripRuntime? runtime) async {
    final latest = runtime?.latestPoint;
    if (latest != null) return latest;
    try {
      final position = await currentPositionWithPermission();
      return LatLng(position.latitude, position.longitude);
    } on Object {
      return null;
    }
  }

  static void _snack(ScaffoldMessengerState messenger, String text) =>
      messenger.showSnackBar(SnackBar(content: Text(text)));

  /// Returns true when a photo memory was saved.
  static Future<bool> photo(
    BuildContext context,
    RoadMateApi api,
    String tripId,
    TripRuntime? runtime, {
    ImageSource source = ImageSource.camera,
  }) async {
    final messenger = ScaffoldMessenger.of(context);
    final image = await ImagePicker().pickImage(
      source: source,
      maxWidth: 1920,
      imageQuality: 80,
    );
    if (image == null) return false;
    _snack(messenger, 'Đang lưu ảnh vào nhật ký…');
    try {
      final point = await _point(runtime);
      final bytes = await image.readAsBytes();
      final name = image.name.toLowerCase();
      await api.addPhotoMemory(
        tripId,
        bytes: bytes,
        contentType: name.endsWith('.png')
            ? 'image/png'
            : name.endsWith('.webp')
            ? 'image/webp'
            : 'image/jpeg',
        lat: point?.latitude,
        lng: point?.longitude,
      );
      _snack(messenger, 'Đã lưu ảnh kỷ niệm');
      return true;
    } on Object catch (exception) {
      _snack(messenger, 'Không lưu được ảnh: $exception');
      return false;
    }
  }

  /// Returns true when a note was saved.
  static Future<bool> note(
    BuildContext context,
    RoadMateApi api,
    String tripId,
    TripRuntime? runtime,
  ) async {
    final messenger = ScaffoldMessenger.of(context);
    final result = await showModalBottomSheet<(String, String?)>(
      context: context,
      isScrollControlled: true,
      builder: (context) => const _NoteSheet(),
    );
    if (result == null || result.$1.trim().isEmpty) return false;
    try {
      final point = await _point(runtime);
      await api.addMemory(
        tripId,
        result.$1.trim(),
        lat: point?.latitude,
        lng: point?.longitude,
        mood: result.$2,
      );
      _snack(messenger, 'Đã lưu ghi chú');
      return true;
    } on Object catch (exception) {
      _snack(messenger, 'Không lưu được ghi chú: $exception');
      return false;
    }
  }
}

const moodLabels = {
  'happy': '😄 Vui vẻ',
  'chill': '😌 Thư giãn',
  'tired': '🥱 Mệt',
  'wow': '🤩 Tuyệt đẹp',
};

class _NoteSheet extends StatefulWidget {
  const _NoteSheet();

  @override
  State<_NoteSheet> createState() => _NoteSheetState();
}

class _NoteSheetState extends State<_NoteSheet> {
  final text = TextEditingController();
  String? mood;

  @override
  void dispose() {
    text.dispose();
    super.dispose();
  }

  @override
  Widget build(BuildContext context) => Padding(
    padding: EdgeInsets.fromLTRB(
      20,
      20,
      20,
      MediaQuery.viewInsetsOf(context).bottom + 20,
    ),
    child: Column(
      mainAxisSize: MainAxisSize.min,
      crossAxisAlignment: CrossAxisAlignment.stretch,
      children: [
        const Text(
          'Ghi chú nhanh',
          style: TextStyle(fontSize: 20, fontWeight: FontWeight.w800),
        ),
        const Text(
          'Tự động gắn vị trí & thời gian vào nhật ký',
          style: TextStyle(color: RoadMateColors.muted),
        ),
        const SizedBox(height: 12),
        TextField(
          controller: text,
          autofocus: true,
          maxLines: 4,
          decoration: const InputDecoration(hintText: 'Bạn đang nghĩ gì?'),
        ),
        const SizedBox(height: 10),
        Wrap(
          spacing: 8,
          children: [
            for (final entry in moodLabels.entries)
              ChoiceChip(
                label: Text(entry.value),
                selected: mood == entry.key,
                onSelected: (selected) =>
                    setState(() => mood = selected ? entry.key : null),
              ),
          ],
        ),
        const SizedBox(height: 12),
        FilledButton(
          onPressed: () => Navigator.pop(context, (text.text, mood)),
          child: const Text('Lưu ghi chú'),
        ),
      ],
    ),
  );
}
