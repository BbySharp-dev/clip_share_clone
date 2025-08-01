# ClipShare - Hướng dẫn khởi động dự án từ A-Z

## 1. Yêu cầu môi trường
- .NET 6 SDK hoặc mới hơn
- SQL Server (hoặc SQL Express)
- Visual Studio 2022 hoặc VS Code

## 2. Clone và mở project
```sh
git clone https://github.com/BbySharp-dev/clip_share_clone.git
cd ClipShare
```
Mở thư mục này bằng Visual Studio hoặc VS Code.

## 3. Cấu hình kết nối database
- Mở file `appsettings.Development.json`
- Sửa chuỗi kết nối tại `ConnectionStrings:DefaultConnection` cho phù hợp với SQL Server của bạn.

```
"Server=DESKTOP-6NN51UP;Database=clipshare;Trusted_connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
```

- Thay DESKTOP-6NN51UP bằng tên máy trong MySQLServer

## 4. Tạo database và migrate
Mở terminal/cmd tại thư mục `ClipShare` ngoài cùng và chạy 2 lệnh sau:

```sh
dotnet tool install --global dotnet-ef
```

```sh
dotnet ef database update
```


## 5. Build project
Chạy lệnh sau tại thư mục `ClipShare` để build project:
```sh
dotnet build
```
Nếu build thành công, tiếp tục bước tiếp theo.

## 6. Chạy project
Chạy lệnh sau tại thư mục `ClipShare`:
```sh
dotnet run
```
Hoặc nhấn F5 trong Visual Studio.

## 6. Truy cập website
Mở trình duyệt và truy cập địa chỉ hiển thị trên terminal, mặc định là:
```
http://localhost:5000
```
hoặc
```
http://localhost:5001
```

## 7. Tài khoản mặc định (tk: admin, password: Password123)

## 8. Các vấn đề thường gặp
- Nếu lỗi database: kiểm tra chuỗi kết nối và quyền truy cập SQL Server.
- Nếu lỗi thiếu package: chạy `dotnet restore`.
- Nếu lỗi port: đổi port trong `launchSettings.json` hoặc tắt ứng dụng chiếm port đó.

---
# ClipShare - Team Controller Responsibilities

## User Assignment & Controller Paths

### User 1: VideoController (phần 1)
- Path: `ClipShare/Controllers/VideoController.cs`
- Chức năng: Xem video, bình luận, lấy/tải file video, tạo/sửa video
  - `Watch`
  - `CreateComment`
  - `GetVideoFile`
  - `DownloadVideoFile`
  - `CreateEditVideo(int id)` (GET)
  - `CreateEditVideo(VideoAddEdit_vm model)` (POST)

---

### User 2: VideoController (phần 2)
- Path: `ClipShare/Controllers/VideoController.cs`
- Chức năng: API lấy danh sách video, xóa video, like/dislike, subscribe channel
  - `GetVideosForChannelGrid`
  - `DeleteVideo`
  - `SubscribeChannel`
  - `LikeDislikeVideo`

---

### User 3: AdminController (phần 1)
- Path: `ClipShare/Controllers/AdminController.cs`
- Chức năng: Quản lý user, role, khóa/mở user, xóa user
  - `Category`, `AllUsers`
  - `AddEditUser(int id)` (GET)
  - `AddEditUser(UserAddEdit_vm model)` (POST)
  - `LockUser`, `UnlockUser`, `DeleteUser`

---

### User 4: AdminController (phần 2)
- Path: `ClipShare/Controllers/AdminController.cs`
- Chức năng: Quản lý category, video chờ duyệt, duyệt video, xóa video, xem video
  - `GetCategories`, `AddEditCategory`, `DeleteCategory`
  - `PendingVideos`, `ApproveVideo`, `ViewVideo`, `DeleteVideo`

---

### User 5: ChannelController
- Path: `ClipShare/Controllers/ChannelController.cs`
- Chức năng: Quản lý kênh: xem, tạo, sửa, analytics kênh
  - `Index`, `CreateChannel`, `EditChannel`, `Analytics`

---

### User 6: AccountController
- Path: `ClipShare/Controllers/AccountController.cs`
- Chức năng: Quản lý tài khoản: đăng nhập, đăng ký, đăng xuất, kiểm tra quyền truy cập
  - `Login`, `Register`, `Logout`, `AccessDenied`

---

### ModeratorController
- Path: `ClipShare/Controllers/ModeratorController.cs`
- Chức năng: Quản lý video cho moderator
  - `AllVideos`, `DeleteVideo`

---

### MemberController
- Path: `ClipShare/Controllers/MemberController.cs`
- Chức năng: Xem kênh, subscribe, lấy video của kênh
  - `Channel`, `SubscribeChannel`, `GetMemberChannelVideos`

---

## Ghi chú

