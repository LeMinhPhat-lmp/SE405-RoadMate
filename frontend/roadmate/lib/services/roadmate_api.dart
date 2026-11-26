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

  /// True when the request never reached the server (timeout, offline).
  bool get isNetworkError => statusCode == null;

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
    final http.StreamedResponse response;
    try {
      response = await _client
          .send(request)
          .timeout(
            const Duration(seconds: 10),
            onTimeout: () => throw RoadMateApiException(
              statusCode: null,
              message: 'Request timed out',
              uri: uri,
            ),
          );
    } on RoadMateApiException {
      rethrow;
    } on Object catch (exception) {
      throw RoadMateApiException(
        statusCode: null,
        message: 'Không kết nối được máy chủ ($exception)',
        uri: uri,
      );
    }
    final text = await response.stream.bytesToString();
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw RoadMateApiException(
        statusCode: response.statusCode,
        message: _problemMessage(text),
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

  static String _problemMessage(String text) {
    if (text.isEmpty) return 'Request failed';
    try {
      final body = jsonDecode(text);
      if (body is Map) {
        final detail = body['detail'] ?? body['title'];
        if (detail != null) return detail.toString();
      }
    } on FormatException {
      // Plain-text error body.
    }
    return text;
  }

  Map<String, dynamic> _map(Object? value) =>
      Map<String, dynamic>.from(value as Map);

  List<Map<String, dynamic>> _list(Object? value) => (value as List? ?? [])
      .map((item) => Map<String, dynamic>.from(item as Map))
      .toList();

  void dispose() => _client.close();

  // ---------- Account

  Future<void> syncUser(String name, {String? fcmToken}) async {
    await _request(
      'POST',
      '/auth/sync',
      body: {'name': name, 'fcmToken': fcmToken},
    );
  }

  Future<Map<String, dynamic>> getMe() async =>
      _map(await _request('GET', '/me'));

  /// Only non-null fields are changed by the backend.
  Future<Map<String, dynamic>> updateMe({
    String? name,
    String? bloodType,
    String? medicalNotes,
    String? vehicleType,
    String? plateNumber,
    int? idleMinutes,
    String? crashSensitivity,
    String? theme,
  }) async => _map(
    await _request(
      'PUT',
      '/me',
      body: {
        'name': name,
        'bloodType': bloodType,
        'medicalNotes': medicalNotes,
        'vehicleType': vehicleType,
        'plateNumber': plateNumber,
        'idleMinutes': idleMinutes,
        'crashSensitivity': crashSensitivity,
        'theme': theme,
      },
    ),
  );

  // ---------- Emergency contacts

  Future<List<Map<String, dynamic>>> getContacts() async =>
      _list(await _request('GET', '/emergency-contacts'));

  Future<void> addContact(
    String name,
    String phone,
    String relation, {
    int priority = 1,
  }) => _request(
    'POST',
    '/emergency-contacts',
    body: {
      'name': name,
      'phone': phone,
      'relation': relation,
      'priority': priority,
    },
  );

  Future<void> updateContact(
    String id, {
    required String name,
    required String phone,
    required String relation,
    required int priority,
  }) => _request(
    'PUT',
    '/emergency-contacts/$id',
    body: {
      'name': name,
      'phone': phone,
      'relation': relation,
      'priority': priority,
    },
  );

  Future<void> deleteContact(String id) =>
      _request('DELETE', '/emergency-contacts/$id');

  Future<dynamic> testContact(String id) =>
      _request('POST', '/emergency-contacts/$id/test');

  // ---------- Trips

  Future<List<Map<String, dynamic>>> getTrips({String? status}) async => _list(
    await _request('GET', status == null ? '/trips' : '/trips?status=$status'),
  );

  Future<Map<String, dynamic>> getTrip(String id) async =>
      _map(await _request('GET', '/trips/$id'));

  Map<String, dynamic> _tripBody({
    required String title,
    required Map<String, dynamic> origin,
    required Map<String, dynamic> destination,
    required List<Map<String, dynamic>> waypoints,
    String? plannedPolyline,
    double? distanceKm,
    int? etaMinutes,
    required bool avoidHighway,
    required bool avoidToll,
    DateTime? expectedReturnTime,
    int? idleMinutes,
    String? crashSensitivity,
    required bool liveShare,
    required List<String> alertContactIds,
  }) => {
    'title': title,
    'origin': origin,
    'destination': destination,
    'waypoints': waypoints,
    'plannedPolyline': plannedPolyline,
    'distanceKm': distanceKm,
    'etaMinutes': etaMinutes,
    'avoidHighway': avoidHighway,
    'avoidToll': avoidToll,
    'expectedReturnTime': expectedReturnTime?.toUtc().toIso8601String(),
    'idleMinutes': idleMinutes,
    'crashSensitivity': crashSensitivity,
    'liveShare': liveShare,
    'alertContactIds': alertContactIds,
  };

  /// [origin]/[destination] are `{name, lat, lng}`; [waypoints] are
  /// `{name, lat, lng, plannedStayMin}` in visiting order.
  Future<Map<String, dynamic>> createTrip({
    required String title,
    required Map<String, dynamic> origin,
    required Map<String, dynamic> destination,
    List<Map<String, dynamic>> waypoints = const [],
    String? plannedPolyline,
    double? distanceKm,
    int? etaMinutes,
    bool avoidHighway = false,
    bool avoidToll = false,
    DateTime? expectedReturnTime,
    int? idleMinutes,
    String? crashSensitivity,
    bool liveShare = true,
    List<String> alertContactIds = const [],
  }) async => _map(
    await _request(
      'POST',
      '/trips',
      body: _tripBody(
        title: title,
        origin: origin,
        destination: destination,
        waypoints: waypoints,
        plannedPolyline: plannedPolyline,
        distanceKm: distanceKm,
        etaMinutes: etaMinutes,
        avoidHighway: avoidHighway,
        avoidToll: avoidToll,
        expectedReturnTime: expectedReturnTime,
        idleMinutes: idleMinutes,
        crashSensitivity: crashSensitivity,
        liveShare: liveShare,
        alertContactIds: alertContactIds,
      ),
    ),
  );

  Future<Map<String, dynamic>> updateTrip(
    String id, {
    required String title,
    required Map<String, dynamic> origin,
    required Map<String, dynamic> destination,
    List<Map<String, dynamic>> waypoints = const [],
    String? plannedPolyline,
    double? distanceKm,
    int? etaMinutes,
    bool avoidHighway = false,
    bool avoidToll = false,
    DateTime? expectedReturnTime,
    int? idleMinutes,
    String? crashSensitivity,
    bool liveShare = true,
    List<String> alertContactIds = const [],
  }) async => _map(
    await _request(
      'PUT',
      '/trips/$id',
      body: _tripBody(
        title: title,
        origin: origin,
        destination: destination,
        waypoints: waypoints,
        plannedPolyline: plannedPolyline,
        distanceKm: distanceKm,
        etaMinutes: etaMinutes,
        avoidHighway: avoidHighway,
        avoidToll: avoidToll,
        expectedReturnTime: expectedReturnTime,
        idleMinutes: idleMinutes,
        crashSensitivity: crashSensitivity,
        liveShare: liveShare,
        alertContactIds: alertContactIds,
      ),
    ),
  );

  Future<void> deleteTrip(String id) => _request('DELETE', '/trips/$id');

  /// Returns `{trip, trackingUrl, warnings}`. Null fields keep the values
  /// saved on the draft.
  Future<Map<String, dynamic>> startTrip(
    String id, {
    DateTime? expectedReturnTime,
    List<String>? alertContactIds,
    bool? liveShare,
  }) async => _map(
    await _request(
      'POST',
      '/trips/$id/start',
      body: {
        'expectedReturnTime': expectedReturnTime?.toUtc().toIso8601String(),
        'alertContactIds': alertContactIds,
        'liveShare': liveShare,
      },
    ),
  );

  Future<void> finishTrip(String id) async {
    await _request('POST', '/trips/$id/finish');
  }

  /// Returns `{trip, stats}`.
  Future<Map<String, dynamic>> getTripSummary(String id) async =>
      _map(await _request('GET', '/trips/$id/summary'));

  Future<void> setRest(String tripId, int minutes) =>
      _request('POST', '/trips/$tripId/rest', body: {'minutes': minutes});

  Future<void> clearRest(String tripId) =>
      _request('DELETE', '/trips/$tripId/rest');

  // ---------- Alerts

  /// [clientId] must stay the same across retries so the backend does not
  /// notify contacts twice.
  Future<Map<String, dynamic>> createAlert(
    String tripId, {
    required String clientId,
    required String type,
    required double lat,
    required double lng,
    int? battery,
  }) async => _map(
    await _request(
      'POST',
      '/trips/$tripId/alerts',
      body: {
        'clientId': clientId,
        'type': type,
        'lat': lat,
        'lng': lng,
        'battery': battery,
      },
    ),
  );

  Future<List<Map<String, dynamic>>> getAlerts(String tripId) async =>
      _list(await _request('GET', '/trips/$tripId/alerts'));

  Future<void> updateAlertLocation(
    String alertId, {
    required double lat,
    required double lng,
    int? battery,
  }) => _request(
    'POST',
    '/alerts/$alertId/locations',
    body: {'lat': lat, 'lng': lng, 'battery': battery},
  );

  /// "Tôi ổn" – false alarm.
  Future<void> cancelAlert(String alertId) =>
      _request('POST', '/alerts/$alertId/cancel');

  /// "Tôi đã an toàn" after contacts were notified.
  Future<void> resolveAlert(String alertId) =>
      _request('POST', '/alerts/$alertId/resolve');

  /// Backend demo simulator (Debug:EnableSimulator). Dry-run by default.
  Future<Map<String, dynamic>> simulateAlert(
    String tripId, {
    required String type,
    double? lat,
    double? lng,
  }) async => _map(
    await _request(
      'POST',
      '/debug/simulate-alert',
      body: {
        'tripId': tripId,
        'type': type,
        'lat': lat,
        'lng': lng,
        'dryRun': true,
      },
    ),
  );

  // ---------- Journal

  Future<Map<String, dynamic>> getJournal(String tripId) async {
    final offsetHours = DateTime.now().timeZoneOffset.inHours;
    return _map(
      await _request('GET', '/trips/$tripId/journal?tzOffset=$offsetHours'),
    );
  }

  Future<List<Map<String, dynamic>>> getMemories(String tripId) async =>
      _list(await _request('GET', '/trips/$tripId/memories'));

  Future<void> addMemory(
    String tripId,
    String text, {
    double? lat,
    double? lng,
    String? mood,
    String? placeName,
    int? rating,
  }) => _request(
    'POST',
    '/trips/$tripId/memories',
    body: {
      'type': 'note',
      'mediaPath': null,
      'text': text,
      'lat': lat,
      'lng': lng,
      'mood': mood,
      'placeName': placeName,
      'rating': rating,
      'takenAt': DateTime.now().toUtc().toIso8601String(),
    },
  );

  /// Uploads a photo through the backend signed URL, then saves the memory.
  Future<void> addPhotoMemory(
    String tripId, {
    required List<int> bytes,
    required String contentType,
    String? text,
    double? lat,
    double? lng,
  }) async {
    final upload = _map(
      await _request(
        'POST',
        '/trips/$tripId/media/upload-url',
        body: {
          'kind': 'photo',
          'contentType': contentType,
          'sizeBytes': bytes.length,
        },
      ),
    );
    final uploadUri = Uri.parse(upload['uploadUrl'] as String);
    final put = http.Request(upload['method'] as String? ?? 'PUT', uploadUri)
      ..bodyBytes = bytes
      ..headers.addAll(
        Map<String, String>.from(upload['headers'] as Map? ?? const {}),
      );
    final response = await _client
        .send(put)
        .timeout(const Duration(seconds: 60));
    await response.stream.drain<void>();
    if (response.statusCode < 200 || response.statusCode >= 300) {
      throw RoadMateApiException(
        statusCode: response.statusCode,
        message: 'Tải ảnh lên thất bại',
        uri: uploadUri,
      );
    }
    await _request(
      'POST',
      '/trips/$tripId/memories',
      body: {
        'type': 'photo',
        'mediaPath': upload['mediaPath'],
        'text': text,
        'lat': lat,
        'lng': lng,
        'mood': null,
        'placeName': null,
        'rating': null,
        'takenAt': DateTime.now().toUtc().toIso8601String(),
      },
    );
  }

  Future<void> deleteMemory(String memoryId) =>
      _request('DELETE', '/memories/$memoryId');

  // ---------- Maps

  Future<Map<String, dynamic>> getDirections({
    required double originLat,
    required double originLng,
    required double destinationLat,
    required double destinationLng,
    List<Map<String, double>> waypoints = const [],
    bool avoidHighway = false,
    bool avoidToll = false,
  }) async => _map(
    await _request(
      'POST',
      '/routes/directions',
      body: {
        'origin': {'lat': originLat, 'lng': originLng},
        'destination': {'lat': destinationLat, 'lng': destinationLng},
        'waypoints': waypoints,
        'avoidHighway': avoidHighway,
        'avoidToll': avoidToll,
        'vehicle': 'motorbike',
      },
    ),
  );

  Future<List<Map<String, dynamic>>> autocomplete(
    String query, {
    double? lat,
    double? lng,
  }) async {
    final near = lat == null || lng == null
        ? ''
        : '&lat=$lat&lng=$lng&withDistance=true';
    return _list(
      await _request(
        'GET',
        '/places/autocomplete?q=${Uri.encodeQueryComponent(query)}$near',
      ),
    );
  }

  Future<Map<String, dynamic>> getPlace(String placeId) async =>
      _map(await _request('GET', '/places/${Uri.encodeComponent(placeId)}'));

  Future<Map<String, dynamic>> reverseGeocode(double lat, double lng) async =>
      _map(await _request('GET', '/places/reverse?lat=$lat&lng=$lng'));

  // ---------- Telemetry

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
      _map(await _request('POST', '/trips/$tripId/tracking-link'));
}
