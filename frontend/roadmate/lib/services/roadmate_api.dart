part of '../main.dart';

const apiBaseUrl = String.fromEnvironment(
  'ROAD_MATE_API_URL',
  defaultValue: 'http://10.0.2.2:5000/api/v1',
);

class RoadMateApi {
  RoadMateApi(this._client);

  final http.Client _client;
  SharedPreferences? _prefs;

  Future<Map<String, String>> _headers() async {
    _prefs ??= await SharedPreferences.getInstance();
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
    if (FirebaseBridge.available) {
      final token = await FirebaseAuth.instance.currentUser?.getIdToken();
      if (token != null) headers['Authorization'] = '******';
    } else {
      headers['X-Dev-Uid'] = _prefs?.getString('devUid') ?? 'user1';
    }
    return headers;
  }

  Future<dynamic> _request(String method, String path, {Object? body}) async {
    final request = http.Request(method, Uri.parse('$apiBaseUrl$path'));
    request.headers.addAll(await _headers());
    if (body != null) request.body = jsonEncode(body);
    final response = await _client
        .send(request)
        .timeout(
          const Duration(seconds: 10),
          onTimeout: () =>
              throw Exception('API request timed out: $apiBaseUrl'),
        );
    final text = await response.stream.bytesToString();
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw Exception(
        'API ${response.statusCode}: ${text.isEmpty ? 'Request failed' : text}',
      );
    }
    return text.isEmpty ? null : jsonDecode(text);
  }

  Future<void> syncUser(String name) async {
    await _request(
      'POST',
      '/auth/sync',
      body: {'name': name, 'fcmToken': null},
    );
  }

  Future<Map<String, dynamic>> getMe() async =>
      Map<String, dynamic>.from(await _request('GET', '/me') as Map);

  Future<List<Map<String, dynamic>>> getTrips() async =>
      (await _request('GET', '/trips') as List)
          .map((item) => Map<String, dynamic>.from(item as Map))
          .toList();

  Future<Map<String, dynamic>> createTrip({
    required String title,
    required String origin,
    required String destination,
  }) async => Map<String, dynamic>.from(
    await _request(
      'POST',
      '/trips',
      body: {
        'title': title,
        'origin': {'name': origin, 'lat': 10.7769, 'lng': 106.7009},
        'destination': {'name': destination, 'lat': 11.0168, 'lng': 106.6297},
        'waypoints': <Map<String, dynamic>>[],
        'plannedPolyline': null,
        'distanceKm': 32.4,
        'etaMinutes': 70,
        'avoidHighway': false,
        'avoidToll': false,
        'expectedReturnTime': null,
        'idleMinutes': 10,
        'crashSensitivity': 'normal',
        'liveShare': true,
        'alertContactIds': <String>[],
      },
    ) as Map,
  );

  Future<Map<String, dynamic>> startTrip(String id) async =>
      Map<String, dynamic>.from(
        await _request(
          'POST',
          '/trips/$id/start',
          body: {
            'expectedReturnTime': null,
            'alertContactIds': <String>[],
            'liveShare': true,
          },
        ) as Map,
      );

  Future<void> finishTrip(String id) async {
    await _request('POST', '/trips/$id/finish');
  }

  Future<List<Map<String, dynamic>>> getContacts() async =>
      (await _request('GET', '/emergency-contacts') as List)
          .map((item) => Map<String, dynamic>.from(item as Map))
          .toList();

  Future<void> addContact(String name, String phone, String relation) =>
      _request(
        'POST',
        '/emergency-contacts',
        body: {
          'name': name,
          'phone': phone,
          'relation': relation,
          'priority': 1,
        },
      );

  Future<Map<String, dynamic>> createAlert(String tripId, String type) async =>
      Map<String, dynamic>.from(
        await _request(
          'POST',
          '/trips/$tripId/alerts',
          body: {
            'clientId': 'demo-${DateTime.now().millisecondsSinceEpoch}',
            'type': type,
            'lat': 10.7769,
            'lng': 106.7009,
            'battery': 82,
          },
        ) as Map,
      );

  Future<Map<String, dynamic>> getJournal(String tripId) async =>
      Map<String, dynamic>.from(
        await _request('GET', '/trips/$tripId/journal') as Map,
      );

  Future<void> addMemory(String tripId, String text) => _request(
    'POST',
    '/trips/$tripId/memories',
    body: {
      'type': 'note',
      'mediaUrl': null,
      'text': text,
      'lat': 10.7769,
      'lng': 106.7009,
      'mood': 'happy',
      'placeName': 'RoadMate demo',
      'rating': 5,
      'takenAt': DateTime.now().toUtc().toIso8601String(),
    },
  );
}
