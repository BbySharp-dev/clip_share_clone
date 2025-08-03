using ClipShare.Core.Entities;
using ClipShare.DataAccess.Data;
using ClipShare.Utility;
using ClipShare.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

// ===================== TOÀN BỘ CONTROLLER NÀY DO USER 6 PHỤ TRÁCH =====================
// Quản lý tài khoản: đăng nhập, đăng ký, đăng xuất, kiểm tra quyền truy cập

namespace ClipShare.Controllers
{
    /// <summary>
    /// Controller quản lý tài khoản người dùng - không yêu cầu authentication
    /// Bao gồm đăng nhập, đăng ký, đăng xuất và xử lý access denied
    /// </summary>
    public class AccountController : Controller
    {
        // Dependency injection - Các service cần thiết cho quản lý tài khoản
        private readonly UserManager<AppUser> _userManager; // Service quản lý user từ ASP.NET Identity
        private readonly SignInManager<AppUser> _signInManager; // Service quản lý đăng nhập/đăng xuất
        private readonly Context _context; // Database context để truy cập data trực tiếp

        /// <summary>
        /// Constructor - Khởi tạo AccountController với các service cần thiết
        /// </summary>
        /// <param name="userManager">Service quản lý user</param>
        /// <param name="signInManager">Service quản lý signin</param>
        /// <param name="context">Database context</param>
        public AccountController(UserManager<AppUser> userManager,
            SignInManager<AppUser> signInManager,
            Context context)
        {
            _userManager = userManager; // Gán service quản lý user
            _signInManager = signInManager; // Gán service quản lý đăng nhập
            _context = context; // Gán database context
        }

        // --- [USER 6] Trang đăng nhập (GET) ---
        /// <summary>
        /// Hiển thị form đăng nhập
        /// Luồng: Tạo ViewModel với returnUrl -> Trả về view đăng nhập
        /// </summary>
        /// <param name="returnUrl">URL để redirect sau khi đăng nhập thành công (optional)</param>
        /// <returns>View form đăng nhập</returns>
        [HttpGet]
        public IActionResult Login(string returnUrl = null)
        {
            var loginVM = new Login_vm()
            {
                ReturnUrl = returnUrl // Lưu URL để redirect sau khi login thành công
            };

            return View(loginVM); // Trả về view với ViewModel
        }

        // --- [USER 6] Xử lý đăng nhập (POST) ---
        /// <summary>
        /// Xử lý submit form đăng nhập
        /// Luồng: Validate -> Tìm user -> Kiểm tra password -> Xử lý đăng nhập -> Redirect
        /// </summary>
        /// <param name="model">Dữ liệu đăng nhập từ form</param>
        /// <returns>Redirect nếu thành công, trả về view với error nếu thất bại</returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF attack
        public async Task<IActionResult> Login(Login_vm model)
        {
            if (!ModelState.IsValid) // Kiểm tra validation cơ bản (Required fields, etc.)
            {
                return View(model); // Trả về form với error message
            }

            // Set default returnUrl nếu không có
            model.ReturnUrl = model.ReturnUrl ?? Url.Content("~/"); // "~/" = trang chủ

            // Tìm user theo username hoặc email (cho phép đăng nhập bằng cả 2)
            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
            {
                // Nếu không tìm thấy theo username, thử tìm theo email
                user = await _userManager.FindByEmailAsync(model.UserName);
            }

            if (user == null)
            {
                // User không tồn tại -> thông báo lỗi chung (không reveal thông tin cụ thể)
                ModelState.AddModelError(string.Empty, "Invalid username or password. Please try again.");
                return View(model);
            }

            // Kiểm tra password (không sử dụng SignInAsync để có control tốt hơn)
            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);

            if (result.Succeeded)
            {
                // ===== ĐĂNG NHẬP THÀNH CÔNG =====
                
                await HandleSignInUserAsync(user); // Xử lý đăng nhập và tạo claims
                return LocalRedirect(model.ReturnUrl); // Redirect về URL đã lưu hoặc trang chủ
            }
            else
            {
                // ===== ĐĂNG NHẬP THẤT BẠI =====
                
                if (result.IsLockedOut)
                {
                    // Tài khoản bị khóa -> hiển thị thời gian unlock
                    ModelState.AddModelError(string.Empty, $"Your account has been locked. You should wait until {user.LockoutEnd} (UTC time) to be able to login");
                }
                else
                {
                    // Sai password hoặc lý do khác -> thông báo chung
                    ModelState.AddModelError(string.Empty, "Invalid username or password. Please try again.");
                }

                return View(model); // Trả về form với error message
            }
        }

        // --- [USER 6] Trang đăng ký (GET) ---
        /// <summary>
        /// Hiển thị form đăng ký tài khoản mới
        /// </summary>
        /// <returns>View form đăng ký</returns>
        [HttpGet]
        public IActionResult Register()
        {
            return View(); // Trả về view đăng ký với ViewModel mặc định
        }

        // --- [USER 6] Xử lý đăng ký (POST) ---
        /// <summary>
        /// Xử lý submit form đăng ký tài khoản mới
        /// Luồng: Validate -> Kiểm tra trùng lặp -> Tạo user -> Gán role -> Đăng nhập tự động -> Redirect
        /// </summary>
        /// <param name="model">Dữ liệu đăng ký từ form</param>
        /// <returns>Redirect về Home nếu thành công, trả về view với error nếu thất bại</returns>
        [HttpPost]
        [ValidateAntiForgeryToken] // Bảo vệ chống CSRF attack
        public async Task<IActionResult> Register(Register_vm model)
        {
            if (ModelState.IsValid) // Kiểm tra validation cơ bản (Required, Email format, etc.)
            {
                // ===== VALIDATION LOGIC RIÊNG =====
                
                // Kiểm tra password confirmation match
                if (!model.Password.Equals(model.ConfirmPassword))
                {
                    ModelState.AddModelError("ConfirmPassword", "Confirm password does not match password.");
                    return View(model);
                }

                // Kiểm tra email đã tồn tại chưa
                if (await CheckEmailExistsAsync(model.Email))
                {
                    ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address");
                    return View(model);
                }

                // Kiểm tra tên user đã tồn tại chưa
                if (await CheckNameExistsAsync(model.Name))
                {
                    ModelState.AddModelError("Name", $"The name of '{model.Name}' is taken. Please try another name.");
                    return View(model);
                }

                // ===== TẠO USER MỚI =====
                
                var userToAdd = new AppUser
                {
                    Name = model.Name, // Tên hiển thị
                    UserName = model.Name.ToLower(), // Username (lowercase để consistency)
                    Email = model.Email.ToLower() // Email (lowercase để consistency)
                };

                // Tạo user với password trong Identity system
                var result = await _userManager.CreateAsync(userToAdd, model.Password);
                
                // Gán role mặc định cho user mới (UserRole = Member)
                await _userManager.AddToRoleAsync(userToAdd, SD.UserRole);

                if (!result.Succeeded)
                {
                    // Có lỗi từ Identity (password policy, etc.) -> hiển thị tất cả lỗi
                    foreach (var error in result.Errors)
                    {
                        ModelState.AddModelError(string.Empty, error.Description);
                    }
                    return View(model);
                }

                // ===== ĐĂNG NHẬP TỰ ĐỘNG SAU KHI ĐĂNG KÝ THÀNH CÔNG =====
                
                await HandleSignInUserAsync(userToAdd); // Tự động đăng nhập user vừa tạo
                return RedirectToAction("Index", "Home"); // Redirect về trang chủ
            }

            return View(model); // Nếu có validation error -> trả về form với error message
        }


        // --- [USER 6] Đăng xuất ---
        /// <summary>
        /// Đăng xuất user khỏi hệ thống
        /// Luồng: Clear session và cookies -> Redirect về trang chủ
        /// </summary>
        /// <returns>Redirect về trang chủ</returns>
        [HttpGet]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync(); // Xóa authentication cookie và session
            return RedirectToAction("Index", "Home"); // Redirect về trang chủ
        }

        // --- [USER 6] Truy cập bị từ chối ---
        /// <summary>
        /// Hiển thị trang Access Denied khi user không có quyền truy cập
        /// Được redirect từ Authorization filter khi user không đủ quyền
        /// </summary>
        /// <returns>View Access Denied</returns>
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View(); // Trả về view thông báo không có quyền truy cập
        }


        #region Private Methods

        // =====================  USER 6 CÓ THỂ THAM KHẢO =====================
        // Các hàm private/phụ trợ dùng chung cho controller
        
        /// <summary>
        /// Kiểm tra email đã tồn tại trong hệ thống chưa
        /// Dùng để validate khi đăng ký tài khoản mới
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <returns>true nếu email đã tồn tại, false nếu chưa</returns>
        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }

        /// <summary>
        /// Kiểm tra tên user đã tồn tại trong hệ thống chưa
        /// Dùng để validate khi đăng ký tài khoản mới
        /// </summary>
        /// <param name="name">Tên user cần kiểm tra</param>
        /// <returns>true nếu tên đã tồn tại, false nếu chưa</returns>
        private async Task<bool> CheckNameExistsAsync(string name)
        {
            return await _userManager.Users.AnyAsync(x => x.Name.ToLower() == name.ToLower());
        }

        /// <summary>
        /// Xử lý đăng nhập user với custom claims
        /// Luồng: Lấy channel ID -> Tạo claims list -> Lấy roles -> Đăng nhập với claims
        /// </summary>
        /// <param name="user">User cần đăng nhập</param>
        /// <returns>Task hoàn thành</returns>
        private async Task HandleSignInUserAsync(AppUser user)
        {
            // Lấy ID của channel thuộc về user này (nếu có)
            var userChannelId = await _context.Channel
              .Where(x => x.AppUserId == user.Id)
              .Select(x => x.Id)
              .FirstOrDefaultAsync();

            // Tạo danh sách claims cho user session
            var claims = new List<Claim>()
            {
                new Claim(ClaimTypes.Name, user.UserName), // Username
                new Claim(ClaimTypes.Email, user.Email), // Email
                new Claim(ClaimTypes.GivenName, user.Name), // Display name
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // User ID
                new Claim(ClaimTypes.Sid, userChannelId.ToString()), // Channel ID (Sid = Security Identifier)
            };

            // Lấy tất cả role của user và thêm vào claims
            var roles = await _userManager.GetRolesAsync(user);
            claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));

            // Đăng nhập user với custom claims (isPersistent: false = session cookie)
            await _signInManager.SignInWithClaimsAsync(user, isPersistent: false, claims);
        }
        #endregion
    }
}
