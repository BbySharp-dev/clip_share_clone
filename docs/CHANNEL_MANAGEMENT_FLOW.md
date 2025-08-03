# 📺 Channel Management System - User Channel Operations

## 📋 Mục Lục
- [Tổng Quan](#tổng-quan)
- [Cấu Trúc Files](#cấu-trúc-files)
- [Channel Dashboard Flow](#channel-dashboard-flow)
- [Channel Creation Flow](#channel-creation-flow)
- [Channel Edit Flow](#channel-edit-flow)
- [Analytics Dashboard Flow](#analytics-dashboard-flow)
- [Session Management](#session-management)
- [Error Handling](#error-handling)

---

## 🎯 Tổng Quan

**ClipShare Channel Management System** cho phép users tạo và quản lý kênh cá nhân. Mỗi user có thể có một kênh để upload videos, xem thống kê, và quản lý subscribers. Hệ thống được thiết kế với UX tốt, sử dụng session management để preserve data và analytics dashboard với charts.

### 📺 Trách Nhiệm USER 5
- ✅ **Channel Dashboard** - Trang chủ quản lý kênh
- ✅ **Channel Creation** - Tạo kênh mới với validation
- ✅ **Channel Editing** - Cập nhật thông tin kênh
- ✅ **Analytics Dashboard** - Thống kê kênh với charts
- ✅ **Session Management** - Preserve form data qua redirects
- ✅ **Video Grid Integration** - Hiển thị videos của kênh

### 🔑 Tính Năng Chính
- ✅ **One Channel per User** (1:1 relationship)
- ✅ **Smart Session Management** với JSON serialization
- ✅ **Real-time Analytics** với Chart.js integration
- ✅ **Responsive Tab Interface** (Videos, Analytics, Profile)
- ✅ **Duplicate Name Validation**
- ✅ **Comprehensive Error Handling**

---

## 📁 Cấu Trúc Files

```
ClipShare/
├── Controllers/
│   └── ChannelController.cs              # 🎯 MAIN: Channel operations
├── ViewModels/Channel/
│   └── ChannelAddEdit_vm.cs              # 📝 Channel form model
├── ViewModels/
│   └── ModelError_vm.cs                  # 🚨 Error handling model
├── Views/Channel/
│   ├── Index.cshtml                      # 🏠 Channel dashboard
│   └── Analytics.cshtml                  # 📊 Analytics page
├── Extensions/
│   └── UserClaimsExtensions.cs           # 🔧 User info helpers
└── Core/Entities/
    ├── Channel.cs                        # 📺 Channel domain model
    ├── Video.cs                          # 🎬 Video model
    ├── Subscribe.cs                      # 👥 Subscription model
    └── VideoView.cs                      # 👁️ View tracking model
```

---

## 🏠 Channel Dashboard Flow

### 📊 Channel Dashboard Flow Diagram
```
                            📺 CHANNEL MANAGEMENT DASHBOARD
                            
   🌐 USER ACCESS                    📡 OPERATIONS                     🗄️ DATABASE
                            
┌─────────────────────┐                                              ┌─────────────────────┐
│   👤 User Access     │────── GET /Channel/Index ─────────────────▶│  🔍 Channel Lookup   │
│   Channel Dashboard │                                             │                     │
└─────────────────────┘                                             │ UnitOfWork          │
           │                                                        │ .ChannelRepo        │
           │                                                        │ .GetFirstOrDefault  │
           ▼                                                        │ (UserId, Subscribers)│
┌─────────────────────┐                                             └─────────────────────┘
│  🎛️ Session Check    │                                                      │
│                     │                                                      ▼
│ HttpContext.Session │                                              ┌─────────────────────┐
│ .GetString          │                                              │  📊 Load Channel     │
│ ("ChannelModel")    │                                              │     Data            │
└─────────────────────┘                                              │                     │
           │                                                         │ • Name              │
           ▼                                                         │ • About             │
┌─────────────────────┐                                              │ • Subscribers.Count │
│  🔄 Conditional     │                                              └─────────────────────┘
│     Display         │                                                      │
│                     │                                                      ▼
│ Channel Exists?     │◀──── Return ChannelAddEdit_vm ──────────────────────┘
└─────────────────────┘
    │             │
    ▼             ▼
┌─────────────┐ ┌─────────────┐
│ 📝 CREATE   │ │ 📊 MANAGE   │
│   FORM      │ │   DASHBOARD │
│             │ │             │
│ [Name    ]  │ │ 📋 Tabs:    │
│ [About   ]  │ │ • My Videos │
│ [Create  ]  │ │ • Analytics │
│             │ │ • Profile   │
└─────────────┘ └─────────────┘
    │               │
    ▼               ▼
┌─────────────────────────────────┐
│      📊 TAB CONTENT             │
│                                 │
│ 1️⃣ MY VIDEOS:                   │
│   • Video grid với AJAX        │
│   • Sort/Filter options        │
│   • Create Video button        │
│                                 │
│ 2️⃣ ANALYTICS:                   │
│   • Total videos/views         │
│   • Subscriber count           │
│   • Chart.js visualization     │
│                                 │
│ 3️⃣ PROFILE:                     │
│   • Edit channel info          │
│   • Update Name/About          │
└─────────────────────────────────┘
```

### 🎯 Chi Tiết Dashboard Logic:

#### **📺 Bước 1: Access Channel Dashboard**
📍 **File**: `Controllers/ChannelController.cs:23`
```csharp
public async Task<IActionResult> Index(string stringModel)
{
    var model = new ChannelAddEdit_vm();
    
    // 🎛️ Check session for preserved error state
    stringModel = HttpContext.Session.GetString("ChannelModelFromSession");

    if (!string.IsNullOrEmpty(stringModel))
    {
        // 📥 Deserialize from session
        model = JsonConvert.DeserializeObject<ChannelAddEdit_vm>(stringModel);
        
        if (model.Errors.Count > 0)
        {
            // 🚨 Restore validation errors
            foreach (var error in model.Errors)
            {
                ModelState.AddModelError(error.Key, error.ErrorMessage);
            }

            HttpContext.Session.Remove("ChannelModelFromSession");
            return View(model);
        }
    }

    // 🔍 Load user's channel
    var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(
        x => x.AppUserId == User.GetUserId(), 
        includeProperties: "Subscribers"    // 👥 Include subscriber count
    );

    if (channel != null)
    {
        // 📊 Populate dashboard data
        model.Name = channel.Name;
        model.About = channel.About;
        model.SubscribersCount = channel.Subscribers.Count();
    }

    return View(model);
}
```

**Flow chi tiết Dashboard Access:**
1. **Session Recovery**: Kiểm tra session có preserved error state từ previous request không
   - Nếu có errors trong session, deserialize và restore vào ModelState
   - Clear session sau khi restore để tránh data persistence
   - Return view với errors để user thấy validation messages
2. **Channel Lookup**: Query database tìm channel của current user
   - Sử dụng User.GetUserId() extension để lấy user ID từ claims
   - Include "Subscribers" để load subscriber count efficiently
   - Sử dụng UnitOfWork pattern để abstract database operations
3. **Data Population**: Nếu user đã có channel, populate ViewModel với existing data
   - Load Name, About, và SubscribersCount để display trong dashboard
   - Nếu chưa có channel, hiển thị create form
4. **Conditional UI**: View sẽ render create form hoặc dashboard based on data availability

**Tính năng đặc biệt:**
- ✅ **Session-based error preservation** qua redirects để maintain UX
- ✅ **Conditional UI rendering** dựa trên channel existence  
- ✅ **Real-time subscriber count** với efficient query
- ✅ **POST-Redirect-GET pattern** để prevent double submission

#### **📈 Bước 2: Channel Analytics Processing**
📍 **File**: `Controllers/ChannelController.cs:110`
```csharp
public async Task<IActionResult> Analytics()
{
    var userId = User.GetUserId();
    
    // 🔍 Get user's channel
    var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(
        x => x.AppUserId == userId
    );

    if (channel == null)
    {
        return RedirectToAction("Index");
    }

    // 📊 Load analytics data with video views
    var videos = await UnitOfWork.VideoRepo.GetAllAsync(
        x => x.ChannelId == channel.Id,
        includeProperties: "VideoViews"
    );

    var model = new ChannelAnalytics_vm
    {
        TotalVideos = videos.Count(),
        TotalViews = videos.Sum(v => v.VideoViews.Count()),
        AverageViewsPerVideo = videos.Any() ? 
            videos.Average(v => v.VideoViews.Count()) : 0,
        MostPopularVideo = videos
            .OrderByDescending(v => v.VideoViews.Count())
            .FirstOrDefault()
    };

    return View(model);
}
```

**Flow chi tiết Analytics:**
1. **User Verification**: Lấy user ID từ claims và verify channel ownership
   - Sử dụng GetUserId() extension để extract từ authentication claims
   - Query channel với userId để ensure security và ownership
   - Redirect về Index nếu user chưa có channel
2. **Performance Analytics**: Load all videos với VideoViews relationship
   - Sử dụng includeProperties để eager load VideoViews efficiently
   - Avoid N+1 query problem bằng cách load relation một lần
   - Filter videos theo channelId để ensure data isolation
3. **Statistical Computation**: Calculate các metrics quan trọng
   - **Total Videos**: Simple count of channel's videos
   - **Total Views**: Sum all VideoViews across all videos
   - **Average Views**: Calculate mean views per video với null check
   - **Most Popular**: OrderBy descending views để find top performer
4. **Dashboard Rendering**: Populate ViewModel với computed analytics data

**Tính năng Analytics:**
- ✅ **Real-time view counting** từ VideoViews table
- ✅ **Performance metrics** với efficient queries
- ✅ **Data visualization ready** cho frontend charts
- ✅ **Security isolation** chỉ show data của owner
    }

    // 🔍 Load user's channel
    var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(
        x => x.AppUserId == User.GetUserId(), 
        includeProperties: "Subscribers"    // 👥 Include subscriber count
    );

    if (channel != null)
    {
        // 📊 Populate dashboard data
        model.Name = channel.Name;
        model.About = channel.About;
        model.SubscribersCount = channel.Subscribers.Count();
    }

    return View(model);
}
```

**Tính năng đặc biệt:**
- ✅ **Session-based error preservation** qua redirects
- ✅ **Conditional UI rendering** (create form vs dashboard)
- ✅ **Real-time subscriber count**

---

## 📝 Channel Creation Flow

### 🎯 Chi Tiết Channel Creation Logic:

#### **📝 Bước 1: Create Form Processing**
📍 **File**: `Controllers/ChannelController.cs:52`
```csharp
[HttpPost]
public async Task<IActionResult> CreateChannel(ChannelAddEdit_vm model)
{
    // ✅ Validate model state
    if (!ModelState.IsValid)
    {
        // 📥 Collect validation errors
        model.Errors = ModelState.Where(ms => ms.Value.Errors.Count > 0)
            .Select(ms => new ModelError_vm
            {
                Key = ms.Key,
                ErrorMessage = ms.Value.Errors.First().ErrorMessage
            }).ToList();

        // 💾 Store in session for preservation across redirect
        HttpContext.Session.SetString("ChannelModelFromSession", 
            JsonConvert.SerializeObject(model));

        return RedirectToAction("Index");
    }

    // 🔍 Check for duplicate channel name
    var duplicateChannel = await UnitOfWork.ChannelRepo.AnyAsync(
        x => x.Name.ToLower() == model.Name.ToLower()
    );

    if (duplicateChannel)
    {
        // ❌ Add business logic error
        ModelState.AddModelError("Name", "Channel name already exists");
        
        // 📥 Collect all errors including the new one
        model.Errors = ModelState.Where(ms => ms.Value.Errors.Count > 0)
            .Select(ms => new ModelError_vm
            {
                Key = ms.Key,
                ErrorMessage = ms.Value.Errors.First().ErrorMessage
            }).ToList();

        // 💾 Persist errors in session
        HttpContext.Session.SetString("ChannelModelFromSession", 
            JsonConvert.SerializeObject(model));

        return RedirectToAction("Index");
    }

    // 🎉 Create new channel
    var channel = new Channel
    {
        Name = model.Name,
        About = model.About,
        AppUserId = User.GetUserId(),
        CreatedAt = DateTime.Now
    };

    await UnitOfWork.ChannelRepo.AddAsync(channel);
    await UnitOfWork.SaveAsync();

    TempData["notification"] = "Channel created successfully!";
    return RedirectToAction("Index");
}
```

**Flow chi tiết Creation Process:**
1. **Validation Layer**: Multi-level validation với comprehensive error handling
   - **ModelState Check**: Validate all DataAnnotation rules trước
   - **Error Collection**: Collect tất cả validation errors vào standardized format
   - **Session Persistence**: Serialize errors để preserve qua POST-Redirect-GET pattern
   - **Early Return**: Redirect về Index với errors nếu validation fails
2. **Business Logic Validation**: Check duplicate name constraint
   - **Case-insensitive Check**: Use ToLower() để avoid case sensitivity issues
   - **Database Query**: Efficient AnyAsync() query để check existence only
   - **Error Addition**: Add business error vào ModelState như validation error
   - **Consistent Error Handling**: Same error collection và session storage pattern
3. **Entity Creation**: Create Channel entity với proper relationships
   - **Data Mapping**: Map from ViewModel to Entity với required fields
   - **User Association**: Link channel với current user qua GetUserId()
   - **Timestamp**: Set CreatedAt để track creation time
   - **Repository Pattern**: Use UnitOfWork để maintain transaction consistency
4. **Success Response**: Handle successful creation với user feedback
   - **Database Persistence**: Save entity với UnitOfWork.SaveAsync()
   - **User Notification**: Set TempData notification cho success message
   - **Redirect**: Navigate về dashboard để show newly created channel

**Tính năng đặc biệt Creation:**
- ✅ **POST-Redirect-GET pattern** để prevent double submission
- ✅ **Session-based error preservation** qua redirects
- ✅ **Comprehensive validation** (client + server + business)
- ✅ **Case-insensitive duplicate check** để ensure uniqueness
- ✅ **Transactional consistency** với UnitOfWork pattern

### 📊 Channel Creation Flow Diagram
```
                        📝 CHANNEL CREATION SYSTEM
                        
   🖥️ CREATE FORM              📡 VALIDATION                🗄️ DATABASE
                        
┌─────────────────────┐                                   ┌─────────────────────┐
│  📝 Create Form     │─── POST /Channel/CreateChannel ──▶│  ✅ ModelState       │
│                     │                                   │    Validation       │
│ ┌─Name──────────┐   │                                   │                     │
│ │ (3-15 chars)  │   │                                   │ • Required fields   │
│ └───────────────┘   │                                   │ • RegExp validation │
│ ┌─About─────────┐   │                                   │ • String length     │
│ │ (20-200 chars)│   │                                   └─────────────────────┘
│ └───────────────┘   │                                            │
│ [🚀 Create]         │                                            ▼
└─────────────────────┘                                   ┌─────────────────────┐
           │                                              │  🔍 Duplicate        │
           │                                              │    Name Check       │
           ▼                                              │                     │
┌─────────────────────┐                                   │ ChannelRepo         │
│  🎯 Form Submission │                                   │ .AnyAsync()         │
│                     │                                   │ (Name.ToLower())    │
│ • Validation check  │                                   └─────────────────────┘
│ • Error collection  │                                            │
│ • Session storage   │                                            ▼
└─────────────────────┘                              ┌─────────────┴─────────────┐
           │                                         ▼                           ▼
           ▼                                ┌─────────────────┐         ┌─────────────────┐
┌─────────────────────┐                    │  ❌ Name Taken   │         │  ✅ Name Valid   │
│  🔄 Error Handling  │                    │                 │         │                 │
│                     │                    │ • Add Error     │         │ • Create Channel│
│ Validation Failed?  │◀───────────────────│ • Store Session │         │ • Save to DB    │
│ Name Taken?         │                    │ • Redirect      │         │ • Success Msg   │
└─────────────────────┘                    └─────────────────┘         └─────────────────┘
           │                                         │                           │
           ▼                                         │                           ▼
┌─────────────────────┐                              │                 ┌─────────────────┐
│  💾 Session Storage │                              │                 │  🎉 Success      │
│                     │                              │                 │                 │
│ • Serialize errors  │◀─────────────────────────────┘                 │ TempData        │
│ • Store in session  │                                                │ ["notification"]│
│ • Redirect to Index │                                                │ = success msg   │
└─────────────────────┘                                                └─────────────────┘
           │                                                                    │
           ▼                                                                    ▼
┌─────────────────────┐                                                ┌─────────────────┐
│  🔄 Redirect        │                                                │  🏠 Dashboard    │
│                     │                                                │    Redirect     │
│ Return to Index()   │                                                │                 │
│ with preserved      │                                                │ Show success    │
│ error state         │                                                │ + new channel   │
└─────────────────────┘                                                └─────────────────┘
```

### 🎯 Chi Tiết Channel Creation:

#### **📝 Bước 1: Form Submission & Validation**
📍 **File**: `Controllers/ChannelController.cs:48`
```csharp
[HttpPost]
public async Task<IActionResult> CreateChannel(ChannelAddEdit_vm model)
{
    if (!ModelState.IsValid)
    {
        // 🚨 Collect validation errors
        foreach (var item in ModelState)
        {
            if (item.Value.Errors.Count > 0)
            {
                model.Errors.Add(new ModelError_vm
                {
                    Key = item.Key,
                    ErrorMessage = item.Value.Errors.Select(x => x.ErrorMessage).FirstOrDefault()
                });
            }
        }

        // 💾 Store errors in session for preservation
        HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));
        return RedirectToAction("Index");
    }

    // 🔍 Check for duplicate channel name
    var channelNameExists = await UnitOfWork.ChannelRepo.AnyAsync(x => x.Name.ToLower() == model.Name.ToLower());
    if (channelNameExists)
    {
        model.Errors.Add(new ModelError_vm
        {
            Key = "Name",
            ErrorMessage = $"Channel name of {model.Name} is taken. Please try other name"
        });

        HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));
        return RedirectToAction("Index");
    }

    // 📺 Create new channel
    var channelToAdd = new Channel
    {
        AppUserId = User.GetUserId(),   // 🔗 Link to current user
        Name = model.Name,
        About = model.About,
    };

    UnitOfWork.ChannelRepo.Add(channelToAdd);
    await UnitOfWork.CompleteAsync();

    // 🎉 Success notification
    TempData["notification"] = "true;Channel Created;Your channel has been created and you can upload clips now";
    return RedirectToAction("Index");
}
```

#### **📋 ViewModel Validation Rules**
📍 **File**: `ViewModels/Channel/ChannelAddEdit_vm.cs`
```csharp
public class ChannelAddEdit_vm
{
    [Required]
    [Display(Name = "Channel name")]
    [RegularExpression("^[a-zA-Z]{3,15}", 
        ErrorMessage = "Name must be between 3 and 15 characters long and can only contain letters (A-Z, a-z)")]
    public string Name { get; set; }
    
    [Required(ErrorMessage = "About field is required")]
    [StringLength(200, MinimumLength = 20, 
        ErrorMessage = "About must be at least {2}, and maximum {1} characters")]
    [Display(Name = "About your channel")]
    public string About { get; set; }
    
    public List<ModelError_vm> Errors { get; set; } = new List<ModelError_vm>();
    public int SubscribersCount { get; set; }
}
```

**Validation Rules:**
- ✅ **Name**: 3-15 characters, letters only (A-Z, a-z)
- ✅ **About**: 20-200 characters, required
- ✅ **Unique Name**: Server-side duplicate check

---

## ✏️ Channel Edit Flow

### 📊 Channel Edit Flow Diagram
```
                        ✏️ CHANNEL EDIT SYSTEM
                        
   🎨 EDIT INTERFACE           📡 UPDATE PROCESS           🗄️ DATABASE
                        
┌─────────────────────┐                                 ┌─────────────────────┐
│  📊 Dashboard Tab   │                                 │  🔍 Channel Lookup   │
│                     │                                 │                     │
│ 📝 Profile Tab:     │                                 │ ChannelRepo         │
│ ┌─Name──────────┐   │                                 │ .GetFirstOrDefault  │
│ │ Current Name  │   │─── POST /Channel/EditChannel ──▶│ (UserId)            │
│ └───────────────┘   │                                 └─────────────────────┘
│ ┌─About─────────┐   │                                          │
│ │ Current About │   │                                          ▼
│ └───────────────┘   │                                 ┌─────────────────────┐
│ [💾 Update]         │                                 │  ✅ Validation       │
└─────────────────────┘                                 │                     │
           │                                            │ • ModelState.IsValid│
           ▼                                            │ • Required fields   │
┌─────────────────────┐                                 │ • Length constraints│
│  📤 Form Submit     │                                 └─────────────────────┘
│                     │                                          │
│ ChannelAddEdit_vm   │                                          ▼
│ with updated data   │                              ┌─────────────┴─────────────┐
└─────────────────────┘                              ▼                           ▼
                                            ┌─────────────────┐         ┌─────────────────┐
                                            │  ❌ Validation   │         │  ✅ Valid Data   │
                                            │    Failed       │         │                 │
                                            │                 │         │ • Update Name   │
                                            │ • Error message │         │ • Update About  │
                                            │ • Redirect      │         │ • Save changes  │
                                            └─────────────────┘         └─────────────────┘
                                                     │                           │
                                                     ▼                           ▼
                                            ┌─────────────────┐         ┌─────────────────┐
                                            │  🚨 Error        │         │  🎉 Success      │
                                            │   Handling      │         │                 │
                                            │                 │         │ TempData        │
                                            │ "Channel not    │         │ "Channel        │
                                            │  found"         │         │  updated"       │
                                            └─────────────────┘         └─────────────────┘
                                                     │                           │
                                                     ▼                           ▼
                                            ┌─────────────────────────────────────────┐
                                            │         🏠 REDIRECT TO INDEX             │
                                            │                                         │
                                            │  • Refresh dashboard with new data     │
                                            │  • Show notification message           │
                                            │  • Updated subscriber count            │
                                            └─────────────────────────────────────────┘
```

### 🎯 Chi Tiết Channel Edit Logic:

#### **✏️ Bước 1: Edit Channel Processing**
📍 **File**: `Controllers/ChannelController.cs:93`
```csharp
[HttpPost]
public async Task<IActionResult> EditChannel(ChannelAddEdit_vm model)
{
    if (ModelState.IsValid)
    {
        // 🔍 Find user's channel for ownership verification
        var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(
            x => x.AppUserId == User.GetUserId()
        );
        
        if (channel != null)
        {
            // 📝 Update channel properties
            channel.Name = model.Name;
            channel.About = model.About;
            channel.UpdatedAt = DateTime.Now;  // Track modification time
            
            // 💾 Persist changes
            await UnitOfWork.CompleteAsync();

            // 🎉 Success notification
            TempData["notification"] = "true;Channel updated;Your channel is updated";
            return RedirectToAction("Index");
        }
        else
        {
            // 🚨 Channel not found error
            TempData["notification"] = "false;Not Found;Your channel was not found";
            return RedirectToAction("Index");
        }
    }

    // ❌ Validation failed - collect errors
    model.Errors = ModelState.Where(ms => ms.Value.Errors.Count > 0)
        .Select(ms => new ModelError_vm
        {
            Key = ms.Key,
            ErrorMessage = ms.Value.Errors.First().ErrorMessage
        }).ToList();

    // 💾 Store errors in session
    HttpContext.Session.SetString("ChannelModelFromSession", 
        JsonConvert.SerializeObject(model));
    
    return RedirectToAction("Index");
}
```

**Flow chi tiết Edit Process:**
1. **Validation Gate**: ModelState validation với comprehensive error handling
   - **Server-side Validation**: Kiểm tra all DataAnnotation rules
   - **Early Exit**: Return errors immediately nếu validation fails
   - **Consistent Error Format**: Same error collection pattern như Create
2. **Ownership Verification**: Security check để ensure user chỉ edit own channel
   - **User Lookup**: Query channel theo current user ID từ claims
   - **Security Layer**: Prevent unauthorized access to other user's channels
   - **Not Found Handling**: Graceful error nếu channel không exist
3. **Data Update**: Direct entity modification với tracked changes
   - **Property Mapping**: Update Name và About từ ViewModel
   - **Timestamp Update**: Track modification time cho audit trail
   - **Change Tracking**: EF Core automatically track entity changes
4. **Persistence & Response**: Save changes và provide user feedback
   - **UnitOfWork Pattern**: Ensure transactional consistency
   - **Success Notification**: TempData message cho user confirmation
   - **Redirect Pattern**: POST-Redirect-GET để prevent double submission

**Tính năng đặc biệt Edit:**
- ✅ **Ownership verification** để ensure security
- ✅ **Same validation pattern** như Create để maintain consistency
- ✅ **Audit trail** với UpdatedAt timestamp
- ✅ **Graceful error handling** cho edge cases
- ✅ **Session error preservation** để maintain UX qua redirects
    return RedirectToAction("Index");
}
```

---

## 📊 Analytics Dashboard Flow

### 📊 Analytics Flow Diagram
```
                        📊 CHANNEL ANALYTICS SYSTEM
                        
   🎯 ANALYTICS ACCESS         📈 DATA PROCESSING          📊 VISUALIZATION
                        
┌─────────────────────┐                                  ┌─────────────────────┐
│  📊 Analytics Tab   │─── GET /Channel/Analytics ─────▶│  🔍 Data Collection  │
│                     │                                 │                     │
│ User clicks         │                                 │ • Channel lookup    │
│ Analytics tab       │                                 │ • Include Videos    │
└─────────────────────┘                                 │ • Include Subscribers│
           │                                            │ • Include VideoViews│
           ▼                                            └─────────────────────┘
┌─────────────────────┐                                          │
│  🌐 Navigate to     │                                          ▼
│   Analytics Page    │                                 ┌─────────────────────┐
└─────────────────────┘                                 │  📈 Calculate        │
           │                                            │    Metrics          │
           ▼                                            │                     │
┌─────────────────────┐                                 │ • Total Videos      │
│  📊 Analytics UI    │◀──── Return ViewBag Data ──────│ • Total Views       │
│                     │                                 │ • Total Subscribers │
│ 📋 Metric Cards:    │                                 │ • Top 5 Videos      │
│ ┌─Total Videos──┐   │                                 │ • Chart Data        │
│ │     42        │   │                                 └─────────────────────┘
│ └───────────────┘   │                                          │
│ ┌─Total Views───┐   │                                          ▼
│ │    1,234      │   │                                 ┌─────────────────────┐
│ └───────────────┘   │                                 │  📊 Chart.js Data   │
│ ┌─Subscribers───┐   │                                 │    Preparation      │
│ │     156       │   │                                 │                     │
│ └───────────────┘   │                                 │ • Labels Array      │
│                     │                                 │ • Data Array        │
│ 📊 Chart.js:        │                                 │ • JSON Serialize    │
│ ┌─Bar Chart─────┐   │                                 │ • ViewBag passing   │
│ │ Top 5 Videos  │   │                                 └─────────────────────┘
│ │ by Views      │   │
│ └───────────────┘   │
└─────────────────────┘
           │
           ▼
┌─────────────────────┐
│  🎨 Real-time       │
│    Visualization    │
│                     │
│ • Responsive design │
│ • Bootstrap cards   │
│ • Chart.js charts   │
│ • Color-coded data  │
└─────────────────────┘
```

### 🎯 Chi Tiết Analytics Processing:

#### **📊 Analytics Data Collection**
📍 **File**: `Controllers/ChannelController.cs:109`
```csharp
[HttpGet]
public async Task<IActionResult> Analytics()
{
    var userId = User.GetUserId();
    
    // 🔍 Load channel with related data
    var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(
        x => x.AppUserId == userId, 
        includeProperties: "Videos,Subscribers"    // 📊 Include for calculations
    );
    
    if (channel == null)
    {
        TempData["notification"] = "false;Not Found;Your channel was not found";
        return RedirectToAction("Index");
    }

    // 📈 Calculate metrics
    
    // 🎬 Total video count
    var totalVideos = channel.Videos?.Count() ?? 0;
    
    // 👁️ Total view count (sum all video views)
    var totalViews = channel.Videos?.SelectMany(v => v.Viewers ?? new List<VideoView>()).Count() ?? 0;
    
    // 👥 Total subscriber count
    var totalSubscribers = channel.Subscribers?.Count() ?? 0;

    // 🏆 Top 5 videos by view count
    var topVideos = (channel.Videos ?? new List<Video>())
        .OrderByDescending(v => (v.Viewers?.Count() ?? 0))
        .Take(5)
        .Select(v => new { v.Title, Views = v.Viewers?.Count() ?? 0 })
        .ToList();

    // 📊 Prepare chart data
    var chartLabels = topVideos.Select(v => v.Title).ToArray();
    var chartData = topVideos.Select(v => v.Views).ToArray();

    // 💾 Pass data to view via ViewBag
    ViewBag.TotalVideos = totalVideos;
    ViewBag.TotalViews = totalViews;
    ViewBag.TotalSubscribers = totalSubscribers;
    ViewBag.ChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(chartLabels);
    ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData);

    return View();
}
```

#### **📊 Analytics View Integration**
📍 **File**: `Views/Channel/Analytics.cshtml`
```html
@{
    var totalVideos = ViewBag.TotalVideos ?? 0;
    var totalViews = ViewBag.TotalViews ?? 0;
    var totalSubscribers = ViewBag.TotalSubscribers ?? 0;
    var chartLabels = ViewBag.ChartLabels ?? "[]";
    var chartData = ViewBag.ChartData ?? "[]";
}

<div class="container py-4">
    <h2 class="mb-4">Channel Analytics</h2>
    
    <!-- 📊 Metric Cards -->
    <div class="row mb-4">
        <div class="col-md-4">
            <div class="card text-center shadow-sm border-0 mb-3">
                <div class="card-body">
                    <h5 class="card-title">Total Videos</h5>
                    <p class="display-6 fw-bold text-primary">@totalVideos</p>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card text-center shadow-sm border-0 mb-3">
                <div class="card-body">
                    <h5 class="card-title">Total Views</h5>
                    <p class="display-6 fw-bold text-success">@totalViews</p>
                </div>
            </div>
        </div>
        <div class="col-md-4">
            <div class="card text-center shadow-sm border-0 mb-3">
                <div class="card-body">
                    <h5 class="card-title">Subscribers</h5>
                    <p class="display-6 fw-bold text-danger">@totalSubscribers</p>
                </div>
            </div>
        </div>
    </div>
    
    <!-- 📈 Chart Visualization -->
    <div class="card shadow-sm border-0 mb-4">
        <div class="card-body">
            <h5 class="card-title mb-3">Top 5 Videos by Views</h5>
            <canvas id="viewsChart" height="120"></canvas>
        </div>
    </div>
</div>

@section Scripts {
    <script src="https://cdn.jsdelivr.net/npm/chart.js"></script>
    <script>
        var ctx = document.getElementById('viewsChart').getContext('2d');
        var chart = new Chart(ctx, {
            type: 'bar',
            data: {
                labels: @Html.Raw(chartLabels),     // 🏷️ Video titles
                datasets: [{
                    label: 'Views',
                    data: @Html.Raw(chartData),     // 📊 View counts
                    backgroundColor: 'rgba(54, 162, 235, 0.2)',
                    borderColor: 'rgba(54, 162, 235, 1)',
                    borderWidth: 1
                }]
            },
            options: {
                responsive: true,
                scales: {
                    y: {
                        beginAtZero: true
                    }
                }
            }
        });
    </script>
}
```

---

## 💾 Session Management

### 🎯 Session-based Error Preservation

ClipShare sử dụng **Session Storage** để preserve form data và errors qua POST-Redirect-GET pattern:

#### **💾 Storing Data in Session**
```csharp
// Serialize model với errors
HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));

// Redirect để tránh double-submit
return RedirectToAction("Index");
```

#### **📥 Retrieving Data from Session**
```csharp
// Check session cho preserved data
stringModel = HttpContext.Session.GetString("ChannelModelFromSession");

if (!string.IsNullOrEmpty(stringModel))
{
    // Deserialize model
    model = JsonConvert.DeserializeObject<ChannelAddEdit_vm>(stringModel);
    
    if (model.Errors.Count > 0)
    {
        // Restore validation errors to ModelState
        foreach (var error in model.Errors)
        {
            ModelState.AddModelError(error.Key, error.ErrorMessage);
        }

        // Clear session after use
        HttpContext.Session.Remove("ChannelModelFromSession");
        return View(model);
    }
}
```

### 🔄 Why Session Management?

1. **POST-Redirect-GET Pattern**: Prevents form resubmission on page refresh
2. **Error Preservation**: Maintains validation errors across redirects
3. **Better UX**: Users don't lose form data on errors
4. **Clean URLs**: No query parameters with error data

---

## 🌐 Integration với Hệ Thống

### 📺 Channel-User Relationship
```csharp
// One-to-One relationship
public class Channel : BaseEntity
{
    public int AppUserId { get; set; }      // 🔗 Foreign key
    public AppUser AppUser { get; set; }    // 👤 Navigation property
    
    // Collections
    public ICollection<Video> Videos { get; set; }         // 🎬 Channel's videos
    public ICollection<Subscribe> Subscribers { get; set; } // 👥 Subscribers
}
```

### 🎬 Video Grid Integration
Channel dashboard includes AJAX-powered video grid:
- Real-time video management
- Sort/filter capabilities  
- Direct link to video creation
- Video analytics integration

### 👥 Subscription System Integration
- Real-time subscriber count display
- Subscribe/Unsubscribe functionality (handled in VideoController)
- Subscriber analytics và tracking

---

## ❌ Error Handling

### 🚨 Error Scenarios & Messages

#### **Channel Creation Errors:**
1. **Validation Errors**
   ```csharp
   // Name validation
   [RegularExpression("^[a-zA-Z]{3,15}", 
       ErrorMessage = "Name must be between 3 and 15 characters long and can only contain letters")]
   
   // About validation  
   [StringLength(200, MinimumLength = 20, 
       ErrorMessage = "About must be at least {2}, and maximum {1} characters")]
   ```

2. **Duplicate Channel Name**
   ```csharp
   model.Errors.Add(new ModelError_vm
   {
       Key = "Name",
       ErrorMessage = $"Channel name of {model.Name} is taken. Please try other name"
   });
   ```

#### **Channel Edit Errors:**
1. **Channel Not Found**
   ```csharp
   TempData["notification"] = "false;Not Found;Your channel was not found";
   ```

2. **Validation Failure**
   ```csharp
   // Automatically handled by ModelState validation
   ```

#### **Analytics Errors:**
1. **No Channel**
   ```csharp
   TempData["notification"] = "false;Not Found;Your channel was not found";
   return RedirectToAction("Index");
   ```

### 🎯 Error Display Patterns

#### **ModelState Errors (in Views)**
```html
<span asp-validation-for="Name" class="text-danger"></span>
<span asp-validation-for="About" class="text-danger"></span>
```

#### **TempData Notifications**
```csharp
// Success format
TempData["notification"] = "true;Channel Created;Your channel has been created and you can upload clips now";

// Error format  
TempData["notification"] = "false;Not Found;Your channel was not found";
```

#### **Custom Error Model**
```csharp
public class ModelError_vm
{
    public string Key { get; set; }          // Field name
    public string ErrorMessage { get; set; } // Error description
}
```

---

## 🎯 Best Practices

### ✅ Session Management Best Practices
1. **Clear session data** after use để avoid memory leaks
2. **JSON serialization** cho complex objects
3. **POST-Redirect-GET** pattern cho better UX
4. **Error preservation** across redirects

### ✅ Analytics Best Practices  
1. **Efficient queries** với selective Include
2. **Null safety** với null-conditional operators
3. **Top-N queries** thay vì loading tất cả data
4. **Client-side charting** cho responsive visualization

### ✅ Validation Best Practices
1. **Server-side validation** for security
2. **Client-side validation** for UX
3. **Custom validation** cho business rules
4. **Duplicate checking** cho unique constraints

### ✅ Performance Optimizations
1. **Selective loading** với includeProperties
2. **Projection queries** cho analytics
3. **Async operations** cho database calls
4. **ViewBag data passing** thay vì ViewData

### ✅ User Experience
1. **Tab-based navigation** trong dashboard
2. **Real-time updates** cho subscriber count
3. **Visual feedback** qua notifications
4. **Responsive design** cho mobile compatibility

---

**📺 Channel Management System - User-friendly, Analytics-rich, và Scalable!**

*Cập nhật: August 2025 | Tác giả: BbySharp-dev*
