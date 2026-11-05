import 'dart:async';
import 'dart:convert';

import 'package:firebase_auth/firebase_auth.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:shared_preferences/shared_preferences.dart';

part 'services/firebase_bridge.dart';
part 'services/roadmate_api.dart';
part 'app/roadmate_app.dart';
part 'features/auth/auth_page.dart';
part 'features/home/home_page.dart';
part 'features/trips/trips_page.dart';
part 'features/journal/journal_page.dart';
part 'features/contacts/contacts_page.dart';
part 'features/profile/profile_page.dart';
part 'widgets/common_widgets.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  unawaited(FirebaseBridge.initialize());
  runApp(RoadMateApp(api: RoadMateApi(http.Client())));
}
