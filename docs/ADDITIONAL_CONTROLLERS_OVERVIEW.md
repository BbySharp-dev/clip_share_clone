# 🏠 Additional Controllers System - Supporting Operations

## 📋 Mục Lục
- [Tổng Quan](#tổng-quan)
- [HomeController - Public Video Grid](#homecontroller---public-video-grid)
- [MemberController - Public Channel Views](#membercontroller---public-channel-views)
- [ModeratorController - Content Moderation](#moderatorcontroller---content-moderation)
- [CoreController - Base Infrastructure](#corecontroller---base-infrastructure)
- [Integration Overview](#integration-overview)

---

## 🎯 Tổng Quan

**ClipShare Additional Controllers** bao gồm các controllers hỗ trợ cho core system, xử lý public views, member interactions, content moderation, và base infrastructure. Mỗi controller có vai trò riêng biệt trong ecosystem của platform.

### 🔧 Controllers Overview
- ✅ **HomeController** - Public homepage, video grid, user data APIs
- ✅ **MemberController** - Public channel views, subscription management
- ✅ **ModeratorController** - Content moderation, video oversight
- ✅ **CoreController** - Base controller với shared services

### 🎭 Roles & Permissions
- 🌐 **Public Access** - HomeController Index (public video grid)
- 👤 **User Role** - HomeController APIs, MemberController
- 🛡️ **Moderator Role** - ModeratorController operations
- 🔧 **Base Services** - CoreController (inherited by all)

---

## 🏠 HomeController - Public Video Grid

### 📊 HomeController Flow Diagram
```
                        🏠 HOME CONTROLLER SYSTEM
                        
   🌐 PUBLIC ACCESS            📡 DATA LOADING              🗄️ DATABASE
                        
┌─────────────────────┐                                   ┌─────────────────────┐
│   🏠 Homepage       │─── GET /Home/Index ─────────────▶│  📂 Category         │
│   (Public View)     │                                  │    Loading          │
│                     │                                  │                     │
│ • Category filter   │                                  │ CategoryRepo        │
│ • Video grid        │                                  │ .GetAllAsync()      │
│ • Search/Sort       │                                  │                     │
└─────────────────────┘                                  │ + Dropdown list     │
           │                                             └─────────────────────┘
           ▼                                                      │
┌─────────────────────┐                                          ▼
│  🔐 Auth Check      │                                 ┌─────────────────────┐
│                     │                                 │  🎬 Video Grid       │
│ User.Identity       │                                 │    API Endpoint     │
│ .IsAuthenticated    │                                 │                     │
│                     │                                 │ GetVideosForHome    │
│ • Show categories   │                                 │ GridAsync()         │
│   if logged in      │                                 └─────────────────────┘
└─────────────────────┘
           │
           ▼
┌─────────────────────────────────────────────────────┐
│                🎮 AJAX API ENDPOINTS                │
│                                                     │
│ 1️⃣ GET /Home/GetVideosForHomeGrid                   │
│   • Paginated video results                        │
│   • Category filtering                             │
│   • Search functionality                           │
│                                                     │
│ 2️⃣ GET /Home/GetSubscriptions                       │
│   • User's subscribed channels                     │
│   • Channel info + video counts                    │
│                                                     │
│ 3️⃣ GET /Home/GetHistory                             │
│   • User's watch history                           │
│   • Video details + timestamps                     │
│                                                     │
│ 4️⃣ GET /Home/GetLikesDislikesVideos                 │
│   • User's liked/disliked videos                   │
│   • Filter by like/dislike status                  │
└─────────────────────────────────────────────────────┘
```

### 🎯 Chi Tiết HomeController Operations:

#### **🏠 Homepage Display**
📍 **File**: `Controllers/HomeController.cs:25`
```csharp
public async Task<IActionResult> Index(string page)
{
    var toReturn = new Home_vm();

    if (User.Identity.IsAuthenticated)
    {
        toReturn.Page = page;

        if (page == null || page == "Home")
        {
            // 📂 Load categories for filter dropdown
            var allCategories = await UnitOfWork.CategoryRepo.GetAllAsync();

            var categoryList = allCategories.Select(category => new SelectListItem
            {
                Text = category.Name,
                Value = category.Id.ToString()
            }).ToList();

            // 🆕 Add "All" option as default
            categoryList.Insert(0, new SelectListItem
            {
                Text = "All",
                Value = "0",
                Selected = true
            });

            toReturn.CategoryDropdown = categoryList;
        }
    }

    return View(toReturn);
}
```

#### **📊 Video Grid API**
📍 **File**: `Controllers/HomeController.cs:66`
```csharp
[Authorize(Roles = $"{SD.UserRole}")]
[HttpGet]
public async Task<IActionResult> GetVideosForHomeGrid(HomeParameters parameters)
{
    var items = await UnitOfWork.VideoRepo.GetVideosForHomeGridAsync(parameters);
    var paginatedResults = new PaginatedResult<VideoForHomeGridDto>(
        items, 
        items.TotalItemsCount, 
        items.PageNumber, 
        items.PageSize, 
        items.TotalPages
    );

    return Json(new ApiResponse(200, result: paginatedResults));
}
```

#### **👥 User Subscriptions API**
📍 **File**: `Controllers/HomeController.cs:74`
```csharp
[Authorize(Roles = $"{SD.UserRole}")]
[HttpGet]
public async Task<IActionResult> GetSubscriptions()
{
    var userSubscribedChannels = await Context.Subscribe
        .Where(x => x.AppUserId == User.GetUserId())
        .Select(x => new
        {
            Id = x.ChannelId,
            ChannelName = x.Channel.Name,
            VideosCount = x.Channel.Videos.Count
        }).ToListAsync();

    return Json(new ApiResponse(200, result: userSubscribedChannels));
}
```

#### **📺 Watch History API**
📍 **File**: `Controllers/HomeController.cs:87`
```csharp
[Authorize(Roles = $"{SD.UserRole}")]
[HttpGet]
public async Task<IActionResult> GetHistory()
{
    var userWatchedVideoHistory = await Context.VideoView
        .Where(x => x.AppUserId == User.GetUserId())
        .Select(x => new
        {
            Id = x.VideoId,
            x.Video.Title,
            ChannelName = x.Video.Channel.Name,
            ChannelId = x.Video.Channel.Id,
            LastVisitTimeAgo = SD.TimeAgo(x.LastVisit),
            x.LastVisit
        }).ToListAsync();

    return Json(new ApiResponse(200, result: userWatchedVideoHistory));
}
```

#### **👍 Likes/Dislikes API**
📍 **File**: `Controllers/HomeController.cs:103`
```csharp
[Authorize(Roles = $"{SD.UserRole}")]
[HttpGet]
public async Task<IActionResult> GetLikesDislikesVideos(bool liked)
{
    var userLikedDislikedVideos = await Context.LikeDislike
        .Where(x => x.AppUserId == User.GetUserId() && x.Liked == liked)
        .Select(x => new
        {
            Id = x.VideoId,
            x.Video.Title,
            x.Video.ThumbnailUrl,
            ChannelName = x.Video.Channel.Name,
            ChannelId = x.Video.Channel.Id,
            CreatedAtTimeAgo = SD.TimeAgo(x.Video.CreatedAt),
            x.Video.CreatedAt
        }).ToListAsync();

    return Json(new ApiResponse(200, result: userLikedDislikedVideos));
}
```

### 🔑 HomeController Features:
- ✅ **Public Video Grid** với category filtering
- ✅ **User Subscriptions** management
- ✅ **Watch History** tracking
- ✅ **Engagement History** (likes/dislikes)
- ✅ **Pagination Support** cho all data
- ✅ **Projection Queries** cho performance

---

## 👥 MemberController - Public Channel Views

### 📊 MemberController Flow Diagram
```
                        👥 MEMBER CONTROLLER SYSTEM
                        
   🌐 PUBLIC CHANNEL VIEW      📡 CHANNEL DATA             🗄️ DATABASE
                        
┌─────────────────────┐                                   ┌─────────────────────┐
│   📺 Channel Page   │─── GET /Member/Channel/123 ─────▶│  🔍 Channel Lookup   │
│   (Public View)     │                                  │                     │
│                     │                                  │ • Channel info      │
│ • Channel info      │                                  │ • Video count       │
│ • Video grid        │                                  │ • Subscriber count  │
│ • Subscribe button  │                                  │ • User subscription │
└─────────────────────┘                                  │   status            │
           │                                             └─────────────────────┘
           ▼                                                      │
┌─────────────────────┐                                          ▼
│  👤 User Actions    │                                 ┌─────────────────────┐
│                     │                                 │  📊 Projection       │
│ • View channel      │                                 │    Query            │
│ • Subscribe/        │                                 │                     │
│   Unsubscribe       │                                 │ MemberChannel_vm    │
│ • Browse videos     │                                 │ • Efficient loading │
└─────────────────────┘                                 │ • User-specific     │
           │                                            │   data              │
           ▼                                            └─────────────────────┘
┌─────────────────────┐                                          │
│  🔄 Subscription    │─── POST /Member/SubscribeChannel ───────▼
│    Toggle           │                                 ┌─────────────────────┐
│                     │                                 │  💾 Subscription     │
│ • Check existing    │                                 │    Management       │
│   subscription      │                                 │                     │
│ • Add/Remove        │                                 │ • Toggle sub status │
│ • Redirect back     │                                 │ • Update database   │
└─────────────────────┘                                 │ • Preserve state    │
           │                                            └─────────────────────┘
           ▼
┌─────────────────────────────────────────────────────┐
│              🎮 AJAX API ENDPOINT                    │
│                                                     │
│ GET /Member/GetMemberChannelVideos                  │
│   • Channel's video list                           │
│   • Video metadata                                 │
│   • View counts                                    │
│   • Creation timestamps                            │
└─────────────────────────────────────────────────────┘
```

### 🎯 Chi Tiết MemberController Operations:

#### **📺 Public Channel View**
📍 **File**: `Controllers/MemberController.cs:18`
```csharp
public async Task<IActionResult> Channel(int id)
{
    var fetchedChannel = await Context.Channel
        .Where(x => x.Id == id)
        .Select(x => new MemberChannel_vm
        {
            ChannelId = x.Id,
            Name = x.Name,
            About = x.About,
            CreatedAt = x.CreatedAt,
            NumberOfAvailableVideos = x.Videos.Count(),
            NumberOfSubscribers = x.Subscribers.Count(),
            
            // 🎯 User-specific subscription status
            UserIsSubscribed = x.Subscribers.Any(s => s.AppUserId == User.GetUserId()),
        }).FirstOrDefaultAsync();

    if (fetchedChannel != null)
    {
        return View(fetchedChannel);
    }

    TempData["notification"] = "false;Not Found;Requested channel was not found";
    return RedirectToAction("Index", "Home");
}
```

#### **👥 Subscription Toggle**
📍 **File**: `Controllers/MemberController.cs:36`
```csharp
[HttpPost]
public async Task<IActionResult> SubscribeChannel(int channelId)
{
    var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(
        x => x.Id == channelId, 
        "Subscribers"
    );

    if (channel != null)
    {
        int userId = User.GetUserId();

        var fetchedSubscribe = channel.Subscribers
            .Where(x => x.ChannelId == channelId && x.AppUserId == userId)
            .FirstOrDefault();

        if (fetchedSubscribe == null)
        {
            // 🆕 Subscribe
            channel.Subscribers.Add(new Subscribe(userId, channelId));
        }
        else
        {
            // ❌ Unsubscribe
            channel.Subscribers.Remove(fetchedSubscribe);
        }

        await UnitOfWork.CompleteAsync();
        
        // 🔄 Redirect back to same channel page
        return RedirectToAction("Channel", new { id = channelId });
    }

    TempData["notification"] = "false;Not Found;Requested channel was not found";
    return RedirectToAction("Index", "Home");
}
```

#### **🎬 Channel Videos API**
📍 **File**: `Controllers/MemberController.cs:71`
```csharp
[HttpGet]
public async Task<IActionResult> GetMemberChannelVideos(int channelId)
{
    var channelVideos = await Context.Video
        .Where(x => x.ChannelId == channelId)
        .Select(x => new
        {
            x.Id,
            x.Title,
            x.ThumbnailUrl,
            CreatedAtTimeAgo = SD.TimeAgo(x.CreatedAt),
            x.CreatedAt,
            NumberOfViews = x.Viewers.Count(),
        })
        .ToListAsync();

    return Json(new ApiResponse(200, result: channelVideos));
}
```

### 🔑 MemberController Features:
- ✅ **Public Channel Pages** cho tất cả users
- ✅ **Subscription Management** với toggle functionality
- ✅ **Channel Video Grid** với AJAX loading
- ✅ **Efficient Queries** với projection
- ✅ **User-specific Data** (subscription status)

---

## 🛡️ ModeratorController - Content Moderation

### 📊 ModeratorController Flow Diagram
```
                        🛡️ MODERATOR CONTROLLER SYSTEM
                        
   🛡️ MODERATOR ACCESS        📡 CONTENT OVERSIGHT        🗄️ DATABASE
                        
┌─────────────────────┐                                   ┌─────────────────────┐
│   🛡️ Moderator      │─── GET /Moderator/AllVideos ────▶│  📊 All Videos       │
│   Dashboard         │                                  │    Loading          │
│                     │                                  │                     │
│ [Authorize(Roles =  │                                  │ VideoRepo           │
│  SD.ModeratorRole)] │                                  │ .GetAllAsync()      │
│                     │                                  │                     │
│ • Video overview    │                                  │ includeProperties:  │
│ • Content actions   │                                  │ "Category,Channel"  │
└─────────────────────┘                                  └─────────────────────┘
           │                                                      │
           ▼                                                      ▼
┌─────────────────────┐                                 ┌─────────────────────┐
│  📋 Video Grid      │                                 │  🎯 AutoMapper       │
│    Display          │◀──── Map to ViewModels ────────│    Conversion       │
│                     │                                 │                     │
│ VideoDisplayGrid_vm │                                 │ Videos -> ViewModels│
│ • Video details     │                                 │ • Structured data   │
│ • Channel info      │                                 │ • Display format    │
│ • Category info     │                                 └─────────────────────┘
│ • Action buttons    │
└─────────────────────┘
           │
           ▼
┌─────────────────────┐                                 ┌─────────────────────┐
│  🗑️ Delete Action   │─── POST /Moderator/DeleteVideo ▶│  🧹 Cleanup          │
│                     │                                 │    Process          │
│ • Confirm deletion  │                                 │                     │
│ • File cleanup      │                                 │ • Delete thumbnail  │
│ • Database removal  │                                 │ • Remove video file │
│ • Success feedback  │                                 │ • Database cleanup  │
└─────────────────────┘                                 │ • Success message   │
                                                        └─────────────────────┘
```

### 🎯 Chi Tiết ModeratorController Operations:

#### **📊 All Videos Overview**
📍 **File**: `Controllers/ModeratorController.cs:15`
```csharp
[Authorize(Roles = $"{SD.ModeratorRole}")]
public async Task<IActionResult> AllVideos()
{
    // 📊 Load all videos with related data
    var videos = await UnitOfWork.VideoRepo.GetAllAsync(
        includeProperties: "Category,Channel"
    );

    // 🎯 Map to display ViewModels
    var toReturn = Mapper.Map<IEnumerable<VideoDisplayGrid_vm>>(videos);

    return View(toReturn);
}
```

#### **🗑️ Video Deletion (Moderator Action)**
📍 **File**: `Controllers/ModeratorController.cs:23`
```csharp
[HttpPost]
public async Task<IActionResult> DeleteVideo(int id)
{
    // 🔍 Get video basic info for deletion
    var video = await Context.Video
        .Where(x => x.Id == id)
        .Select(x => new
        {
            x.Id,
            x.ThumbnailUrl,
            x.Title
        }).FirstOrDefaultAsync();

    if (video != null)
    {
        // 🧹 Clean up associated files
        PhotoService.DeletePhotoLocally(video.ThumbnailUrl);
        
        // 🗑️ Remove video from database
        await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id);
        await UnitOfWork.CompleteAsync();

        TempData["notification"] = $"true;Deleted;Video of {video.Title} has been deleted";
        return RedirectToAction("AllVideos");
    }

    TempData["notification"] = $"false;Not Found;Requested video was not found";
    return RedirectToAction("AllVideos");
}
```

### 🔑 ModeratorController Features:
- ✅ **Role-based Authorization** (Moderator only)
- ✅ **Complete Video Overview** với category và channel info
- ✅ **Content Deletion** với file cleanup
- ✅ **AutoMapper Integration** cho clean ViewModels
- ✅ **Audit Trail** qua TempData notifications

---

## 🔧 CoreController - Base Infrastructure

### 📊 CoreController Architecture
```
                        🔧 CORE CONTROLLER INFRASTRUCTURE
                        
┌─────────────────────────────────────────────────────────────────┐
│                        CoreController                           │
│                     (Base Class)                               │
├─────────────────────────────────────────────────────────────────┤
│                    🔌 SERVICE INJECTION                        │
│                                                                 │
│  💾 IUnitOfWork UnitOfWork                                     │
│    └── Database operations, repositories                       │
│                                                                 │
│  🖼️ IPhotoService PhotoService                                 │
│    └── File upload, thumbnail management                       │
│                                                                 │
│  🗄️ Context Context                                            │
│    └── Direct EF Core database context                        │
│                                                                 │
│  ⚙️ IConfiguration Configuration                               │
│    └── App settings, connection strings                       │
│                                                                 │
│  🎯 IMapper Mapper                                             │
│    └── AutoMapper for object mapping                          │
└─────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌─────────────────────────────────────────────────────────────────┐
│                    📡 INHERITED BY ALL                         │
│                                                                 │
│  • AccountController                                           │
│  • AdminController                                             │
│  • ChannelController                                           │
│  • VideoController                                             │
│  • HomeController                                              │
│  • MemberController                                            │
│  • ModeratorController                                         │
└─────────────────────────────────────────────────────────────────┘
```

### 🎯 Chi Tiết CoreController Implementation:

#### **🔧 Service Injection Pattern**
📍 **File**: `Controllers/CoreController.cs:10`
```csharp
public class CoreController : Controller
{
    // 💾 Private fields for services
    private IUnitOfWork _unitOfWork;
    private IPhotoService _photoService;
    private Context _context;
    private IConfiguration _configuration;
    private IMapper _mapper;
    
    // 🔌 Protected properties với lazy loading
    protected IUnitOfWork UnitOfWork => 
        _unitOfWork ??= HttpContext.RequestServices.GetService<IUnitOfWork>();
    
    protected IPhotoService PhotoService => 
        _photoService ??= HttpContext.RequestServices.GetService<IPhotoService>();
    
    protected Context Context => 
        _context ??= HttpContext.RequestServices.GetService<Context>();
    
    protected IConfiguration Configuration => 
        _configuration ??= HttpContext.RequestServices.GetService<IConfiguration>();
    
    protected IMapper Mapper => 
        _mapper ??= HttpContext.RequestServices.GetService<IMapper>();
}
```

### 🔑 CoreController Benefits:
- ✅ **Lazy Service Loading** - Only instantiate when needed
- ✅ **Shared Infrastructure** - Available to all controllers
- ✅ **Clean Architecture** - Separation of concerns
- ✅ **Dependency Injection** - Testable và maintainable
- ✅ **Performance Optimization** - No unnecessary service creation

---

## 🌐 Integration Overview

### 📊 Controller Interaction Flow
```
                        🌐 CONTROLLER ECOSYSTEM INTEGRATION
                        
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│   🏠 HomeController  │    │  👥 MemberController │    │ 🛡️ ModeratorController│
│                     │    │                     │    │                     │
│ • Public video grid │    │ • Public channels   │    │ • Content oversight │
│ • User data APIs    │    │ • Subscriptions     │    │ • Video moderation  │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
           │                           │                           │
           ▼                           ▼                           ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                        🔧 CoreController                                │
│               (Shared Services & Infrastructure)                        │
│                                                                         │
│  💾 UnitOfWork  🖼️ PhotoService  🗄️ Context  ⚙️ Configuration  🎯 Mapper │
└─────────────────────────────────────────────────────────────────────────┘
           ▲                           ▲                           ▲
           │                           │                           │
┌─────────────────────┐    ┌─────────────────────┐    ┌─────────────────────┐
│  🔐 AccountController│    │ 📺 ChannelController │    │  🎬 VideoController  │
│                     │    │                     │    │                     │
│ • Authentication    │    │ • Channel management│    │ • Video operations  │
│ • Authorization     │    │ • Analytics         │    │ • File handling     │
└─────────────────────┘    └─────────────────────┘    └─────────────────────┘
                                       │
                                       ▼
                            ┌─────────────────────┐
                            │ 👑 AdminController   │
                            │                     │
                            │ • User management   │
                            │ • System admin      │
                            └─────────────────────┘
```

### 🔄 Data Flow Patterns

#### **🌐 Public → Private Flow**
```
HomeController (Public) → MemberController (Channel View) → VideoController (Watch) → ChannelController (Owner Dashboard)
```

#### **👤 User Journey Flow**
```
1. Homepage (HomeController) - Browse videos
2. Channel View (MemberController) - Explore channel
3. Video Watch (VideoController) - View content
4. Subscribe/Like (VideoController APIs) - Engage
5. Create Channel (ChannelController) - Become creator
```

#### **🛡️ Moderation Flow**
```
VideoController (Upload) → ModeratorController (Review) → AdminController (User Actions)
```

### 🎯 Common Patterns Across Controllers

#### **🔧 Service Usage Pattern**
```csharp
// All controllers inherit from CoreController
public class SomeController : CoreController
{
    public async Task<IActionResult> SomeAction()
    {
        // 💾 Database operations
        var data = await UnitOfWork.SomeRepo.GetAsync();
        
        // 🖼️ File operations
        var filePath = PhotoService.UploadPhotoLocally(file);
        
        // 🎯 Object mapping
        var viewModel = Mapper.Map<SomeViewModel>(data);
        
        return View(viewModel);
    }
}
```

#### **📊 API Response Pattern**
```csharp
// Consistent across all controllers
return Json(new ApiResponse(200, result: data));
return Json(new ApiResponse(404, message: "Not found"));
return Json(new ApiResponse(200, "Success", "Operation completed"));
```

#### **🔐 Authorization Pattern**
```csharp
// Role-based authorization
[Authorize(Roles = $"{SD.UserRole}")]        // HomeController APIs
[Authorize(Roles = $"{SD.UserRole}")]        // MemberController
[Authorize(Roles = $"{SD.ModeratorRole}")]   // ModeratorController
[Authorize(Roles = $"{SD.AdminRole}")]       // AdminController
```

---

## 🎯 Best Practices Summary

### ✅ Architecture Best Practices
1. **Single Responsibility** - Mỗi controller có clear purpose
2. **Inheritance Hierarchy** - CoreController cho shared services
3. **Role-based Authorization** - Proper security boundaries
4. **API Consistency** - Unified response format

### ✅ Performance Optimizations
1. **Projection Queries** - Load only needed data
2. **Lazy Service Loading** - Initialize services when needed
3. **Efficient Mappings** - AutoMapper cho clean transformations
4. **Pagination Support** - Handle large datasets

### ✅ Security Measures
1. **Authorization Attributes** - Protect sensitive operations
2. **Input Validation** - Sanitize user data
3. **File Cleanup** - Proper resource management
4. **Error Handling** - Safe error messages

### ✅ User Experience
1. **Consistent Navigation** - Logical controller flow
2. **Real-time Feedback** - TempData notifications
3. **AJAX Integration** - Dynamic content loading
4. **Mobile Responsive** - Cross-device compatibility

---

**🏠 Additional Controllers System - Complete Supporting Infrastructure!**

*Cập nhật: August 2025 | Tác giả: BbySharp-dev*
