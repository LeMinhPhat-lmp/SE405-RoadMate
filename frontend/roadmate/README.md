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

Use the existing Firebase project [`se405-roadmate`](https://console.firebase.google.com/project/se405-roadmate/overview).
Enable **Authentication > Sign-in method > Email/Password**. Register an
Android app with package name `com.example.roadmate`, then download
`google-services.json` into `android/app/`. The Gradle build applies the
Google Services plugin when this file exists. Run `flutter pub get` and start
the app. The app displays a Firebase email/password sign-in screen when Firebase
is configured.

After sign-in or registration, the app calls `/api/v1/auth/sync` and sends the
Firebase ID token as `Authorization: Bearer <token>` on subsequent requests.
The backend validates that token and maps its Firebase UID to the RoadMate
user.

After sign-in, RoadMate requests notification permission, obtains an FCM token,
and syncs it to `/api/v1/auth/sync`; refreshed tokens are synced again. The app
shows incoming foreground FCM alerts as local notifications. The backend sends
FCM to emergency contacts who have a RoadMate account linked to their verified
phone number. SMS remains the fallback for contacts without a linked account.

For the backend, create a Firebase service-account key from **Project
settings > Service accounts** and store it outside the repository. Configure
the backend with .NET user secrets (run from `backend/src/RoadMate`):

```powershell
cd backend/src/RoadMate
dotnet user-secrets init
dotnet user-secrets set "Firebase:ProjectId" "se405-roadmate"
dotnet user-secrets set "Firebase:CredentialPath" "D:\Secrets\roadmate-firebase-admin.json"
```

For real SMS delivery, configure an eSMS account and set its credentials as
user secrets; the default provider only logs SMS and does not send anything:

```powershell
dotnet user-secrets set "Sms:Provider" "esms"
dotnet user-secrets set "Sms:ESms:ApiKey" "<your-eSMS-api-key>"
dotnet user-secrets set "Sms:ESms:SecretKey" "<your-eSMS-secret-key>"
dotnet user-secrets set "Sms:ESms:Sandbox" "true"
```

Confirm the eSMS account's supported SMS type/brandname before using live
delivery. Test first with sandbox mode or a controlled test number. Never put
service-account keys or SMS credentials in app settings, source control, or the
Flutter app. Set `Auth:UseDevAuth` to `false` when testing Firebase
authentication; keep it `true` for the development-header fallback before
Firebase is configured.

Do not commit `google-services.json` or the Firebase service-account JSON.
FCM push requires the backend service-account credential; Firebase client
configuration alone only enables client authentication and token registration.

### Safe notification smoke test

1. Start the backend with the Firebase service account and eSMS configured.
2. Sign in on two devices/accounts. Add the second account's verified phone
	number as an emergency contact on the first account.
3. Start a trip on the first account and trigger the SOS demo only with consent
	and a controlled test recipient. Verify the backend alert delivery records
	show SMS and, for the linked account, push.
4. Do not test by sending messages to emergency services or uninvolved people.

## MVP trip flow

The current Android MVP includes route calculation through
`/api/v1/routes/directions`, trip lifecycle through `/api/v1/trips`, GPS point
and heartbeat uploads through `/api/v1/trips/{id}/points` and `/heartbeat`,
tracking-link creation and sharing, long-press SOS, emergency-call shortcut,
crash-alert demo, text notes, and camera photo memories uploaded to Firebase
Storage before being saved through `/api/v1/trips/{id}/memories`.

The backend needs a Goong API key for directions/place search and a Firebase
Admin service-account key for push notifications. Location tracking requires
granting Android location permission on the device. When Firebase is enabled,
the app re-syncs a restored Firebase session with `/api/v1/auth/sync` before
showing the authenticated shell.

Telemetry points that cannot be uploaded immediately are persisted locally and
retried when tracking resumes. The current tracker flushes on app lifecycle
changes; a true Android foreground service is still required for guaranteed
recording after the operating system suspends the Flutter process.
