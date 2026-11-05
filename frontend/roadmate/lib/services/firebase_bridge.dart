part of '../main.dart';

class FirebaseBridge {
  static bool available = false;
  static final ValueNotifier<bool> availability = ValueNotifier(false);

  static Future<void> initialize() async {
    try {
      await Firebase.initializeApp();
      available = true;
      availability.value = true;
    } on Object {
      available = false;
      availability.value = false;
    }
  }
}
