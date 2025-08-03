using ClipShare.Core.Entities;
using ClipShare.Utility;
using ClipShare.ViewModels;
using ClipShare.ViewModels.Admin;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ClipShare.Controllers
{
    /// <summary>
    /// Controller quản lý admin - chỉ những user có role Admin mới có thể truy cập
    /// Bao gồm quản lý user, role, category và video
    /// </summary>
    [Authorize(Roles = $"{SD.AdminRole}")] // Chỉ user có role Admin mới truy cập được
    public class AdminController : CoreController
    {
        // Dependency injection - Quản lý user và role trong hệ thống Identity
        private readonly UserManager<AppUser> _userManager; // Service quản lý user (tạo, sửa, xóa, khóa user)
        private readonly RoleManager<AppRole> _roleManager; // Service quản lý role (Admin, Member, Moderator)

        /// <summary>
        /// Constructor - Khởi tạo AdminController với các service cần thiết
        /// </summary>
        /// <param name="userManager">Service quản lý user</param>
        /// <param name="roleManager">Service quản lý role</param>
        public AdminController(UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _userManager = userManager; // Gán service quản lý user
            _roleManager = roleManager; // Gán service quản lý role
        }
        // ===================== PHẦN 1: USER 3 PHỤ TRÁCH =====================
        // Quản lý user, role, khóa/mở user, xóa user, các chức năng liên quan đến người dùng

        // --- [USER 3] Quản lý danh sách user ---
        /// <summary>
        /// Hiển thị trang quản lý category - có thể là trang cũ hoặc redirect
        /// </summary>
        /// <returns>View Category</returns>
        public IActionResult Category()
        {
            return View(); // Trả về view Category
        }

        /// <summary>
        /// Lấy danh sách tất cả user trong hệ thống (trừ admin) và hiển thị thông tin chi tiết
        /// Luồng: Lấy user từ DB -> Map sang ViewModel -> Kiểm tra trạng thái khóa -> Lấy role của user
        /// </summary>
        /// <returns>View với danh sách user</returns>
        public async Task<IActionResult> AllUsers()
        {
            var toReturn = new List<UserDisplayGrid_vm>(); // Danh sách user để hiển thị trên grid
            
            // Lấy tất cả user từ database, bao gồm thông tin Channel, loại trừ admin
            var users = await _userManager.Users
                .Include(x => x.Channel) // Eager loading - lấy luôn thông tin Channel của user
                .Where(x => x.UserName != "admin") // Loại trừ user admin khỏi danh sách
                .ToListAsync();

            // Duyệt qua từng user để tạo ViewModel cho grid hiển thị
            foreach (var user in users)
            {
                var userDisplayToAdd = new UserDisplayGrid_vm(); // Tạo ViewModel mới cho mỗi user
                Mapper.Map(user, userDisplayToAdd); // Map từ Entity sang ViewModel (AutoMapper)
                
                // Kiểm tra user có bị khóa không (lockout)
                userDisplayToAdd.IsLocked = _userManager.IsLockedOutAsync(user).GetAwaiter().GetResult();
                
                // Lấy danh sách role được gán cho user này
                userDisplayToAdd.AssignedRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
                
                toReturn.Add(userDisplayToAdd); // Thêm vào danh sách trả về
            }

            return View(toReturn); // Trả về view với danh sách user
        }

        // --- [USER 3] Giao diện thêm/sửa user (GET) ---
        /// <summary>
        /// Hiển thị form thêm mới hoặc chỉnh sửa user
        /// Luồng: Kiểm tra id -> Nếu id > 0 thì load thông tin user để edit -> Lấy danh sách role -> Trả về view
        /// </summary>
        /// <param name="id">ID của user (0 = thêm mới, >0 = chỉnh sửa)</param>
        /// <returns>View form thêm/sửa user</returns>
        public async Task<IActionResult> AddEditUser(int id)
        {
            var toReturn = new UserAddEdit_vm(); // ViewModel cho form thêm/sửa user
            
            // Lấy danh sách tất cả role có trong hệ thống để hiển thị trong dropdown
            toReturn.ApplicationRoles = await GetApplicationRolesAsync();

            if (id > 0)
            {
                // Chế độ edit - id > 0 nghĩa là đang sửa user có sẵn
                var user = await _userManager.FindByIdAsync(id.ToString()); // Tìm user theo ID
                Mapper.Map(user, toReturn); // Map thông tin user vào ViewModel

                // Lấy danh sách role hiện tại của user để pre-select trong form
                var userRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
                toReturn.UserRoles = userRoles.ToList();
            }
            // Nếu id = 0 thì là chế độ thêm mới, không cần load gì thêm

            return View(toReturn); // Trả về view với ViewModel đã chuẩn bị
        }

        // --- [USER 3] Xử lý thêm/sửa user (POST) ---
        /// <summary>
        /// Xử lý submit form thêm/sửa user
        /// Luồng: Validate dữ liệu -> Kiểm tra trùng lặp -> Thêm mới hoặc cập nhật user -> Gán role
        /// </summary>
        /// <param name="model">Dữ liệu user từ form</param>
        /// <returns>Redirect về AllUsers nếu thành công, trả về view với error nếu thất bại</returns>
        [HttpPost]
        public async Task<IActionResult> AddEditUser(UserAddEdit_vm model)
        {
            if (ModelState.IsValid) // Kiểm tra validation cơ bản (Required, Email format, etc.)
            {
                bool proceed = true; // Flag để kiểm tra có thể tiến hành không

                if (model.Id == 0)
                {
                    // ===== CHỨC NĂNG THÊM MỚI USER =====
                    
                    // Kiểm tra password có được nhập không (bắt buộc khi tạo mới)
                    if (string.IsNullOrEmpty(model.Password))
                    {
                        proceed = false;
                        ModelState.AddModelError("Password", "Password is required");
                    }

                    // Kiểm tra phải chọn ít nhất 1 role
                    if (proceed && model.UserRoles.Count == 0)
                    {
                        proceed = false;
                        ModelState.AddModelError("UserRoles", "Please select at least one role");
                    }

                    // Kiểm tra tên user đã tồn tại chưa
                    if (proceed && CheckNameExistsAsync(model.Name).GetAwaiter().GetResult())
                    {
                        proceed = false;
                        ModelState.AddModelError("Name", $"The name of '{model.Name} is taken. Please try another name.");
                    }

                    // Kiểm tra email đã tồn tại chưa
                    if (proceed && CheckEmailExistsAsync(model.Email).GetAwaiter().GetResult())
                    {
                        proceed = false;
                        ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address.");
                    }

                    if (proceed)
                    {
                        // Tạo user mới với thông tin từ form
                        var userToAdd = new AppUser
                        {
                            Name = model.Name, // Tên hiển thị
                            UserName = model.Name.ToLower(), // Username (chuyển về lowercase)
                            Email = model.Email, // Email
                        };

                        // Tạo user với password trong Identity system
                        var result = await _userManager.CreateAsync(userToAdd, model.Password);
                        
                        // Gán role cho user mới tạo
                        await _userManager.AddToRolesAsync(userToAdd, model.UserRoles);

                        if (result.Succeeded)
                        {
                            return RedirectToAction("AllUsers"); // Thành công -> quay về danh sách user
                        }
                        else
                        {
                            // Thất bại -> hiển thị lỗi từ Identity
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                }
                else
                {
                    // ===== CHỨC NĂNG CHỈNH SỬA USER =====
                    
                    var user = await _userManager.FindByIdAsync(model.Id.ToString()); // Tìm user cần sửa

                    if (user == null)
                    {
                        TempData["notification"] = "false;Not Found;The requested user was not found";
                        return RedirectToAction("AllUsers");
                    }

                    // Không cho phép sửa super admin
                    if (IsSuperAdminUserId(model.Id))
                    {
                        TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                        return RedirectToAction("AllUsers");
                    }

                    // Kiểm tra phải chọn ít nhất 1 role
                    if (model.UserRoles.Count == 0)
                    {
                        proceed = false;
                        ModelState.AddModelError("UserRoles", "Please select at least one role");
                    }

                    // Kiểm tra tên mới có trùng với user khác không (nếu thay đổi tên)
                    if (proceed && !user.Name.Equals(model.Name))
                    {
                        if (CheckNameExistsAsync(model.Name).GetAwaiter().GetResult())
                        {
                            proceed = false;
                            ModelState.AddModelError("Name", $"The name of '{model.Name} is taken. Please try another name.");
                        }
                    }

                    // Kiểm tra email mới có trùng với user khác không (nếu thay đổi email)
                    if (proceed && !user.Email.Equals(model.Email))
                    {
                        if (CheckEmailExistsAsync(model.Email).GetAwaiter().GetResult())
                        {
                            proceed = false;
                            ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address.");
                        }
                    }

                    // Nếu có nhập password mới thì đổi password
                    if (proceed && !string.IsNullOrEmpty(model.Password))
                    {
                        // Xóa password cũ và thêm password mới
                        await _userManager.RemovePasswordAsync(user);
                        var result = await _userManager.AddPasswordAsync(user, model.Password);

                        if (!result.Succeeded)
                        {
                            proceed = false;
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }

                    if (proceed)
                    {
                        // Cập nhật thông tin user
                        user.Name = model.Name;
                        user.UserName = model.Name.ToLower();
                        user.Email = model.Email;

                        // Cập nhật role: xóa role cũ và thêm role mới
                        var userRoles = await _userManager.GetRolesAsync(user);
                        await _userManager.RemoveFromRolesAsync(user, userRoles); // Xóa tất cả role cũ

                        // Thêm role mới
                        foreach (var role in model.UserRoles)
                        {
                            var roleToAdd = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == role);
                            if (roleToAdd != null)
                            {
                                await _userManager.AddToRoleAsync(user, role);
                            }
                        }

                        return RedirectToAction("AllUsers"); // Thành công -> quay về danh sách user
                    }
                }
            }

            // Nếu có lỗi -> load lại form với dữ liệu và error message
            model.ApplicationRoles = await GetApplicationRolesAsync();
            return View(model);
        }

        // --- [USER 3] Khóa user ---
        /// <summary>
        /// Khóa user trong 5 ngày (không cho đăng nhập)
        /// Luồng: Tìm user -> Kiểm tra không phải super admin -> Set lockout time -> Redirect
        /// </summary>
        /// <param name="id">ID của user cần khóa</param>
        /// <returns>Redirect về AllUsers</returns>
        [HttpPost]
        public async Task<IActionResult> LockUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString()); // Tìm user theo ID

            if (user == null)
            {
                TempData["notification"] = "false;Not Found;The requested user was not found";
                return RedirectToAction("AllUsers");
            }

            // Không cho phép khóa super admin
            if (IsSuperAdminUserId(id))
            {
                TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                return RedirectToAction("AllUsers");
            }

            // Khóa user trong 5 ngày kể từ bây giờ
            user.LockoutEnabled = true; // Cho phép lockout trên user này
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddDays(5));

            if (!result.Succeeded)
            {
                TempData["notification"] = "false;Server Error;Server Error";
            }

            return RedirectToAction("AllUsers"); // Quay về danh sách user
        }

        // --- [USER 3] Mở khóa user ---
        /// <summary>
        /// Mở khóa user (cho phép đăng nhập trở lại)
        /// Luồng: Tìm user -> Kiểm tra không phải super admin -> Xóa lockout time -> Redirect
        /// </summary>
        /// <param name="id">ID của user cần mở khóa</param>
        /// <returns>Redirect về AllUsers</returns>
        [HttpPost]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString()); // Tìm user theo ID

            if (user == null)
            {
                TempData["notification"] = "false;Not Found;The requested user was not found";
                return RedirectToAction("AllUsers");
            }

            // Không cho phép thao tác với super admin
            if (IsSuperAdminUserId(id))
            {
                TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                return RedirectToAction("AllUsers");
            }

            // Xóa thời gian khóa (set về null = mở khóa)
            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                TempData["notification"] = "false;Server Error;Server Error";
            }

            return RedirectToAction("AllUsers"); // Quay về danh sách user
        }


        // --- [USER 3] Xóa user (API) ---
        /// <summary>
        /// Xóa user khỏi hệ thống (bao gồm cả channel và video của user đó)
        /// Luồng: Tìm user -> Kiểm tra không phải super admin -> Xóa video và thumbnail -> Xóa user -> Trả về JSON
        /// </summary>
        /// <param name="id">ID của user cần xóa</param>
        /// <returns>JSON response với status và message</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(int id)
        {
            // Tìm user cần xóa, bao gồm thông tin Channel
            var user = await _userManager.Users
                .Include(x => x.Channel) // Lấy luôn thông tin Channel của user
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                // Không cho phép xóa super admin
                if (IsSuperAdminUserId(id))
                {
                    return Json(new ApiResponse(400, message: "Super admin cannot be deleted"));
                }

                // Nếu user có channel thì cần xóa hết video của channel đó
                if (user.Channel != null)
                {
                    // Lấy danh sách video của channel này để xóa file thumbnail
                    var userChannelWithVideos = await Context.Channel
                        .Where(x => x.AppUserId == id)
                        .Select(x => new
                        {
                            Videos = x.Videos.Select(x => new
                            {
                                x.Id, // ID video để xóa khỏi DB
                                x.ThumbnailUrl // URL thumbnail để xóa file physical
                            })
                        }).FirstOrDefaultAsync();

                    // Xóa từng video và file thumbnail tương ứng
                    foreach (var video in userChannelWithVideos.Videos)
                    {
                        PhotoService.DeletePhotoLocally(video.ThumbnailUrl); // Xóa file thumbnail từ server
                        await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id); // Xóa video từ database
                        await UnitOfWork.CompleteAsync(); // Save changes
                    }
                }

                // Xóa user khỏi Identity system
                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["notification"] = $"true;Deleted;User of {user.Name} has been permanently removed";
                    return Json(new ApiResponse(200)); // Thành công
                }
                else
                {
                    // Lấy lỗi đầu tiên từ Identity
                    return Json(new ApiResponse(400, message: result.Errors.Select(x => x.Description).FirstOrDefault()));
                }
            }

            return Json(new ApiResponse(404, message: "The requested user was not found")); // User không tồn tại
        }

        #region API Endpoints
        // ===================== PHẦN 2: USER 4 PHỤ TRÁCH =====================
        // Quản lý category, video chờ duyệt, duyệt video, xóa video, xem video, các chức năng liên quan đến video & category

        // --- [USER 4] API lấy danh sách category ---
        /// <summary>
        /// API endpoint để lấy danh sách tất cả category trong hệ thống
        /// Được sử dụng cho AJAX call từ frontend để hiển thị trong grid/table
        /// </summary>
        /// <returns>JSON chứa danh sách category</returns>
        [HttpGet]
        public async Task<IActionResult> GetCategories()
        {
            var categories = await UnitOfWork.CategoryRepo.GetAllAsync(); // Lấy tất cả category từ database
            
            // Map từ Entity sang ViewModel để trả về client
            var toReturn = categories.Select(x => new CategoryAddEdit_vm
            {
                Id = x.Id, // ID category
                Name = x.Name, // Tên category
            });

            return Json(new ApiResponse(200, result: toReturn)); // Trả về JSON với status 200 và data
        }

        // --- [USER 4] Thêm/sửa category (API) ---
        /// <summary>
        /// API endpoint để thêm mới hoặc chỉnh sửa category
        /// Luồng: Validate input -> Kiểm tra ID (0=thêm mới, >0=sửa) -> Thực hiện operation -> Trả về JSON
        /// </summary>
        /// <param name="model">Dữ liệu category (ID=0 cho thêm mới, ID>0 cho chỉnh sửa)</param>
        /// <returns>JSON response với status và message</returns>
        [HttpPost]
        public async Task<IActionResult> AddEditCategory(CategoryAddEdit_vm model)
        {
            if (ModelState.IsValid) // Kiểm tra validation (Name required, etc.)
            {
                if (model.Id == 0)
                {
                    // ===== THÊM MỚI CATEGORY =====
                    UnitOfWork.CategoryRepo.Add(new Category() { Name = model.Name }); // Tạo entity mới
                    await UnitOfWork.CompleteAsync(); // Save vào database
                    return Json(new ApiResponse(201, "Created", "New Category was added")); // Status 201 = Created
                }
                else
                {
                    // ===== CHỈNH SỬA CATEGORY =====
                    var category = await UnitOfWork.CategoryRepo.GetByIdAsync(model.Id); // Tìm category cần sửa
                    if (category == null) return Json(new ApiResponse(404)); // Không tìm thấy

                    var oldName = category.Name; // Lưu tên cũ để hiển thị trong message
                    category.Name = model.Name; // Cập nhật tên mới
                    await UnitOfWork.CompleteAsync(); // Save changes
                    return Json(new ApiResponse(200, "Editted", $"Category of {oldName} has been renamed to {model.Name}"));
                }
            }

            return Json(new ApiResponse(400, message: "Name is required")); // Validation failed
        }

        // --- [USER 4] Xóa category (API) ---
        /// <summary>
        /// API endpoint để xóa category (bao gồm tất cả video thuộc category đó)
        /// Luồng: Tìm category -> Lấy video thuộc category -> Xóa file thumbnail -> Xóa video -> Xóa category
        /// </summary>
        /// <param name="id">ID của category cần xóa</param>
        /// <returns>JSON response với status và message</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await UnitOfWork.CategoryRepo.GetByIdAsync(id); // Tìm category theo ID
            if (category != null)
            {
                // Lấy danh sách video thuộc category này để xóa
                var categoryVideoIdsAndThumbnailUrls = await Context.Video
                    .Where(x => x.CategoryId == id) // Lọc video theo category
                    .Select(x => new
                    {
                        x.Id, // ID để xóa khỏi DB
                        x.ThumbnailUrl // URL để xóa file thumbnail
                    })
                    .ToListAsync();

                // Nếu có video thuộc category này thì xóa hết
                if (categoryVideoIdsAndThumbnailUrls.Any())
                {
                    foreach (var video in categoryVideoIdsAndThumbnailUrls)
                    {
                        PhotoService.DeletePhotoLocally(video.ThumbnailUrl); // Xóa file thumbnail
                        await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id); // Xóa video khỏi DB
                        await UnitOfWork.CompleteAsync(); // Save changes sau mỗi video
                    }
                }

                // Xóa category khỏi database
                UnitOfWork.CategoryRepo.Remove(category);
                await UnitOfWork.CompleteAsync();

                return Json(new ApiResponse(200, "Deleted", "Category of " + category.Name + " has been removed"));
            }

            return Json(new ApiResponse(404, message: "The requested category was not found")); // Category không tồn tại
        }

        #endregion



        // --- [USER 4] Xem danh sách video chờ duyệt ---
        /// <summary>
        /// Hiển thị danh sách tất cả video chưa được approve (IsApproved = false)
        /// Dành cho admin để xem xét và duyệt các video mới upload
        /// </summary>
        /// <returns>View với danh sách video chờ duyệt</returns>
        [HttpGet]
        public async Task<IActionResult> PendingVideos()
        {
            // Lấy tất cả video có IsApproved = false (chưa được duyệt)
            var videos = await Context.Video
                .Where(x => !x.IsApproved) // Lọc video chưa được approve
                .ToListAsync();
            return View(videos); // Trả về view với danh sách video
        }

        // --- [USER 4] Duyệt video ---
        /// <summary>
        /// Approve video (cho phép hiển thị công khai)
        /// Luồng: Tìm video -> Set IsApproved = true -> Save -> Redirect về danh sách
        /// </summary>
        /// <param name="id">ID của video cần duyệt</param>
        /// <returns>Redirect về PendingVideos</returns>
        [HttpPost]
        public async Task<IActionResult> ApproveVideo(int id)
        {
            var video = await Context.Video.FindAsync(id); // Tìm video theo ID
            if (video == null) return NotFound(); // Video không tồn tại
            
            video.IsApproved = true; // Đánh dấu video đã được duyệt
            await Context.SaveChangesAsync(); // Lưu thay đổi vào database
            return RedirectToAction("PendingVideos"); // Quay về danh sách video chờ duyệt
        }

        // --- [USER 4] Xem chi tiết video ---
        /// <summary>
        /// Xem thông tin chi tiết của một video (bao gồm thông tin channel)
        /// Dành cho admin để xem xét nội dung trước khi approve
        /// </summary>
        /// <param name="id">ID của video cần xem</param>
        /// <returns>View chi tiết video</returns>
        [HttpGet]
        public async Task<IActionResult> ViewVideo(int id)
        {
            // Lấy video kèm thông tin channel của video đó
            var video = await Context.Video
                .Include(v => v.Channel) // Eager loading - lấy luôn thông tin Channel
                .FirstOrDefaultAsync(v => v.Id == id);
            
            if (video == null) return NotFound(); // Video không tồn tại
            return View(video); // Trả về view với thông tin video
        }

        // --- [USER 4] Xóa video (POST) ---
        /// <summary>
        /// Xóa video khỏi hệ thống (thường dùng khi video vi phạm policy)
        /// Luồng: Tìm video -> Xóa khỏi DB -> Redirect về danh sách
        /// </summary>
        /// <param name="id">ID của video cần xóa</param>
        /// <returns>Redirect về PendingVideos</returns>
        [HttpPost]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var video = await Context.Video.FindAsync(id); // Tìm video theo ID
            if (video == null) return NotFound(); // Video không tồn tại
            
            Context.Video.Remove(video); // Đánh dấu xóa video
            await Context.SaveChangesAsync(); // Lưu thay đổi (video bị xóa khỏi DB)
            return RedirectToAction("PendingVideos"); // Quay về danh sách video chờ duyệt
        }

        // ===================== CẢ HAI USER 3 & 4 CÓ THỂ THAM KHẢO =====================
        // Các hàm private/phụ trợ dùng chung cho controller
        #region Private Methods
        
        /// <summary>
        /// Lấy danh sách tất cả role có trong hệ thống
        /// Sử dụng để populate dropdown/checkbox trong form thêm/sửa user
        /// </summary>
        /// <returns>List tên các role (Admin, Member, Moderator, etc.)</returns>
        public async Task<List<string>> GetApplicationRolesAsync()
        {
            return await _roleManager.Roles
                .Select(x => x.Name) // Chỉ lấy tên role
                .ToListAsync();
        }

        /// <summary>
        /// Kiểm tra email đã tồn tại trong hệ thống chưa
        /// Dùng để validate khi tạo mới hoặc sửa user
        /// </summary>
        /// <param name="email">Email cần kiểm tra</param>
        /// <returns>true nếu email đã tồn tại, false nếu chưa</returns>
        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }

        /// <summary>
        /// Kiểm tra tên user đã tồn tại trong hệ thống chưa
        /// Dùng để validate khi tạo mới hoặc sửa user
        /// </summary>
        /// <param name="name">Tên user cần kiểm tra</param>
        /// <returns>true nếu tên đã tồn tại, false nếu chưa</returns>
        private async Task<bool> CheckNameExistsAsync(string name)
        {
            return await _userManager.Users.AnyAsync(x => x.Name.ToLower() == name.ToLower());
        }

        /// <summary>
        /// Kiểm tra user có phải là super admin không
        /// Super admin không được phép sửa/xóa để đảm bảo luôn có ít nhất 1 admin trong hệ thống
        /// </summary>
        /// <param name="userId">ID của user cần kiểm tra</param>
        /// <returns>true nếu là super admin (username = "admin"), false nếu không</returns>
        private bool IsSuperAdminUserId(int userId)
        {
            return _userManager.FindByIdAsync(userId.ToString()).GetAwaiter().GetResult().UserName.Equals("admin");
        }
        #endregion
    }
}


