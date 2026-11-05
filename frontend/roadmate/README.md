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

## Firebase setup

Create a Firebase project and enable **Authentication > Sign-in method >
Email/Password**. Register an Android app with package name
`com.example.roadmate`, then download `google-services.json` into
`android/app/`. Run `flutter pub get` and start the app. The app displays a
Firebase email/password sign-in screen when Firebase is configured.

After sign-in or registration, the app calls `/api/v1/auth/sync` and sends the
Firebase ID token as `Authorization: Bearer <token>` on subsequent requests.
The backend validates that token and maps its Firebase UID to the RoadMate
user.

For the backend, create a Firebase service-account key from **Project
settings > Service accounts** and store it outside the repository. Configure
the backend with .NET user secrets:

```powershell
cd backend/src/RoadMate
dotnet user-secrets init
dotnet user-secrets set "Firebase:ProjectId" "your-project-id"
dotnet user-secrets set "Firebase:CredentialPath" "D:\Secrets\roadmate-firebase-admin.json"
```

Set `Auth:UseDevAuth` to `false` when testing Firebase authentication. Keep it
`true` for the development-header fallback before Firebase is configured.

Do not commit `google-services.json` or the Firebase service-account JSON.

Firebase Storage is intended for photo uploads. The journal currently stores
text notes through the API and leaves media upload for the Firebase setup step.
