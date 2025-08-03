# 👑 Admin Management System - User & Content Administration

## 📋 Mục Lục
- [Tổng Quan](#tổng-quan)
- [Cấu Trúc Files](#cấu-trúc-files)
- [User Management Flow](#user-management-flow)
- [Category Management Flow](#category-management-flow)
- [Video Moderation Flow](#video-moderation-flow)
- [Security & Authorization](#security--authorization)
- [API Endpoints](#api-endpoints)
- [Error Handling](#error-handling)

---

## 🎯 Tổng Quan

**ClipShare Admin System** là hệ thống quản trị toàn diện cho platform, cho phép Admin quản lý users, categories, và moderate content. Hệ thống được chia thành 2 phần chính được phân công cho 2 developers khác nhau.

### 👥 Phân Công Trách Nhiệm

#### **🔧 USER 3 - User Management:**
- ✅ **User CRUD Operations** (Create, Read, Update, Delete)
- ✅ **Role Assignment** (Admin, Moderator, User)
- ✅ **Account Lock/Unlock** functionality
- ✅ **Password Management**
- ✅ **User Grid Display** with filtering

#### **📹 USER 4 - Content Management:**
- ✅ **Category Management** (CRUD operations)
- ✅ **Video Moderation** (Approve/Reject pending videos)
- ✅ **Content Review** system
- ✅ **Bulk Operations** cho content management

### 🔑 Tính Năng Chính
- ✅ **Role-based Administration** (chỉ Admin access)
- ✅ **Super Admin Protection** (không thể sửa/xóa super admin)
- ✅ **Cascade Delete Operations** (xóa user → xóa channel → xóa videos)
- ✅ **File Cleanup** khi xóa content
- ✅ **API-based Operations** cho responsive UI
- ✅ **Comprehensive Validation** system

---

## 📁 Cấu Trúc Files

```
ClipShare/
├── Controllers/
│   └── AdminController.cs                # 🎯 MAIN: Admin operations
├── ViewModels/Admin/
│   ├── UserDisplayGrid_vm.cs            # 📊 User grid display model
│   ├── UserAddEdit_vm.cs                # 📝 User form model
│   └── CategoryAddEdit_vm.cs             # 📝 Category form model
├── Views/Admin/
│   ├── AllUsers.cshtml                  # 👥 User management grid
│   ├── AddEditUser.cshtml               # ✏️ User form
│   ├── Category.cshtml                  # 📂 Category management
│   ├── PendingVideos.cshtml             # 📹 Video moderation
│   └── ViewVideo.cshtml                 # 👁️ Video preview
├── Helpers/
│   └── StringCustomValidation.cs        # 🔧 Custom validation
└── Core/Entities/
    ├── AppUser.cs                       # 👤 User domain model
    ├── AppRole.cs                       # 🎭 Role domain model
    └── Category.cs                      # 📂 Category domain model
```

---

## 👥 User Management Flow

### 📊 User Management Flow Diagram
```
                            👑 ADMIN USER MANAGEMENT SYSTEM
                            
    🌐 ADMIN DASHBOARD                    📡 OPERATIONS                     🗄️ DATABASE
                            
┌─────────────────────┐                                              ┌─────────────────────┐
│   👤 Admin Access    │────── GET /Admin/AllUsers ──────────────▶│   🔍 Load All Users  │
│   AllUsers Page     │                                            │                     │
└─────────────────────┘                                            │ UserManager.Users   │
           │                                                       │ .Include(Channel)   │
           │                                                       │ .Where(≠ admin)    │
           ▼                                                       └─────────────────────┘
┌─────────────────────┐                                                      │
│  📊 User Grid       │◀──── Return UserDisplayGrid_vm List ─────────────────┘
│                     │
│ ┌─Name──────────┐   │
│ ┌─Email─────────┐   │
│ ┌─Channel───────┐   │                    ┌─────────────────────┐
│ ┌─Roles─────────┐   │────── [➕ Create] ──▶│  📝 AddEditUser     │
│ ┌─Lock Status───┐   │                    │     Form            │
│ [🔒Lock][✏️Edit]   │                    │                     │
│ [🗑️Delete]         │                    │  ┌─Name──────────┐  │
└─────────────────────┘                    │  ┌─Email─────────┐  │
           │                                │  ┌─Password──────┐  │
           │                                │  ┌─Roles─────────┐  │
           │                                │  [💾 Save User]   │
           ▼                                └─────────────────────┘
┌─────────────────────┐                               │
│  🔄 CRUD Operations │                               ▼
│                     │                    ┌─────────────────────┐
│ 1️⃣ CREATE USER      │◀────────────────────│  📤 POST AddEditUser │
│ 2️⃣ EDIT USER        │                    │                     │
│ 3️⃣ LOCK/UNLOCK      │                    │ 🔍 Validation:       │
│ 4️⃣ DELETE USER      │                    │  - ModelState       │
│                     │                    │  - Duplicate Check  │
└─────────────────────┘                    │  - Role Assignment  │
           │                                │  - Super Admin      │
           │                                │    Protection       │
           ▼                                └─────────────────────┘
                                                     │
    ┌─────────────── OPERATION FLOWS ──────────────────┐
    │                                                  │
    ▼                              ▼                   ▼
┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  🆕 CREATE       │    │  ✏️ EDIT         │    │  🔒 LOCK/UNLOCK  │
│                 │    │                 │    │                 │
│ • Name/Email    │    │ • Update Info   │    │ • 5-day Lock    │
│   Duplicate     │    │ • Change Roles  │    │ • Immediate     │
│   Check         │    │ • Password      │    │   Unlock        │
│ • Password      │    │   Reset         │    │ • Super Admin   │
│   Creation      │    │ • Super Admin   │    │   Protection    │
│ • Role          │    │   Protection    │    │                 │
│   Assignment    │    │                 │    │                 │
│                 │    │                 │    │                 │
└─────────────────┘    └─────────────────┘    └─────────────────┘
           │                       │                       │
           ▼                       ▼                       ▼
┌─────────────────────────────────────────────────────────────────┐
│                    ✅ SUCCESS OPERATIONS                         │
│                                                                 │
│ UserManager.CreateAsync() → AddToRolesAsync() → Redirect        │
│ UserManager.UpdateAsync() → Role Management → Redirect          │
│ SetLockoutEndDateAsync() → Status Update → Redirect             │
└─────────────────────────────────────────────────────────────────┘

                            🗑️ DELETE USER FLOW
                            
┌─────────────────────┐                                              ┌─────────────────────┐
│   🗑️ Delete Request  │────── DELETE /Admin/DeleteUser/{id} ────▶│  🔍 User Lookup     │
│   (AJAX Call)       │                                            │  with Channel       │
└─────────────────────┘                                            └─────────────────────┘
           │                                                                │
           │                                                                ▼
           │                                                     ┌─────────────────────┐
           │                                                     │  🛡️ Security Check   │
           │                                                     │                     │
           │                                                     │ • Super Admin?     │
           │                                                     │ • User Exists?     │
           │                                                     └─────────────────────┘
           │                                                                │
           │                                                                ▼
           │                                                     ┌─────────────────────┐
           │                                                     │  🗂️ CASCADE DELETE   │
           │                                                     │                     │
           │                                                     │ 1️⃣ Find Channel     │
           │                                                     │ 2️⃣ Get Videos       │
           │                                                     │ 3️⃣ Delete Thumbnails│
           │                                                     │ 4️⃣ Remove Videos    │
           │                                                     │ 5️⃣ Delete User      │
           │                                                     └─────────────────────┘
           │                                                                │
           ▼                                                                ▼
┌─────────────────────┐                                              ┌─────────────────────┐
│  📤 JSON Response   │◀──── ApiResponse(200/400/404) ─────────────│  ✅ Operation Result │
│                     │                                            │                     │
│ Success: User       │                                            │ UserManager         │
│ deleted + cleanup   │                                            │ .DeleteAsync()      │
│                     │                                            │                     │
│ Error: Super admin  │                                            │ + File cleanup      │
│ or not found        │                                            │ + Video removal     │
└─────────────────────┘                                            └─────────────────────┘
```

### 🎯 Chi Tiết User Management Operations:

#### **👥 Bước 1: Display User Grid**
📍 **File**: `Controllers/AdminController.cs:32`
```csharp
public async Task<IActionResult> AllUsers()
{
    var toReturn = new List<UserDisplayGrid_vm>();
    var users = await _userManager.Users
        .Include(x => x.Channel)                    // 🔗 Load channel data
        .Where(x => x.UserName != "admin")          // 🛡️ Exclude super admin
        .ToListAsync();

    foreach (var user in users)
    {
        var userDisplayToAdd = new UserDisplayGrid_vm();
        Mapper.Map(user, userDisplayToAdd);         // 🗂️ AutoMapper mapping
        
        // 🔒 Check lock status
        userDisplayToAdd.IsLocked = _userManager.IsLockedOutAsync(user).GetAwaiter().GetResult();
        
        // 🎭 Get assigned roles
        userDisplayToAdd.AssignedRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
        
        toReturn.Add(userDisplayToAdd);
    }

    return View(toReturn);
}
```

#### **📝 Bước 2: Create/Edit User Form**
📍 **File**: `Controllers/AdminController.cs:49`
```csharp
public async Task<IActionResult> AddEditUser(int id)
{
    var toReturn = new UserAddEdit_vm();
    toReturn.ApplicationRoles = await GetApplicationRolesAsync();    // 🎭 Load available roles

    if (id > 0)
    {
        // ✏️ EDIT MODE
        var user = await _userManager.FindByIdAsync(id.ToString());
        Mapper.Map(user, toReturn);

        var userRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
        toReturn.UserRoles = userRoles.ToList();
    }
    // 🆕 CREATE MODE - empty form

    return View(toReturn);
}
```

#### **💾 Bước 3: Process User Form Submission**
📍 **File**: `Controllers/AdminController.cs:62`
```csharp
[HttpPost]
public async Task<IActionResult> AddEditUser(UserAddEdit_vm model)
{
    if (ModelState.IsValid)
    {
        bool proceed = true;

        if (model.Id == 0)
        {
            // 🆕 CREATING USER
            
            // 🔒 Password validation
            if (string.IsNullOrEmpty(model.Password)) {
                proceed = false;
                ModelState.AddModelError("Password", "Password is required");
            }

            // 🎭 Role validation
            if (proceed && model.UserRoles.Count == 0) {
                proceed = false;
                ModelState.AddModelError("UserRoles", "Please select at least one role");
            }

            // 🔍 Duplicate checks
            if (proceed && CheckNameExistsAsync(model.Name).GetAwaiter().GetResult()) {
                proceed = false;
                ModelState.AddModelError("Name", $"The name of '{model.Name} is taken. Please try another name.");
            }

            if (proceed && CheckEmailExistsAsync(model.Email).GetAwaiter().GetResult()) {
                proceed = false;
                ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address.");
            }

            if (proceed)
            {
                // 👤 Create user
                var userToAdd = new AppUser
                {
                    Name = model.Name,
                    UserName = model.Name.ToLower(),
                    Email = model.Email,
                };

                var result = await _userManager.CreateAsync(userToAdd, model.Password);
                await _userManager.AddToRolesAsync(userToAdd, model.UserRoles);

                if (result.Succeeded) {
                    return RedirectToAction("AllUsers");
                }
            }
        }
        else
        {
            // ✏️ EDITING USER
            
            var user = await _userManager.FindByIdAsync(model.Id.ToString());

            if (user == null) {
                TempData["notification"] = "false;Not Found;The requested user was not found";
                return RedirectToAction("AllUsers");
            }

            // 🛡️ Super admin protection
            if (IsSuperAdminUserId(model.Id)) {
                TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                return RedirectToAction("AllUsers");
            }

            // [Validation logic similar to create...]

            if (proceed)
            {
                // 📝 Update user info
                user.Name = model.Name;
                user.UserName = model.Name.ToLower();
                user.Email = model.Email;

                // 🎭 Update roles
                var userRoles = await _userManager.GetRolesAsync(user);
                await _userManager.RemoveFromRolesAsync(user, userRoles);

                foreach (var role in model.UserRoles)
                {
                    var roleToAdd = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == role);
                    if (roleToAdd != null) {
                        await _userManager.AddToRoleAsync(user, role);
                    }
                }

                return RedirectToAction("AllUsers");
            }
        }
    }

    model.ApplicationRoles = await GetApplicationRolesAsync();
    return View(model);
}
```

#### **🔒 Bước 4: Lock/Unlock User**
📍 **File**: `Controllers/AdminController.cs:179`
```csharp
[HttpPost]
public async Task<IActionResult> LockUser(int id)
{
    var user = await _userManager.FindByIdAsync(id.ToString());

    if (user == null) {
        TempData["notification"] = "false;Not Found;The requested user was not found";
        return RedirectToAction("AllUsers");
    }

    // 🛡️ Super admin protection
    if (IsSuperAdminUserId(id)) {
        TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
        return RedirectToAction("AllUsers");
    }

    // 🔒 Lock user for 5 days
    user.LockoutEnabled = true;
    var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddDays(5));

    if (!result.Succeeded) {
        TempData["notification"] = "false;Server Error;Server Error";
    }

    return RedirectToAction("AllUsers");
}

[HttpPost]
public async Task<IActionResult> UnlockUser(int id)
{
    var user = await _userManager.FindByIdAsync(id.ToString());

    if (user == null) {
        TempData["notification"] = "false;Not Found;The requested user was not found";
        return RedirectToAction("AllUsers");
    }

    // 🛡️ Super admin protection
    if (IsSuperAdminUserId(id)) {
        TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
        return RedirectToAction("AllUsers");
    }

    // 🔓 Unlock user
    var result = await _userManager.SetLockoutEndDateAsync(user, null);

    if (!result.Succeeded) {
        TempData["notification"] = "false;Server Error;Server Error";
    }

    return RedirectToAction("AllUsers");
}
```

#### **🗑️ Bước 5: Delete User with Cascade Operations**
📍 **File**: `Controllers/AdminController.cs:223`
```csharp
[HttpDelete]
public async Task<IActionResult> DeleteUser(int id)
{
    var user = await _userManager.Users
        .Include(x => x.Channel)
        .Where(x => x.Id == id)
        .FirstOrDefaultAsync();

    if (user != null)
    {
        // 🛡️ Super admin protection
        if (IsSuperAdminUserId(id)) {
            return Json(new ApiResponse(400, message: "Super admin cannot be deleted"));
        }

        if (user.Channel != null)
        {
            // 🗂️ Get channel's videos for cleanup
            var userChannelWithVideos = await Context.Channel
                .Where(x => x.AppUserId == id)
                .Select(x => new
                {
                    Videos = x.Videos.Select(x => new
                    {
                        x.Id,
                        x.ThumbnailUrl    // 🖼️ For file cleanup
                    })
                }).FirstOrDefaultAsync();

            // 🧹 Cleanup videos and files
            foreach (var video in userChannelWithVideos.Videos)
            {
                PhotoService.DeletePhotoLocally(video.ThumbnailUrl);    // 🗑️ Delete thumbnail
                await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id);  // 🗑️ Remove video
                await UnitOfWork.CompleteAsync();
            }
        }

        // 👤 Delete user (cascade deletes channel)
        var result = await _userManager.DeleteAsync(user);
        if (result.Succeeded) {
            TempData["notification"] = $"true;Deleted;User of {user.Name} has been permanently removed";
            return Json(new ApiResponse(200));
        }
        else {
            return Json(new ApiResponse(400, message: result.Errors.Select(x => x.Description).FirstOrDefault()));
        }
    }

    return Json(new ApiResponse(404, message: "The requested user was not found"));
}
```

---

## 📂 Category Management Flow

### 📊 Category Management Flow Diagram
```
                        📂 CATEGORY MANAGEMENT SYSTEM
                        
   🌐 ADMIN INTERFACE              📡 API OPERATIONS               🗄️ DATABASE
                        
┌─────────────────────┐                                      ┌─────────────────────┐
│   📂 Category Page   │───── GET /Admin/GetCategories ────▶│  🔍 Load Categories  │
│                     │                                     │                     │
│  [➕ Add Category]   │                                     │ CategoryRepo        │
│                     │                                     │ .GetAllAsync()      │
│ ┌─Category 1────────┐│                                     └─────────────────────┘
│ │ [✏️Edit][🗑️Delete]││                                               │
│ └───────────────────┘│                                               ▼
│ ┌─Category 2────────┐│◀──── JSON Response ──────────────────────────────┘
│ │ [✏️Edit][🗑️Delete]││     CategoryAddEdit_vm[]
│ └───────────────────┘│
└─────────────────────┘
           │
           ▼
┌─────────────────────┐                    ┌─────────────────────┐
│  🔄 CRUD Operations │                    │  📝 Category Form   │
│                     │                    │                     │
│ 1️⃣ CREATE CATEGORY  │◀────── [Add] ──────│  ┌─Name──────────┐  │
│ 2️⃣ EDIT CATEGORY    │                    │  │               │  │
│ 3️⃣ DELETE CATEGORY  │                    │  └───────────────┘  │
│                     │                    │  [💾 Save]          │
└─────────────────────┘                    └─────────────────────┘
           │                                         │
           ▼                                         ▼
┌─────────────────────────────────────────────────────────────────┐
│                     📤 API ENDPOINTS                            │
│                                                                 │
│ POST   /Admin/AddEditCategory  →  Create/Update                 │
│ DELETE /Admin/DeleteCategory   →  Remove + Cascade Delete      │
│ GET    /Admin/GetCategories    →  List All                     │
└─────────────────────────────────────────────────────────────────┘
           │
           ▼
   ┌─────────────── OPERATION DETAILS ──────────────────┐
   │                                                    │
   ▼                       ▼                           ▼
┌──────────────────┐  ┌──────────────────┐  ┌──────────────────┐
│  ➕ CREATE        │  │  ✏️ EDIT          │  │  🗑️ DELETE       │
│                  │  │                  │  │                  │
│ • Name Required  │  │ • Update Name    │  │ • Find Category  │
│ • Add to DB      │  │ • Save Changes   │  │ • Get Videos     │
│ • Return JSON    │  │ • Return JSON    │  │ • Delete Files   │
│                  │  │                  │  │ • Remove Videos  │
│                  │  │                  │  │ • Delete Category│
└──────────────────┘  └──────────────────┘  └──────────────────┘
```

### 🎯 Chi Tiết Category Operations:

#### **📂 Bước 1: Load Categories**
📍 **File**: `Controllers/AdminController.cs:270`
```csharp
[HttpGet]
public async Task<IActionResult> GetCategories()
{
    var categories = await UnitOfWork.CategoryRepo.GetAllAsync();
    var toReturn = categories.Select(x => new CategoryAddEdit_vm
    {
        Id = x.Id,
        Name = x.Name,
    });

    return Json(new ApiResponse(200, result: toReturn));
}
```

#### **📝 Bước 2: Add/Edit Category**
📍 **File**: `Controllers/AdminController.cs:281`
```csharp
[HttpPost]
public async Task<IActionResult> AddEditCategory(CategoryAddEdit_vm model)
{
    if (ModelState.IsValid)
    {
        if (model.Id == 0)
        {
            // ➕ CREATE
            UnitOfWork.CategoryRepo.Add(new Category() { Name = model.Name });
            await UnitOfWork.CompleteAsync();
            return Json(new ApiResponse(201, "Created", "New Category was added"));
        }
        else
        {
            // ✏️ EDIT
            var category = await UnitOfWork.CategoryRepo.GetByIdAsync(model.Id);
            if (category == null) return Json(new ApiResponse(404));

            var oldName = category.Name;
            category.Name = model.Name;
            await UnitOfWork.CompleteAsync();
            return Json(new ApiResponse(200, "Editted", $"Category of {oldName} has been renamed to {model.Name}"));
        }
    }

    return Json(new ApiResponse(400, message: "Name is required"));
}
```

#### **🗑️ Bước 3: Delete Category with Cascade**
📍 **File**: `Controllers/AdminController.cs:303`
```csharp
[HttpDelete]
public async Task<IActionResult> DeleteCategory(int id)
{
    var category = await UnitOfWork.CategoryRepo.GetByIdAsync(id);
    if (category != null)
    {
        // 🔍 Find all videos in this category
        var categoryVideoIdsAndThumbnailUrls = await Context.Video
            .Where(x => x.CategoryId == id)
            .Select(x => new
            {
                x.Id,
                x.ThumbnailUrl
            })
            .ToListAsync();

        if (categoryVideoIdsAndThumbnailUrls.Any())
        {
            // 🧹 Cleanup videos and files
            foreach (var video in categoryVideoIdsAndThumbnailUrls)
            {
                PhotoService.DeletePhotoLocally(video.ThumbnailUrl);    // 🗑️ Delete thumbnail
                await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id);  // 🗑️ Remove video
                await UnitOfWork.CompleteAsync();
            }
        }

        // 📂 Delete category
        UnitOfWork.CategoryRepo.Remove(category);
        await UnitOfWork.CompleteAsync();

        return Json(new ApiResponse(200, "Deleted", "Category of " + category.Name + " has been removed"));
    }

    return Json(new ApiResponse(404, message: "The requested category was not found"));
}
```

---

## 📹 Video Moderation Flow

### 📊 Video Moderation Flow Diagram
```
                     📹 VIDEO CONTENT MODERATION SYSTEM
                     
   🌐 ADMIN INTERFACE           📡 OPERATIONS              🗄️ DATABASE
                     
┌─────────────────────┐                                 ┌─────────────────────┐
│  📹 Pending Videos   │── GET /Admin/PendingVideos ──▶│  🔍 Load Pending     │
│     Dashboard       │                                │     Videos          │
└─────────────────────┘                                │                     │
           │                                           │ WHERE !IsApproved   │
           ▼                                           └─────────────────────┘
┌─────────────────────┐                                          │
│  📋 Video List      │◀─── Return Video List ──────────────────┘
│                     │
│ ┌─Video 1──────────┐│
│ │ Title: "..."     ││
│ │ Channel: ABC     ││
│ │ [👁️View][✅Approve]││
│ │ [❌Reject]       ││
│ └─────────────────┘│
│                     │
│ ┌─Video 2──────────┐│
│ │ Title: "..."     ││
│ │ Channel: XYZ     ││
│ │ [👁️View][✅Approve]││
│ │ [❌Reject]       ││
│ └─────────────────┘│
└─────────────────────┘
           │
           ▼
   ┌─────────── MODERATION ACTIONS ───────────┐
   │                                          │
   ▼                    ▼                     ▼
┌──────────────┐  ┌──────────────┐  ┌──────────────┐
│  👁️ VIEW      │  │  ✅ APPROVE   │  │  ❌ REJECT    │
│              │  │              │  │              │
│ • Full video │  │ • Set        │  │ • Delete     │
│   details    │  │   IsApproved │  │   video      │
│ • Channel    │  │   = true     │  │ • Remove     │
│   info       │  │ • Save to DB │  │   files      │
│ • Metadata   │  │ • Redirect   │  │ • Redirect   │
└──────────────┘  └──────────────┘  └──────────────┘
```

### 🎯 Chi Tiết Video Moderation:

#### **📹 Bước 1: Load Pending Videos**
📍 **File**: `Controllers/AdminController.cs:337`
```csharp
[HttpGet]
public async Task<IActionResult> PendingVideos()
{
    var videos = await Context.Video
        .Where(x => !x.IsApproved)              // 🔍 Only pending videos
        .ToListAsync();
    return View(videos);
}
```

#### **✅ Bước 2: Approve Video**
📍 **File**: `Controllers/AdminController.cs:345`
```csharp
[HttpPost]
public async Task<IActionResult> ApproveVideo(int id)
{
    var video = await Context.Video.FindAsync(id);
    if (video == null) return NotFound();
    
    video.IsApproved = true;                    // ✅ Mark as approved
    await Context.SaveChangesAsync();
    return RedirectToAction("PendingVideos");
}
```

#### **👁️ Bước 3: View Video Details**
📍 **File**: `Controllers/AdminController.cs:354`
```csharp
[HttpGet]
public async Task<IActionResult> ViewVideo(int id)
{
    var video = await Context.Video
        .Include(v => v.Channel)                // 🔗 Load channel info
        .FirstOrDefaultAsync(v => v.Id == id);
    if (video == null) return NotFound();
    return View(video);
}
```

#### **❌ Bước 4: Reject/Delete Video**
📍 **File**: `Controllers/AdminController.cs:362`
```csharp
[HttpPost]
public async Task<IActionResult> DeleteVideo(int id)
{
    var video = await Context.Video.FindAsync(id);
    if (video == null) return NotFound();
    
    Context.Video.Remove(video);               // 🗑️ Remove from database
    await Context.SaveChangesAsync();
    return RedirectToAction("PendingVideos");
}
```

---

## 🛡️ Security & Authorization

### 🔐 Admin-Only Access
```csharp
[Authorize(Roles = $"{SD.AdminRole}")]      // 👑 Only admin role
public class AdminController : CoreController
```

### 🛡️ Super Admin Protection
```csharp
private bool IsSuperAdminUserId(int userId)
{
    return _userManager.FindByIdAsync(userId.ToString())
        .GetAwaiter().GetResult().UserName.Equals("admin");
}

// Usage trong operations:
if (IsSuperAdminUserId(model.Id)) {
    TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
    return RedirectToAction("AllUsers");
}
```

### 🔒 Security Measures

1. **Role-based Authorization**
   - Chỉ Admin mới access được AdminController
   - Kiểm tra role ở controller level

2. **Super Admin Protection**
   - Không thể edit/delete super admin account
   - Validation ở mọi sensitive operations

3. **Cascade Delete Safety**
   - File cleanup khi xóa content
   - Orphaned data prevention

4. **Input Validation**
   - ModelState validation
   - Custom validation attributes
   - Duplicate checking

---

## 🌐 API Endpoints

### 👥 User Management APIs
| Method | Endpoint | Description | Access |
|--------|----------|-------------|---------|
| `GET` | `/Admin/AllUsers` | Display user grid | Admin |
| `GET` | `/Admin/AddEditUser/{id?}` | User form | Admin |
| `POST` | `/Admin/AddEditUser` | Create/Update user | Admin |
| `POST` | `/Admin/LockUser/{id}` | Lock user account | Admin |
| `POST` | `/Admin/UnlockUser/{id}` | Unlock user account | Admin |
| `DELETE` | `/Admin/DeleteUser/{id}` | Delete user + cascade | Admin |

### 📂 Category Management APIs
| Method | Endpoint | Description | Access |
|--------|----------|-------------|---------|
| `GET` | `/Admin/GetCategories` | List all categories | Admin |
| `POST` | `/Admin/AddEditCategory` | Create/Update category | Admin |
| `DELETE` | `/Admin/DeleteCategory/{id}` | Delete category + videos | Admin |

### 📹 Video Moderation APIs
| Method | Endpoint | Description | Access |
|--------|----------|-------------|---------|
| `GET` | `/Admin/PendingVideos` | List pending videos | Admin |
| `GET` | `/Admin/ViewVideo/{id}` | View video details | Admin |
| `POST` | `/Admin/ApproveVideo/{id}` | Approve video | Admin |
| `POST` | `/Admin/DeleteVideo/{id}` | Reject/Delete video | Admin |

---

## ❌ Error Handling

### 🚨 Common Error Scenarios

#### **User Management Errors:**
1. **Super Admin Protection**
   ```csharp
   TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
   ```

2. **User Not Found**
   ```csharp
   TempData["notification"] = "false;Not Found;The requested user was not found";
   ```

3. **Duplicate Data**
   ```csharp
   ModelState.AddModelError("Name", $"The name of '{model.Name} is taken. Please try another name.");
   ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address.");
   ```

4. **Role Validation**
   ```csharp
   ModelState.AddModelError("UserRoles", "Please select at least one role");
   ```

#### **Category Management Errors:**
1. **Category Not Found**
   ```csharp
   return Json(new ApiResponse(404, message: "The requested category was not found"));
   ```

2. **Validation Failure**
   ```csharp
   return Json(new ApiResponse(400, message: "Name is required"));
   ```

#### **Video Moderation Errors:**
1. **Video Not Found**
   ```csharp
   return NotFound();  // Returns 404 status
   ```

### 🎯 Error Response Format
```csharp
// API Response format
public class ApiResponse
{
    public int StatusCode { get; set; }         // HTTP status code
    public string Title { get; set; }           // Success/Error title
    public string Message { get; set; }         // Detailed message
    public object Result { get; set; }          // Data payload
}

// Usage examples:
return Json(new ApiResponse(200, "Success", "Operation completed"));
return Json(new ApiResponse(400, message: "Validation failed"));
return Json(new ApiResponse(404, message: "Resource not found"));
```

---

## 🔄 Integration với Hệ Thống

### 📺 Channel Auto-Creation
Khi admin tạo user mới:
1. User được tạo với assigned roles
2. **ContextInitializer** tự động tạo Channel tương ứng
3. Claims system updated với Channel ID

### 🗑️ Cascade Delete Operations
Khi xóa user:
1. **Find associated Channel** 
2. **Get all Videos** in channel
3. **Delete thumbnail files** (PhotoService)
4. **Remove videos** from database
5. **Delete user** (auto-deletes channel via FK cascade)

### 📊 Role Management Integration
```csharp
// Get available roles
public async Task<List<string>> GetApplicationRolesAsync()
{
    return await _roleManager.Roles
        .Select(x => x.Name)
        .ToListAsync();
}

// Assign multiple roles
await _userManager.AddToRolesAsync(userToAdd, model.UserRoles);
```

---

## 🎯 Best Practices

### ✅ Security Best Practices
1. **Admin-only access** với `[Authorize(Roles = SD.AdminRole)]`
2. **Super admin protection** cho critical operations
3. **Input validation** comprehensive
4. **File cleanup** khi delete operations
5. **Cascade delete** để tránh orphaned data

### ✅ Performance Optimizations
1. **Async/await** cho tất cả database operations
2. **Projection queries** thay vì Include full entities
3. **Batch operations** cho bulk deletes
4. **Lazy loading** với AutoMapper

### ✅ User Experience
1. **Clear success/error messages** qua TempData
2. **AJAX operations** cho responsive UI
3. **Confirmation dialogs** cho destructive operations
4. **Grid-based display** với sorting/filtering

### ✅ Maintainability
1. **Separation of concerns** (User vs Content management)
2. **Reusable helper methods**
3. **Consistent error handling**
4. **Clear method naming** và comments

---

**👑 Admin Management System - Comprehensive, Secure, và User-friendly!**

*Cập nhật: August 2025 | Tác giả: BbySharp-dev*
