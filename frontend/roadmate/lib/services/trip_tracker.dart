part of '../main.dart';

class TripTracker {
  TripTracker({
    required this.api,
    required this.tripId,
    required this.onError,
    this.onPosition,
  });

  final RoadMateApi api;
  final String tripId;
  final ValueChanged<Object> onError;
  final ValueChanged<Position>? onPosition;
  final Battery _battery = Battery();
  int? batteryLevel;
  DateTime? _batteryReadAt;

  /// Debug Simulator: pretend there is no network until this time. Points
  /// keep being stored locally and sync once it passes.
  DateTime? offlineUntil;
  bool get simulatedOffline =>
      offlineUntil != null && DateTime.now().isBefore(offlineUntil!);
  int get pendingCount => _pending.length;
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
      locationSettings: _locationSettings(),
    ).listen(_record, onError: onError);
    _heartbeatTimer = Timer.periodic(
      const Duration(minutes: 1),
      (_) => flush(),
    );
    if (_pending.isNotEmpty) {
      unawaited(flush());
    }
  }

  /// Android keeps a foreground service (persistent notification) so GPS is
  /// still recorded while the screen is locked.
  static LocationSettings _locationSettings() {
    if (Platform.isAndroid) {
      return AndroidSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 5,
        intervalDuration: const Duration(seconds: 3),
        foregroundNotificationConfig: const ForegroundNotificationConfig(
          notificationTitle: 'RoadMate đang bảo vệ chuyến đi',
          notificationText: 'Đang ghi lộ trình và theo dõi an toàn',
          notificationChannelName: 'Theo dõi chuyến đi',
          enableWakeLock: true,
          setOngoing: true,
        ),
      );
    }
    if (Platform.isIOS) {
      return AppleSettings(
        accuracy: LocationAccuracy.high,
        distanceFilter: 5,
        activityType: ActivityType.automotiveNavigation,
        pauseLocationUpdatesAutomatically: false,
        allowBackgroundLocationUpdates: true,
        showBackgroundLocationIndicator: true,
      );
    }
    return const LocationSettings(
      accuracy: LocationAccuracy.high,
      distanceFilter: 5,
    );
  }

  Future<int?> readBattery() async {
    final readAt = _batteryReadAt;
    if (readAt != null &&
        DateTime.now().difference(readAt) < const Duration(minutes: 1)) {
      return batteryLevel;
    }
    try {
      batteryLevel = await _battery.batteryLevel;
      _batteryReadAt = DateTime.now();
    } on Object {
      // Battery level is optional.
    }
    return batteryLevel;
  }

  Future<void> _record(Position position) async {
    latest = position;
    onPosition?.call(position);
    final battery = await readBattery();
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
    if (simulatedOffline) return;
    _flushInProgress = true;
    final connectivity = await Connectivity().checkConnectivity();
    final network = !connectivity.contains(ConnectivityResult.none);
    if (!network) {
      _flushInProgress = false;
      return;
    }
    try {
      final batch = List<Map<String, dynamic>>.from(_pending);
      await api.addPoints(tripId, batch);
      // Points recorded while uploading stay queued for the next flush.
      _pending.removeRange(0, math.min(batch.length, _pending.length));
      await _persistPending();
      if (latest != null) {
        final battery = await readBattery();
        await api.heartbeat(
          tripId,
          lat: latest!.latitude,
          lng: latest!.longitude,
          speed: latest!.speed * 3.6,
          battery: battery,
          networkOk: network,
        );
      }
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
