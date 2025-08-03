using ClipShare.Core.Entities;
using ClipShare.Core.IRepo;
using ClipShare.Extensions;
using ClipShare.Utility;
using ClipShare.ViewModels;
using ClipShare.ViewModels.Channel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ClipShare.Controllers
// ===================== TOÀN BỘ CONTROLLER NÀY DO USER 5 PHỤ TRÁCH =====================
// Quản lý kênh: xem, tạo, sửa, analytics kênh
{
    /// <summary>
    /// Controller quản lý channel - chỉ những user đã đăng nhập mới có thể truy cập
    /// Bao gồm tạo channel, chỉnh sửa thông tin channel, và xem analytics
    /// </summary>
    [Authorize(Roles = $"{SD.UserRole}")] // Chỉ user đã đăng nhập mới truy cập được
    public class ChannelController : CoreController
    {
        // --- [USER 5] Trang chủ kênh, hiển thị thông tin kênh ---
        /// <summary>
        /// Hiển thị trang chủ channel - có thể là form tạo mới (nếu chưa có channel) hoặc thông tin channel hiện tại
        /// Luồng: Kiểm tra session error -> Tìm channel của user -> Hiển thị form phù hợp
        /// </summary>
        /// <param name="stringModel">JSON string từ session chứa error state (optional)</param>
        /// <returns>View với form tạo channel hoặc thông tin channel hiện tại</returns>
        public async Task<IActionResult> Index(string stringModel)
        {
            var model = new ChannelAddEdit_vm(); // ViewModel cho form channel
            
            // Lấy error state từ session (nếu có) - dùng khi redirect sau validation fail
            stringModel = HttpContext.Session.GetString("ChannelModelFromSession");

            if (!string.IsNullOrEmpty(stringModel))
            {
                // Deserialize ViewModel từ session để giữ lại data và error state
                model = JsonConvert.DeserializeObject<ChannelAddEdit_vm>(stringModel);
                
                if (model.Errors.Count > 0)
                {
                    // Thêm error từ session vào ModelState để hiển thị trên form
                    foreach (var error in model.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.ErrorMessage);
                    }

                    // Xóa session sau khi đã sử dụng
                    HttpContext.Session.Remove("ChannelModelFromSession");

                    return View(model); // Trả về form với error state
                }
            }

            // Tìm channel hiện tại của user (bao gồm thông tin subscribers)
            var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.AppUserId == User.GetUserId(), includeProperties: "Subscribers");

            if (channel != null)
            {
                // ===== USER ĐÃ CÓ CHANNEL =====
                // Load thông tin channel vào ViewModel để hiển thị
                model.Name = channel.Name;
                model.About = channel.About;
                model.SubscribersCount = channel.Subscribers.Count(); // Đếm số subscriber
            }
            // Nếu channel == null thì user chưa có channel -> hiển thị form tạo mới

            return View(model); // Trả về view với ViewModel đã chuẩn bị
        }

        // --- [USER 5] Tạo kênh mới ---
        /// <summary>
        /// Xử lý tạo channel mới cho user
        /// Luồng: Validate form -> Kiểm tra tên trùng lặp -> Tạo channel -> Save -> Redirect
        /// </summary>
        /// <param name="model">Dữ liệu channel từ form</param>
        /// <returns>Redirect về Index với notification</returns>
        [HttpPost]
        public async Task<IActionResult> CreateChannel(ChannelAddEdit_vm model)
        {
            if (!ModelState.IsValid)
            {
                // ===== XỬ LÝ VALIDATION ERROR =====
                
                // Chuyển ModelState error thành custom error format
                foreach (var item in ModelState)
                {
                    if (item.Value.Errors.Count > 0)
                    {
                        model.Errors.Add(new ModelError_vm
                        {
                            Key = item.Key, // Field name có lỗi
                            ErrorMessage = item.Value.Errors.Select(x => x.ErrorMessage).FirstOrDefault() // Message lỗi đầu tiên
                        });
                    }
                }

                // Lưu ViewModel vào session để giữ lại state khi redirect
                HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));

                return RedirectToAction("Index"); // Redirect về Index để hiển thị error
            }

            // Kiểm tra tên channel đã tồn tại chưa (case-insensitive)
            var channelNameExists = await UnitOfWork.ChannelRepo.AnyAsync(x => x.Name.ToLower() == model.Name.ToLower());
            if (channelNameExists)
            {
                // Thêm error về tên trùng lặp
                model.Errors.Add(new ModelError_vm
                {
                    Key = "Name",
                    ErrorMessage = $"Channel name of {model.Name} is taken. Please try other name"
                });

                // Lưu vào session và redirect để hiển thị error
                HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));
                return RedirectToAction("Index");
            }

            // ===== TẠO CHANNEL MỚI =====
            
            var channelToAdd = new Channel
            {
                AppUserId = User.GetUserId(), // Gán channel cho user hiện tại
                Name = model.Name, // Tên channel
                About = model.About, // Mô tả channel
            };

            UnitOfWork.ChannelRepo.Add(channelToAdd); // Thêm vào repository
            await UnitOfWork.CompleteAsync(); // Save vào database

            // Set notification thành công
            TempData["notification"] = "true;Channel Created;Your channel has been created and you can upload clips now";

            return RedirectToAction("Index"); // Redirect về trang chủ channel
        }

        // --- [USER 5] Sửa thông tin kênh ---
        /// <summary>
        /// Chỉnh sửa thông tin channel hiện có
        /// Luồng: Validate form -> Tìm channel của user -> Cập nhật thông tin -> Save -> Redirect
        /// </summary>
        /// <param name="model">Dữ liệu channel mới từ form</param>
        /// <returns>Redirect về Index với notification</returns>
        [HttpPost]
        public async Task<IActionResult> EditChannel(ChannelAddEdit_vm model)
        {
            if (ModelState.IsValid) // Kiểm tra validation cơ bản
            {
                // Tìm channel hiện tại của user
                var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.AppUserId == User.GetUserId());
                if (channel != null)
                {
                    // ===== CẬP NHẬT THÔNG TIN CHANNEL =====
                    
                    channel.Name = model.Name; // Cập nhật tên
                    channel.About = model.About; // Cập nhật mô tả
                    await UnitOfWork.CompleteAsync(); // Lưu thay đổi vào database

                    TempData["notification"] = "true;Channel updated;Your channel is updated";
                    return RedirectToAction("Index"); // Quay về trang chủ channel
                }
            }

            // Channel không tồn tại hoặc validation fail
            TempData["notification"] = "false;Not Found;Your channel was not found";
            return RedirectToAction("Index");
        }





        // --- [USER 5] Thống kê/analytics kênh ---
        /// <summary>
        /// Hiển thị trang analytics với các thống kê về channel
        /// Luồng: Tìm channel -> Tính toán thống kê -> Chuẩn bị data cho chart -> Trả về view
        /// </summary>
        /// <returns>View analytics với các số liệu thống kê</returns>
        [HttpGet]
        public async Task<IActionResult> Analytics()
        {
            var userId = User.GetUserId(); // Lấy ID user hiện tại
            
            // Lấy channel kèm thông tin videos, subscribers và viewers
            var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.AppUserId == userId, includeProperties: "Videos,Subscribers");
            if (channel == null)
            {
                // User chưa có channel
                TempData["notification"] = "false;Not Found;Your channel was not found";
                return RedirectToAction("Index");
            }

            // ===== TÍNH TOÁN CÁC THỐNG KÊ TỔNG QUAN =====
            
            // Tổng số video đã upload
            var totalVideos = channel.Videos?.Count() ?? 0;
            
            // Tổng lượt xem (tính từ tất cả video của channel)
            var totalViews = channel.Videos?.SelectMany(v => v.Viewers ?? new List<VideoView>()).Count() ?? 0;
            
            // Tổng số subscriber
            var totalSubscribers = channel.Subscribers?.Count() ?? 0;

            // ===== PHÂN TÍCH TOP VIDEO =====
            
            // Top 5 video có nhiều lượt xem nhất
            var topVideos = (channel.Videos ?? new List<Video>())
                .OrderByDescending(v => (v.Viewers?.Count() ?? 0)) // Sắp xếp theo lượt xem giảm dần
                .Take(5) // Lấy 5 video đầu
                .Select(v => new { v.Title, Views = v.Viewers?.Count() ?? 0 }) // Chỉ lấy Title và Views
                .ToList();

            // ===== CHUẨN BỊ DỮ LIỆU CHO BIỂU ĐỒ =====
            
            // Dữ liệu cho Chart.js (hoặc thư viện chart khác)
            var chartLabels = topVideos.Select(v => v.Title).ToArray(); // Tên video cho trục X
            var chartData = topVideos.Select(v => v.Views).ToArray(); // Số lượt xem cho trục Y

            // ===== TRUYỀN DỮ LIỆU QUA VIEWBAG =====
            
            ViewBag.TotalVideos = totalVideos; // Tổng video
            ViewBag.TotalViews = totalViews; // Tổng view
            ViewBag.TotalSubscribers = totalSubscribers; // Tổng subscriber
            ViewBag.ChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(chartLabels); // JSON cho chart labels
            ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData); // JSON cho chart data

            return View(); // Trả về view analytics
        }
    }
}
