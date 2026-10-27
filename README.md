# RoadMate

```
RoadMate/
├── frontend/                       # Flutter app
├── backend/src/RoadMate/           # ASP.NET Core 10 Web API
│   ├── Controllers/                # Layer 1: HTTP, chỉ gọi Service và map Result -> HTTP
│   ├── Services/                   # Layer 2: nghiệp vụ, trả Result / Result<T>
│   ├── Repositories/               # Layer 3: truy vấn EF Core + IUnitOfWork
│   ├── Entities/  Data/  Dtos/     # Model, DbContext, DTO + mapper
│   ├── Infrastructure/             # SMS, FCM, DevAuth, background worker
│   └── Common/                     # Result, Error, helper, StatsCalculator
└── docker-compose.yml              # PostgreSQL 16
```

## Chạy nhanh
```bash
docker compose up -d                 # Postgres :5432
cd backend/src/RoadMate
dotnet restore
dotnet run                           # Development: EnsureCreated + DevAuth + Swagger tại /swagger
```
Dev auth: thêm header `X-Dev-Uid: user1` (tuỳ chọn `X-Dev-Email`, `X-Dev-Phone`), bỏ qua Firebase token.
Production: đặt `Auth:UseDevAuth=false`, `Firebase:ProjectId`, (tuỳ chọn) `Firebase:CredentialPath` để gửi FCM, `Sms:Provider=esms`, `Goong:ApiKey`.

## Chuyển sang EF migrations (khi chốt schema)
```bash
dotnet tool install --global dotnet-ef
dotnet ef migrations add Init && dotnet ef database update
# và đặt Database:EnsureCreated=false
```

## Quy ước
- Controller -> Service -> `IUnitOfWork` (Repository). Service trả `Result`/`Result<T>`; controller dùng `Handle(...)`.
- Auth: `Authorization: Bearer <Firebase ID token>`. Sau đăng nhập app gọi `POST /api/v1/auth/sync` trước.
- Ảnh/video: client upload lên Firebase Storage rồi gửi `mediaUrl` vào `/trips/{id}/memories`.
- Thời gian ISO 8601 UTC. Alert có `clientId` để retry không gửi trùng.

## Endpoint đã làm
P0: auth/sync, me, emergency-contacts (CRUD + test), routes/directions, places/*, trips (CRUD, start, finish, summary, rest, points),
alerts (create, list, locations, cancel, resolve), debug/simulate-alert, trips/{id}/journal, memories.
P1 (một phần): heartbeat, tracking-link, public/track/*, config/emergency-numbers.
Chưa làm: places/nearby, weather, groups, job mất sóng/về trễ/tụt đoàn (entity group đã có sẵn).
