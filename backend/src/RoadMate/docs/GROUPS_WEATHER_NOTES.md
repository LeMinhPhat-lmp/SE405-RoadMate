# Nhóm phượt + Thời tiết – hướng dẫn merge và ghi chú

> Build trên nền `backend_v3`. **Chưa `dotnet build`/chạy** (như v3). Đã kiểm tra bằng Roslyn: không có lỗi cú pháp/kiểu ở phần service, lỗi còn lại chỉ do thiếu package (NuGet không tải được ở môi trường tác giả).
> **Không có bảng mới**: entity `Group/GroupMember/GroupMessage` đã có sẵn trong schema từ v2, nên không cần reset DB hay migration thêm.

## 1. Merge

**File mới** – chép thẳng vào project (giữ nguyên đường dẫn trong zip): 35 file `Services/Groups/*`, `Services/Weather/*`, `Dtos/Groups/*`, `Dtos/Weather/*`, `Infrastructure/Weather/*`, `Infrastructure/Background/{Group,Weather}*Worker.cs`, `Repositories/**/Group*Repository.cs` + interface, `Entities/Groups/Group{Role,Status}.cs`, `Controllers/{Groups,Weather}Controller.cs`.

**File sửa** (zip chứa bản đầy đủ dựa trên v3; nếu bạn đã tự sửa các file này thì dùng `merge.patch` để áp từng đoạn):

| File | Thay đổi |
|---|---|
| `GlobalUsings.cs` | +4 dòng `global using` (Dtos/Services của Groups, Weather) |
| `Startup/ServiceExtensions.cs` | Đăng ký GroupService, GroupMonitorService + worker, WeatherClient (HttpClient), WeatherService, WeatherWatchService + worker |
| `Startup/OptionsExtensions.cs` | `Configure<GroupOptions>("Groups")`, `Configure<WeatherOptions>("Weather")` |
| `Startup/PersistenceExtensions.cs` | Đăng ký 3 repository nhóm |
| `Repositories/{Interfaces/IUnitOfWork,Implements/UnitOfWork}.cs` | Thêm `GroupRepository`, `GroupMemberRepository`, `GroupMessageRepository` |
| `Repositories/{Interfaces/ITripRepository,Implements/TripRepository}.cs` | Thêm `ListByGroupAndOwnerAsync`; `GetActiveForMonitoringAsync` nay Include `Waypoints` |
| `Services/Tracking/{ITelemetryService,TelemetryService}.cs`, `Controllers/TelemetryController.cs`, `Dtos/Tracking/TelemetryDtos.cs` | Heartbeat trả `HeartbeatResponse`, cập nhật vị trí lên nhóm |
| `Dtos/Trips/{TripDtos,TripMappers}.cs` | `TripDto` thêm `GroupId` ở cuối |
| `Entities/Groups/Group.cs` | Chỉ bỏ comment "chưa có service/endpoint" |

**appsettings.json** – thêm (mọi khoá đều có mặc định trong code, chỉ thêm khi muốn đổi):
```json
"Groups": { "MaxMembers": 10, "MessageRetentionDays": 7, "MaxMessagesPerPoll": 50, "StragglerMeters": 2000, "StragglerSeconds": 120,
            "LocationFreshSeconds": 120, "NotifyCooldownMinutes": 10, "MonitorIntervalSeconds": 30 },
"Weather": { "BaseUrl": "https://api.open-meteo.com", "TimeoutSeconds": 10, "CacheMinutes": 30, "GridDegrees": 0.1, "MaxParallelRequests": 4,
             "ForecastHours": 6, "HeavyRainMmPerHour": 8, "StrongGustKmh": 50, "LookaheadHours": 2,
             "WatchIntervalMinutes": 20, "WatchMaxPoints": 3, "WatchLookaheadKm": 100, "WatchArrivedMeters": 1000, "NotifyCooldownMinutes": 60 }
```

## 2. Nhóm phượt

| Endpoint | Ai | Mô tả |
|---|---|---|
| `POST groups` | user | Tạo nhóm (mã 6 ký tự, người tạo là trưởng đoàn). Body: `name, meetingName?, meetingLat?, meetingLng?, departAt?` |
| `POST groups/join` `{code}` | user | Vào nhóm; idempotent; tối đa 10 người; nhóm `closed` thì từ chối; giới hạn tần suất chống dò mã |
| `GET groups` / `GET groups/{id}` | thành viên | Danh sách nhóm của tôi / chi tiết nhóm (kèm mã, điểm tập trung, vị trí thành viên, khoảng cách tới trưởng đoàn) |
| `POST groups/{id}/leave` | thành viên | Rời nhóm. Trưởng đoàn rời → chuyển quyền cho người vào sớm nhất; người cuối rời → nhóm `closed`. Chuyến của người rời được gỡ khỏi nhóm |
| `DELETE groups/{id}/members/{userId}` | trưởng đoàn | Xoá thành viên |
| `PUT groups/{id}/meeting-point` | trưởng đoàn | `{name?, lat, lng, departAt?}` (giờ xuất phát phải ở tương lai) |
| `POST groups/{id}/start` / `close` | trưởng đoàn | `open → riding` (bật cảnh báo tụt đoàn) / đóng nhóm |
| `POST groups/{id}/code` | trưởng đoàn | Đổi mã mời (mã cũ vô hiệu) |
| `PUT trips/{tripId}/group` `{groupId}` | chủ chuyến | Gắn chuyến vào nhóm (`null` = gỡ) |
| `GET groups/{id}/messages?since=` | thành viên | Tin nhắn (tối đa 50 tin mới nhất) |
| `POST groups/{id}/messages` `{quickType}` | thành viên | `rest`, `out_of_fuel`, `need_help`, `police`. Chỉ `need_help` push cho cả nhóm |

**Realtime = polling lồng heartbeat.** Khi chuyến đã gắn nhóm, `POST trips/{id}/heartbeat` (và `POST trips/{id}/points`) cập nhật vị trí/pin của bạn lên nhóm. Response heartbeat nay là `200 { "group": { groupId, status, departAt, members[], newMessages[] } | null }` (trước đây `204`). Gửi thêm `messagesSince` (UTC) trong body để nhận tin mới; app nên lưu mốc `sentAt` lớn nhất đã thấy. Nên heartbeat 30 s/lần khi chạy.

**Tụt đoàn** (`GroupMonitorWorker`, 30 s/lần, chỉ nhóm `riding`): thành viên cách trưởng đoàn > 2 km liên tục > 2 phút (vị trí của cả hai không cũ quá 2 phút) → push cho trưởng đoàn và thành viên đó, không SMS; cooldown 10 phút. Worker còn xoá tin nhắn quá 7 ngày (mỗi giờ một lần).

**Quyết định/giới hạn cần biết**
- Người không phải thành viên nhận `404 GROUP_NOT_FOUND` (không lộ nhóm có tồn tại).
- Chỉ tin định sẵn, chưa có chat tự do (đúng khuyến nghị).
- Mốc "bắt đầu tụt" và cooldown lưu trong bộ nhớ: restart server thì đếm lại; **chỉ chạy một instance**.
- Race hiếm: hai người cùng vào khi nhóm còn đúng 1 chỗ có thể vượt 10. Chấp nhận cho đồ án.
- Push dùng FCM token của `User`; chưa có token thì chỉ ghi log. Channel `roadmate_emergency` không áp cho push nhóm (chỉ push loại `alert`).
- Chưa gửi push khi đổi điểm tập trung / khi xuất phát (chưa được yêu cầu).

## 3. Thời tiết (Open-Meteo, không cần key)

| Endpoint | Mô tả |
|---|---|
| `GET weather?lat=&lng=` | Hiện tại + dự báo `ForecastHours` giờ tới + `warnings` |
| `GET trips/{id}/weather` | Như trên cho điểm đi, từng điểm dừng, điểm đến. Điểm nào lỗi thì bỏ qua và báo `failedCount`; tất cả lỗi mới trả lỗi |

- Cảnh báo (`warnings[].code`): `HEAVY_RAIN` (≥ 8 mm/giờ), `STRONG_WIND` (gió giật ≥ 50 km/h), `THUNDERSTORM` (mã 95/96/99), xét hiện tại + 2 giờ tới.
- Cache 30 phút theo ô lưới 0.1° (~11 km): các điểm gần nhau dùng chung một lần gọi. Lỗi không được cache. Rate limit dùng policy `maps` (120 req/phút/user).
- Mốc thời gian trong response là UTC. Lượng mưa hourly là mm của giờ trước mốc đó.
- **Job `WeatherWatchWorker`** (20 phút/lần): với mỗi chuyến đang chạy có vị trí, lấy tối đa 3 điểm phía trước gần nhất (điểm dừng + điểm đến) trong 100 km (bỏ điểm cách < 1 km), nếu dự báo xấu thì gửi **một** push cho chủ chuyến (`type=weather_warning`, `codes`), không nhắc lại cùng điểm trong 60 phút (cooldown trong bộ nhớ). Chuyến chưa có heartbeat (chưa có vị trí) thì bị bỏ qua. "Điểm phía trước" là heuristic theo khoảng cách chứ chưa theo lộ trình/điểm đã đi qua.
- Lỗi/mã: `WEATHER_UNREACHABLE`/`WEATHER_TIMEOUT`/`WEATHER_PROVIDER_ERROR` (502), `WEATHER_QUOTA_EXCEEDED` (429).

## 4. Chưa kiểm chứng

1. Định dạng phản hồi Open-Meteo (`current`, `hourly`, `timeformat=unixtime`, `timezone=UTC`) viết theo tài liệu, chưa gọi thật. `WeatherParser` chịu thiếu field (dùng 0) nhưng nếu tên field khác thì số liệu sẽ là 0: kiểm tra bằng `GET weather?lat=10.77&lng=106.70`.
2. EF: `ExecuteDeleteAsync` (xoá tin cũ), `Include/ThenInclude` + `AsSplitQuery` trên quan hệ nhóm có query filter xoá mềm.
3. Chưa có unit test. Nên test trước các hàm thuần: `WeatherParser`, `WeatherAssessor`, `GroupCode`, `GroupRequestValidator`, `QuickMessages`; và `GroupMonitorService` bằng `FakeTimeProvider`.
