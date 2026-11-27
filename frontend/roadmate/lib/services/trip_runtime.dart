part of '../main.dart';

enum SafetyEventType { crash, idle, restEnded }

class SafetyEvent {
  const SafetyEvent(this.type, {this.crash, this.stoppedFor});
  final SafetyEventType type;
  final CrashEvent? crash;
  final Duration? stoppedFor;
}

/// Everything that runs while a trip is active: GPS recording, crash and
/// idle detection, emergency alerts, navigation progress and live stats.
/// Safety logic runs on the device so it keeps working offline.
class TripRuntime extends ChangeNotifier with WidgetsBindingObserver {
  TripRuntime(this.api) {
    WidgetsBinding.instance.addObserver(this);
    alerts = SafetyAlertController(
      api: api,
      positionProvider: _currentPosition,
      batteryProvider: () async => _tracker?.readBattery(),
      onLog: log,
    )..addListener(notifyListeners);
    crashDetector = CrashDetector(onLog: log);
  }

  final RoadMateApi api;
  TripTracker? _tracker;
  String? activeTripId;
  String? lastError;

  // Trip & route
  Map<String, dynamic>? trip;
  List<LatLng> routePoints = const [];
  List<double> _routeCumulative = const [];
  int _routeIndex = 0;
  List<Map<String, dynamic>> steps = const [];
  int stepIndex = 0;
  final List<LatLng> traveled = [];

  // Live stats
  DateTime? startedAt;
  double distanceMeters = 0;
  int movingSeconds = 0;
  int restSeconds = 0;
  double maxSpeedKmh = 0;
  double currentSpeedKmh = 0;
  double? headingDegrees;
  DateTime? _lastSampleAt;
  LatLng? _lastPoint;

  // Safety
  late final SafetyAlertController alerts;
  late final CrashDetector crashDetector;
  final IdleDetector idleDetector = IdleDetector();
  SafetyProfile profile = const SafetyProfile();
  StreamSubscription<AccelerometerEvent>? _accelerometer;
  Timer? _ticker;
  final StreamController<SafetyEvent> _events =
      StreamController<SafetyEvent>.broadcast();
  Stream<SafetyEvent> get events => _events.stream;
  bool _crashPromptOpen = false;
  bool _idlePromptOpen = false;

  // Voice guidance
  final FlutterTts _tts = FlutterTts();
  bool voiceEnabled = true;
  int _spokenStep = -1;
  bool _spokenNear = false;

  // Debug Simulator
  double? simulatedSpeedKmh;
  final List<String> logs = [];

  bool get isTracking => _tracker?.running == true;
  Position? get latestPosition => _tracker?.latest;
  LatLng? get latestPoint {
    final position = latestPosition;
    return position == null
        ? null
        : LatLng(position.latitude, position.longitude);
  }

  int? get batteryLevel => _tracker?.batteryLevel;
  int get pendingPoints => _tracker?.pendingCount ?? 0;
  bool get simulatedOffline => _tracker?.simulatedOffline ?? false;

  List<String> get alertContactIds =>
      ((trip?['alertContactIds'] as List?) ?? const [])
          .map((id) => id.toString())
          .toList();

  List<Map<String, dynamic>> get recipients =>
      profile.recipients(alertContactIds);

  DateTime? get restUntil => idleDetector.restUntil;
  bool get resting => idleDetector.isResting(DateTime.now());

  double get averageSpeedKmh =>
      movingSeconds == 0 ? 0 : distanceMeters / movingSeconds * 3.6;

  void log(String message) {
    final now = DateTime.now();
    final stamp =
        '${formatClock(now)}:${now.second.toString().padLeft(2, '0')}';
    logs.insert(0, '$stamp $message');
    if (logs.length > 60) logs.removeLast();
    debugPrint('RoadMate safety: $message');
  }

  Future<void> restore() async {
    unawaited(_loadVoicePreference());
    await alerts.restore();
    final prefs = await SharedPreferences.getInstance();
    final tripId = prefs.getString('active_trip_id');
    if (tripId == null || isTracking) return;
    try {
      await startTracking(tripId);
    } catch (exception) {
      lastError = exception.toString();
      await prefs.remove('active_trip_id');
      notifyListeners();
    }
  }

  Future<void> _loadVoicePreference() async {
    final prefs = await SharedPreferences.getInstance();
    voiceEnabled = prefs.getBool('voice_guidance') ?? true;
    try {
      await _tts.setLanguage('vi-VN');
      await _tts.setSpeechRate(0.5);
    } on Object {
      // TTS engine without Vietnamese: keep default voice.
    }
  }

  Future<void> setVoiceEnabled(bool value) async {
    voiceEnabled = value;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setBool('voice_guidance', value);
    if (!value) await _tts.stop();
    notifyListeners();
  }

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.inactive) {
      unawaited(_tracker?.flush());
    }
  }

  /// Starts recording and safety monitoring for [tripId]. [tripData] avoids
  /// an extra request when the caller already has the trip.
  Future<void> startTracking(
    String tripId, {
    Map<String, dynamic>? tripData,
  }) async {
    await _tracker?.stop();
    final tracker = TripTracker(
      api: api,
      tripId: tripId,
      onPosition: _onPosition,
      onError: (exception) {
        lastError = exception.toString();
        notifyListeners();
      },
    );
    await tracker.start();
    _tracker = tracker;
    activeTripId = tripId;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString('active_trip_id', tripId);
    _restoreStats(prefs, tripId);
    lastError = null;
    _startSafetyMonitoring();
    notifyListeners();
    unawaited(_loadTripContext(tripId, tripData));
  }

  Future<void> _loadTripContext(
    String tripId,
    Map<String, dynamic>? tripData,
  ) async {
    final prefs = await SharedPreferences.getInstance();
    try {
      trip = tripData ?? await api.getTrip(tripId);
      await prefs.setString('active_trip_$tripId', jsonEncode(trip));
    } on Object {
      final cached = prefs.getString('active_trip_$tripId');
      if (cached != null) {
        trip = Map<String, dynamic>.from(jsonDecode(cached) as Map);
      }
    }
    profile = await SafetyProfile.load(api);
    final current = trip;
    if (current != null) {
      routePoints = decodePolyline(current['plannedPolyline'] as String?);
      _computeRouteCumulative();
      idleDetector.idleMinutes =
          (current['idleMinutes'] as num?)?.toInt() ??
          (profile.me['idleMinutes'] as num?)?.toInt() ??
          15;
      crashDetector.thresholdG = crashThresholdG(
        current['crashSensitivity'] as String? ??
            profile.me['crashSensitivity'] as String?,
      );
      idleDetector.plannedStops = ((current['waypoints'] as List?) ?? const [])
          .map((item) => Map<String, dynamic>.from(item as Map))
          .map(
            (waypoint) => PlannedStop(
              point: LatLng(
                asDouble(waypoint['lat'])!,
                asDouble(waypoint['lng'])!,
              ),
              stayMinutes: (waypoint['plannedStayMin'] as num?)?.toInt() ?? 30,
            ),
          )
          .toList();
      final restUntil = parseTime(current['restUntil']);
      if (restUntil != null && restUntil.isAfter(DateTime.now())) {
        idleDetector.restUntil = restUntil;
      }
      startedAt ??= parseTime(current['startTime']) ?? DateTime.now();
      notifyListeners();
      await _loadSteps(tripId, current);
    }
  }

  /// Turn-by-turn steps are not stored on the trip, so they are fetched once
  /// per trip and cached for offline use.
  Future<void> _loadSteps(String tripId, Map<String, dynamic> current) async {
    final prefs = await SharedPreferences.getInstance();
    final key = 'trip_steps_$tripId';
    final cached = prefs.getString(key);
    if (cached != null) {
      steps = (jsonDecode(cached) as List)
          .map((item) => Map<String, dynamic>.from(item as Map))
          .toList();
      notifyListeners();
      return;
    }
    try {
      final origin = Map<String, dynamic>.from(current['origin'] as Map);
      final destination = Map<String, dynamic>.from(
        current['destination'] as Map,
      );
      final waypoints = ((current['waypoints'] as List?) ?? const [])
          .map((item) => Map<String, dynamic>.from(item as Map))
          .map((w) => {'lat': asDouble(w['lat'])!, 'lng': asDouble(w['lng'])!})
          .toList();
      final route = await api.getDirections(
        originLat: asDouble(origin['lat'])!,
        originLng: asDouble(origin['lng'])!,
        destinationLat: asDouble(destination['lat'])!,
        destinationLng: asDouble(destination['lng'])!,
        waypoints: waypoints,
        avoidHighway: current['avoidHighway'] == true,
        avoidToll: current['avoidToll'] == true,
      );
      steps = ((route['steps'] as List?) ?? const [])
          .map((item) => Map<String, dynamic>.from(item as Map))
          .toList();
      if (routePoints.isEmpty) {
        routePoints = decodePolyline(route['polyline'] as String?);
        _computeRouteCumulative();
      }
      await prefs.setString(key, jsonEncode(steps));
      notifyListeners();
    } on Object catch (exception) {
      log('turn-by-turn unavailable: $exception');
    }
  }

  void _computeRouteCumulative() {
    final cumulative = <double>[];
    var total = 0.0;
    for (var i = 0; i < routePoints.length; i++) {
      if (i > 0) total += metersBetween(routePoints[i - 1], routePoints[i]);
      cumulative.add(total);
    }
    _routeCumulative = cumulative;
    _routeIndex = 0;
  }

  void _startSafetyMonitoring() {
    unawaited(_accelerometer?.cancel());
    _accelerometer = accelerometerEventStream(
      samplingPeriod: SensorInterval.gameInterval,
    ).listen(_onAcceleration, onError: (Object error) => log('$error'));
    _ticker?.cancel();
    _ticker = Timer.periodic(const Duration(seconds: 5), (_) => _tick());
  }

  void _stopSafetyMonitoring() {
    unawaited(_accelerometer?.cancel());
    _accelerometer = null;
    _ticker?.cancel();
    _ticker = null;
  }

  void _onAcceleration(AccelerometerEvent event) {
    crashDetector.enabled =
        isTracking && !resting && !alerts.active && !_crashPromptOpen;
    final crash = crashDetector.addAcceleration(
      event.x,
      event.y,
      event.z,
      DateTime.now(),
    );
    if (crash != null) _emitCrash(crash);
  }

  void _emitCrash(CrashEvent crash) {
    if (_crashPromptOpen || alerts.active) return;
    _crashPromptOpen = true;
    unawaited(SafetyAlarm.start());
    unawaited(
      NotificationService.show(
        1001,
        'Bạn có ổn không?',
        'RoadMate phát hiện va chạm mạnh. Mở app để xác nhận.',
      ),
    );
    _events.add(SafetyEvent(SafetyEventType.crash, crash: crash));
  }

  void _onPosition(Position position) {
    final now = DateTime.now();
    final speed = simulatedSpeedKmh ?? math.max(0, position.speed * 3.6);
    final point = LatLng(position.latitude, position.longitude);
    _accumulate(point, speed.toDouble(), now);
    if (position.heading >= 0 && speed > 3) headingDegrees = position.heading;
    _updateNavigation(point);
    notifyListeners();
  }

  void _accumulate(LatLng point, double speedKmh, DateTime now) {
    final last = _lastPoint;
    final lastAt = _lastSampleAt;
    if (lastAt != null) {
      final seconds = now.difference(lastAt).inSeconds.clamp(0, 30);
      if (speedKmh >= idleDetector.idleSpeedKmh) {
        movingSeconds += seconds;
      } else {
        restSeconds += seconds;
      }
    }
    if (last != null) {
      final delta = metersBetween(last, point);
      // Ignore GPS jitter while standing still.
      if (delta > 3 && speedKmh >= idleDetector.idleSpeedKmh) {
        distanceMeters += delta;
      }
    }
    if (last == null || metersBetween(last, point) > 3) traveled.add(point);
    _lastPoint = point;
    _lastSampleAt = now;
    currentSpeedKmh = speedKmh;
    if (speedKmh > maxSpeedKmh) maxSpeedKmh = speedKmh;
    crashDetector.addSpeed(speedKmh, now);
    idleDetector.update(speedKmh, now, point: point);
    unawaited(_persistStats());
  }

  void _tick() {
    final now = DateTime.now();
    if (simulatedSpeedKmh != null) {
      crashDetector.addSpeed(simulatedSpeedKmh!, now);
      idleDetector.update(simulatedSpeedKmh!, now);
    }
    final rest = idleDetector.restUntil;
    if (rest != null && !now.isBefore(rest)) {
      idleDetector.endRest(now);
      unawaited(SafetyAlarm.pulse());
      unawaited(
        NotificationService.show(
          1003,
          'Hết giờ nghỉ chân',
          'Sẵn sàng tiếp tục hành trình chưa?',
        ),
      );
      _events.add(const SafetyEvent(SafetyEventType.restEnded));
      notifyListeners();
    }
    if (!alerts.active && !_idlePromptOpen && idleDetector.check(now)) {
      _idlePromptOpen = true;
      unawaited(SafetyAlarm.pulse());
      final stopped = idleDetector.stoppedFor(now);
      unawaited(
        NotificationService.show(
          1002,
          'Bạn đã dừng ${stopped.inMinutes} phút, mọi thứ ổn chứ?',
          'Không phản hồi trong 3 phút, RoadMate sẽ báo người thân.',
        ),
      );
      log('idle ${stopped.inMinutes} min → ask rider');
      _events.add(SafetyEvent(SafetyEventType.idle, stoppedFor: stopped));
    }
  }

  // ---------- Navigation progress

  Map<String, dynamic>? get nextStep {
    final index = stepIndex + 1 < steps.length ? stepIndex + 1 : stepIndex;
    return index < steps.length ? steps[index] : null;
  }

  double? distanceToNextStep;

  double? get remainingMeters {
    if (_routeCumulative.isEmpty) return null;
    return _routeCumulative.last - _routeCumulative[_routeIndex];
  }

  DateTime? get eta {
    final remaining = remainingMeters;
    if (remaining == null) return null;
    final planned = asDouble(trip?['distanceKm']);
    final plannedMin = asDouble(trip?['etaMinutes']);
    double minutes;
    if (planned != null && planned > 0 && plannedMin != null) {
      minutes = plannedMin * (remaining / 1000) / planned;
    } else {
      minutes = remaining / 1000 / math.max(averageSpeedKmh, 30) * 60;
    }
    return DateTime.now().add(Duration(minutes: minutes.round()));
  }

  void _updateNavigation(LatLng point) {
    if (routePoints.isNotEmpty) {
      // Search forward along the route from the last matched vertex.
      var best = _routeIndex;
      var bestDistance = double.infinity;
      final end = math.min(routePoints.length, _routeIndex + 60);
      for (var i = _routeIndex; i < end; i++) {
        final d = metersBetween(point, routePoints[i]);
        if (d < bestDistance) {
          bestDistance = d;
          best = i;
        }
      }
      _routeIndex = best;
    }
    if (steps.isEmpty) return;
    final step = nextStep;
    final lat = asDouble(step?['lat']);
    final lng = asDouble(step?['lng']);
    if (lat == null || lng == null) {
      distanceToNextStep = null;
      return;
    }
    final distance = metersBetween(point, LatLng(lat, lng));
    distanceToNextStep = distance;
    if (distance < 25 && stepIndex + 1 < steps.length) {
      stepIndex++;
      _spokenNear = false;
      _speakCurrent();
    } else if (!_spokenNear && distance < 200) {
      _spokenNear = true;
      _speak('Sau ${formatDistance(distance)}, ${step!['instruction']}');
    } else if (_spokenStep < 0) {
      _speakCurrent();
    }
  }

  void _speakCurrent() {
    final step = nextStep;
    if (step == null) return;
    _spokenStep = stepIndex;
    _speak(step['instruction'].toString());
  }

  void _speak(String text) {
    if (!voiceEnabled) return;
    unawaited(_tts.speak(text).catchError((Object _) => null));
  }

  // ---------- Stats persistence (survives app restarts mid-trip)

  Future<void> _persistStats() async {
    final id = activeTripId;
    if (id == null) return;
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      'trip_stats_$id',
      jsonEncode({
        'distance': distanceMeters,
        'moving': movingSeconds,
        'rest': restSeconds,
        'max': maxSpeedKmh,
      }),
    );
  }

  void _restoreStats(SharedPreferences prefs, String tripId) {
    distanceMeters = 0;
    movingSeconds = 0;
    restSeconds = 0;
    maxSpeedKmh = 0;
    traveled.clear();
    stepIndex = 0;
    _spokenStep = -1;
    _lastPoint = null;
    _lastSampleAt = null;
    startedAt = null;
    final saved = prefs.getString('trip_stats_$tripId');
    if (saved == null) return;
    final json = Map<String, dynamic>.from(jsonDecode(saved) as Map);
    distanceMeters = asDouble(json['distance']) ?? 0;
    movingSeconds = (json['moving'] as num?)?.toInt() ?? 0;
    restSeconds = (json['rest'] as num?)?.toInt() ?? 0;
    maxSpeedKmh = asDouble(json['max']) ?? 0;
  }

  Future<void> stopTracking() async {
    final id = activeTripId;
    await _tracker?.stop();
    _tracker = null;
    _stopSafetyMonitoring();
    await _tts.stop();
    activeTripId = null;
    trip = null;
    routePoints = const [];
    steps = const [];
    idleDetector.restUntil = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('active_trip_id');
    if (id != null) {
      await prefs.remove('trip_stats_$id');
      await prefs.remove('trip_steps_$id');
      await prefs.remove('active_trip_$id');
    }
    notifyListeners();
  }

  // ---------- Rider actions

  Future<Position?> _currentPosition() async {
    final latest = latestPosition;
    if (latest != null &&
        DateTime.now().difference(latest.timestamp) <
            const Duration(seconds: 30)) {
      return latest;
    }
    try {
      return await Geolocator.getCurrentPosition(
        locationSettings: const LocationSettings(
          accuracy: LocationAccuracy.high,
          timeLimit: Duration(seconds: 8),
        ),
      );
    } on Object {
      return latest ?? await Geolocator.getLastKnownPosition();
    }
  }

  /// Sends an emergency alert of [type] (crash | idle | sos) to contacts.
  Future<void> raiseAlert(String type) async {
    final id = activeTripId;
    if (id == null) throw Exception('Hãy bắt đầu một chuyến đi trước.');
    _crashPromptOpen = false;
    _idlePromptOpen = false;
    await alerts.raise(id, type);
  }

  /// "Tôi ổn" on the crash countdown: false alarm, nothing is sent.
  Future<void> dismissCrash() async {
    _crashPromptOpen = false;
    crashDetector.reset();
    await SafetyAlarm.stop();
    log('user: "Tôi ổn" → false alarm logged');
    notifyListeners();
  }

  /// "Tôi ổn, đi tiếp" on the idle prompt.
  Future<void> acknowledgeIdle() async {
    _idlePromptOpen = false;
    idleDetector.acknowledge(DateTime.now());
    await SafetyAlarm.stop();
    log('user: idle ok');
    notifyListeners();
  }

  Future<void> startRest(int minutes) async {
    _idlePromptOpen = false;
    await SafetyAlarm.stop();
    idleDetector.startRest(DateTime.now(), Duration(minutes: minutes));
    log('rest $minutes min');
    notifyListeners();
    final id = activeTripId;
    if (id != null) {
      unawaited(
        api.setRest(id, minutes.clamp(5, 480)).catchError((Object error) {
          log('rest sync failed: $error');
        }),
      );
    }
  }

  Future<void> extendRest(int minutes) async {
    final now = DateTime.now();
    final until = idleDetector.restUntil;
    final base = until != null && until.isAfter(now) ? until : now;
    final total = base.add(Duration(minutes: minutes)).difference(now);
    await startRest(total.inMinutes + 1);
  }

  Future<void> endRest() async {
    idleDetector.endRest(DateTime.now());
    log('rest ended');
    notifyListeners();
    final id = activeTripId;
    if (id != null) {
      unawaited(
        api.clearRest(id).catchError((Object error) {
          log('clear rest failed: $error');
        }),
      );
    }
  }

  // ---------- Debug Simulator

  void setSimulatedSpeed(double? kmh) {
    simulatedSpeedKmh = kmh;
    if (kmh != null) {
      final now = DateTime.now();
      crashDetector.addSpeed(kmh, now);
      idleDetector.update(kmh, now);
      currentSpeedKmh = kmh;
    }
    notifyListeners();
  }

  /// Runs the real detection algorithm with a synthetic 4.2g impact at
  /// 46 km/h followed by 5 seconds of stillness at 1 km/h.
  void simulateCrash() {
    final start = DateTime.now();
    crashDetector.reset();
    crashDetector.enabled = true;
    crashDetector.addSpeed(46, start.subtract(const Duration(seconds: 2)));
    crashDetector.addAcceleration(25, 30, 20, start);
    crashDetector.addSpeed(1, start.add(const Duration(seconds: 1)));
    CrashEvent? event;
    for (var ms = 1000; ms <= 5200 && event == null; ms += 100) {
      event = crashDetector.addAcceleration(
        0.05,
        0.1,
        9.8,
        start.add(Duration(milliseconds: ms)),
      );
    }
    if (event != null) _emitCrash(event);
  }

  void simulateIdle() {
    idleDetector.update(0, DateTime.now(), point: latestPoint);
    idleDetector.simulateElapsed(DateTime.now());
    _idlePromptOpen = false;
    log('simulate idle ${idleDetector.threshold().inMinutes} min');
    _tick();
  }

  void simulateOffline(Duration duration) {
    final tracker = _tracker;
    if (tracker == null) return;
    tracker.offlineUntil = DateTime.now().add(duration);
    log('simulate offline ${duration.inMinutes} min (points stay local)');
    notifyListeners();
  }

  void clearSimulatedOffline() {
    _tracker?.offlineUntil = null;
    log('network back → sync ${_tracker?.pendingCount ?? 0} points');
    unawaited(_tracker?.flush());
    notifyListeners();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    _stopSafetyMonitoring();
    final tracker = _tracker;
    _tracker = null;
    unawaited(tracker?.stop());
    unawaited(_events.close());
    alerts
      ..removeListener(notifyListeners)
      ..dispose();
    super.dispose();
  }
}
