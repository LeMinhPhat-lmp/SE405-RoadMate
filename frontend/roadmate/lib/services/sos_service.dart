part of '../main.dart';

/// Vietnamese emergency numbers (doc section 10). The app only opens the
/// dialer; it never calls automatically.
const emergencyNumbers = [
  ('115', 'Cấp cứu'),
  ('113', 'Công an'),
  ('112', 'Khẩn cấp'),
  ('114', 'Cứu hoả'),
];

Future<void> openDialer(String number) =>
    launchUrl(Uri(scheme: 'tel', path: number));

String alertTypeLabel(String? type) => switch (type) {
  'crash' => 'Nghi va chạm / té ngã',
  'idle' => 'Dừng quá lâu',
  'sos' => 'SOS',
  'offroute' => 'Lệch tuyến',
  'lowBattery' => 'Pin yếu',
  'lostSignal' => 'Mất sóng',
  'lateReturn' => 'Về trễ',
  _ => 'Cảnh báo',
};

/// Loud siren + strong vibration until stopped.
class SafetyAlarm {
  static bool _playing = false;
  static bool get playing => _playing;

  static Future<void> start() async {
    if (_playing) return;
    _playing = true;
    try {
      await FlutterRingtonePlayer().playAlarm(looping: true, asAlarm: true);
    } on Object catch (error) {
      debugPrint('RoadMate alarm sound failed: $error');
    }
    try {
      if (await Vibration.hasVibrator()) {
        await Vibration.vibrate(pattern: [0, 800, 400, 800], repeat: 0);
      }
    } on Object catch (error) {
      debugPrint('RoadMate vibration failed: $error');
    }
  }

  static Future<void> pulse() async {
    try {
      if (await Vibration.hasVibrator()) {
        await Vibration.vibrate(pattern: [0, 300, 200, 300]);
      }
    } on Object {
      // Vibration is best effort.
    }
  }

  static Future<void> stop() async {
    _playing = false;
    try {
      await FlutterRingtonePlayer().stop();
    } on Object {
      // Ignore: nothing playing.
    }
    try {
      await Vibration.cancel();
    } on Object {
      // Ignore: no vibrator.
    }
  }
}

/// Local (on-device) notifications for safety prompts. Works without
/// Firebase so the safety features stay usable offline.
class NotificationService {
  static const channelId = FirebaseBridge.emergencyChannelId;
  static final FlutterLocalNotificationsPlugin _plugin =
      FlutterLocalNotificationsPlugin();
  static bool _ready = false;

  static Future<void> _ensureReady() async {
    if (_ready) return;
    await _plugin.initialize(
      const InitializationSettings(
        android: AndroidInitializationSettings('@mipmap/ic_launcher'),
        iOS: DarwinInitializationSettings(),
      ),
    );
    final android = _plugin
        .resolvePlatformSpecificImplementation<
          AndroidFlutterLocalNotificationsPlugin
        >();
    await android?.createNotificationChannel(
      const AndroidNotificationChannel(
        channelId,
        'RoadMate emergency alerts',
        description: 'High-priority safety alerts for active trips.',
        importance: Importance.max,
        playSound: true,
      ),
    );
    await android?.requestNotificationsPermission();
    _ready = true;
  }

  static Future<void> show(int id, String title, String body) async {
    try {
      await _ensureReady();
      await _plugin.show(
        id,
        title,
        body,
        const NotificationDetails(
          android: AndroidNotificationDetails(
            channelId,
            'RoadMate emergency alerts',
            channelDescription: 'High-priority safety alerts for active trips.',
            importance: Importance.max,
            priority: Priority.high,
            fullScreenIntent: true,
          ),
          iOS: DarwinNotificationDetails(
            interruptionLevel: InterruptionLevel.timeSensitive,
          ),
        ),
      );
    } on Object catch (error) {
      debugPrint('RoadMate local notification failed: $error');
    }
  }
}

/// Snapshot of what the app needs to alert contacts, cached on the device so
/// SOS works without network.
class SafetyProfile {
  const SafetyProfile({this.me = const {}, this.contacts = const []});
  final Map<String, dynamic> me;
  final List<Map<String, dynamic>> contacts;

  static const _key = 'safety_profile';

  Map<String, dynamic> toJson() => {'me': me, 'contacts': contacts};

  factory SafetyProfile.fromJson(Map<String, dynamic> json) => SafetyProfile(
    me: Map<String, dynamic>.from(json['me'] as Map? ?? const {}),
    contacts: (json['contacts'] as List? ?? const [])
        .map((item) => Map<String, dynamic>.from(item as Map))
        .toList(),
  );

  /// Contacts chosen for this trip (all contacts when none were chosen).
  List<Map<String, dynamic>> recipients(List<String> alertContactIds) {
    final chosen = contacts
        .where((contact) => alertContactIds.contains(contact['id']))
        .toList();
    final list = chosen.isEmpty ? [...contacts] : chosen;
    list.sort(
      (a, b) => ((a['priority'] as num?) ?? 9).compareTo(
        (b['priority'] as num?) ?? 9,
      ),
    );
    return list;
  }

  static Future<SafetyProfile> load(RoadMateApi api) async {
    final prefs = await SharedPreferences.getInstance();
    try {
      final profile = SafetyProfile(
        me: await api.getMe(),
        contacts: await api.getContacts(),
      );
      await prefs.setString(_key, jsonEncode(profile.toJson()));
      return profile;
    } on Object {
      final cached = prefs.getString(_key);
      if (cached == null) return const SafetyProfile();
      return SafetyProfile.fromJson(
        Map<String, dynamic>.from(jsonDecode(cached) as Map),
      );
    }
  }
}

/// One emergency alert from raise to resolve (doc 3.4a "Luồng xử lý").
///
/// The backend notifies contacts (SMS + push) and repeats every 2 minutes;
/// the app keeps sending its newest position. A stable clientId means
/// retries after a network failure never notify contacts twice.
class SafetyAlertController extends ChangeNotifier {
  SafetyAlertController({
    required this.api,
    required this.positionProvider,
    required this.batteryProvider,
    this.onLog,
  });

  final RoadMateApi api;
  final Future<Position?> Function() positionProvider;
  final Future<int?> Function() batteryProvider;
  final void Function(String message)? onLog;

  static const repeatInterval = Duration(minutes: 2);
  static const _retryInterval = Duration(seconds: 20);
  static const _prefsKey = 'active_safety_alert';

  String? tripId;
  String? type;
  String? clientId;
  Map<String, dynamic>? alert;
  DateTime? raisedAt;
  DateTime? nextUpdateAt;
  double? lat;
  double? lng;
  int? battery;
  String? error;
  bool sending = false;
  Timer? _timer;

  bool get active => type != null;

  /// Raised locally but not yet accepted by the server (offline).
  bool get queued => active && alert == null;

  String? get alertId => alert?['id'] as String?;

  Future<void> restore() async {
    final prefs = await SharedPreferences.getInstance();
    final saved = prefs.getString(_prefsKey);
    if (saved == null) return;
    final json = Map<String, dynamic>.from(jsonDecode(saved) as Map);
    tripId = json['tripId'] as String?;
    type = json['type'] as String?;
    clientId = json['clientId'] as String?;
    lat = asDouble(json['lat']);
    lng = asDouble(json['lng']);
    raisedAt = parseTime(json['raisedAt']);
    final alertJson = json['alert'];
    alert = alertJson == null
        ? null
        : Map<String, dynamic>.from(alertJson as Map);
    notifyListeners();
    _schedule(alert == null ? Duration.zero : repeatInterval);
  }

  Future<void> _persist() async {
    final prefs = await SharedPreferences.getInstance();
    await prefs.setString(
      _prefsKey,
      jsonEncode({
        'tripId': tripId,
        'type': type,
        'clientId': clientId,
        'lat': lat,
        'lng': lng,
        'raisedAt': raisedAt?.toUtc().toIso8601String(),
        'alert': alert,
      }),
    );
  }

  /// Starts an alert of [alertType] for [forTripId]. Does nothing when an
  /// alert is already running.
  Future<void> raise(String forTripId, String alertType) async {
    if (active) return;
    tripId = forTripId;
    type = alertType;
    clientId = '$alertType-${DateTime.now().microsecondsSinceEpoch}';
    raisedAt = DateTime.now();
    alert = null;
    error = null;
    onLog?.call('raise $alertType alert');
    notifyListeners();
    await _refreshLocation();
    await _persist();
    await _send();
  }

  Future<void> _refreshLocation() async {
    try {
      final position = await positionProvider();
      if (position != null) {
        lat = position.latitude;
        lng = position.longitude;
      }
    } on Object {
      // Keep the last known position.
    }
    try {
      battery = await batteryProvider();
    } on Object {
      // Battery is optional.
    }
  }

  Future<void> _send() async {
    final id = tripId;
    if (id == null || !active || sending) return;
    sending = true;
    notifyListeners();
    try {
      await _refreshLocation();
      final latitude = lat ?? defaultMapCenter.latitude;
      final longitude = lng ?? defaultMapCenter.longitude;
      if (alert == null) {
        alert = await api.createAlert(
          id,
          clientId: clientId!,
          type: type!,
          lat: latitude,
          lng: longitude,
          battery: battery,
        );
        onLog?.call('alert ${alert!['id']} sent to contacts');
      } else {
        await api.updateAlertLocation(
          alertId!,
          lat: latitude,
          lng: longitude,
          battery: battery,
        );
        await _refreshDeliveries(id);
        onLog?.call('alert location updated');
      }
      error = null;
      await _persist();
      _schedule(repeatInterval);
    } on Object catch (exception) {
      error = exception.toString();
      onLog?.call('alert send failed, retrying: $exception');
      _schedule(alert == null ? _retryInterval : repeatInterval);
    } finally {
      sending = false;
      notifyListeners();
    }
  }

  Future<void> _refreshDeliveries(String forTripId) async {
    try {
      final alerts = await api.getAlerts(forTripId);
      final current = alerts.where((item) => item['id'] == alertId);
      if (current.isNotEmpty) alert = current.first;
    } on Object {
      // Delivery status is informational only.
    }
  }

  void _schedule(Duration delay) {
    _timer?.cancel();
    if (!active) return;
    nextUpdateAt = DateTime.now().add(delay);
    _timer = Timer(delay, () => unawaited(_send()));
  }

  /// SMS composer prefilled for [recipients]: the manual fallback when the
  /// server cannot be reached (iOS and Android both need the user to tap
  /// send).
  Uri smsFallbackUri(
    SafetyProfile profile,
    List<Map<String, dynamic>> recipients,
  ) {
    final phones = recipients.map((contact) => contact['phone']).join(',');
    final me = profile.me;
    final parts = <String>[
      '[RoadMate] ${me['name'] ?? 'Mình'} cần giúp: ${alertTypeLabel(type)}.',
      if (lat != null && lng != null) 'Vị trí: ${googleMapsLink(lat!, lng!)}',
      if (raisedAt != null) 'Lúc ${formatClock(raisedAt!)}.',
      if (me['bloodType'] != null) 'Nhóm máu ${me['bloodType']}.',
      if (me['plateNumber'] != null) 'Biển số ${me['plateNumber']}.',
      if (battery != null) 'Pin $battery%.',
    ];
    return Uri.parse(
      'sms:$phones?body=${Uri.encodeComponent(parts.join(' '))}',
    );
  }

  /// The rider is safe: closes the alert on the server and stops repeating.
  Future<void> resolve() async {
    final id = alertId;
    _timer?.cancel();
    await SafetyAlarm.stop();
    if (id != null) {
      try {
        await api.resolveAlert(id);
      } on Object catch (exception) {
        onLog?.call('resolve failed: $exception');
      }
    }
    onLog?.call('alert resolved by user');
    await _clear();
  }

  Future<void> _clear() async {
    tripId = null;
    type = null;
    clientId = null;
    alert = null;
    raisedAt = null;
    nextUpdateAt = null;
    error = null;
    final prefs = await SharedPreferences.getInstance();
    await prefs.remove(_prefsKey);
    notifyListeners();
  }

  @override
  void dispose() {
    _timer?.cancel();
    super.dispose();
  }
}
