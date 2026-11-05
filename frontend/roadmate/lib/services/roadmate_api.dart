part of '../main.dart';

const apiBaseUrl = String.fromEnvironment(
  'ROAD_MATE_API_URL',
  defaultValue: 'http://10.0.2.2:5000/api/v1',
);

class RoadMateApiException implements Exception {
  const RoadMateApiException({
    required this.statusCode,
    required this.message,
    required this.uri,
  });

  final int? statusCode;
  final String message;
  final Uri uri;

  @override
  String toString() =>
      'RoadMate API${statusCode == null ? '' : ' $statusCode'}: $message';
}

class RoadMateApi {
  RoadMateApi(this._client, {String baseUrl = apiBaseUrl})
    : _baseUri = Uri.parse(baseUrl);

  final http.Client _client;
  final Uri _baseUri;
  SharedPreferences? _prefs;

  Uri _uri(String path) {
    final relative = Uri.parse(path);
    final basePath = _baseUri.path.endsWith('/')
        ? _baseUri.path
        : '${_baseUri.path}/';
    final relativePath = relative.path.startsWith('/')
        ? relative.path.substring(1)
        : relative.path;
    return _baseUri.replace(
      path: '$basePath$relativePath',
      query: relative.hasQuery ? relative.query : null,
    );
  }

  Future<Map<String, String>> _headers() async {
    _prefs ??= await SharedPreferences.getInstance();
    final headers = {
      'Content-Type': 'application/json',
      'Accept': 'application/json',
    };
    if (FirebaseBridge.available) {
      final token = await FirebaseAuth.instance.currentUser?.getIdToken();
      if (token != null) headers['Authorization'] = 'Bearer $token';
    } else {
      headers['X-Dev-Uid'] = _prefs?.getString('devUid') ?? 'user1';
    }
    return headers;
  }

  Future<dynamic> _request(String method, String path, {Object? body}) async {
    final uri = _uri(path);
    final request = http.Request(method, uri);
    request.headers.addAll(await _headers());
    if (body != null) request.body = jsonEncode(body);
    final response = await _client
        .send(request)
        .timeout(
          const Duration(seconds: 10),
          onTimeout: () => throw RoadMateApiException(
            statusCode: null,
            message: 'Request timed out',
            uri: uri,
          ),
        );
    final text = await response.stream.bytesToString();
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw RoadMateApiException(
        statusCode: response.statusCode,
        message: text.isEmpty ? 'Request failed' : text,
        uri: uri,
      );
    }
    if (text.isEmpty) return null;
    try {
      return jsonDecode(text);
    } on FormatException {
      throw RoadMateApiException(
        statusCode: response.statusCode,
        message: 'The server returned invalid JSON',
        uri: uri,
      );
    }
  }

  void dispose() => _client.close();

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
    double originLat = 10.7769,
    double originLng = 106.7009,
    double destinationLat = 11.0168,
    double destinationLng = 106.6297,
    List<Map<String, dynamic>> waypoints = const [],
    String? plannedPolyline,
    double? distanceKm,
    int? etaMinutes,
    bool avoidHighway = false,
    bool avoidToll = false,
  }) async => Map<String, dynamic>.from(
    await _request(
      'POST',
      '/trips',
      body: {
        'title': title,
        'origin': {'name': origin, 'lat': originLat, 'lng': originLng},
        'destination': {
          'name': destination,
          'lat': destinationLat,
          'lng': destinationLng,
        },
        'waypoints': waypoints,
        'plannedPolyline': plannedPolyline,
        'distanceKm': distanceKm,
        'etaMinutes': etaMinutes,
        'avoidHighway': avoidHighway,
        'avoidToll': avoidToll,
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

  Future<Map<String, dynamic>> getDirections({
    required double originLat,
    required double originLng,
    required double destinationLat,
    required double destinationLng,
    List<Map<String, double>> waypoints = const [],
    bool avoidHighway = false,
    bool avoidToll = false,
  }) async => Map<String, dynamic>.from(
    await _request(
      'POST',
      '/routes/directions',
      body: {
        'origin': {'lat': originLat, 'lng': originLng},
        'destination': {'lat': destinationLat, 'lng': destinationLng},
        'waypoints': waypoints,
        'avoidHighway': avoidHighway,
        'avoidToll': avoidToll,
        'vehicle': 'motorcycle',
      },
    ) as Map,
  );

  Future<List<Map<String, dynamic>>> autocomplete(String query) async =>
      (await _request(
        'GET',
        '/places/autocomplete?q=${Uri.encodeQueryComponent(query)}',
      ) as List).map((item) => Map<String, dynamic>.from(item as Map)).toList();

  Future<Map<String, dynamic>> getPlace(String placeId) async =>
      Map<String, dynamic>.from(
        await _request('GET', '/places/$placeId') as Map,
      );

  Future<void> addPoints(String tripId, List<Map<String, dynamic>> points) =>
      _request('POST', '/trips/$tripId/points', body: {'points': points});

  Future<void> heartbeat(
    String tripId, {
    required double lat,
    required double lng,
    double? speed,
    int? battery,
    required bool networkOk,
  }) => _request(
    'POST',
    '/trips/$tripId/heartbeat',
    body: {
      'lat': lat,
      'lng': lng,
      'speed': speed,
      'battery': battery,
      'networkOk': networkOk,
    },
  );

  Future<Map<String, dynamic>> createTrackingLink(String tripId) async =>
      Map<String, dynamic>.from(
        await _request('POST', '/trips/$tripId/tracking-link') as Map,
      );

  Future<List<Map<String, dynamic>>> getMemories(String tripId) async =>
      (await _request('GET', '/trips/$tripId/memories') as List)
          .map((item) => Map<String, dynamic>.from(item as Map))
          .toList();

  Future<void> addMemoryMedia(
    String tripId, {
    required String mediaUrl,
    String? text,
    required double lat,
    required double lng,
  }) => _request(
    'POST',
    '/trips/$tripId/memories',
    body: {
      'type': 'photo',
      'mediaUrl': mediaUrl,
      'text': text,
      'lat': lat,
      'lng': lng,
      'mood': 'happy',
      'placeName': null,
      'rating': null,
      'takenAt': DateTime.now().toUtc().toIso8601String(),
    },
  );
}
