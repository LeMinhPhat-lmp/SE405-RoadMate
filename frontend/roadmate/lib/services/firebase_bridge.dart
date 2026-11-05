part of '../main.dart';

class FirebaseBridge {
  static bool available = false;

  static Future<void> initialize() async {
    try {
      await Firebase.initializeApp();
      available = true;
    } on Object {
      available = false;
    }
  }
}
