# 🎬 ClipShare - Video Sharing Platform - Hướng Dẫn Kiến Trúc Toàn Diện

## 📑 Mục Lục
- [Tổng Quan Dự Án](#tổng-quan-dự-án)
- [Kiến Trúc Clean Architecture](#kiến-trúc-clean-architecture)
- [Công Nghệ Sử Dụng](#công-nghệ-sử-dụng)
- [Cấu Trúc Dự Án](#cấu-trúc-dự-án)
- [Domain Models](#domain-models)
- [Design Patterns](#design-patterns)
- [Tính Năng Chính](#tính-năng-chính)
- [API Endpoints](#api-endpoints)
- [Database Schema](#database-schema)
- [Security & Authentication](#security--authentication)
- [Hướng Dẫn Setup](#hướng-dẫn-setup)
- [Best Practices](#best-practices)

---

## 🎯 Tổng Quan Dự Án

**ClipShare** là một nền tảng chia sẻ video tương tự YouTube, được xây dựng bằng **.NET 8** với kiến trúc **Clean Architecture**. Dự án cho phép người dùng upload, xem, tương tác với video và quản lý kênh cá nhân.

### 🎨 Tính Năng Nổi Bật
- ✅ **Video Sharing Platform** hoàn chỉnh
- ✅ **Multi-Role System** (Admin, Moderator, User)
- ✅ **Content Moderation** system
- ✅ **Real-time Interactions** (Like, Comment, Subscribe)
- ✅ **Channel Management** system
- ✅ **View Tracking & Analytics**
- ✅ **File Upload Management**

---

## 🏗️ Kiến Trúc Clean Architecture

### 📊 Sơ Đồ Kiến Trúc

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTATION LAYER                       │
│                     (ClipShare)                             │
│  Controllers • Views • ViewModels • Services • Extensions  │
├─────────────────────────────────────────────────────────────┤
│                   APPLICATION LAYER                         │
│               (Business Logic & Services)                   │
│         PhotoService • Extensions • Helpers                │
├─────────────────────────────────────────────────────────────┤
│                     DOMAIN LAYER                            │
│                   (ClipShare.Core)                          │
│       Entities • IRepositories • DTOs • Pagination         │
├─────────────────────────────────────────────────────────────┤
│                  INFRASTRUCTURE LAYER                       │
│                (ClipShare.DataAccess)                       │
│        DbContext • Repositories • Migrations • Config      │
├─────────────────────────────────────────────────────────────┤
│                     SHARED LAYER                            │
│                  (ClipShare.Utility)                        │
│              Constants • Helpers • Extensions               │
└─────────────────────────────────────────────────────────────┘
```

### 🔗 Dependency Flow
```
ClipShare → ClipShare.DataAccess → ClipShare.Core ← ClipShare.Utility
```

**Nguyên Tắc:** Các layer bên trong không biết gì về layer bên ngoài (Dependency Inversion Principle)

---

## 🎯 Chức Năng Chính Từng Layer

### 🎨 1. PRESENTATION LAYER (ClipShare)
**Trách nhiệm:** Xử lý HTTP requests, UI rendering, và user interactions

#### 📋 Controllers & Chức năng:
```csharp
// AccountController - Quản lý Authentication
- Login/Logout user
- Register new accounts  
- Password management
- Account verification

// VideoController - Core video operations
- Watch video (với view tracking)
- Upload/Edit video (với file validation)
- Download video files
- Delete videos
- Create/manage comments
- Like/Dislike videos
- API endpoints cho AJAX calls

// ChannelController - Channel management
- Create personal channels
- Manage channel information
- View channel analytics
- Channel video grid display

// HomeController - Public pages
- Display homepage with video grid
- Search functionality
- Category filtering
- Pagination handling

// AdminController - System administration
- User management (CRUD)
- System settings
- Global statistics
- Role assignment

// ModeratorController - Content moderation
- Review pending videos (IsApproved)
- Content approval/rejection
- Comment moderation
- Report handling

// MemberController - User features
- Profile management
- Subscription management
- View history
- Personal dashboard
```

#### 🎭 ViewModels & Data Shaping:
```csharp
// Video ViewModels
- VideoWatch_vm: Tất cả data cần thiết cho video player
- VideoAddEdit_vm: Form data cho upload/edit
- VideoGrid_vm: Data cho grid display

// Account ViewModels  
- Login_vm: Login form với validation
- Register_vm: Registration form
- Profile_vm: User profile data

// Channel ViewModels
- Channel_vm: Channel information display
- ChannelCreate_vm: Channel creation form
```

#### 🔧 Services & Business Logic:
```csharp
// PhotoService - File management
- UploadPhotoLocally(): Upload thumbnails
- DeletePhotoLocally(): Clean up files
- Validate file types và sizes

// Extensions
- UserClaimsExtensions: Get user info from claims
- WebApplicationBuilderExtensions: DI configuration
```

### 🏛️ 2. DOMAIN LAYER (ClipShare.Core)
**Trách nhiệm:** Business rules, domain logic, và data contracts

#### 🎭 Entities & Domain Models:
```csharp
// AppUser - User domain logic
- Name validation rules
- CreatedAt timestamp tracking
- Navigation properties để access related data

// Video - Core business entity
- Title/Description validation
- IsApproved moderation flag
- CreatedAt tracking
- File association (VideoFile)
- Social interactions (Comments, LikeDislikes, Views)

// Channel - Channel business logic  
- One-to-one relationship với AppUser
- Video collection management
- Subscriber tracking

// Comment - Comment system logic
- Content validation
- Timestamp tracking
- User/Video associations

// Subscribe - Subscription logic
- Many-to-many User ↔ Channel
- Subscription date tracking

// LikeDislike - Social interaction logic
- Boolean flag cho Like/Dislike
- User/Video association
- Prevent duplicate entries

// VideoView - Analytics logic
- IP tracking để prevent spam
- NumberOfVisit counting
- View history tracking

// Category - Content organization
- Video categorization
- Hierarchical structure support
```

#### 📊 DTOs & Data Transfer:
```csharp
// VideoForHomeGridDto - Homepage video display
- Optimized data cho homepage grid
- Includes thumbnail, title, view count
- Performance-optimized projection

// VideoGridChannelDto - Channel video grid
- Channel-specific video data
- Upload date, status, analytics

// IP2LocationResultDto - Location tracking
- User location data
- Geographic analytics
```

#### 🔍 Repository Interfaces - Data contracts:
```csharp
// IUnitOfWork - Transaction management
- Coordinate multiple repository operations
- Single SaveChanges() call
- Transaction rollback support

// IVideoRepo - Video data operations
- GetVideoWithDetailsAsync(): Full video data
- GetVideosForChannelGridAsync(): Paginated grid data
- HandleVideoViewAsync(): View tracking logic

// IChannelRepo - Channel operations
- GetChannelIdByUserId(): Channel lookup
- Channel subscription management
- Channel analytics queries
```

#### 📄 Pagination Logic:
```csharp
// BaseParameters - Common pagination
- PageNumber, PageSize properties
- Sorting parameters

// PaginatedList<T> - Paginated results
- Metadata: TotalPages, CurrentPage
- Data collection với pagination info

// PaginatedResult<T> - API response wrapper
- Standardized pagination response
- Includes count và page information
```

### 🗄️ 3. INFRASTRUCTURE LAYER (ClipShare.DataAccess)
**Trách nhiệm:** Data persistence, external services, và infrastructure concerns

#### 💾 DbContext & Data Access:
```csharp
// Context - Main database context
- Entity Framework configuration
- DbSets cho tất cả entities
- Relationship configuration
- Migration support

// Entity Configurations - Database schema
- CommentConfig: Comment table structure
- SubscribeConfig: Many-to-many User ↔ Channel
- LikeDislikeConfig: User ↔ Video interactions
- VideoViewConfig: View tracking table
- Foreign key relationships
- Index definitions cho performance
```

#### 🔄 Repository Implementations:
```csharp
// BaseRepo<T> - Generic CRUD operations
- Add(), Update(), Remove()
- GetByIdAsync(), GetAllAsync()
- GetFirstOrDefaultAsync() with filters
- AnyAsync() existence checks

// VideoRepo - Video-specific operations
- GetVideoWithDetailsAsync(): Include related data
- GetVideosForChannelGridAsync(): Optimized queries
- RemoveVideoAsync(): Cascade delete logic
- GetUserIdByVideoIdAsync(): Ownership validation

// ChannelRepo - Channel operations
- GetChannelIdByUserId(): User → Channel mapping
- Subscription management queries
- Channel analytics calculations

// UnitOfWork - Coordinate repositories
- Lazy initialization của repositories
- Single database context
- Transaction management
- Change tracking optimization
```

#### 🗃️ Migrations & Schema:
```csharp
// Migration Files - Database versioning
- Initial database creation
- Schema updates over time
- Data seeding scripts
- Index creation/optimization

// Data Seeding
- Default roles (Admin, Moderator, User)
- Sample categories
- Test data cho development
```

### 🔧 4. SHARED LAYER (ClipShare.Utility)
**Trách nhiệm:** Cross-cutting concerns, constants, và helper utilities

#### 📋 Constants & Static Data:
```csharp
// SD (Static Data) class
// Role Management
- AdminRole = "admin"
- ModeratorRole = "moderator" 
- UserRole = "user"
- Roles list cho validation

// File Handling
- MB constant (1,000,000 bytes)
- GetFileExtension(): Extract file extensions
- File size validation helpers

// Network & Security
- LocalIpAddresses: localhost detection
- IP validation helpers

// UI Helpers
- IsActive(): CSS class cho active navigation
- IsActivePage(): Page-specific CSS classes
- Random number generation với seed
```

#### 🎯 Extension Methods:
```csharp
// HTML Extensions
- IsActive(): Navigation highlighting
- IsActivePage(): Page state management
- CSS class application helpers

// Utility Functions
- String manipulation helpers
- Date formatting utilities
- File type validation
- Security helpers
```

---

## 🔄 Layer Interaction Flow

### 📊 Typical Request Flow:
```
1. USER REQUEST → Controller (Presentation)
2. Controller → UnitOfWork (Infrastructure) 
3. UnitOfWork → Repository (Infrastructure)
4. Repository → DbContext → Database
5. Database → Entity (Domain)
6. Entity → DTO mapping (Domain)
7. DTO → ViewModel (Presentation)
8. ViewModel → View → User Response
```

### 🎯 Dependency Injection Flow:
```
Program.cs → WebApplicationBuilderExtensions 
→ Register Services (IUnitOfWork, IPhotoService)
→ Controllers receive dependencies
→ Controllers use repositories through UnitOfWork
```

### 📈 Business Logic Distribution:
- **Domain (Core)**: Entity validation, business rules
- **Application (Services)**: File handling, external APIs  
- **Infrastructure**: Data persistence, caching
- **Presentation**: UI logic, user input validation

---

## 🛠️ Công Nghệ Sử Dụng

### Backend Framework
```xml
<TargetFramework>net8.0</TargetFramework>
```

| Công Nghệ | Version | Mục Đích |
|-----------|---------|----------|
| **ASP.NET Core** | 8.0 | Web Framework chính |
| **Entity Framework Core** | 8.0.7 | ORM & Database Access |
| **ASP.NET Core Identity** | 8.0.7 | Authentication & Authorization |
| **SQL Server** | Latest | Primary Database |
| **AutoMapper** | 13.0.1 | Object-Object Mapping |
| **Razor Runtime Compilation** | 8.0.7 | Hot Reload Views |

### Frontend Technologies
- **Razor Pages** - Server-side rendering
- **HTML5/CSS3/JavaScript** - Frontend technologies
- **Bootstrap** - UI Framework
- **jQuery** - DOM manipulation & AJAX

---

## 📁 Cấu Trúc Dự Án

### 🎨 1. ClipShare (Presentation Layer)
```
ClipShare/
├── Controllers/           # MVC Controllers
│   ├── AccountController.cs      # Authentication
│   ├── AdminController.cs        # Admin functions
│   ├── ChannelController.cs      # Channel management
│   ├── VideoController.cs        # Video operations
│   ├── HomeController.cs         # Home page
│   ├── MemberController.cs       # Member features
│   └── ModeratorController.cs    # Content moderation
├── Views/                # Razor Views
│   ├── Account/          # Login, Register views
│   ├── Admin/            # Admin dashboard
│   ├── Channel/          # Channel management
│   ├── Video/            # Video pages
│   ├── Home/             # Home page
│   └── Shared/           # Layout, partials
├── ViewModels/           # View-specific models
│   ├── Account/          # Auth ViewModels
│   ├── Video/            # Video ViewModels
│   ├── Channel/          # Channel ViewModels
│   └── ApiResponse.cs    # API response wrapper
├── Services/             # Application services
│   ├── PhotoService.cs   # File upload service
│   └── IServices/        # Service interfaces
├── Extensions/           # Extension methods
│   ├── UserClaimsExtensions.cs
│   └── WebApplicationBuilderExtensions.cs
├── Helpers/              # Helper classes
├── wwwroot/              # Static files
│   ├── css/              # Stylesheets
│   ├── js/               # JavaScript files
│   └── lib/              # Libraries
└── Program.cs            # Application entry point
```

### 🏛️ 2. ClipShare.Core (Domain Layer)
```
ClipShare.Core/
├── Entities/             # Domain Models
│   ├── AppUser.cs        # User entity
│   ├── AppRole.cs        # Role entity
│   ├── Video.cs          # Video entity
│   ├── Channel.cs        # Channel entity
│   ├── Comment.cs        # Comment entity
│   ├── Category.cs       # Category entity
│   ├── Subscribe.cs      # Subscription entity
│   ├── LikeDislike.cs    # Like/Dislike entity
│   ├── VideoView.cs      # View tracking
│   ├── VideoFile.cs      # Video file storage
│   └── BaseEntity.cs     # Base entity class
├── IRepo/                # Repository Interfaces
│   ├── IUnitOfWork.cs    # Unit of Work pattern
│   ├── IBaseRepo.cs      # Generic repository
│   ├── IVideoRepo.cs     # Video repository
│   ├── IChannelRepo.cs   # Channel repository
│   └── ...               # Other repo interfaces
├── DTOs/                 # Data Transfer Objects
│   ├── VideoForHomeGridDto.cs
│   ├── VideoGridChannelDto.cs
│   └── IP2LocationResultDto.cs
└── Pagination/           # Pagination logic
    ├── BaseParameters.cs
    ├── HomeParameters.cs
    ├── PaginatedList.cs
    └── PaginatedResult.cs
```

### 🗄️ 3. ClipShare.DataAccess (Infrastructure Layer)
```
ClipShare.DataAccess/
├── Data/                 # DbContext & Configurations
│   ├── Context.cs        # Main DbContext
│   └── Config/           # Entity configurations
│       ├── CommentConfig.cs
│       ├── SubscribeConfig.cs
│       └── ...
├── Repo/                 # Repository Implementations
│   ├── UnitOfWork.cs     # Unit of Work implementation
│   ├── BaseRepo.cs       # Generic repository implementation
│   ├── VideoRepo.cs      # Video repository
│   ├── ChannelRepo.cs    # Channel repository
│   └── ...               # Other repositories
└── Migrations/           # EF Core migrations
    └── [Generated migration files]
```

### 🔧 4. ClipShare.Utility (Shared Layer)
```
ClipShare.Utility/
└── SD.cs                 # Static Data & Constants
    ├── Role Constants    # Admin, Moderator, User
    ├── Helper Methods    # File extensions, validation
    └── Extension Methods # HTML helpers
```

---

## 🎭 Domain Models

### 👤 AppUser (Identity User)
```csharp
public class AppUser : IdentityUser<int>
{
    [Required]
    public string Name { get; set; }
    public DateTime CreatedAt { get; set; }
    
    // Navigation Properties
    public Channel Channel { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<Subscribe> Subscriptions { get; set; }
    public ICollection<LikeDislike> LikeDislikes { get; set; }
    public ICollection<VideoView> Histories { get; set; }
}
```

### 🎬 Video Entity
```csharp
public class Video : BaseEntity
{
    [Required]
    public string ThumbnailUrl { get; set; }
    [Required]
    public string Title { get; set; }
    [Required]
    public string Description { get; set; }
    public int CategoryId { get; set; }
    public int ChannelId { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsApproved { get; set; } = false; // Moderation
    
    // Navigation Properties
    public Category Category { get; set; }
    public Channel Channel { get; set; }
    public VideoFile VideoFile { get; set; }
    public ICollection<Comment> Comments { get; set; }
    public ICollection<LikeDislike> LikeDislikes { get; set; }
    public ICollection<VideoView> Viewers { get; set; }
}
```

### 📺 Channel Entity
```csharp
public class Channel : BaseEntity
{
    public string Name { get; set; }
    public string About { get; set; }
    public DateTime CreatedAt { get; set; }
    public int AppUserId { get; set; }
    
    // Navigation Properties
    public AppUser AppUser { get; set; }
    public ICollection<Video> Videos { get; set; }
    public ICollection<Subscribe> Subscribers { get; set; }
}
```

### 💬 Comment Entity
```csharp
public class Comment : BaseEntity
{
    public int AppUserId { get; set; }
    public int VideoId { get; set; }
    public string Content { get; set; }
    public DateTime PostedAt { get; set; }
    
    // Navigation Properties
    public AppUser AppUser { get; set; }
    public Video Video { get; set; }
}
```

---

## 🎨 Design Patterns

### 1. 📚 Repository Pattern
```csharp
// Interface trong Core Layer
public interface IVideoRepo : IBaseRepo<Video>
{
    Task<Video> GetVideoWithDetailsAsync(int id);
    Task<PaginatedList<VideoGridChannelDto>> GetVideosForChannelGridAsync(int channelId, BaseParameters parameters);
}

// Implementation trong DataAccess Layer
public class VideoRepo : BaseRepo<Video>, IVideoRepo
{
    public VideoRepo(Context context) : base(context) { }
    
    public async Task<Video> GetVideoWithDetailsAsync(int id)
    {
        return await _contextSet
            .Include(v => v.Channel)
            .Include(v => v.Comments)
            .FirstOrDefaultAsync(v => v.Id == id);
    }
}
```

### 2. 🏭 Unit of Work Pattern
```csharp
public interface IUnitOfWork : IDisposable
{
    IVideoRepo VideoRepo { get; }
    IChannelRepo ChannelRepo { get; }
    ICategoryRepo CategoryRepo { get; }
    ICommentRepo CommentRepo { get; }
    Task<bool> CompleteAsync(); // Single transaction
}

public class UnitOfWork : IUnitOfWork
{
    private readonly Context _context;
    
    public IVideoRepo VideoRepo => new VideoRepo(_context);
    public IChannelRepo ChannelRepo => new ChannelRepo(_context);
    
    public async Task<bool> CompleteAsync()
    {
        return await _context.SaveChangesAsync() > 0;
    }
}
```

### 3. 💉 Dependency Injection
```csharp
// Program.cs
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();
builder.Services.AddScoped<IPhotoService, PhotoService>();

// Controller sử dụng
public class VideoController : CoreController
{
    // UnitOfWork được inject thông qua CoreController
    public async Task<IActionResult> Watch(int id)
    {
        var video = await UnitOfWork.VideoRepo.GetByIdAsync(id);
        // ...
    }
}
```

### 4. 🎯 ViewModel Pattern
```csharp
public class VideoWatch_vm
{
    public int Id { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public DateTime CreatedAt { get; set; }
    public int ChannelId { get; set; }
    public string ChannelName { get; set; }
    
    // Social features
    public bool IsSubscribed { get; set; }
    public bool IsLiked { get; set; }
    public bool IsDisiked { get; set; }
    
    // Statistics
    public int SubscribersCount { get; set; }
    public int ViewersCount { get; set; }
    public int LikesCount { get; set; }
    public int DislikesCount { get; set; }
    
    // Comments
    public Comment_vm CommentVM { get; set; }
}
```

---

## 🚀 Tính Năng Chính

### 🎬 Video Management
```csharp
// VideoController example methods:

// 1. Xem video + tracking views
public async Task<IActionResult> Watch(int id)

// 2. Upload video với validation
[HttpPost]
public async Task<IActionResult> CreateEditVideo(VideoAddEdit_vm model)

// 3. Tải video file
public async Task<IActionResult> DownloadVideoFile(int videoId)

// 4. Xóa video
[HttpDelete]
public async Task<IActionResult> DeleteVideo(int id)
```

### 👥 User Management & Roles
```csharp
// 3 levels of authorization
public const string AdminRole = "admin";
public const string ModeratorRole = "moderator"; 
public const string UserRole = "user";

// Controllers với role-based authorization
[Authorize(Roles = SD.AdminRole)]
public class AdminController : CoreController

[Authorize(Roles = SD.ModeratorRole)]
public class ModeratorController : CoreController

[Authorize(Roles = SD.UserRole)]
public class VideoController : CoreController
```

### 📺 Channel Management
- Tạo kênh cá nhân
- Quản lý video của kênh
- Subscribe/Unsubscribe system
- Channel analytics

### 💬 Social Features
```csharp
// Like/Dislike system
[HttpPut]
public async Task<IActionResult> LikeDislikeVideo(int videoId, string action, bool like)

// Comment system
[HttpPost]
public async Task<IActionResult> CreateComment(Comment_vm model)

// Subscribe system
[HttpPut]
public async Task<IActionResult> SubscribeChannel(int channelId)
```

### 📊 Analytics & Tracking
- View tracking với IP detection
- Like/Dislike statistics
- Subscriber count tracking
- Video performance metrics

---

## 🌐 API Endpoints

### 📹 Video APIs
| Method | Endpoint | Description |
|--------|----------|-------------|
| `GET` | `/Video/Watch/{id}` | Xem video |
| `POST` | `/Video/CreateEditVideo` | Tạo/sửa video |
| `DELETE` | `/Video/DeleteVideo/{id}` | Xóa video |
| `GET` | `/Video/GetVideoFile/{videoId}` | Stream video file |
| `GET` | `/Video/DownloadVideoFile/{videoId}` | Download video |
| `GET` | `/Video/GetVideosForChannelGrid` | API lấy videos cho grid |

### 💝 Social APIs
| Method | Endpoint | Description |
|--------|----------|-------------|
| `PUT` | `/Video/LikeDislikeVideo` | Like/Dislike video |
| `PUT` | `/Video/SubscribeChannel` | Subscribe/Unsubscribe |
| `POST` | `/Video/CreateComment` | Tạo comment |

### 📊 API Response Format
```csharp
public class ApiResponse
{
    public int StatusCode { get; set; }
    public string Title { get; set; }
    public string Message { get; set; }
    public object Result { get; set; }
    
    public ApiResponse(int statusCode, string title = "", string message = "", object result = null)
    {
        StatusCode = statusCode;
        Title = title;
        Message = message;
        Result = result;
    }
}
```

---

## 🗄️ Database Schema

### 📊 Core Tables
```sql
-- Users (Identity Tables)
AspNetUsers
AspNetRoles  
AspNetUserRoles

-- Application Tables
Categories
Channels
Videos
VideoFiles (Blob storage)
Comments
Subscribes (Many-to-Many: Users ↔ Channels)
LikeDislikes (Many-to-Many: Users ↔ Videos)
VideoViews (View tracking)
```

### 🔗 Relationships
```
AppUser 1:1 Channel
Channel 1:* Videos
Video 1:1 VideoFile
Video 1:* Comments
Video 1:* LikeDislikes
Video 1:* VideoViews
User *:* Channels (Subscribe)
User *:* Videos (LikeDislike)
```

### 📝 Entity Configurations
```csharp
// Example: CommentConfig.cs
public void Configure(EntityTypeBuilder<Comment> builder)
{
    builder.HasKey(c => c.Id);
    
    builder.HasOne(c => c.AppUser)
           .WithMany(u => u.Comments)
           .HasForeignKey(c => c.AppUserId)
           .OnDelete(DeleteBehavior.Cascade);
           
    builder.HasOne(c => c.Video)
           .WithMany(v => v.Comments)
           .HasForeignKey(c => c.VideoId)
           .OnDelete(DeleteBehavior.Cascade);
}
```

---

## 🔐 Security & Authentication

### 🛡️ ASP.NET Core Identity Setup
```csharp
builder.Services.AddIdentity<AppUser, AppRole>(options =>
{
    // Password policy
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = true;
    options.Password.RequireLowercase = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
    
    // Sign in settings
    options.SignIn.RequireConfirmedEmail = false;
    options.Lockout.AllowedForNewUsers = false;
})
.AddEntityFrameworkStores<Context>();
```

### 🍪 Cookie Authentication
```csharp
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.ExpireTimeSpan = TimeSpan.FromHours(24);
        options.LoginPath = "/Account/Login";
        options.AccessDeniedPath = "/Account/AccessDenied";
    });
```

### 🔒 Authorization Policies
```csharp
[Authorize(Roles = $"{SD.AdminRole},{SD.ModeratorRole}")]
public class AdminController : CoreController

[Authorize(Roles = SD.UserRole)]
public class VideoController : CoreController
```

### 🛡️ File Upload Security
```csharp
// Validation in VideoController
if (!IsAcceptableContentType("video", model.VideoUpload.ContentType))
{
    ModelState.AddModelError("VideoUpload", "Invalid content type");
}

if (model.VideoUpload.Length > maxSizeInMB * SD.MB)
{
    ModelState.AddModelError("VideoUpload", "File too large");
}
```

---

## ⚙️ Hướng Dẫn Setup

### 📋 Prerequisites
- .NET 8 SDK
- SQL Server (hoặc SQL Express)
- Visual Studio 2022 hoặc VS Code

### 🚀 Installation Steps

1. **Clone Repository**
```bash
git clone https://github.com/BbySharp-dev/clip_share_clone.git
cd clip_share_clone
```

2. **Configure Database**
```json
// appsettings.Development.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=clipshare;Trusted_connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True"
  }
}
```

3. **Install EF Tools**
```bash
dotnet tool install --global dotnet-ef
```

4. **Create Database**
```bash
dotnet ef database update --project ClipShare.DataAccess --startup-project ClipShare
```

5. **Run Application**
```bash
dotnet run --project ClipShare
```

### 🔧 Configuration Files

#### File Upload Settings
```json
// appsettings.json
{
  "FileUpload": {
    "ImageMaxSizeInMB": 5,
    "VideoMaxSizeInMB": 100,
    "ImageContentTypes": ["image/jpeg", "image/png", "image/gif"],
    "VideoContentTypes": ["video/mp4", "video/avi", "video/mkv"]
  }
}
```

---

## 🏆 Best Practices

### 🎯 Clean Architecture Principles

1. **Dependency Inversion**
   - Core layer không reference layer nào khác
   - Infrastructure implements Core interfaces

2. **Separation of Concerns**
   - Mỗi layer có trách nhiệm riêng biệt
   - Business logic ở Core, không ở Controllers

3. **Single Responsibility**
   - Mỗi class/method có 1 trách nhiệm duy nhất

### 🔄 Repository Pattern Best Practices

```csharp
// ✅ Good: Generic + Specific methods
public interface IVideoRepo : IBaseRepo<Video>
{
    Task<Video> GetVideoWithDetailsAsync(int id);
    Task<PaginatedList<VideoGridChannelDto>> GetVideosForChannelGridAsync(int channelId, BaseParameters parameters);
}

// ✅ Good: Projection for performance
private async Task<VideoWatch_vm> GetVideoWatch_vmWithProjections(int id)
{
    return await Context.Video
        .Where(x => x.Id == id)
        .Select(x => new VideoWatch_vm
        {
            Id = x.Id,
            Title = x.Title,
            // Only select needed fields
        })
        .FirstOrDefaultAsync();
}
```

### 📊 Performance Optimizations

1. **Use Projections instead of Include**
```csharp
// ❌ Bad: Include loads unnecessary data
var video = await context.Videos
    .Include(v => v.Channel)
    .Include(v => v.Comments)
    .FirstOrDefaultAsync();

// ✅ Good: Projection loads only needed fields  
var videoDto = await context.Videos
    .Select(v => new VideoDto 
    {
        Id = v.Id,
        Title = v.Title,
        ChannelName = v.Channel.Name
    })
    .FirstOrDefaultAsync();
```

2. **Pagination for Large Data Sets**
```csharp
public class PaginatedList<T> : List<T>
{
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public int TotalItemsCount { get; set; }
}
```

### 🛡️ Security Best Practices

1. **Always Validate User Ownership**
```csharp
var video = await Context.Video
    .Where(x => x.Id == id && x.Channel.AppUserId == User.GetUserId())
    .FirstOrDefaultAsync();
```

2. **Use Authorization Attributes**
```csharp
[Authorize(Roles = SD.UserRole)]
public class VideoController : CoreController
```

3. **Validate File Uploads**
```csharp
private bool IsAcceptableContentType(string type, string contentType)
{
    var allowedTypes = Configuration.GetSection($"FileUpload:{type}ContentTypes").Get<string[]>();
    return allowedTypes.Contains(contentType.ToLower());
}
```

---

## 📈 Scalability Considerations

### 🔄 Future Enhancements

1. **Microservices Migration**
   - Video Service
   - User Service  
   - Notification Service

2. **Caching Strategy**
   - Redis for session management
   - Memory cache for frequently accessed data

3. **File Storage**
   - Azure Blob Storage / AWS S3
   - CDN for video streaming

4. **Real-time Features**
   - SignalR for live comments
   - Push notifications

---

## 🤝 Contributing

### 👥 Team Structure
- **Frontend Team**: Views, ViewModels, Client-side logic
- **Backend Team**: Controllers, Services, Business logic  
- **Database Team**: Entities, Repositories, Migrations
- **DevOps Team**: Deployment, CI/CD, Infrastructure

### 📝 Code Review Checklist
- [ ] Follows Clean Architecture principles
- [ ] Implements proper error handling
- [ ] Includes unit tests
- [ ] Validates user input
- [ ] Checks authorization
- [ ] Uses proper logging
- [ ] Optimizes database queries

---

## 📞 Support & Documentation

- **GitHub Issues**: [Project Issues](https://github.com/BbySharp-dev/clip_share_clone/issues)
- **Documentation**: This README + inline code comments
- **Architecture Decisions**: See `/docs/architecture-decisions/`

---

**🎬 ClipShare - Nền tảng chia sẻ video với Clean Architecture!**

*Tạo bởi: BbySharp-dev | Ngày cập nhật: August 2025*
