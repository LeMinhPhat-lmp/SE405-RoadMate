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
}
