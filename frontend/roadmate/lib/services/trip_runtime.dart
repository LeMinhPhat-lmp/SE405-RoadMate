part of '../main.dart';

class TripRuntime extends ChangeNotifier with WidgetsBindingObserver {
  TripRuntime(this.api) {
    WidgetsBinding.instance.addObserver(this);
  }

  final RoadMateApi api;
  TripTracker? _tracker;
  String? activeTripId;
  String? lastError;

  bool get isTracking => _tracker?.running == true;
  Position? get latestPosition => _tracker?.latest;

  Future<void> restore() async {
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

  @override
  void didChangeAppLifecycleState(AppLifecycleState state) {
    if (state == AppLifecycleState.paused ||
        state == AppLifecycleState.inactive) {
      unawaited(_tracker?.flush());
    }
  }

  Future<void> startTracking(String tripId) async {
    await _tracker?.stop();
    final tracker = TripTracker(
      api: api,
      tripId: tripId,
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
    lastError = null;
    notifyListeners();
  }

  Future<void> stopTracking() async {
    await _tracker?.stop();
    _tracker = null;
    activeTripId = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove('active_trip_id');
    notifyListeners();
  }

  @override
  void dispose() {
    WidgetsBinding.instance.removeObserver(this);
    final tracker = _tracker;
    _tracker = null;
    unawaited(tracker?.stop());
    super.dispose();
  }
}
