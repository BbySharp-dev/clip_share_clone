using ClipShare.Core.Entities;
using ClipShare.Extensions;
using ClipShare.Utility;
using ClipShare.ViewModels;
using ClipShare.ViewModels.Member;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;

// ===================== TOÀN BỘ CONTROLLER NÀY DO USER 5 PHỤ TRÁCH =====================
// Quản lý thành viên: xem channel, subscribe/unsubscribe, API lấy video channel

namespace ClipShare.Controllers
{
    /// <summary>
    /// Controller quản lý chức năng của thành viên (Member role)
    /// Bao gồm xem thông tin channel, subscribe/unsubscribe, API lấy video
    /// Chỉ user có role Member mới truy cập được
    /// </summary>
    [Authorize(Roles = $"{SD.UserRole}")] // Chỉ cho phép user có role UserRole (Member) truy cập
    public class MemberController : CoreController // Kế thừa từ CoreController (có UnitOfWork và Context)
    {
        // --- [USER 5] Xem thông tin chi tiết channel ---
        /// <summary>
        /// Hiển thị trang chi tiết channel với thông tin cơ bản và trạng thái subscribe
        /// Luồng: Lấy channel theo ID -> Kiểm tra user đã subscribe chưa -> Trả về view hoặc redirect lỗi
        /// </summary>
        /// <param name="id">ID của channel cần xem</param>
        /// <returns>View chi tiết channel hoặc redirect về Home nếu không tìm thấy</returns>
        public async Task<IActionResult> Channel(int id)
        {
            // Lấy thông tin channel với projection để optimize performance
            var fetchedChannel = await Context.Channel
                .Where(x => x.Id == id) // Lọc theo ID channel
                .Select(x => new MemberChannel_vm // Sử dụng projection thay vì Include để giảm data load
                {
                    ChannelId = x.Id, // ID của channel
                    Name = x.Name, // Tên channel
                    About = x.About, // Mô tả về channel
                    CreatedAt = x.CreatedAt, // Ngày tạo channel
                    NumberOfAvailableVideos = x.Videos.Count(), // Đếm số video công khai của channel
                    NumberOfSubscribers = x.Subscribers.Count(), // Đếm số subscriber của channel
                    UserIsSubscribed = x.Subscribers.Any(s => s.AppUserId == User.GetUserId()), // Kiểm tra user hiện tại đã subscribe chưa
                }).FirstOrDefaultAsync(); // Lấy channel đầu tiên hoặc null nếu không tìm thấy

            if (fetchedChannel != null)
            {
                // Channel tồn tại -> trả về view với dữ liệu channel
                return View(fetchedChannel);
            }

            // Channel không tồn tại -> thông báo lỗi và redirect về trang chủ
            TempData["notification"] = "false;Not Found;Requested channel was not found";
            return RedirectToAction("Index", "Home");
        }

        // --- [USER 5] Subscribe/Unsubscribe channel ---
        /// <summary>
        /// Toggle subscribe/unsubscribe cho channel (POST request)
        /// Luồng: Tìm channel -> Lấy user ID -> Kiểm tra đã subscribe chưa -> Toggle trạng thái -> Lưu DB -> Redirect
        /// </summary>
        /// <param name="channelId">ID của channel cần subscribe/unsubscribe</param>
        /// <returns>Redirect về trang channel hoặc Home nếu có lỗi</returns>
        [HttpPost]
        public async Task<IActionResult> SubscribeChannel(int channelId)
        {
            // Lấy channel cùng với danh sách subscribers (sử dụng Include)
            var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.Id == channelId, "Subscribers");

            if (channel != null)
            {
                // Lấy ID của user hiện tại từ claims
                int userId = User.GetUserId();

                // Tìm record subscribe hiện tại (nếu có)
                var fetchedSubscribe = channel.Subscribers.Where(x => x.ChannelId == channelId && x.AppUserId == userId).FirstOrDefault();

                if (fetchedSubscribe == null)
                {
                    // User chưa subscribe -> thêm subscription mới
                    channel.Subscribers.Add(new Subscribe(userId, channelId));
                }
                else
                {
                    // User đã subscribe -> xóa subscription (unsubscribe)
                    channel.Subscribers.Remove(fetchedSubscribe);
                }

                // Lưu thay đổi vào database
                await UnitOfWork.CompleteAsync();
                
                // Redirect về trang channel để refresh UI
                return RedirectToAction("Channel", new { id = channelId });
            }

            // Channel không tồn tại -> thông báo lỗi và redirect về trang chủ
            TempData["notification"] = "false;Not Found;Requested channel was not found";
            return RedirectToAction("Index", "Home");
        }

        #region API Endpoints
        // ===================== CÁC API ENDPOINTS CHO FRONTEND =====================
        
        // --- [USER 5] API lấy danh sách video của channel ---
        /// <summary>
        /// API endpoint trả về danh sách video của một channel (JSON format)
        /// Dùng cho AJAX call từ frontend để load video dynamically
        /// Luồng: Lấy tất cả video của channel -> Projection cho performance -> Trả về JSON
        /// </summary>
        /// <param name="channelId">ID của channel cần lấy video</param>
        /// <returns>JSON response chứa danh sách video với thông tin cơ bản</returns>
        [HttpGet]
        public async Task<IActionResult> GetMemberChannelVideos(int channelId)
        {
            // Query tất cả video của channel với projection để tối ưu performance
            var channelVideos = await Context.Video
             .Where(x => x.ChannelId == channelId) // Lọc video theo channel ID
             .Select(x => new // Anonymous object chứa thông tin cần thiết
             {
                 x.Id, // ID video để tạo link
                 x.Title, // Tiêu đề video
                 x.ThumbnailUrl, // URL thumbnail
                 CreatedAtTimeAgo = SD.TimeAgo(x.CreatedAt), // Thời gian tạo dạng "X ago" 
                 x.CreatedAt, // Thời gian tạo gốc
                 NumberOfViews = x.Viewers.Count(), // Số lượt xem (đếm từ VideoView)
             })
             .ToListAsync(); // Execute query và lấy kết quả

            // Trả về JSON response với status 200 và data video
            return Json(new ApiResponse(200, result: channelVideos));
        }
        #endregion
    }
}
