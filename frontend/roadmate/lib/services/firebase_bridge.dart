part of '../main.dart';

class FirebaseBridge {
  static const emergencyChannelId = 'roadmate_emergency';
  static bool available = false;
  static final ValueNotifier<bool> availability = ValueNotifier(false);
  static final FlutterLocalNotificationsPlugin _localNotifications =
      FlutterLocalNotificationsPlugin();
  static StreamSubscription<RemoteMessage>? _foregroundMessages;

  static Future<void> initialize() async {
    try {
      await Firebase.initializeApp();
      await _configureNotifications();
      available = true;
      availability.value = true;
    } on Object {
      available = false;
      availability.value = false;
    }
  }

  static Future<void> _configureNotifications() async {
    try {
      await _localNotifications.initialize(
        const InitializationSettings(
          android: AndroidInitializationSettings('@mipmap/ic_launcher'),
          iOS: DarwinInitializationSettings(),
        ),
      );
      const channel = AndroidNotificationChannel(
        emergencyChannelId,
        'RoadMate emergency alerts',
        description: 'High-priority safety alerts for active trips.',
        importance: Importance.max,
        playSound: true,
      );
      await _localNotifications
          .resolvePlatformSpecificImplementation<
            AndroidFlutterLocalNotificationsPlugin
          >()
          ?.createNotificationChannel(channel);
      await FirebaseMessaging.instance
          .setForegroundNotificationPresentationOptions(
            alert: true,
            badge: true,
            sound: true,
          );
      await _foregroundMessages?.cancel();
      _foregroundMessages = FirebaseMessaging.onMessage.listen(
        (message) => unawaited(_showForegroundMessage(message)),
      );
    } on Object catch (error) {
      debugPrint('RoadMate notification setup failed: $error');
    }
  }

  static Future<void> _showForegroundMessage(RemoteMessage message) async {
    if (!Platform.isAndroid || message.notification == null) return;
    try {
      await _localNotifications.show(
        message.messageId?.hashCode ?? DateTime.now().millisecondsSinceEpoch,
        message.notification?.title ?? 'RoadMate alert',
        message.notification?.body ?? '',
        const NotificationDetails(
          android: AndroidNotificationDetails(
            emergencyChannelId,
            'RoadMate emergency alerts',
            channelDescription: 'High-priority safety alerts for active trips.',
            importance: Importance.max,
            priority: Priority.high,
          ),
        ),
      );
    } on Object catch (error) {
      debugPrint('RoadMate foreground notification failed: $error');
    }
  }

  static Future<String?> requestPushToken() async {
    if (!available) return null;
    final settings = await FirebaseMessaging.instance.requestPermission(
      alert: true,
      badge: true,
      sound: true,
    );
    if (settings.authorizationStatus == AuthorizationStatus.denied) {
      return null;
    }
    return FirebaseMessaging.instance.getToken();
  }

  static Stream<String> get tokenRefresh =>
      FirebaseMessaging.instance.onTokenRefresh;
}
