part of '../main.dart';

class TripTracker {
  TripTracker({required this.api, required this.tripId, required this.onError});

  final RoadMateApi api;
  final String tripId;
  final ValueChanged<Object> onError;
  StreamSubscription<Position>? _subscription;
  Timer? _heartbeatTimer;
  final List<Map<String, dynamic>> _pending = [];
  bool _flushInProgress = false;
  SharedPreferences? _prefs;
  Position? latest;
  bool running = false;

  Future<void> start() async {
    if (running) return;
    if (!await Geolocator.isLocationServiceEnabled()) {
      throw Exception('Location services are disabled.');
    }
    var permission = await Geolocator.checkPermission();
    if (permission == LocationPermission.denied) {
      permission = await Geolocator.requestPermission();
    }
    if (permission == LocationPermission.denied ||
        permission == LocationPermission.deniedForever) {
      throw Exception('Location permission is required for trip tracking.');
    }
    _prefs ??= await SharedPreferences.getInstance();
    final saved = _prefs!.getStringList(_storageKey);
    if (saved != null) {
      _pending
        ..clear()
        ..addAll(
          saved.map(
            (item) => Map<String, dynamic>.from(jsonDecode(item) as Map),
          ),
        );
    }
    running = true;
    _subscription = Geolocator.getPositionStream(
      locationSettings: const LocationSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 25,
      ),
    ).listen(_record);
    _heartbeatTimer = Timer.periodic(
      const Duration(minutes: 1),
      (_) => flush(),
    );
    if (_pending.isNotEmpty) {
      unawaited(flush());
    }
  }

  Future<void> _record(Position position) async {
    latest = position;
    final battery = await Battery().batteryLevel;
    _pending.add({
      'lat': position.latitude,
      'lng': position.longitude,
      'speed': position.speed * 3.6,
      'altitude': position.altitude,
      'battery': battery,
      'recordedAt': DateTime.now().toUtc().toIso8601String(),
    });
    await _persistPending();
    if (_pending.length >= 5) await flush();
  }

  Future<void> flush() async {
    if (_pending.isEmpty || _flushInProgress) return;
    _flushInProgress = true;
    final network = (await Connectivity().checkConnectivity()).isNotEmpty;
    try {
      await api.addPoints(tripId, List<Map<String, dynamic>>.from(_pending));
      if (latest != null) {
        final battery = await Battery().batteryLevel;
        await api.heartbeat(
          tripId,
          lat: latest!.latitude,
          lng: latest!.longitude,
          speed: latest!.speed * 3.6,
          battery: battery,
          networkOk: network,
        );
      }
      _pending.clear();
      await _persistPending();
    } catch (exception) {
      onError(exception);
    } finally {
      _flushInProgress = false;
    }
  }

  String get _storageKey => 'trip_pending_points_$tripId';

  Future<void> _persistPending() async {
    _prefs ??= await SharedPreferences.getInstance();
    await _prefs!.setStringList(_storageKey, _pending.map(jsonEncode).toList());
  }

  Future<void> stop() async {
    await flush();
    await _subscription?.cancel();
    _heartbeatTimer?.cancel();
    _subscription = null;
    _heartbeatTimer = null;
    running = false;
  }
}
