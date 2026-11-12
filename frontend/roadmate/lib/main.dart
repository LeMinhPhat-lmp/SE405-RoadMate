import 'dart:async';
import 'dart:convert';
import 'dart:io';

import 'package:firebase_auth/firebase_auth.dart';
import 'package:firebase_core/firebase_core.dart';
import 'package:firebase_messaging/firebase_messaging.dart';
import 'package:firebase_storage/firebase_storage.dart';
import 'package:flutter_local_notifications/flutter_local_notifications.dart';
import 'package:geolocator/geolocator.dart';
import 'package:image_picker/image_picker.dart';
import 'package:battery_plus/battery_plus.dart';
import 'package:connectivity_plus/connectivity_plus.dart';
import 'package:flutter/material.dart';
import 'package:http/http.dart' as http;
import 'package:share_plus/share_plus.dart';
import 'package:shared_preferences/shared_preferences.dart';
import 'package:url_launcher/url_launcher.dart';

part 'services/firebase_bridge.dart';
part 'services/roadmate_api.dart';
part 'services/trip_tracker.dart';
part 'services/trip_runtime.dart';
part 'app/roadmate_app.dart';
part 'features/auth/auth_page.dart';
part 'features/trips/trip_planner_page.dart';
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
