part of '../../main.dart';

/// Gets a GPS fix, asking for permission when needed.
Future<Position> currentPositionWithPermission() async {
  if (!await Geolocator.isLocationServiceEnabled()) {
    throw Exception('Hãy bật dịch vụ vị trí (GPS).');
  }
  var permission = await Geolocator.checkPermission();
  if (permission == LocationPermission.denied) {
    permission = await Geolocator.requestPermission();
  }
  if (permission == LocationPermission.deniedForever) {
    await Geolocator.openAppSettings();
    throw Exception('Quyền vị trí bị từ chối. Hãy bật lại trong Cài đặt.');
  }
  if (permission == LocationPermission.denied) {
    throw Exception('RoadMate cần quyền vị trí để lập chuyến đi.');
  }
  return Geolocator.getCurrentPosition(
    locationSettings: const LocationSettings(
      accuracy: LocationAccuracy.high,
      timeLimit: Duration(seconds: 15),
    ),
  );
}

/// RM/08 – returns `{name, lat, lng}` or null.
class PlaceSearchPage extends StatefulWidget {
  const PlaceSearchPage({required this.api, required this.title, super.key});
  final RoadMateApi api;
  final String title;

  static Future<Map<String, dynamic>?> pick(
    BuildContext context,
    RoadMateApi api,
    String title,
  ) => Navigator.push<Map<String, dynamic>>(
    context,
    MaterialPageRoute(
      builder: (_) => PlaceSearchPage(api: api, title: title),
    ),
  );

  @override
  State<PlaceSearchPage> createState() => _PlaceSearchPageState();
}

class _PlaceSearchPageState extends State<PlaceSearchPage> {
  static const _recentKey = 'recent_places';
  final query = TextEditingController();
  Timer? _debounce;
  List<Map<String, dynamic>> results = [];
  List<Map<String, dynamic>> recent = [];
  Position? here;
  bool searching = false;
  bool resolving = false;
  String? error;

  @override
  void initState() {
    super.initState();
    unawaited(_loadRecent());
    unawaited(
      Geolocator.getLastKnownPosition()
          .then((value) {
            if (mounted) setState(() => here = value);
          })
          .catchError((Object _) {}),
    );
  }

  Future<void> _loadRecent() async {
    final prefs = await SharedPreferences.getInstance();
    final saved = prefs.getStringList(_recentKey) ?? [];
    if (!mounted) return;
    setState(
      () => recent = saved
          .map((item) => Map<String, dynamic>.from(jsonDecode(item) as Map))
          .toList(),
    );
  }

  Future<void> _finish(Map<String, dynamic> place) async {
    final prefs = await SharedPreferences.getInstance();
    final list = [
      place,
      ...recent.where((item) => item['name'] != place['name']),
    ].take(6);
    await prefs.setStringList(_recentKey, list.map(jsonEncode).toList());
    if (mounted) Navigator.pop(context, place);
  }

  void _onChanged(String value) {
    _debounce?.cancel();
    if (value.trim().length < 2) {
      setState(() => results = []);
      return;
    }
    _debounce = Timer(const Duration(milliseconds: 350), () => _search(value));
  }

  Future<void> _search(String value) async {
    setState(() {
      searching = true;
      error = null;
    });
    try {
      final found = await widget.api.autocomplete(
        value.trim(),
        lat: here?.latitude,
        lng: here?.longitude,
      );
      if (mounted && query.text == value) setState(() => results = found);
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => searching = false);
    }
  }

  Future<void> _choosePrediction(Map<String, dynamic> prediction) async {
    setState(() => resolving = true);
    try {
      final detail = await widget.api.getPlace(prediction['placeId'] as String);
      await _finish({
        'name':
            prediction['mainText'] ??
            detail['name'] ??
            prediction['description'],
        'address': detail['address'] ?? prediction['secondaryText'],
        'lat': asDouble(detail['lat']),
        'lng': asDouble(detail['lng']),
      });
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => resolving = false);
    }
  }

  Future<void> _useCurrentLocation() async {
    setState(() {
      resolving = true;
      error = null;
    });
    try {
      final position = await currentPositionWithPermission();
      await _finish(
        await _namedPoint(
          position.latitude,
          position.longitude,
          'Vị trí hiện tại',
        ),
      );
    } on Object catch (exception) {
      if (mounted) setState(() => error = '$exception');
    } finally {
      if (mounted) setState(() => resolving = false);
    }
  }

  Future<Map<String, dynamic>> _namedPoint(
    double lat,
    double lng,
    String fallback,
  ) async {
    var name = fallback;
    try {
      final reverse = await widget.api.reverseGeocode(lat, lng);
      name = (reverse['name'] ?? reverse['address'] ?? fallback).toString();
    } on Object {
      // Reverse geocoding is optional (needs the Goong key).
    }
    return {'name': name, 'lat': lat, 'lng': lng};
  }

  Future<void> _chooseOnMap() async {
    final point = await Navigator.push<LatLng>(
      context,
      MaterialPageRoute(
        builder: (_) => _MapPickerPage(
          initial: here == null
              ? defaultMapCenter
              : LatLng(here!.latitude, here!.longitude),
        ),
      ),
    );
    if (point == null) return;
    setState(() => resolving = true);
    try {
      await _finish(
        await _namedPoint(point.latitude, point.longitude, 'Điểm đã chọn'),
      );
    } finally {
      if (mounted) setState(() => resolving = false);
    }
  }

  @override
  void dispose() {
    _debounce?.cancel();
    query.dispose();
    super.dispose();
  }

  Widget _row({
    required IconData icon,
    required String title,
    String? subtitle,
    String? trailing,
    required VoidCallback onTap,
  }) => ListTile(
    contentPadding: EdgeInsets.zero,
    onTap: resolving ? null : onTap,
    leading: CircleAvatar(
      backgroundColor: RoadMateColors.primarySoft,
      child: Icon(icon, color: RoadMateColors.primaryDeep, size: 20),
    ),
    title: Text(title, style: const TextStyle(fontWeight: FontWeight.w700)),
    subtitle: subtitle == null
        ? null
        : Text(subtitle, maxLines: 1, overflow: TextOverflow.ellipsis),
    trailing: trailing == null
        ? null
        : Text(trailing, style: const TextStyle(color: RoadMateColors.muted)),
  );

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(
      titleSpacing: 0,
      title: TextField(
        controller: query,
        autofocus: true,
        onChanged: _onChanged,
        textInputAction: TextInputAction.search,
        onSubmitted: _search,
        decoration: InputDecoration(
          hintText: widget.title,
          prefixIcon: const Icon(Icons.search),
          suffixIcon: query.text.isEmpty
              ? null
              : IconButton(
                  icon: const Icon(Icons.close),
                  onPressed: () {
                    query.clear();
                    setState(() => results = []);
                  },
                ),
        ),
      ),
    ),
    body: ListView(
      padding: const EdgeInsets.symmetric(horizontal: 20),
      children: [
        if (resolving || searching) const LinearProgressIndicator(),
        const SizedBox(height: 8),
        Wrap(
          spacing: 8,
          children: [
            ActionChip(
              avatar: const Icon(Icons.my_location, size: 16),
              label: const Text('Vị trí hiện tại'),
              onPressed: resolving ? null : _useCurrentLocation,
            ),
            ActionChip(
              avatar: const Icon(Icons.map_outlined, size: 16),
              label: const Text('Chọn trên bản đồ'),
              onPressed: resolving ? null : _chooseOnMap,
            ),
          ],
        ),
        if (error != null) ErrorBanner(error!),
        if (results.isNotEmpty) const SectionLabel('Kết quả'),
        for (final item in results)
          _row(
            icon: Icons.place_outlined,
            title: (item['mainText'] ?? item['description']).toString(),
            subtitle: item['secondaryText']?.toString(),
            trailing: item['distanceMeters'] == null
                ? null
                : formatDistance(item['distanceMeters'] as num),
            onTap: () => _choosePrediction(item),
          ),
        if (results.isEmpty && recent.isNotEmpty) ...[
          const SectionLabel('Gần đây'),
          for (final item in recent)
            _row(
              icon: Icons.history,
              title: item['name'].toString(),
              subtitle: item['address']?.toString(),
              onTap: () => _finish(item),
            ),
        ],
      ],
    ),
  );
}

class _MapPickerPage extends StatefulWidget {
  const _MapPickerPage({required this.initial});
  final LatLng initial;

  @override
  State<_MapPickerPage> createState() => _MapPickerPageState();
}

class _MapPickerPageState extends State<_MapPickerPage> {
  late LatLng center = widget.initial;

  @override
  Widget build(BuildContext context) => Scaffold(
    appBar: AppBar(title: const Text('Chọn trên bản đồ')),
    body: Stack(
      children: [
        FlutterMap(
          options: MapOptions(
            initialCenter: widget.initial,
            initialZoom: 15,
            onPositionChanged: (camera, _) => center = camera.center,
          ),
          children: [
            TileLayer(
              urlTemplate: 'https://tile.openstreetmap.org/{z}/{x}/{y}.png',
              userAgentPackageName: 'com.example.roadmate',
            ),
          ],
        ),
        const IgnorePointer(
          child: Center(
            child: Padding(
              padding: EdgeInsets.only(bottom: 40),
              child: Icon(
                Icons.location_on,
                size: 48,
                color: RoadMateColors.coral,
              ),
            ),
          ),
        ),
        Positioned(
          left: 20,
          right: 20,
          bottom: 24,
          child: SafeArea(
            child: FilledButton(
              onPressed: () => Navigator.pop(context, center),
              child: const Text('Chọn điểm này'),
            ),
          ),
        ),
      ],
    ),
  );
}
