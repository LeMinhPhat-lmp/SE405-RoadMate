import 'dart:convert';

import 'package:flutter_test/flutter_test.dart';
import 'package:http/http.dart' as http;
import 'package:roadmate/main.dart';
import 'package:shared_preferences/shared_preferences.dart';

class _RecordingClient extends http.BaseClient {
  Uri? requestedUri;

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requestedUri = request.url;
    return http.StreamedResponse(
      Stream.value(utf8.encode('[]')),
      200,
      request: request,
    );
  }
}

/// Replies per request so multi-step flows can be checked.
class _ScriptedClient extends http.BaseClient {
  final requests = <http.BaseRequest>[];
  final bodies = <String>[];

  @override
  Future<http.StreamedResponse> send(http.BaseRequest request) async {
    requests.add(request);
    bodies.add(request is http.Request ? request.body : '');
    final path = request.url.path;
    var status = 200;
    var body = '{}';
    if (path.endsWith('/media/upload-url')) {
      body = jsonEncode({
        'mediaPath': 'trips/t1/photo.jpg',
        'uploadUrl': 'https://storage.example/upload?sig=1',
        'method': 'PUT',
        'headers': {'Content-Type': 'image/jpeg'},
        'expiresAt': '2026-11-26T10:00:00Z',
        'maxBytes': 5242880,
      });
    } else if (path.endsWith('/alerts')) {
      status = 404;
      body = jsonEncode({
        'title': 'TRIP_NOT_ACTIVE',
        'detail': 'Chuyến đi chưa bắt đầu.',
      });
    }
    return http.StreamedResponse(
      Stream.value(utf8.encode(body)),
      status,
      request: request,
    );
  }
}

void main() {
  TestWidgetsFlutterBinding.ensureInitialized();
  SharedPreferences.setMockInitialValues({});

  test('keeps the configured API prefix for endpoint paths', () async {
    final client = _RecordingClient();
    final api = RoadMateApi(client, baseUrl: 'http://localhost:5000/api/v1');

    await api.getTrips();

    expect(
      client.requestedUri.toString(),
      'http://localhost:5000/api/v1/trips',
    );
    api.dispose();
  });

  test('preserves endpoint query parameters', () async {
    final client = _RecordingClient();
    final api = RoadMateApi(client, baseUrl: 'http://localhost:5000/api/v1');

    await api.autocomplete('Ho Chi Minh');

    expect(
      client.requestedUri.toString(),
      'http://localhost:5000/api/v1/places/autocomplete?q=Ho+Chi+Minh',
    );
    api.dispose();
  });

  test('uploads photos through the signed URL then saves mediaPath', () async {
    final client = _ScriptedClient();
    final api = RoadMateApi(client, baseUrl: 'http://localhost:5000/api/v1');

    await api.addPhotoMemory(
      't1',
      bytes: [1, 2, 3],
      contentType: 'image/jpeg',
      lat: 10.1,
      lng: 106.2,
    );

    expect(client.requests.map((r) => '${r.method} ${r.url.path}').toList(), [
      'POST /api/v1/trips/t1/media/upload-url',
      'PUT /upload',
      'POST /api/v1/trips/t1/memories',
    ]);
    expect(jsonDecode(client.bodies[0]), {
      'kind': 'photo',
      'contentType': 'image/jpeg',
      'sizeBytes': 3,
    });
    expect(client.requests[1].headers['Content-Type'], 'image/jpeg');
    final memory = jsonDecode(client.bodies[2]) as Map;
    expect(memory['type'], 'photo');
    expect(memory['mediaPath'], 'trips/t1/photo.jpg');
    api.dispose();
  });

  test('surfaces the ProblemDetails message on errors', () async {
    final api = RoadMateApi(
      _ScriptedClient(),
      baseUrl: 'http://localhost:5000/api/v1',
    );

    await expectLater(
      api.createAlert('t1', clientId: 'c1', type: 'sos', lat: 10, lng: 106),
      throwsA(
        isA<RoadMateApiException>()
            .having((e) => e.statusCode, 'statusCode', 404)
            .having((e) => e.message, 'message', 'Chuyến đi chưa bắt đầu.'),
      ),
    );
    api.dispose();
  });
}
