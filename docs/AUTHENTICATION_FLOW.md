# 🔐 Authentication Flow - Login & Register System

## 📋 Mục Lục
- [Tổng Quan](#tổng-quan)
- [Cấu Trúc Files](#cấu-trúc-files)
- [Luồng Login](#luồng-login)
- [Luồng Register](#luồng-register)
- [Luồng Logout](#luồng-logout)
- [Claims Management](#claims-management)
- [Security Features](#security-features)
- [Error Handling](#error-handling)

---

## 🎯 Tổng Quan

**ClipShare Authentication System** sử dụng **ASP.NET Core Identity** với **Cookie Authentication** để quản lý người dùng. Hệ thống hỗ trợ đăng nhập bằng username hoặc email, đăng ký account mới với validation phức tạp, và quản lý claims để authorization.

### 🔑 Tính Năng Chính
- ✅ **Dual Login**: Username hoặc Email
- ✅ **Strong Validation**: Client & Server side
- ✅ **Claims-based Authorization**
- ✅ **Role Management** (Admin, Moderator, User)
- ✅ **Channel Integration** (Automatic channel creation)
- ✅ **Security Features** (Anti-forgery, lockout protection)

---

## 📁 Cấu Trúc Files

```
ClipShare/
├── Controllers/
│   └── AccountController.cs           # 🎯 MAIN: Authentication logic
├── ViewModels/Account/
│   ├── Login_vm.cs                   # 📝 Login form model
│   └── Register_vm.cs                # 📝 Register form model
├── Views/Account/
│   ├── Login.cshtml                  # 🎨 Login UI
│   ├── Register.cshtml               # 🎨 Register UI
│   └── AccessDenied.cshtml           # 🚫 Access denied page
├── Extensions/
│   └── UserClaimsExtensions.cs       # 🔧 Claims helper methods
└── Core/Entities/
    └── AppUser.cs                    # 👤 User domain model
```

---

## 🔄 Luồng Login

### 📊 Login Flow Diagram
```
                                    🔐 CLIPSHARE LOGIN AUTHENTICATION FLOW
                                    
   🌐 CLIENT SIDE                           📡 HTTP REQUEST                        🖥️ SERVER SIDE
                                    
┌─────────────────────┐                                                    ┌─────────────────────┐
│    👤 User truy cập  │────────────── GET /Account/Login ──────────────▶│  AccountController  │
│    Login Page       │                                                    │     Login(GET)      │
└─────────────────────┘                                                    └─────────────────────┘
           │                                                                          │
           │                                                                          ▼
           │                                                               ┌─────────────────────┐
           │◀────────── Trả về Login.cshtml + Login_vm ────────────────────│   Tạo Login_vm      │
           │                                                               │   với ReturnUrl     │
           ▼                                                               └─────────────────────┘
┌─────────────────────┐
│  📝 Login Form      │
│  ┌─Username/Email─┐ │
│  │               │ │
│  └───────────────┘ │
│  ┌─Password──────┐ │
│  │               │ │
│  └───────────────┘ │
│  [🔑 Login Button] │
└─────────────────────┘
           │
           │ User nhập thông tin và submit
           │
           ▼
┌─────────────────────┐                                                    ┌─────────────────────┐
│  📤 Form Submit     │────────────── POST /Account/Login ─────────────▶│  AccountController  │
│  + CSRF Token      │                 với Login_vm                      │     Login(POST)     │
└─────────────────────┘                                                    └─────────────────────┘
                                                                                     │
                                                                                     ▼
                                                                          ┌─────────────────────┐
                                                                          │  ✅ ModelState      │
                                                                          │    Validation       │
                                                                          │  - Required fields  │
                                                                          │  - Data annotations │
                                                                          └─────────────────────┘
                                                                                     │
                                                ┌─────────────────────────────────────┴─────────────────────────────────────┐
                                                ▼                                                                         ▼
                                    ┌─────────────────────┐                                                  ┌─────────────────────┐
                                    │   ❌ Validation      │──── Return View(model) ────────────────────▶│  📤 Trả về form     │
                                    │     Failed          │     với error messages                       │  với lỗi validation │
                                    └─────────────────────┘                                              └─────────────────────┘
                                                                                                                   │
                                    ┌─────────────────────┐                                                      │
                                    │   ✅ Validation      │                                                      │
                                    │     Success         │                                                      │
                                    └─────────────────────┘                                                      │
                                               │                                                                  │
                                               ▼                                                                  │
                                    ┌─────────────────────┐                                                      │
                                    │  🔍 DUAL USER       │                                                      │
                                    │     LOOKUP          │                                                      │
                                    │                     │                                                      │
                                    │  1️⃣ FindByNameAsync  │                                                      │
                                    │     (Username)      │                                                      │
                                    │         │           │                                                      │
                                    │         ▼           │                                                      │
                                    │  2️⃣ FindByEmailAsync │                                                      │
                                    │     (Email fallback)│                                                      │
                                    └─────────────────────┘                                                      │
                                               │                                                                  │
                        ┌─────────────────────┼─────────────────────┐                                          │
                        ▼                     │                     ▼                                          │
              ┌─────────────────────┐         │           ┌─────────────────────┐                              │
              │   👤 User Found     │         │           │   ❌ User Not Found  │──── Error Message ─────────┘
              │                     │         │           │                     │     "Invalid credentials"
              └─────────────────────┘         │           └─────────────────────┘
                         │                    │
                         ▼                    │
              ┌─────────────────────┐         │
              │  🔒 PASSWORD        │         │
              │    VERIFICATION     │         │
              │                     │         │
              │ CheckPasswordSignIn │         │
              │    Async()          │         │
              └─────────────────────┘         │
                         │                    │
        ┌────────────────┼────────────────┐   │
        ▼                │                ▼   │
┌─────────────────┐      │      ┌─────────────────┐
│  ❌ Password     │      │      │  🔒 Account     │
│   Failed        │      │      │    Locked       │
└─────────────────┘      │      └─────────────────┘
        │                │                │
        │                │                │
        └────────────────┼────────────────┘
                         │
                Error Messages
                         │
                         │
              ┌─────────────────────┐
              │  ✅ Password        │
              │    Success          │
              └─────────────────────┘
                         │
                         ▼
              ┌─────────────────────┐
              │  🎫 CLAIMS          │
              │   CREATION          │
              │                     │
              │ 1️⃣ Lấy Channel ID    │
              │ 2️⃣ Tạo Claims List   │
              │   - Name            │
              │   - Email           │
              │   - UserId          │
              │   - ChannelId       │
              │ 3️⃣ Lấy Roles         │
              │ 4️⃣ SignInWithClaims  │
              └─────────────────────┘
                         │
                         ▼
              ┌─────────────────────┐                   ┌─────────────────────┐
              │  🍪 COOKIE          │────── Success ────▶│  🏠 REDIRECT        │
              │   AUTHENTICATION    │                   │                     │
              │                     │                   │  LocalRedirect()    │
              │ - 24h expiration    │                   │  ▪️ ReturnUrl        │
              │ - Claims stored     │                   │  ▪️ /Home/Index      │
              │ - HttpOnly secure   │                   │                     │
              └─────────────────────┘                   └─────────────────────┘
```

### 🎯 Mô Tả Chi Tiết Từng Bước:

#### **🔄 PHASE 1: Request Initiation (Khởi tạo yêu cầu)**
1. **User Navigation**: Người dùng truy cập `/Account/Login` hoặc được redirect khi cần authentication
2. **ReturnUrl Preservation**: Hệ thống lưu URL hiện tại để redirect về sau khi login thành công
3. **Form Rendering**: Server trả về form login với CSRF token và validation scripts

#### **📝 PHASE 2: User Input & Client Validation (Nhập liệu & Validation phía client)**
1. **Form Fields**: Username/Email và Password fields với placeholder và labels
2. **Client Validation**: jQuery validation kiểm tra required fields ngay lập tức
3. **UX Features**: Auto-lowercase cho username, real-time validation feedback

#### **📡 PHASE 3: Form Submission (Gửi form)**
1. **CSRF Protection**: `[ValidateAntiForgeryToken]` kiểm tra token để chống Cross-Site Request Forgery
2. **Model Binding**: ASP.NET Core tự động bind form data vào `Login_vm` object
3. **Server Validation**: Kiểm tra `ModelState.IsValid` với Data Annotations

#### **🔍 PHASE 4: User Lookup (Tìm kiếm người dùng)**
1. **Primary Lookup**: `UserManager.FindByNameAsync()` tìm theo username trước
2. **Fallback Lookup**: Nếu không tìm thấy, `FindByEmailAsync()` tìm theo email
3. **Dual Support**: Cho phép user login bằng username hoặc email

#### **🔒 PHASE 5: Password Verification (Xác thực mật khẩu)**
1. **Secure Check**: `SignInManager.CheckPasswordSignInAsync()` so sánh password hash
2. **Lockout Check**: Kiểm tra account có bị khóa không
3. **Failed Attempts**: Tracking số lần đăng nhập sai (nếu enable lockout)

#### **🎫 PHASE 6: Claims Creation (Tạo Claims)**
1. **Channel Lookup**: Query database lấy Channel ID của user
2. **Claims Building**: Tạo danh sách claims với user information
3. **Role Assignment**: Lấy roles từ database và add vào claims
4. **Security Context**: Tạo authentication context cho request tiếp theo

#### **🍪 PHASE 7: Authentication & Redirect (Xác thực & Chuyển hướng)**
1. **Cookie Creation**: `SignInWithClaimsAsync()` tạo authentication cookie
2. **Session Setup**: Thiết lập session với 24h expiration
3. **Redirect Logic**: `LocalRedirect()` về ReturnUrl hoặc homepage

### 🎯 Chi Tiết Từng Bước Code:

#### **Bước 1: GET /Account/Login - Hiển thị Form Login**
📍 **File**: `Controllers/AccountController.cs:40`
```csharp
[HttpGet]
public IActionResult Login(string returnUrl = null)
{
    var loginVM = new Login_vm()
    {
        ReturnUrl = returnUrl  // Lưu URL để redirect sau login
    };
    return View(loginVM);
}
```
**Mục đích:** 
- Hiển thị form login với returnUrl để redirect về trang trước đó
- Khởi tạo ViewModel với ReturnUrl để preserve navigation state
- Render HTML form với validation scripts và CSRF protection

**Flow chi tiết:**
1. User truy cập `/Account/Login` hoặc được redirect khi chưa authenticate
2. Controller nhận returnUrl parameter từ query string
3. Tạo Login_vm object với ReturnUrl được preserve
4. Return view với model để render HTML form
5. Client nhận HTML với form fields, validation scripts, và anti-forgery token

#### **Bước 2: User Interface**
📍 **File**: `Views/Account/Login.cshtml`
```html
@model ClipShare.ViewModels.Account.Login_vm

<form method="post">
    <input hidden type="text" asp-for="ReturnUrl" />
    
    <!-- Username/Email Input -->
    <div class="form-floating mt-5 mb-3">
        <input asp-for="UserName" placeholder="Username or Email" class="form-control" />
        <label asp-for="UserName"></label>
        <span asp-validation-for="UserName" class="text-danger"></span>
    </div>
    
    <!-- Password Input -->
    <div class="form-floating mb-3">
        <input asp-for="Password" type="password" placeholder="Password" class="form-control" />
        <label asp-for="Password"></label>
        <span asp-validation-for="Password" class="text-danger"></span>
    </div>
    
    <button class="btn btn-lg btn-info" type="submit">Login</button>
</form>
```

#### **Bước 3: ViewModel Validation**
📍 **File**: `ViewModels/Account/Login_vm.cs`
```csharp
public class Login_vm
{
    private string _username;

    [Display(Name = "Username or Email")]
    [Required(ErrorMessage = "Username is required")]
    public string UserName {
        get => _username; 
        set => _username = !string.IsNullOrEmpty(value) ? value.ToLower() : null; // 🔥 Auto lowercase
    }
    
    [Required(ErrorMessage = "Password is required")]
    public string Password { get; set; }
    
    public string ReturnUrl { get; set; }
}
```
**Tính năng đặc biệt:**
- ✅ Auto convert username to lowercase
- ✅ Required validation
- ✅ Custom error messages

#### **Bước 4: POST /Account/Login Processing**
📍 **File**: `Controllers/AccountController.cs:48`
```csharp
[HttpPost]
[ValidateAntiForgeryToken]  // 🛡️ CSRF Protection
public async Task<IActionResult> Login(Login_vm model)
{
    if (!ModelState.IsValid) {
        return View(model);  // Return với validation errors
    }

    model.ReturnUrl = model.ReturnUrl ?? Url.Content("~/");

    // 🔍 Bước 4.1: Dual User Lookup
    var user = await _userManager.FindByNameAsync(model.UserName);
    if (user == null) {
        user = await _userManager.FindByEmailAsync(model.UserName); // Fallback to email
    }

    if (user == null) {
        ModelState.AddModelError(string.Empty, "Invalid username or password. Please try again.");
        return View(model);
    }

    // 🔒 Bước 4.2: Password Verification
    var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

    if (result.Succeeded) {
        // 🎯 Bước 4.3: Claims Creation & Sign In
        await HandleSignInUserAsync(user);
        return LocalRedirect(model.ReturnUrl);
    }
    else {
        // 🚨 Handle lockout và failed attempts
        if (result.IsLockedOut) {
            ModelState.AddModelError(string.Empty, 
                $"Your account has been locked. You should wait until {user.LockoutEnd} (UTC time) to be able to login");
        }
        else {
            ModelState.AddModelError(string.Empty, "Invalid username or password. Please try again.");
        }
        return View(model);
    }
}
```

#### **Bước 5: Claims Creation & Sign In**
📍 **File**: `Controllers/AccountController.cs:170`
```csharp
private async Task HandleSignInUserAsync(AppUser user)
{
    // 🔗 Lấy Channel ID của user (important cho authorization)
    var userChannelId = await _context.Channel
        .Where(x => x.AppUserId == user.Id)
        .Select(x => x.Id)
        .FirstOrDefaultAsync();

    // 🎫 Tạo Claims cho user
    var claims = new List<Claim>()
    {
        new Claim(ClaimTypes.Name, user.UserName),           // Username
        new Claim(ClaimTypes.Email, user.Email),             // Email
        new Claim(ClaimTypes.GivenName, user.Name),          // Display Name
        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // User ID
        new Claim(ClaimTypes.Sid, userChannelId.ToString()), // Channel ID
    };

    // 🎭 Thêm Role Claims
    var roles = await _userManager.GetRolesAsync(user);
    claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

    // 🍪 Sign in với Claims
    await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);
}
```

**Flow chi tiết Claims Creation:**
1. **Channel Lookup**: Query database để lấy Channel ID của user (cần cho authorization later)
2. **Claims Building**: Tạo danh sách claims với thông tin user cần thiết
   - `ClaimTypes.Name`: Username cho hiển thị
   - `ClaimTypes.Email`: Email cho liên lạc
   - `ClaimTypes.GivenName`: Display name (họ tên thật)
   - `ClaimTypes.NameIdentifier`: User ID (primary key)
   - `ClaimTypes.Sid`: Channel ID (cho authorization)
3. **Role Integration**: Lấy tất cả roles của user từ database và add vào claims
4. **Authentication Cookie**: Tạo secure HTTP-only cookie với 24h expiration
5. **Session Establishment**: Thiết lập authentication context cho subsequent requests

**Security Features:**
- Claims được encrypted trong cookie
- HTTP-only flag ngăn JavaScript access
- Secure flag yêu cầu HTTPS
- SameSite protection chống CSRF
```

---

## 📝 Luồng Register

### 📊 Register Flow Diagram
```
User Input → Client Validation → Server Validation → Duplicate Check → User Creation → Role Assignment → Auto Login
    ↓              ↓                    ↓                 ↓               ↓              ↓              ↓
Register.cshtml → Register_vm → ModelState.IsValid → CheckExists → UserManager → AddToRole → HandleSignIn
```

### 🎯 Chi Tiết Từng Bước:

#### **Bước 1: GET /Account/Register**
📍 **File**: `Controllers/AccountController.cs:82`
```csharp
[HttpGet]
public IActionResult Register()
{
    return View();  // Empty form
}
```

#### **Bước 2: Register Form UI**
📍 **File**: `Views/Account/Register.cshtml`
```html
@model ClipShare.ViewModels.Account.Register_vm

<form method="post">
    <!-- Name Input -->
    <div class="form-floating mt-5 mb-3">
        <input asp-for="Name" placeholder="Name (Username)" class="form-control" />
        <label asp-for="Name"></label>
        <span asp-validation-for="Name" class="text-danger"></span>
    </div>
    
    <!-- Email Input -->
    <div class="form-floating mb-3">
        <input asp-for="Email" placeholder="Email" class="form-control" />
        <label asp-for="Email"></label>
        <span asp-validation-for="Email" class="text-danger"></span>
    </div>
    
    <!-- Password Input -->
    <div class="form-floating mb-3">
        <input asp-for="Password" type="password" placeholder="Password" class="form-control" />
        <label asp-for="Password"></label>
        <span asp-validation-for="Password" class="text-danger"></span>
    </div>
    
    <!-- Confirm Password Input -->
    <div class="form-floating mb-3">
        <input asp-for="ConfirmPassword" type="password" placeholder="Confirm Password" class="form-control" />
        <label asp-for="ConfirmPassword"></label>
        <span asp-validation-for="ConfirmPassword" class="text-danger"></span>
    </div>
    
    <button class="btn btn-lg btn-warning" type="submit">Register</button>
</form>
```

#### **Bước 3: Register ViewModel với Complex Validation**
📍 **File**: `ViewModels/Account/Register_vm.cs`
```csharp
public class Register_vm
{
    [Required(ErrorMessage = "Email is required")]
    [RegularExpression("^\\w+@[a-zA-Z_]+?\\.[a-zA-Z]{2,3}$", ErrorMessage = "Invalid email address")]
    public string Email { get; set; }

    [Display(Name = "Name (Username)")]
    [Required(ErrorMessage = "Name (Username) is required")]
    [StringLength(15, MinimumLength = 3, ErrorMessage = "Name must be at least {2}, and maximum {1} characters")]
    [RegularExpression("^[a-zA-Z0-9_.-]*$", ErrorMessage = "Name must contain only a-z A-Z 0-9 characters")]
    public string Name { get; set; }

    [Required(ErrorMessage = "Password is required")]
    [RegularExpression("^(?=.*[0-9]+.*)(?=.*[a-zA-Z]+.*)[0-9a-zA-Z]{6,15}$", 
        ErrorMessage = "Password must contain at least one letter, at least one number, and be between 6-15 characters in length with no special characters.")]
    public string Password { get; set; }

    [Required(ErrorMessage = "Confirm password is required")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; }
}
```
**Validation Rules:**
- ✅ **Email**: Valid email format
- ✅ **Name**: 3-15 characters, alphanumeric + special chars
- ✅ **Password**: 6-15 chars, must have letters + numbers
- ✅ **Confirm Password**: Required field

#### **Bước 4: POST /Account/Register Processing**
📍 **File**: `Controllers/AccountController.cs:87`
```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> Register(Register_vm model)
{
    if (ModelState.IsValid)
    {
        // 🔍 Bước 4.1: Password Confirmation Check
        if (!model.Password.Equals(model.ConfirmPassword))
        {
            ModelState.AddModelError("ConfirmPassword", "Confirm password does not match password.");
            return View(model);
        }

        // 🔍 Bước 4.2: Email Duplicate Check
        if (await CheckEmailExistsAsync(model.Email))
        {
            ModelState.AddModelError("Email", "Email address is already taken.");
            return View(model);
        }

        // 🔍 Bước 4.3: Username Duplicate Check  
        if (await CheckUsernameExistsAsync(model.Name))
        {
            ModelState.AddModelError("Name", "Name (Username) is already taken.");
            return View(model);
        }

        // 👤 Bước 4.4: User Creation
        var user = new AppUser()
        {
            UserName = model.Name.ToLower(),  // Lowercase để consistency
            Email = model.Email.ToLower(),    // Lowercase để consistency  
            Name = model.Name,                // Original case cho display
        };

        var result = await _userManager.CreateAsync(user, model.Password);

        if (result.Succeeded)
        {
            // 🎭 Bước 4.5: Role Assignment
            await _userManager.AddToRoleAsync(user, SD.UserRole);

            // 🔄 Bước 4.6: Auto Login sau khi register
            await HandleSignInUserAsync(user);

            return RedirectToAction("Index", "Home");
        }
        else
        {
            // 🚨 Bước 4.7: Handle Creation Errors
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
        }
    }

    return View(model);
}
```

**Flow chi tiết Register Processing:**
1. **Password Confirmation**: So sánh Password và ConfirmPassword fields
2. **Email Uniqueness**: Query database kiểm tra email đã tồn tại chưa
3. **Username Uniqueness**: Query database kiểm tra username đã tồn tại chưa
4. **User Entity Creation**: Tạo AppUser object với data được normalize (lowercase)
5. **Password Hashing**: UserManager tự động hash password trước khi lưu database
6. **Role Assignment**: Gán role "User" cho account mới tạo
7. **Auto Authentication**: Tự động đăng nhập user sau khi register thành công
8. **Error Handling**: Display validation errors nếu có lỗi trong quá trình tạo user

**Security Measures:**
- Password được hash với ASP.NET Core Identity
- Email và username được normalize về lowercase
- CSRF protection với ValidateAntiForgeryToken
- Comprehensive validation cả client và server side
        {
            ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address");
            return View(model);
        }

        // 🔍 Bước 4.3: Username Duplicate Check
        if (await CheckNameExistsAsync(model.Name))
        {
            ModelState.AddModelError("Name", $"The name of '{model.Name}' is taken. Please try another name.");
            return View(model);
        }

        // 👤 Bước 4.4: User Creation
        var userToAdd = new AppUser
        {
            Name = model.Name,
            UserName = model.Name.ToLower(),    // Username = Name (lowercase)
            Email = model.Email.ToLower()       // Email lowercase
        };

        var result = await _userManager.CreateAsync(userToAdd, model.Password);
        
        // 🎭 Bước 4.5: Assign Default Role
        await _userManager.AddToRoleAsync(userToAdd, SD.UserRole);

        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            return View(model);
        }

        // 🔑 Bước 4.6: Auto Login sau register
        await HandleSignInUserAsync(userToAdd);
        return RedirectToAction("Index", "Home");
    }

    return View(model);
}
```

#### **Bước 5: Duplicate Check Methods**
📍 **File**: `Controllers/AccountController.cs:155`
```csharp
private async Task<bool> CheckEmailExistsAsync(string email)
{
    return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
}

private async Task<bool> CheckNameExistsAsync(string name)
{
    return await _userManager.Users.AnyAsync(x => x.Name.ToLower() == name.ToLower());
}
```

---

## 🚪 Luồng Logout

### 📊 Logout Flow
```
User Click Logout → SignOutAsync → Clear Cookies → Redirect Home
       ↓               ↓             ↓              ↓
   Logout Link → AccountController → Identity → Homepage
```

#### **Logout Implementation**
📍 **File**: `Controllers/AccountController.cs:140`
```csharp
[HttpGet]
public async Task<IActionResult> Logout()
{
    await _signInManager.SignOutAsync();  // 🍪 Clear authentication cookies
    return RedirectToAction("Index", "Home");
}
```

---

## 🎫 Claims Management

### 📊 Claims Structure
📍 **File**: `Extensions/UserClaimsExtensions.cs`
```csharp
public static class UserClaimsExtensions
{
    // 🏷️ Get Username (login identifier)
    public static string GetUserName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Name)?.Value;
    }

    // 📧 Get Email Address
    public static string GetEmail(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.Email)?.Value;
    }

    // 👤 Get Display Name
    public static string GetName(this ClaimsPrincipal user)
    {
        return user.FindFirst(ClaimTypes.GivenName)?.Value;
    }

    // 🆔 Get User ID (Primary Key)
    public static int GetUserId(this ClaimsPrincipal user)
    {
        return int.Parse(user.FindFirst(ClaimTypes.NameIdentifier)?.Value);
    }

    // 📺 Get User's Channel ID (Important for authorization)
    public static int GetUserChannelId(this ClaimsPrincipal user)
    {
        return int.Parse(user.FindFirst(ClaimTypes.Sid)?.Value);
    }
}
```

### 🎯 Claims Usage trong Controllers
```csharp
// Sử dụng trong bất kỳ controller nào
public class VideoController : CoreController
{
    public async Task<IActionResult> CreateVideo()
    {
        int userId = User.GetUserId();           // Lấy User ID
        int channelId = User.GetUserChannelId(); // Lấy Channel ID
        string displayName = User.GetName();     // Lấy tên hiển thị
        
        // Business logic với user info
    }
}
```

---

## 🛡️ Security Features

### 🔐 Authentication Configuration
📍 **File**: `Extensions/WebApplicationBuilderExtensions.cs`
```csharp
public static WebApplicationBuilder AddAuthenticationServices(this WebApplicationBuilder builder)
{
    // 🔑 Identity Configuration
    builder.Services.AddIdentity<AppUser, AppRole>(options =>
    {
        // Password Policy
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

    // 🍪 Cookie Authentication
    builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
        .AddCookie(options =>
        {
            options.ExpireTimeSpan = TimeSpan.FromHours(24);      // 24h session
            options.LoginPath = "/Account/Login";                 // Login redirect
            options.AccessDeniedPath = "/Account/AccessDenied";   // Access denied page
        });

    return builder;
}
```

### 🛡️ Security Measures

1. **CSRF Protection**
   ```csharp
   [ValidateAntiForgeryToken]  // All POST actions có CSRF protection
   ```

2. **Password Policy**
   - Minimum 6 characters
   - Must contain digits
   - No special character requirements

3. **Account Lockout** (có thể enable)
   ```csharp
   if (result.IsLockedOut) {
       // Handle lockout scenario
   }
   ```

4. **Input Validation**
   - Client-side validation (jQuery Validation)
   - Server-side validation (Data Annotations)
   - Duplicate check for email/username

---

## ❌ Error Handling

### 🚨 Error Scenarios & Messages

#### **Login Errors:**
1. **Invalid Credentials**
   ```csharp
   ModelState.AddModelError(string.Empty, "Invalid username or password. Please try again.");
   ```

2. **Account Locked**
   ```csharp
   ModelState.AddModelError(string.Empty, 
       $"Your account has been locked. You should wait until {user.LockoutEnd} (UTC time) to be able to login");
   ```

#### **Registration Errors:**
1. **Password Mismatch**
   ```csharp
   ModelState.AddModelError("ConfirmPassword", "Confirm password does not match password.");
   ```

2. **Duplicate Email**
   ```csharp
   ModelState.AddModelError("Email", 
       $"Email address of {model.Email} is taken. Please try using another email address");
   ```

3. **Duplicate Username**
   ```csharp
   ModelState.AddModelError("Name", 
       $"The name of '{model.Name}' is taken. Please try another name.");
   ```

4. **Identity Errors** (từ UserManager)
   ```csharp
   foreach (var error in result.Errors)
   {
       ModelState.AddModelError(string.Empty, error.Description);
   }
   ```

### 🎯 Error Display
📍 **File**: `Views/Account/Login.cshtml` & `Register.cshtml`
```html
<!-- Field-specific errors -->
<span asp-validation-for="UserName" class="text-danger"></span>

<!-- All validation errors -->
<div asp-validation-summary="All" class="text-danger"></div>
```

---

## 🔄 Integration với hệ thống

### 📺 Channel Auto-Creation
Khi user register, hệ thống tự động:
1. Tạo AppUser trong AspNetUsers table
2. Assign role "user" 
3. **ContextInitializer.cs** sẽ tạo Channel tương ứng (thông qua seeding process)
4. Claims sẽ include Channel ID để authorization

### 🎭 Role-based Authorization
```csharp
[Authorize(Roles = SD.UserRole)]        // Chỉ user thường
[Authorize(Roles = SD.AdminRole)]       // Chỉ admin
[Authorize(Roles = SD.ModeratorRole)]   // Chỉ moderator
```

### 🔗 Với Video System
```csharp
// Trong VideoController - ownership validation
var video = await Context.Video
    .Where(x => x.Id == id && x.Channel.AppUserId == User.GetUserId())
    .FirstOrDefaultAsync();
```

---

## 🎯 Best Practices

### ✅ Security Best Practices
1. **Always use HTTPS** trong production
2. **CSRF tokens** cho tất cả forms
3. **Input validation** client & server side
4. **Password hashing** qua Identity framework
5. **Claims-based authorization** thay vì session

### ✅ Performance Optimizations
1. **Async/await** cho tất cả database calls
2. **Specific queries** thay vì Include toàn bộ
3. **Claims caching** trong cookie
4. **Lowercase normalization** để tránh duplicate

### ✅ User Experience
1. **Dual login** (username/email)
2. **Clear error messages**
3. **Auto-login** sau register
4. **Return URL preservation**
5. **Client-side validation** for immediate feedback

---

**🔐 Authentication System - Secure, User-friendly, và Scalable!**

*Cập nhật: August 2025 | Tác giả: BbySharp-dev*
