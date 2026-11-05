# RoadMate Android frontend

The app runs in development mode without a Firebase cloud project. It uses
the ASP.NET Core API with `X-Dev-Uid: user1`, matching the backend's
development authentication mode. The UI also opens when the backend is offline;
API-backed screens show their request error until the backend is available.

## Run against the local backend

From `frontend/roadmate`:

```bash
flutter pub get
flutter run --dart-define=ROAD_MATE_API_URL=http://10.0.2.2:5000/api/v1
```

`10.0.2.2` is the Android emulator alias for the host machine. For a physical
Android device, replace it with the computer's LAN IP, for example:

```bash
flutter run --dart-define=ROAD_MATE_API_URL=http://192.168.1.10:5000/api/v1
```

Make sure the backend listens on the LAN interface, Windows Firewall allows
port `5000`, and the phone and computer are on the same network.

## Firebase setup later

Create a Firebase Android app, download `google-services.json` into
`android/app/`, and run FlutterFire configuration. The app already attempts
Firebase initialization. When Firebase is available, it sends the current
Firebase ID token to the API; otherwise it falls back to the development
header.

Firebase Storage is intended for photo uploads. The journal currently stores
text notes through the API and leaves media upload for the Firebase setup step.
