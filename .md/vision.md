# **RoadMate – Trợ lý phượt toàn diện**

*Tài liệu dự án đồ án môn Lập trình Mobile · Dùng làm README và đặc tả cho cả nhóm và AI agent (vibe coding)*

***Tagline:** Đi phượt vui hơn, về nhà an toàn hơn. Dẫn đường – An toàn – Nhật ký – Đi nhóm, tất cả trong một app.*

## **1\. Tổng quan dự án**

### **1.1 Vấn đề**

Người đi phượt (xe máy, ô tô) hiện phải dùng nhiều app rời rạc: Google Maps để dẫn đường, Zalo để gửi vị trí, Facebook để ghi lại kỷ niệm, app thời tiết để xem mưa gió. Khi gặp tai nạn, té xe ở đoạn đèo vắng, hết pin, mất sóng hoặc lạc đường thì **không ai biết mình đang ở đâu** và không có cơ chế tự cảnh báo.

### **1.2 Giải pháp**

**RoadMate** là app di động đi kèm người phượt từ lúc lên kế hoạch đến khi về nhà:

1. **Trước chuyến đi:** chọn điểm đi, điểm đến, các điểm dừng, xem thời tiết, tải bản đồ offline, chọn người thân nhận cảnh báo.  
2. **Trong chuyến đi:** dẫn đường, theo dõi hành trình, tự phát hiện va chạm/té ngã, cảnh báo dừng quá lâu, lệch tuyến, pin yếu, mất sóng; chụp ảnh và ghi chú nhanh.  
3. **Sau chuyến đi:** tự tạo nhật ký (bản đồ lộ trình, ảnh, thống kê), chia sẻ cho bạn bè.

### **1.3 Đối tượng sử dụng**

| Nhóm | Nhu cầu chính |
| :---- | :---- |
| Người đi phượt một mình | An toàn, người nhà theo dõi được |
| Nhóm bạn đi phượt (3–10 xe) | Biết vị trí nhau, không bị lạc đoàn |
| Người thân ở nhà | Theo dõi chuyến đi, nhận cảnh báo khi có sự cố |
| Người thích ghi nhớ | Nhật ký tự động, ảnh theo bản đồ |

### **1.4 Điểm khác biệt so với app có sẵn**

* Google Maps / VietMap: dẫn đường tốt nhưng **không có lớp an toàn** và nhật ký chuyến đi.  
* Life360: theo dõi gia đình, không hỗ trợ lộ trình phượt, không có nhật ký.  
* Polarsteps / Strava / Rever: nhật ký và cộng đồng tốt nhưng **thiếu phát hiện tai nạn và cảnh báo khẩn cấp** phù hợp thói quen người Việt.  
* Tính năng phát hiện va chạm của Pixel/iPhone: chỉ có ở một số máy, không gắn với lộ trình và danh sách người nhận tùy chỉnh theo chuyến.

**\=\> RoadMate gộp cả ba mảng (dẫn đường \+ an toàn \+ nhật ký) và tối ưu cho bối cảnh Việt Nam (số khẩn cấp VN, bản đồ VN, hỗ trợ SMS).**

## **2\. Phạm vi và mức ưu tiên**

Dùng nhãn **P0 \= bắt buộc (MVP để demo đồ án)**, **P1 \= nên có**, **P2 \= mở rộng nếu còn thời gian**.

| Module | Tính năng | Ưu tiên |
| :---- | :---- | :---- |
| Tài khoản | Đăng ký/đăng nhập, hồ sơ, thông tin xe | P0 |
| Lập chuyến đi | Chọn điểm đi/đến, điểm dừng, xem lộ trình, khoảng cách, thời gian | P0 |
| Dẫn đường | Hiển thị vị trí realtime, đường đi, hướng dẫn rẽ | P0 |
| An toàn | Phát hiện va chạm/té ngã \+ đếm ngược hỏi thăm | P0 |
| An toàn | Cảnh báo dừng quá lâu | P0 |
| An toàn | Nút SOS thủ công, liên hệ khẩn cấp, gửi SMS kèm vị trí | P0 |
| Nhật ký | Lưu lộ trình, ảnh, ghi chú gắn vị trí | P0 |
| An toàn | Cảnh báo lệch tuyến, pin yếu, mất sóng | P1 |
| Tiện ích | Tìm trạm xăng, sửa xe, bệnh viện, ATM, nhà nghỉ dọc đường | P1 |
| Thời tiết | Thời tiết theo điểm dừng, cảnh báo mưa/gió | P1 |
| Theo dõi | Người thân xem vị trí realtime qua link/app | P1 |
| Nhóm | Đi đoàn, xem vị trí thành viên, cảnh báo tụt đoàn | P1 |
| Bản đồ | Bản đồ offline | P2 |
| Tiện ích | Checklist đồ mang, sổ chi phí, nhắc bảo dưỡng xe | P2 |
| Xã hội | Chia sẻ nhật ký công khai, huy hiệu thành tích | P2 |
| Nâng cao | Phát hiện cua gắt/phanh gấp, chấm điểm lái xe an toàn | P2 |

## **3\. Mô tả chi tiết chức năng**

### **3.1 Tài khoản và hồ sơ (P0)**

* Đăng ký/đăng nhập bằng email \+ mật khẩu, Google Sign-In (và số điện thoại OTP nếu còn thời gian).  
* Hồ sơ: tên, ảnh đại diện, nhóm máu, dị ứng/bệnh nền, loại xe, biển số (dùng khi gửi cảnh báo cứu hộ).  
* **Danh sách liên hệ khẩn cấp** (tối đa 5 người): tên, số điện thoại, mối quan hệ, thứ tự ưu tiên.  
* Cài đặt: ngưỡng thời gian dừng, độ nhạy phát hiện va chạm, ngôn ngữ, chế độ sáng/tối.

### **3.2 Lập kế hoạch chuyến đi (P0)**

Giao diện giống Grab: ô *Điểm đi* và *Điểm đến* ở trên, bản đồ ở dưới.

* Tìm kiếm địa điểm (autocomplete), chọn trên bản đồ hoặc dùng vị trí hiện tại.  
* Thêm **nhiều điểm dừng** (waypoint) có thể kéo thả đổi thứ tự.  
* Tùy chọn tuyến: tránh cao tốc, tránh đường thu phí, ưu tiên đường ngắn nhất (hữu ích cho xe máy vì nhiều cao tốc cấm xe máy).  
* Hiển thị: tổng quãng đường, thời gian ước tính, thời tiết theo từng điểm.  
* Cấu hình **an toàn cho chuyến**: người nhận cảnh báo, giờ dự kiến về, bật/tắt theo dõi realtime.  
* Lưu chuyến dưới dạng *Bản nháp* hoặc *Bắt đầu ngay*.

### **3.3 Chế độ dẫn đường và theo dõi chuyến đi (P0)**

* Hiển thị vị trí xe realtime, xoay bản đồ theo hướng đi.  
* Hướng dẫn rẽ bằng chữ và giọng nói (Text-to-Speech).  
* Bảng thống kê: tốc độ hiện tại, tốc độ trung bình, quãng đường đã đi, thời gian di chuyển, thời gian nghỉ, ETA.  
* Chạy **foreground service** để vẫn ghi vị trí khi khóa màn hình.  
* Chế độ tiết kiệm pin: giảm tần suất cập nhật GPS khi xe đứng yên hoặc pin thấp.  
* Giao diện lái xe: chữ lớn, nút to, chế độ tối, thao tác một tay.

### **3.4 Hệ thống an toàn (P0, điểm nhấn của đồ án)**

#### **a) Phát hiện va chạm / té ngã**

**Dữ liệu cảm biến:** gia tốc kế (accelerometer), con quay (gyroscope), GPS (tốc độ).

**Điều kiện kích hoạt (cần đồng thời để giảm báo nhầm):**

1. Có cú sốc mạnh: độ lớn gia tốc vượt ngưỡng (ví dụ ≥ 3g đến 4g, cấu hình được).  
2. Trước đó xe đang chạy (tốc độ GPS \> 15 km/h).  
3. Sau cú sốc: tốc độ giảm đột ngột về gần 0 và/hoặc điện thoại gần như đứng yên trong 3–5 giây.

**Luồng xử lý:**

Phát hiện nghi ngờ tai nạn  
  \-\> Rung mạnh \+ còi \+ màn hình đỏ "Bạn có ổn không?"  
  \-\> Đếm ngược 30 giây (có nút "Tôi ổn" và nút "Gọi cấp cứu")  
     \|-- Người dùng bấm "Tôi ổn"  \-\> hủy cảnh báo, ghi log sự kiện báo nhầm  
     \|-- Hết 30 giây không phản hồi hoặc bấm "Cần giúp"  
           \-\> Gửi SMS \+ push (+ tùy chọn email) cho liên hệ khẩn cấp  
              kèm: tên, vị trí Google Maps, thời gian, nhóm máu, biển số, pin còn lại  
           \-\> Hiển thị nút gọi nhanh 115 / 113 / 112  
           \-\> Lặp lại gửi mỗi 2 phút kèm vị trí mới cho đến khi người dùng xác nhận an toàn  
**Pseudo-code tham khảo:**

magnitude \= sqrt(ax^2 \+ ay^2 \+ az^2) / 9.81  
if tripActive and speedBefore \> 15 and magnitude \> THRESHOLD:  
    startStillnessWindow(5s)  
    if speedNow \< 3 and variance(accelerometer) \< small:  
        triggerCrashAlert()  
**Mẹo giảm báo nhầm:** bỏ qua khi điện thoại rơi lúc xe đang đứng yên; cho phép hoãn cảnh báo khi người dùng tạm dừng chủ động; ghi log để chỉnh ngưỡng.

#### **b) Cảnh báo dừng quá lâu**

* Nếu tốc độ \< 2 km/h liên tục hơn **N phút** (mặc định 15 phút) mà người dùng **chưa bấm "Nghỉ chân"** thì hiện thông báo "Bạn đã dừng 15 phút, mọi thứ ổn chứ?".  
* Không phản hồi sau 3 phút thì leo thang: rung và âm thanh lớn, rồi gửi cảnh báo cho người thân.  
* Chế độ **"Nghỉ chân"**: người dùng đặt hẹn giờ (30 phút, 1 giờ, 2 giờ) để không bị làm phiền; hết giờ thì nhắc tiếp tục hành trình.  
* Ngưỡng dừng có thể khác nhau tùy bối cảnh: ở điểm dừng đã lên kế hoạch (quán cà phê, trạm xăng) được phép lâu hơn.

#### **c) Nút SOS thủ công**

* Nút SOS luôn hiện trên màn hình dẫn đường, **giữ 3 giây** để kích hoạt (tránh bấm nhầm).  
* Gửi ngay vị trí cho liên hệ khẩn cấp, bật còi, mở nhanh bàn phím gọi 115/113/112.

#### **d) Các cảnh báo an toàn khác (P1)**

| Cảnh báo | Điều kiện | Hành động |
| :---- | :---- | :---- |
| Lệch tuyến | Cách đường dự kiến \> 300 m trong 1 phút | Hỏi "Bạn đổi lộ trình à?" \+ gợi ý tính lại |
| Pin yếu | Pin \< 15% khi đang đi | Nhắc bật tiết kiệm pin, gửi vị trí cuối cho người thân |
| Mất sóng | Mất mạng \> 10 phút | Lưu vị trí offline, tự đồng bộ khi có sóng, gửi vị trí cuối cùng |
| Về trễ giờ | Quá giờ dự kiến về \+ 60 phút | Nhắc người dùng; không phản hồi thì báo người thân |
| Thời tiết xấu | Dự báo mưa to/gió mạnh/sạt lở ở điểm phía trước | Thông báo gợi ý dừng hoặc đổi tuyến |
| Đi đêm | Sau 22h hoặc trước 5h | Nhắc nghỉ ngơi, gợi ý nhà nghỉ gần nhất |
| Mệt mỏi | Lái liên tục \> 3 giờ | Nhắc nghỉ chân |

### **3.5 Theo dõi từ người thân (P1)**

* Khi bắt đầu chuyến, app tạo **link theo dõi** (ví dụ https\://roadmate.app/track/abc123). Người thân mở bằng trình duyệt (không cần cài app) để xem vị trí realtime, pin, tốc độ, lộ trình.  
* Link tự hết hạn sau khi chuyến kết thúc.  
* Người thân có app thì nhận push notification khi có cảnh báo.

### **3.6 Nhật ký chuyến đi (P0)**

* **Tự động ghi:** lộ trình thực tế (đường màu), quãng đường, thời gian, tốc độ, điểm dừng.  
* **Ghi nhanh:** chụp ảnh/video ngắn, ghi chú văn bản hoặc giọng nói, gắn tự động vị trí và thời gian.  
* Dòng thời gian (timeline) theo ngày: từng điểm dừng kèm ảnh, ghi chú, thời tiết.  
* **Bản đồ kỷ niệm:** hiển thị ghim ảnh trên lộ trình.  
* Tổng kết cuối chuyến: tổng km, thời gian, số điểm dừng, tốc độ cao nhất, độ cao lớn nhất, số cảnh báo.  
* Chia sẻ: xuất ảnh tổng kết chuyến đi (đẹp để đăng Facebook/Instagram), PDF hoặc link công khai.  
* Gắn nhãn tâm trạng, đánh giá điểm dừng (quán ăn, nhà nghỉ, cảnh đẹp).

### **3.7 Phượt theo nhóm (P1)**

* Tạo **phòng chuyến đi**, mời bạn bè bằng mã 6 ký tự hoặc link.  
* Bản đồ chung hiển thị vị trí mọi thành viên realtime.  
* Cảnh báo **tụt đoàn**: thành viên cách trưởng đoàn \> 2 km hoặc dừng lâu khi cả đoàn đang chạy.  
* Chat nhanh và nút gửi tin định sẵn ("Dừng nghỉ", "Hết xăng", "Cần hỗ trợ", "Có cảnh sát giao thông").  
* Điểm tập trung chung và đếm ngược giờ xuất phát.

### **3.8 Tiện ích dọc đường (P1)**

* Tìm quanh vị trí/điểm sắp tới: trạm xăng, tiệm sửa xe, bệnh viện/trạm y tế, ATM, nhà nghỉ/homestay, quán ăn.  
* Chế độ **"Hết xăng/thủng lốp"**: một chạm tìm trạm gần nhất, gọi cứu hộ.  
* Danh bạ khẩn cấp theo vùng: công an, cấp cứu, cứu hộ giao thông địa phương (người dùng tự lưu hoặc nhập sẵn).  
* Checklist đồ mang (áo mưa, vá xe, bơm, thuốc, sạc dự phòng...), có mẫu theo loại chuyến.  
* Sổ chi phí chuyến đi (xăng, ăn uống, nhà nghỉ), tính tổng và chia tiền theo đầu người.  
* Nhắc bảo dưỡng xe theo số km (thay nhớt, kiểm tra lốp, xích).

### **3.9 Gợi ý ý tưởng mở rộng (P2)**

* Chấm điểm lái xe an toàn dựa trên phanh gấp, tăng tốc mạnh, tốc độ cao.  
* Huy hiệu thành tích: "Chinh phục đèo", "1000 km", "Phượt đêm".  
* Đề xuất cung đường phượt nổi tiếng do cộng đồng chia sẻ.  
* Widget màn hình khóa, tích hợp smartwatch (gửi nhịp tim, phát hiện té ngã).  
* Chế độ giọng nói "Hey RoadMate": ghi chú, gọi SOS rảnh tay.  
* Tích hợp AI: tóm tắt nhật ký thành bài viết, gợi ý lịch trình theo thời gian và sở thích.

## **4\. Luồng người dùng chính**

### **Luồng 1: Chuẩn bị và bắt đầu chuyến**

Mở app \-\> Tạo chuyến mới \-\> Chọn điểm đi/đến \+ điểm dừng  
\-\> Xem lộ trình, thời tiết \-\> Chọn người nhận cảnh báo  
\-\> Bật theo dõi (tùy chọn) \-\> Bấm "Bắt đầu" \-\> Chuyển sang Chế độ dẫn đường

### **Luồng 2: Sự cố trên đường**

Cảm biến phát hiện va chạm \-\> Hỏi thăm 30s  
\-\> (Không phản hồi) Gửi SMS/Push cho người thân kèm vị trí  
\-\> Hiện nút gọi 115/113/112 \-\> Cập nhật vị trí mỗi 2 phút  
\-\> Người dùng bấm "Tôi ổn" hoặc người thân xác nhận \-\> Kết thúc cảnh báo

### **Luồng 3: Kết thúc và xem nhật ký**

Bấm "Kết thúc chuyến" \-\> Tổng kết thống kê \-\> Thêm/chỉnh ảnh, ghi chú  
\-\> Lưu nhật ký \-\> Chia sẻ (ảnh/PDF/link)

## **5\. Danh sách màn hình**

| \# | Màn hình | Mô tả |
| :---- | :---- | :---- |
| 1 | Splash / Onboarding | Giới thiệu, xin quyền (vị trí, thông báo, camera, cảm biến) |
| 2 | Đăng nhập / Đăng ký | Email, Google |
| 3 | Trang chủ | Chuyến sắp tới, chuyến gần đây, nút "Chuyến mới", thời tiết |
| 4 | Lập chuyến đi | Điểm đi/đến, waypoint, tùy chọn tuyến |
| 5 | Chi tiết lộ trình | Bản đồ, thống kê, thời tiết, cài đặt an toàn |
| 6 | Dẫn đường | Bản đồ, hướng rẽ, thống kê, nút SOS, nút Nghỉ chân, nút Chụp ảnh |
| 7 | Cảnh báo va chạm | Màn hình đỏ toàn màn hình, đếm ngược, nút "Tôi ổn" / "Cần giúp" |
| 8 | Hỏi thăm dừng lâu | Popup nhắc, chọn nghỉ chân |
| 9 | Nhật ký chuyến đi | Danh sách và chi tiết (timeline \+ bản đồ ảnh) |
| 10 | Tổng kết chuyến | Thống kê, ảnh, chia sẻ |
| 11 | Liên hệ khẩn cấp | Thêm/sửa/xóa, gửi thử |
| 12 | Nhóm đi phượt | Tạo/tham gia phòng, bản đồ nhóm, chat |
| 13 | Tiện ích quanh đây | Trạm xăng, sửa xe, bệnh viện... |
| 14 | Hồ sơ và Cài đặt | Thông tin cá nhân, xe, ngưỡng cảnh báo, giao diện |

## **6\. Công nghệ đề xuất**

### **6.1 Lựa chọn khuyến nghị (dễ làm, phù hợp đồ án)**

| Lớp | Công nghệ | Lý do |
| :---- | :---- | :---- |
| Mobile | **Flutter (Dart)** | 1 codebase cho Android \+ iOS, UI đẹp, nhiều package cảm biến/bản đồ, AI agent sinh code Flutter tốt |
| State management | **Riverpod** (hoặc Bloc) | Dễ tổ chức, dễ test |
| Bản đồ | **Google Maps SDK** *hoặc* **MapLibre/Goong/VietMap** | Google dễ dùng; Goong/VietMap dữ liệu VN tốt, có gói miễn phí |
| Dẫn đường (routing) | Google Directions API / Goong Directions / OpenRouteService | Lấy polyline và hướng dẫn rẽ |
| Tìm địa điểm | Google Places / Goong Place API | Autocomplete và tìm quanh đây |
| Backend | **Firebase** (Auth, Firestore, Storage, Cloud Functions, FCM) | Không cần tự dựng server, realtime sẵn có |
| Gửi SMS | Android SmsManager (gửi trực tiếp, chỉ Android) hoặc API SMS (eSMS, Twilio) gọi qua Cloud Functions | iOS không cho app tự gửi SMS nền |
| Thông báo | Firebase Cloud Messaging \+ flutter\_local\_notifications | Push cho người thân và cảnh báo cục bộ |
| Thời tiết | OpenWeatherMap hoặc Open-Meteo (miễn phí, không cần key) | Dự báo theo tọa độ |
| Lưu cục bộ/offline | Drift (SQLite) hoặc Hive/Isar | Lưu lộ trình khi mất mạng, đồng bộ sau |
| Phân phối bản demo | Firebase App Distribution / APK | Gửi cho giảng viên và thành viên test |

*Có thể thay bằng **React Native (Expo)** nếu cả nhóm quen JavaScript; tài liệu này viết theo Flutter nhưng kiến trúc áp dụng được cho cả hai.*

### **6.2 Các package Flutter gợi ý**

| Chức năng | Package |
| :---- | :---- |
| Vị trí/GPS | geolocator, location |
| Chạy nền | flutter\_background\_service hoặc flutter\_foreground\_task |
| Cảm biến | sensors\_plus (gia tốc kế, con quay) |
| Bản đồ | google\_maps\_flutter hoặc maplibre\_gl |
| Quyền | permission\_handler |
| Gửi SMS (Android) | telephony / another\_telephony hoặc flutter\_sms |
| Gọi điện | url\_launcher (mở tel:115) |
| Camera/ảnh | image\_picker, camera |
| Giọng nói | flutter\_tts (đọc hướng dẫn), speech\_to\_text |
| Thông báo | firebase\_messaging, flutter\_local\_notifications |
| Pin | battery\_plus |
| Mạng | connectivity\_plus |
| Âm thanh/rung | audioplayers, vibration |
| Chia sẻ | share\_plus, screenshot, pdf |
| Giữ màn hình sáng | wakelock\_plus |

### **6.3 Kiến trúc tổng thể**

\+---------------------------+         \+---------------------------+  
\|        Flutter App        \|         \|         Firebase          \|  
\|  UI (Screens, Widgets)    \| \<-----\> \|  Auth  \| Firestore        \|  
\|  State (Riverpod)         \|         \|  Storage \| Functions      \|  
\|  Services:                \|         \|  FCM (push)               \|  
\|   \- LocationService       \|         \+-------------+-------------+  
\|   \- CrashDetectionService \|                       \|  
\|   \- IdleDetectionService  \|                       v  
\|   \- SosService (SMS/Push) \|         \+---------------------------+  
\|   \- TripRecorder          \|         \|  API ngoài: Maps, Places, \|  
\|   \- Local DB (Drift)      \|         \|  Directions, Weather, SMS \|  
\+---------------------------+         \+---------------------------+  
**Nguyên tắc:** các tính năng an toàn (phát hiện va chạm, dừng lâu, SOS) phải **chạy được offline** ở phía app; phần cloud chỉ để đồng bộ, theo dõi realtime và gửi push/SMS dự phòng.

## **7\. Thiết kế dữ liệu (Firestore)**

users/{uid}  
  name, email, avatarUrl, bloodType, medicalNotes, vehicleType, plateNumber, fcmToken  
  emergencyContacts/{id}: name, phone, relation, priority

trips/{tripId}  
  ownerId, title, status (draft \| active \| finished)  
  origin {lat, lng, name}, destination {lat, lng, name}  
  waypoints \[ {lat, lng, name, order} \]  
  plannedPolyline, distanceKm, etaMinutes  
  startTime, endTime, expectedReturnTime  
  settings {idleMinutes, crashSensitivity, alertContactIds\[\], liveShare}  
  trackingToken, groupId  
  stats {distanceKm, movingMin, restMin, maxSpeed, avgSpeed, alertCount}  
  points/{pointId}: lat, lng, speed, altitude, battery, timestamp   \# lộ trình thực tế  
  memories/{id}: type (photo\|note\|video), mediaUrl, text, lat, lng, timestamp, mood  
  alerts/{id}: type (crash\|idle\|sos\|offroute\|lowBattery), status (pending\|sent\|cancelled\|resolved), lat, lng, timestamp

groups/{groupId}  
  code, leaderId, tripId, members \[uid\]  
  messages/{id}: senderId, text, quickType, timestamp  
  locations/{uid}: lat, lng, battery, updatedAt  
**Quy tắc bảo mật:** chỉ chủ chuyến đọc/ghi trips; thành viên nhóm chỉ đọc dữ liệu của nhóm mình; link theo dõi dùng trackingToken ngẫu nhiên và chỉ đọc.

## **8\. Cấu trúc thư mục dự án (Flutter)**

roadmate/  
├── lib/  
│   ├── main.dart  
│   ├── app.dart                  \# MaterialApp, theme, router  
│   ├── core/  
│   │   ├── constants/            \# key, ngưỡng, số khẩn cấp  
│   │   ├── theme/  
│   │   ├── utils/  
│   │   └── router/  
│   ├── data/  
│   │   ├── models/               \# Trip, Memory, Alert, Contact, UserProfile  
│   │   ├── repositories/         \# TripRepository, AuthRepository...  
│   │   └── local/                \# Drift/Hive  
│   ├── services/  
│   │   ├── location\_service.dart  
│   │   ├── trip\_recorder.dart  
│   │   ├── crash\_detection\_service.dart  
│   │   ├── idle\_detection\_service.dart  
│   │   ├── sos\_service.dart  
│   │   ├── notification\_service.dart  
│   │   ├── routing\_service.dart  
│   │   └── weather\_service.dart  
│   ├── features/  
│   │   ├── auth/  
│   │   ├── home/  
│   │   ├── trip\_planner/  
│   │   ├── navigation/  
│   │   ├── safety/               \# màn hình cảnh báo, SOS, hỏi thăm  
│   │   ├── journal/  
│   │   ├── group/  
│   │   ├── nearby/  
│   │   └── profile/  
│   └── widgets/                  \# component dùng chung  
├── assets/  
├── test/  
├── functions/                    \# Cloud Functions (gửi SMS/push)  
├── pubspec.yaml  
└── README.md

## **9\. Hướng dẫn cài đặt và chạy dự án**

### **9.1 Yêu cầu**

* Flutter SDK \>= 3.x (flutter \--version), Dart đi kèm  
* Android Studio (Android SDK, emulator) hoặc VS Code \+ extension Flutter  
* Xcode (nếu build iOS, chỉ trên macOS)  
* Node.js \>= 18 (cho Firebase CLI và Cloud Functions)  
* Tài khoản Google (Firebase) và các API key bản đồ  
* **Máy thật Android** để test cảm biến và SMS (emulator không mô phỏng tốt va chạm)

### **9.2 Các bước**

\# 1\. Clone dự án  
git clone \<repo-url\> roadmate  
cd roadmate

\# 2\. Cài dependency  
flutter pub get

\# 3\. Cài Firebase CLI và FlutterFire  
npm install \-g firebase-tools  
dart pub global activate flutterfire\_cli  
firebase login  
flutterfire configure          \# chọn project Firebase, sinh firebase\_options.dart

\# 4\. Tạo file cấu hình môi trường  
cp .env.example .env  
\# Điền: MAPS\_API\_KEY, WEATHER\_API\_KEY, SMS\_API\_KEY (nếu dùng)

\# 5\. Chạy app  
flutter run                    \# trên thiết bị/emulator đã kết nối

### **9.3 Cấu hình Firebase**

1. Tạo project trên Firebase Console.  
2. Bật **Authentication** (Email/Password, Google), **Firestore**, **Storage**, **Cloud Messaging**.  
3. Thêm app Android (package name khớp applicationId), tải google-services.json vào android/app/.  
4. (Nếu dùng iOS) thêm app iOS, tải GoogleService-Info.plist vào ios/Runner/.  
5. Triển khai Cloud Functions: cd functions && npm install && firebase deploy \--only functions.

### **9.4 Quyền cần khai báo**

**Android (AndroidManifest.xml):**

\<uses-permission android:name="android.permission.ACCESS\_FINE\_LOCATION"/\>  
\<uses-permission android:name="android.permission.ACCESS\_COARSE\_LOCATION"/\>  
\<uses-permission android:name="android.permission.ACCESS\_BACKGROUND\_LOCATION"/\>  
\<uses-permission android:name="android.permission.FOREGROUND\_SERVICE"/\>  
\<uses-permission android:name="android.permission.FOREGROUND\_SERVICE\_LOCATION"/\>  
\<uses-permission android:name="android.permission.POST\_NOTIFICATIONS"/\>  
\<uses-permission android:name="android.permission.SEND\_SMS"/\>  
\<uses-permission android:name="android.permission.CAMERA"/\>  
\<uses-permission android:name="android.permission.VIBRATE"/\>  
\<uses-permission android:name="android.permission.WAKE\_LOCK"/\>  
**iOS (Info.plist):** NSLocationWhenInUseUsageDescription, NSLocationAlwaysAndWhenInUseUsageDescription, NSCameraUsageDescription, NSPhotoLibraryUsageDescription, NSMotionUsageDescription; bật Background Modes (Location updates).

### **9.5 Biến môi trường (.env.example)**

MAPS\_API\_KEY=  
WEATHER\_API\_KEY=  
SMS\_API\_KEY=  
EMERGENCY\_NUMBERS=115,113,112  
*Không commit file .env và khóa API lên Git; hạn chế key theo package name/SHA-1.*

## **10\. Số khẩn cấp tại Việt Nam**

| Số | Dịch vụ |
| :---- | :---- |
| 113 | Công an |
| 114 | Cứu hỏa, cứu nạn cứu hộ |
| 115 | Cấp cứu y tế |
| 112 | Số khẩn cấp thống nhất |

App chỉ **hiển thị nút gọi** (mở trình quay số), **không tự động gọi** các số này khi chưa được người dùng xác nhận, để tránh gọi nhầm gây phiền cho tổng đài.

## **11\. Kế hoạch thực hiện (gợi ý 8 tuần)**

| Tuần | Nội dung | Kết quả |
| :---- | :---- | :---- |
| 1 | Chốt ý tưởng, thiết kế UI (Figma), dựng project, cấu hình Firebase, Auth | Đăng nhập chạy được |
| 2 | Bản đồ, vị trí realtime, tìm địa điểm, lập chuyến đi, vẽ lộ trình | Chọn A–B xem đường đi |
| 3 | Chế độ dẫn đường, ghi lộ trình, foreground service, thống kê | Đi thử và ghi được hành trình |
| 4 | Phát hiện va chạm \+ đếm ngược \+ SOS \+ gửi SMS/push | Demo tai nạn giả lập |
| 5 | Cảnh báo dừng lâu, lệch tuyến, pin yếu, mất sóng | Bộ cảnh báo hoàn chỉnh |
| 6 | Nhật ký chuyến đi (ảnh, ghi chú, timeline, tổng kết, chia sẻ) | Nhật ký hoàn chỉnh |
| 7 | Link theo dõi, đi nhóm, tiện ích quanh đây, thời tiết (P1) | Tính năng nâng cao |
| 8 | Test, sửa lỗi, tối ưu pin, quay video demo, viết báo cáo, slide | Nộp đồ án |

### **Gợi ý phân công cho nhóm 4 người**

| Thành viên | Phụ trách |
| :---- | :---- |
| A | Bản đồ, lập chuyến, dẫn đường, routing |
| B | Module an toàn: cảm biến, va chạm, dừng lâu, SOS, SMS |
| C | Nhật ký, camera, lưu trữ, chia sẻ, thống kê |
| D | Auth, hồ sơ, Firebase backend, nhóm, theo dõi realtime, UI/UX chung |

## **12\. Kiểm thử**

| Tình huống | Cách test | Kết quả mong đợi |
| :---- | :---- | :---- |
| Va chạm giả lập | Cầm máy lắc mạnh và dừng đột ngột khi đang "đi" (có thể bật chế độ debug giả lập tốc độ) | Hiện màn hình đếm ngược |
| Không phản hồi | Để hết 30 giây | Gửi SMS/push kèm vị trí đúng |
| Báo nhầm | Bấm "Tôi ổn" | Hủy cảnh báo, ghi log |
| Dừng lâu | Đặt ngưỡng 1 phút, đứng yên | Hiện popup hỏi thăm, sau đó leo thang |
| Mất mạng | Bật chế độ máy bay giữa chuyến | Vẫn ghi lộ trình, đồng bộ khi có mạng |
| Pin yếu | Giả lập pin \< 15% | Hiện cảnh báo và gửi vị trí |
| Khóa màn hình | Khóa máy 10 phút khi đang đi | Vẫn ghi vị trí liên tục |
| Quyền | Từ chối quyền vị trí/SMS | App giải thích và dẫn đến phần cài đặt |

***Mẹo demo:** thêm **Chế độ giả lập (Debug Simulator)** với nút "Giả lập va chạm", "Giả lập dừng lâu", "Giả lập mất mạng", vì khi bảo vệ không ai muốn thật sự ngã xe. Nên có sẵn.*

## **13\. Rủi ro và lưu ý**

| Rủi ro | Cách xử lý |
| :---- | :---- |
| Hao pin do GPS \+ cảm biến liên tục | Giảm tần suất GPS khi đứng yên, chế độ tiết kiệm pin, lấy mẫu cảm biến vừa đủ |
| Hệ điều hành tắt service nền (đặc biệt máy Xiaomi, Oppo, Samsung) | Foreground service có thông báo cố định, hướng dẫn tắt tối ưu pin cho app |
| Báo nhầm khi xóc mạnh, đi đường xấu | Kết hợp nhiều điều kiện (gia tốc \+ tốc độ \+ đứng yên), cho chỉnh độ nhạy |
| iOS hạn chế gửi SMS tự động và chạy nền | Dùng push/API SMS qua server; ưu tiên demo trên Android |
| Google Play hạn chế quyền SEND\_SMS | Chấp nhận khi chỉ chạy đồ án; khi phát hành thật dùng API SMS qua server |
| Quyền riêng tư dữ liệu vị trí | Chỉ chia sẻ khi người dùng bật; link theo dõi tự hết hạn; mã hóa và phân quyền Firestore |
| Hạn mức API bản đồ | Dùng gói miễn phí, cache kết quả, đặt giới hạn quota |
| An toàn khi sử dụng | Giao diện tối giản khi lái xe, **nhắc không thao tác điện thoại khi đang chạy**, ưu tiên giọng nói |

**Tuyên bố miễn trừ:** đây là sản phẩm học tập, không thay thế dịch vụ cứu hộ chuyên nghiệp. Người dùng cần chủ động gọi cấp cứu khi có sự cố nghiêm trọng.

## **14\. Hướng dẫn dùng AI agent để vibe coding**

### **14.1 Quy tắc chung (dán vào đầu mỗi phiên làm việc với AI)**

Bạn là senior Flutter developer. Dự án: RoadMate (app phượt: dẫn đường \+ an toàn \+ nhật ký).  
Đọc kỹ tài liệu dự án (README) trước khi code.  
Quy ước:  
\- Flutter \+ Dart, state management bằng Riverpod, backend Firebase.  
\- Kiến trúc theo thư mục: core / data / services / features / widgets.  
\- Mỗi feature có: screen, controller (provider), model, repository.  
\- Code sạch, đặt tên tiếng Anh, comment ngắn gọn, xử lý lỗi và trạng thái loading/empty/error.  
\- Không hard-code API key, đọc từ .env.  
\- Tính năng an toàn phải hoạt động offline.  
\- Mỗi lần chỉ làm MỘT nhiệm vụ, sau đó liệt kê file đã tạo/sửa và cách chạy thử.

### **14.2 Chuỗi prompt gợi ý theo từng bước**

1. *"Khởi tạo project Flutter theo cấu trúc thư mục trong README, thêm các package ở mục 6.2, cấu hình Riverpod, router (go\_router) và theme sáng/tối."*  
2. *"Làm module Auth với Firebase (email \+ Google), màn hình đăng nhập/đăng ký và hồ sơ người dùng theo mô hình dữ liệu mục 7."*  
3. *"Làm màn hình Lập chuyến đi giống Grab: hai ô điểm đi/điểm đến có autocomplete, bản đồ phía dưới, thêm nhiều waypoint, gọi Directions API và vẽ polyline, hiển thị km và thời gian."*  
4. *"Làm LocationService \+ TripRecorder: foreground service ghi vị trí mỗi 3–5 giây vào local DB, đồng bộ lên Firestore khi có mạng, tính thống kê."*  
5. *"Làm CrashDetectionService dùng sensors\_plus \+ geolocator theo thuật toán mục 3.4a, có cấu hình độ nhạy, và màn hình cảnh báo đếm ngược 30 giây."*  
6. *"Làm SosService: gửi SMS (Android) và push (FCM qua Cloud Function) tới liên hệ khẩn cấp kèm link Google Maps, tên, nhóm máu, pin; lặp lại mỗi 2 phút đến khi hủy."*  
7. *"Làm IdleDetectionService: cảnh báo dừng lâu, chế độ Nghỉ chân, leo thang cảnh báo theo mục 3.4b."*  
8. *"Làm module Nhật ký: chụp ảnh/ghi chú gắn vị trí, timeline theo ngày, bản đồ ghim ảnh, màn hình tổng kết, xuất ảnh chia sẻ."*  
9. *"Làm Debug Simulator để giả lập va chạm, dừng lâu, mất mạng, pin yếu phục vụ demo."*  
10. *"Thêm tính năng P1: link theo dõi, nhóm đi phượt, tiện ích quanh đây, thời tiết."*  
11. *"Viết unit test cho thuật toán phát hiện va chạm và dừng lâu, rà soát quyền, tối ưu pin."*

### **14.3 Mẹo khi vibe coding**

* Đưa cho AI **đúng đoạn tài liệu** liên quan đến nhiệm vụ thay vì cả file dài.  
* Làm từng tính năng nhỏ, chạy thử rồi mới sang bước tiếp theo; commit Git sau mỗi bước chạy được.  
* Gặp lỗi: dán nguyên log lỗi \+ file liên quan cho AI.  
* Mỗi thành viên làm trên **nhánh riêng** (feature/safety, feature/journal...), tránh sửa chung một file; thống nhất model dữ liệu (mục 7\) trước.  
* Luôn test cảm biến, GPS, SMS trên **máy thật**.

## **15\. Tiêu chí hoàn thành (Definition of Done) cho bản demo**

* ☐ Đăng nhập, lưu liên hệ khẩn cấp  
* ☐ Chọn A → B, có điểm dừng, xem lộ trình  
* ☐ Dẫn đường chạy ổn, ghi lộ trình khi khóa màn hình  
* ☐ Phát hiện va chạm → đếm ngược → gửi SMS/push kèm vị trí  
* ☐ Cảnh báo dừng quá lâu và chế độ Nghỉ chân  
* ☐ Nút SOS giữ 3 giây  
* ☐ Nhật ký có ảnh, ghi chú, bản đồ, tổng kết, chia sẻ  
* ☐ Debug Simulator để demo an toàn  
* ☐ Video demo, báo cáo, slide hoàn chỉnh

## **16\. Tài liệu tham khảo**

* Flutter: https\://docs.flutter.dev  
* Firebase cho Flutter: https\://firebase.flutter.dev  
* Package sensors\_plus, geolocator, flutter\_background\_service trên https\://pub.dev  
* Goong Maps: https\://goong.io · VietMap: https\://maps.vietmap.vn  
* OpenRouteService: https\://openrouteservice.org · Open-Meteo: https\://open-meteo.com  
* Material Design 3: https\://m3.material.io

*Phiên bản 1.0 · Tài liệu sống: cả nhóm cập nhật khi thay đổi phạm vi hoặc công nghệ.*