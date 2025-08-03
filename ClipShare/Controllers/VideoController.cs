using ClipShare.Core.DTOs;
using ClipShare.Core.Entities;
using ClipShare.Core.Pagination;
using ClipShare.Extensions;
using ClipShare.Services.IServices;
using ClipShare.Utility;
using ClipShare.ViewModels;
using ClipShare.ViewModels.Video;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace ClipShare.Controllers
{
    /// <summary>
    /// Controller quản lý video - chỉ những user đã đăng nhập mới có thể truy cập
    /// Bao gồm xem video, tạo/sửa video, upload file, comment, like/dislike, subscribe
    /// </summary>
    [Authorize(Roles = $"{SD.UserRole}")] // Chỉ user đã đăng nhập mới truy cập được (UserRole bao gồm tất cả role)
    public class VideoController : CoreController
    {
        // ===================== PHẦN 1: NGƯỜI 1 PHỤ TRÁCH =====================
        // Các chức năng: Xem video, tạo/sửa video, upload file, tải file, comment, lấy file video

        // --- [NGƯỜI 1] Xem video ---
        /// <summary>
        /// Hiển thị trang xem video với đầy đủ thông tin (comments, likes, views, channel info)
        /// Luồng: Lấy thông tin video -> Ghi nhận view -> Hiển thị trang watch
        /// </summary>
        /// <param name="id">ID của video cần xem</param>
        /// <returns>View trang xem video hoặc redirect về Home nếu không tìm thấy</returns>
        public async Task<IActionResult> Watch(int id)
        {
            // Cách không hiệu quả - lấy quá nhiều dữ liệu không cần thiết với Include
            // var toReturn = await GetVideoWatch_vmWithIncludeProperties(id);

            // Cách hiệu quả - chỉ lấy những cột cần thiết bằng Projection
            var toReturn = await GetVideoWatch_vmWithProjections(id);

            if (toReturn != null)
            {
                // Lấy IP address của user để tracking view (tránh spam view từ cùng 1 IP)
                var userIpAddress = Request.HttpContext.Connection.RemoteIpAddress.ToString();
                
                // Ghi nhận lượt xem video (có logic kiểm tra để tránh spam)
                await UnitOfWork.VideoViewRepo.HandleVideoViewAsync(User.GetUserId(), id, userIpAddress);
                await UnitOfWork.CompleteAsync();

                return View(toReturn); // Trả về view với ViewModel đã chuẩn bị
            }

            // Video không tồn tại -> thông báo lỗi và redirect về trang chủ
            TempData["notification"] = "false;Not Found;Requested video was not found";
            return RedirectToAction("Index", "Home");
        }

        // --- [NGƯỜI 1] Bình luận video ---
        /// <summary>
        /// Tạo comment mới cho video
        /// Luồng: Tìm video -> Thêm comment mới -> Save -> Redirect về trang watch
        /// </summary>
        /// <param name="model">ViewModel chứa nội dung comment và video ID</param>
        /// <returns>Redirect về trang Watch hoặc Home nếu lỗi</returns>
        [HttpPost]
        public async Task<IActionResult> CreateComment(Comment_vm model)
        {
            // Tìm video cần comment kèm theo danh sách comment hiện có
            var video = await UnitOfWork.VideoRepo.GetFirstOrDefaultAsync(x => x.Id == model.PostComment.VideoId, "Comments");
            if (video != null)
            {
                // Thêm comment mới vào video (User.GetUserId() lấy ID của user hiện tại)
                video.Comments.Add(new Comment(User.GetUserId(), model.PostComment.VideoId, model.PostComment.Content.Trim()));
                await UnitOfWork.CompleteAsync(); // Lưu vào database

                // Quay về trang xem video để thấy comment vừa tạo
                return RedirectToAction("Watch", new { id = model.PostComment.VideoId });
            }

            // Video không tồn tại -> thông báo lỗi
            TempData["notification"] = "false;Not Found;Requested video was not found";
            return RedirectToAction("Index", "Home");
        }

        // --- [NGƯỜI 1] Lấy file video để phát ---
        /// <summary>
        /// Trả về file video để browser có thể phát (streaming)
        /// Dùng cho HTML5 video player hoặc các video player khác
        /// </summary>
        /// <param name="videoId">ID của video cần lấy file</param>
        /// <returns>File video hoặc redirect nếu không tìm thấy</returns>
        public async Task<IActionResult> GetVideoFile(int videoId)
        {
            // Tìm file video trong database (lưu dưới dạng binary)
            var fetcehdVideoFile = await UnitOfWork.VideoFileRepo.GetFirstOrDefaultAsync(x => x.VideoId == videoId);
            if (fetcehdVideoFile != null)
            {
                // Trả về file với ContentType phù hợp để browser biết cách xử lý
                return File(fetcehdVideoFile.Contents, fetcehdVideoFile.ContentType);
            }

            TempData["notification"] = "false;Not Found;Requested video was not found";
            return RedirectToAction("Index", "Home");
        }

        // --- [NGƯỜI 1] Tải file video về máy ---
        /// <summary>
        /// Download video file về máy tính của user
        /// Khác với GetVideoFile (để phát), function này để download về máy
        /// </summary>
        /// <param name="videoId">ID của video cần tải</param>
        /// <returns>File download hoặc redirect nếu không tìm thấy</returns>
        public async Task<IActionResult> DownloadVideoFile(int videoId)
        {
            // Lấy video kèm thông tin VideoFile
            var fetchedVideo = await UnitOfWork.VideoRepo.GetFirstOrDefaultAsync(x => x.Id == videoId, "VideoFile");
            if (fetchedVideo != null)
            {
                // Tạo tên file download: TênVideo + Extension (vd: "MyVideo.mp4")
                string fileDownloadName = fetchedVideo.Title + fetchedVideo.VideoFile.Extension;
                
                // Trả về file với tên download tùy chỉnh
                return File(fetchedVideo.VideoFile.Contents, fetchedVideo.VideoFile.ContentType, fileDownloadName);
            }

            TempData["notification"] = "false;Not Found;Requested video was not found";
            return RedirectToAction("Index", "Home");
        }

        // --- [NGƯỜI 1] Giao diện tạo/sửa video (GET) ---
        /// <summary>
        /// Hiển thị form để tạo mới hoặc chỉnh sửa video
        /// Luồng: Kiểm tra user có channel -> Load thông tin video (nếu edit) -> Chuẩn bị form
        /// </summary>
        /// <param name="id">ID video (0 = tạo mới, >0 = chỉnh sửa)</param>
        /// <returns>View form upload/edit video</returns>
        public async Task<IActionResult> CreateEditVideo(int id)
        {
            // Kiểm tra user có channel chưa (bắt buộc phải có channel mới upload được video)
            if (!await UnitOfWork.ChannelRepo.AnyAsync(x => x.AppUserId == User.GetUserId()))
            {
                TempData["notfication"] = "false;Not Found;No channel associated with your account was found.";
                return RedirectToAction("Index", "Channel"); // Redirect để tạo channel trước
            }

            var toReturn = new VideoAddEdit_vm(); // ViewModel cho form upload/edit video
            
            // Chuẩn bị danh sách content type được phép cho client validation
            toReturn.ImageContentTypes = string.Join(",", AcceptableContentTypes("image")); // Các loại file ảnh được phép (jpg, png, etc.)
            toReturn.VideoContentTypes = string.Join(",", AcceptableContentTypes("video")); // Các loại file video được phép (mp4, avi, etc.)

            if (id > 0)
            {
                // ===== CHẾ ĐỘ CHỈNH SỬA VIDEO =====

                // Kiểm tra video có thuộc về user hiện tại không (security check)
                var userId = await UnitOfWork.VideoRepo.GetUserIdByVideoIdAsync(id);
                if (!userId.Equals(User.GetUserId()))
                {
                    TempData["notfication"] = "false;Not Found;Requested video was not found.";
                    return RedirectToAction("Index", "Channel");
                }

                // Lấy thông tin video để fill vào form
                var fetchedVideo = await UnitOfWork.VideoRepo.GetByIdAsync(id);
                if (fetchedVideo == null)
                {
                    TempData["notfication"] = "false;Not Found;Requested video was not found.";
                    return RedirectToAction("Index", "Channel");
                }

                // Map thông tin video vào ViewModel
                toReturn.Id = fetchedVideo.Id;
                toReturn.Title = fetchedVideo.Title;
                toReturn.Description = fetchedVideo.Description;
                toReturn.CategoryId = fetchedVideo.CategoryId;
                toReturn.ImageUrl = fetchedVideo.ThumbnailUrl; // Thumbnail hiện tại để preview
            }
            // Nếu id = 0 thì là tạo mới, không cần load thông tin

            // Load danh sách category cho dropdown
            toReturn.CategoryDropdown = await GetCategoryDropdownAsync();

            return View(toReturn); // Trả về form với ViewModel đã chuẩn bị
        }

        // --- [NGƯỜI 1] Xử lý tạo/sửa video (POST) ---
        /// <summary>
        /// Xử lý submit form tạo/sửa video (bao gồm upload file và validation)
        /// Luồng: Validate form -> Kiểm tra file upload -> Xử lý file -> Tạo/cập nhật video -> Save
        /// </summary>
        /// <param name="model">ViewModel chứa thông tin video và file upload</param>
        /// <returns>Redirect về Channel nếu thành công, trả về View với error nếu thất bại</returns>
        [HttpPost]
        public async Task<IActionResult> CreateEditVideo(VideoAddEdit_vm model)
        {
            if (ModelState.IsValid) // Kiểm tra validation cơ bản (Required fields, etc.)
            {
                bool proceed = true; // Flag để kiểm tra có thể tiến hành không

                if (model.Id == 0)
                {
                    // ===== VALIDATION CHO CHỨC NĂNG TẠO MỚI =====
                    
                    // Khi tạo mới bắt buộc phải có thumbnail
                    if (model.ImageUpload == null)
                    {
                        ModelState.AddModelError("ImageUpload", "Please upload thumbnail");
                        proceed = false;
                    }

                    // Khi tạo mới bắt buộc phải có file video
                    if (proceed && model.VideoUpload == null)
                    {
                        ModelState.AddModelError("VideoUpload", "Please upload your video");
                        proceed = false;
                    }
                }

                // ===== VALIDATION CHO FILE THUMBNAIL (nếu có upload) =====
                if (model.ImageUpload != null)
                {
                    // Kiểm tra content type của ảnh (chỉ cho phép jpg, png, etc.)
                    if (proceed && !IsAcceptableContentType("image", model.ImageUpload.ContentType))
                    {
                        ModelState.AddModelError("ImageUpload", string.Format("Invalid content type. It must be one of the following: {0}",
                            string.Join(", ", AcceptableContentTypes("image"))));
                        proceed = false;
                    }

                    // Kiểm tra kích thước file ảnh (không được quá config trong appsettings)
                    if (proceed && model.ImageUpload.Length > int.Parse(Configuration["FileUpload:ImageMaxSizeInMB"]) * SD.MB)
                    {
                        ModelState.AddModelError("ImageUpload", string.Format("The uploaded file should not exceed {0} MB",
                            int.Parse(Configuration["FileUpload:ImageMaxSizeInMB"])));
                        proceed = false;
                    }
                }

                // ===== VALIDATION CHO FILE VIDEO (nếu có upload) =====
                if (model.VideoUpload != null)
                {
                    // Kiểm tra content type của video (chỉ cho phép mp4, avi, etc.)
                    if (proceed && !IsAcceptableContentType("video", model.VideoUpload.ContentType))
                    {
                        ModelState.AddModelError("VideoUpload", string.Format("Invalid content type. It must be one of the following: {0}",
                            string.Join(", ", AcceptableContentTypes("video"))));
                        proceed = false;
                    }

                    // Kiểm tra kích thước file video (không được quá config trong appsettings)
                    if (proceed && model.VideoUpload.Length > int.Parse(Configuration["FileUpload:VideoMaxSizeInMB"]) * SD.MB)
                    {
                        ModelState.AddModelError("VideoUpload", string.Format("The uploaded file should not exceed {0} MB",
                            int.Parse(Configuration["FileUpload:VideoMaxSizeInMB"])));
                        proceed = false;
                    }
                }

                if (proceed)
                {
                    string title = ""; // Tiêu đề notification
                    string message = ""; // Nội dung notification

                    if (model.Id == 0)
                    {
                        // ===== CHỨC NĂNG TẠO MỚI VIDEO =====
                        
                        var videoToAdd = new Video()
                        {
                            Title = model.Title, // Tiêu đề video
                            Description = model.Description, // Mô tả video
                            VideoFile = new VideoFile // Tạo VideoFile entity để lưu file video
                            {
                                ContentType = model.VideoUpload.ContentType, // Loại file (video/mp4, etc.)
                                Contents = GetContentsAsync(model.VideoUpload).GetAwaiter().GetResult(), // Convert file thành byte array
                                Extension = SD.GetFileExtension(model.VideoUpload.ContentType) // Lấy extension từ content type
                            },
                            CategoryId = model.CategoryId, // Category được chọn
                            ChannelId = UnitOfWork.ChannelRepo.GetChannelIdByUserId(User.GetUserId()).GetAwaiter().GetResult(), // Channel của user hiện tại
                            ThumbnailUrl = PhotoService.UploadPhotoLocally(model.ImageUpload) // Upload thumbnail và lưu URL
                        };

                        UnitOfWork.VideoRepo.Add(videoToAdd); // Thêm video vào database

                        title = "Created";
                        message = "New video has been created";
                    }
                    else
                    {
                        // ===== CHỨC NĂNG CHỈNH SỬA VIDEO =====
                        
                        var fetchedVideo = await UnitOfWork.VideoRepo.GetByIdAsync(model.Id); // Lấy video cần sửa
                        if (fetchedVideo == null)
                        {
                            TempData["notification"] = "false;Not Found;Requested video was not found";
                            return RedirectToAction("Index", "Channel");
                        }

                        // Cập nhật thông tin cơ bản
                        fetchedVideo.Title = model.Title;
                        fetchedVideo.Description = model.Description;
                        fetchedVideo.CategoryId = model.CategoryId;

                        // Chỉ cập nhật thumbnail nếu user upload ảnh mới
                        if (model.ImageUpload != null)
                        {
                            // PhotoService sẽ xóa ảnh cũ và upload ảnh mới
                            fetchedVideo.ThumbnailUrl = PhotoService.UploadPhotoLocally(model.ImageUpload, fetchedVideo.ThumbnailUrl);
                        }

                        title = "Edited";
                        message = "Video has been updated";
                    }

                    // Set notification và save changes
                    TempData["notification"] = $"true;{title};{message}";
                    await UnitOfWork.CompleteAsync(); // Lưu tất cả thay đổi vào database

                    return RedirectToAction("Index", "Channel"); // Quay về trang quản lý channel
                }
            }

            // Nếu có lỗi validation -> load lại form với dữ liệu và error message
            model.CategoryDropdown = await GetCategoryDropdownAsync();
            return View(model);
        }

        // ===================== PHẦN 2: NGƯỜI 2 PHỤ TRÁCH =====================
        // Các chức năng: API lấy danh sách video, xóa video, like/dislike, subscribe channel

        #region API Endpoints
        // --- [NGƯỜI 2] API lấy danh sách video cho channel (dùng cho grid) ---
        /// <summary>
        /// API endpoint để lấy danh sách video của channel hiện tại (cho pagination grid)
        /// Được gọi từ AJAX để hiển thị video trong trang quản lý channel
        /// </summary>
        /// <param name="parameters">Tham số phân trang (page, size, search, etc.)</param>
        /// <returns>JSON chứa danh sách video với pagination info</returns>
        [HttpGet]
        public async Task<IActionResult> GetVideosForChannelGrid(BaseParameters parameters)
        {
            // Lấy ID channel của user hiện tại
            var userChannelId = await UnitOfWork.ChannelRepo.GetChannelIdByUserId(User.GetUserId());
            
            // Lấy danh sách video với pagination (chỉ video của channel này)
            var videosForGrid = await UnitOfWork.VideoRepo.GetVideosForChannelGridAsync(userChannelId, parameters);
            
            // Wrap kết quả trong PaginatedResult để frontend biết thông tin phân trang
            var paginatedResults = new PaginatedResult<VideoGridChannelDto>(videosForGrid, videosForGrid.TotalItemsCount,
                videosForGrid.PageNumber, videosForGrid.PageSize, videosForGrid.TotalPages);

            return Json(new ApiResponse(200, result: paginatedResults)); // Trả về JSON
        }

        // --- [NGƯỜI 2] Xóa video (API) ---
        /// <summary>
        /// API endpoint để xóa video (chỉ chủ video mới xóa được)
        /// Luồng: Kiểm tra ownership -> Xóa file thumbnail -> Xóa video khỏi DB -> Trả về JSON
        /// </summary>
        /// <param name="id">ID của video cần xóa</param>
        /// <returns>JSON response với status và message</returns>
        [HttpDelete]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            // Lấy thông tin video (chỉ những field cần thiết) và đảm bảo video thuộc về user hiện tại
            var video = await Context.Video
                .Where(x => x.Id == id && x.Channel.AppUserId == User.GetUserId()) // Security check
                .Select(x => new
                {
                    x.Id, // ID để xóa
                    x.ThumbnailUrl, // URL để xóa file thumbnail
                    x.Title // Tên để hiển thị trong message
                }).FirstOrDefaultAsync();

            if (video != null)
            {
                PhotoService.DeletePhotoLocally(video.ThumbnailUrl); // Xóa file thumbnail khỏi server
                await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id); // Xóa video và related data khỏi DB
                await UnitOfWork.CompleteAsync(); // Save changes

                return Json(new ApiResponse(200, "Deleted", "Your video of " + video.Title + " has been deleted"));
            }
            return Json(new ApiResponse(404, message: "The requested video was not found")); // Video không tồn tại hoặc không phải của user
        }


        // --- [NGƯỜI 2] Đăng ký/hủy đăng ký kênh (API) ---
        /// <summary>
        /// API endpoint để subscribe/unsubscribe channel
        /// Luồng: Tìm channel -> Kiểm tra đã subscribe chưa -> Subscribe hoặc Unsubscribe -> Trả về JSON
        /// </summary>
        /// <param name="channelId">ID của channel cần subscribe/unsubscribe</param>
        /// <returns>JSON response với trạng thái subscribe mới</returns>
        [HttpPut]
        public async Task<IActionResult> SubscribeChannel(int channelId)
        {
            // Lấy channel kèm danh sách subscriber hiện tại
            var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.Id == channelId, "Subscribers");

            if (channel != null)
            {
                int userId = User.GetUserId(); // ID của user hiện tại

                // Kiểm tra user đã subscribe channel này chưa
                var fetchedSubscribe = channel.Subscribers.Where(x => x.ChannelId == channelId && x.AppUserId == userId).FirstOrDefault();

                if (fetchedSubscribe == null)
                {
                    // ===== CHƯA SUBSCRIBE -> THỰC HIỆN SUBSCRIBE =====
                    channel.Subscribers.Add(new Subscribe(userId, channelId)); // Thêm subscription mới
                    await UnitOfWork.CompleteAsync();
                    return Json(new ApiResponse(200, "Subscribed", "Subscribed"));
                }
                else
                {
                    // ===== ĐÃ SUBSCRIBE -> THỰC HIỆN UNSUBSCRIBE =====
                    channel.Subscribers.Remove(fetchedSubscribe); // Xóa subscription
                    await UnitOfWork.CompleteAsync();
                    return Json(new ApiResponse(200, "Unsubscribed", "Unsubscribed"));
                }
            }

            return Json(new ApiResponse(404, message: "Channel was not found")); // Channel không tồn tại
        }

        // --- [NGƯỜI 2] Like/Dislike video (API) ---
        /// <summary>
        /// API endpoint để like/dislike video với logic phức tạp
        /// Luồng: Tìm video -> Kiểm tra trạng thái like/dislike hiện tại -> Cập nhật -> Trả về command cho frontend
        /// </summary>
        /// <param name="videoId">ID của video</param>
        /// <param name="action">Hành động: "like" hoặc "dislike"</param>
        /// <param name="like">Boolean value (không sử dụng trong logic, chỉ để compatibility)</param>
        /// <returns>JSON với command để frontend cập nhật UI</returns>
        [HttpPut]
        public async Task<IActionResult> LikeDislikeVideo(int videoId, string action, bool like)
        {
            // Lấy video kèm danh sách like/dislike hiện tại
            var video = await UnitOfWork.VideoRepo.GetFirstOrDefaultAsync(x => x.Id == videoId, "LikeDislikes");
            if (video != null)
            {
                int userId = User.GetUserId(); // ID của user hiện tại

                // Tìm record like/dislike của user này cho video này (nếu có)
                var fetchedLikeDislike = video.LikeDislikes.Where(x => x.VideoId == videoId && x.AppUserId == userId).FirstOrDefault();
                string clienCommand = ""; // Command trả về để frontend biết cách cập nhật UI

                if (action.Equals("like"))
                {
                    // ===== XỬ LÝ HÀNH ĐỘNG LIKE =====
                    
                    if (fetchedLikeDislike == null)
                    {
                        // User chưa like/dislike -> thêm mới với Liked = true
                        video.LikeDislikes.Add(new LikeDislike(userId, videoId, true));
                        await UnitOfWork.CompleteAsync();
                        clienCommand = "addLike"; // Frontend sẽ tăng số like lên 1
                        return Json(new ApiResponse(200, clienCommand));
                    }
                    else
                    {
                        // User đã có action trước đó -> cần xử lý logic
                        if (fetchedLikeDislike.Liked == false)
                        {
                            // User đã dislike trước đó, bây giờ chuyển sang like
                            fetchedLikeDislike.Liked = true;
                            clienCommand = "removeDislike-addLike"; // Frontend giảm dislike và tăng like
                        }
                        else
                        {
                            // User đã like trước đó, bây giờ bỏ like (không like cũng không dislike)
                            video.LikeDislikes.Remove(fetchedLikeDislike);
                            clienCommand = "removeLike"; // Frontend giảm like xuống 1
                        }

                        await UnitOfWork.CompleteAsync();
                        return Json(new ApiResponse(200, clienCommand));
                    }
                }
                else if (action.Equals("dislike"))
                {
                    // ===== XỬ LÝ HÀNH ĐỘNG DISLIKE =====
                    
                    if (fetchedLikeDislike == null)
                    {
                        // User chưa like/dislike -> thêm mới với Liked = false
                        video.LikeDislikes.Add(new LikeDislike(userId, videoId, false));
                        await UnitOfWork.CompleteAsync();
                        clienCommand = "addDislike"; // Frontend sẽ tăng số dislike lên 1
                        return Json(new ApiResponse(200, clienCommand));
                    }
                    else
                    {
                        // User đã có action trước đó -> cần xử lý logic
                        if (fetchedLikeDislike.Liked == true)
                        {
                            // User đã like trước đó, bây giờ chuyển sang dislike
                            fetchedLikeDislike.Liked = false;
                            clienCommand = "removeLike-addDislike"; // Frontend giảm like và tăng dislike
                        }
                        else
                        {
                            // User đã dislike trước đó, bây giờ bỏ dislike (không like cũng không dislike)
                            video.LikeDislikes.Remove(fetchedLikeDislike);
                            clienCommand = "removeDislike"; // Frontend giảm dislike xuống 1
                        }

                        await UnitOfWork.CompleteAsync();
                        return Json(new ApiResponse(200, clienCommand));
                    }
                }
                else
                {
                    // ===== ACTION KHÔNG HỢP LỆ =====
                    return Json(new ApiResponse(400, message: "Invalid action")); // Action phải là "like" hoặc "dislike"
                }
            }

            return Json(new ApiResponse(404, message: "Requested video was not found")); // Video không tồn tại
        }
        #endregion

        // ===================== CẢ HAI NGƯỜI CÓ THỂ THAM KHẢO =====================
        // Các hàm private/phụ trợ dùng chung cho controller
        #region Private Methods
        
        /// <summary>
        /// Tạo dropdown list cho category selection
        /// Sử dụng trong form tạo/sửa video
        /// </summary>
        /// <returns>IEnumerable SelectListItem cho dropdown</returns>
        public async Task<IEnumerable<SelectListItem>> GetCategoryDropdownAsync()
        {
            var allCategories = await UnitOfWork.CategoryRepo.GetAllAsync(); // Lấy tất cả category

            // Convert sang SelectListItem để sử dụng trong HTML dropdown
            return allCategories.Select(category => new SelectListItem()
            {
                Text = category.Name, // Text hiển thị
                Value = category.Id.ToString() // Value khi submit form
            });
        }

        /// <summary>
        /// Lấy danh sách content type được phép từ configuration
        /// </summary>
        /// <param name="type">"image" hoặc "video"</param>
        /// <returns>Array các content type được phép</returns>
        private string[] AcceptableContentTypes(string type)
        {
            if (type.Equals("image"))
            {
                // Lấy từ appsettings.json: FileUpload:ImageContentTypes
                return Configuration.GetSection("FileUpload:ImageContentTypes").Get<string[]>();
            }
            else
            {
                // Lấy từ appsettings.json: FileUpload:VideoContentTypes
                return Configuration.GetSection("FileUpload:VideoContentTypes").Get<string[]>();
            }
        }

        /// <summary>
        /// Kiểm tra content type có được phép không
        /// </summary>
        /// <param name="type">"image" hoặc "video"</param>
        /// <param name="contentType">Content type cần kiểm tra (vd: "image/jpeg")</param>
        /// <returns>true nếu được phép, false nếu không</returns>
        private bool IsAcceptableContentType(string type, string contentType)
        {
            var allowedContentTypes = AcceptableContentTypes(type); // Lấy danh sách được phép
            
            // Duyệt qua từng content type được phép
            foreach (var allowedContentType in allowedContentTypes)
            {
                if (contentType.ToLower().Equals(allowedContentType.ToLower())) // So sánh không phân biệt hoa thường
                {
                    return true;
                }
            }

            return false; // Không tìm thấy content type phù hợp
        }

        /// <summary>
        /// Convert IFormFile thành byte array để lưu vào database
        /// </summary>
        /// <param name="file">File upload từ form</param>
        /// <returns>Byte array của file</returns>
        private async Task<byte[]> GetContentsAsync(IFormFile file)
        {
            byte[] contents;
            using var memoryStream = new MemoryStream(); // Tạo memory stream
            await file.CopyToAsync(memoryStream); // Copy file vào memory stream
            contents = memoryStream.ToArray(); // Convert thành byte array
            return contents;
        }

        /// <summary>
        /// Phương pháp KHÔNG HIỆU QUẢ để lấy thông tin video cho trang watch
        /// Sử dụng Include để load nhiều related data cùng lúc -> có thể chậm với database lớn
        /// Được giữ lại để tham khảo, không sử dụng trong production
        /// </summary>
        /// <param name="id">ID của video</param>
        /// <returns>VideoWatch_vm hoặc null nếu không tìm thấy</returns>
        private async Task<VideoWatch_vm> GetVideoWatch_vmWithIncludeProperties(int id)
        {
            // Sử dụng Include để load tất cả related data (có thể chậm)
            // Cú pháp "Channel.Subscribers" = ThenInclude
            var fetchedVideo = await UnitOfWork.VideoRepo.GetFirstOrDefaultAsync(x => x.Id == id, "Channel.Subscribers,LikeDislikes,Comments.AppUser,Viewers");
            if (fetchedVideo != null)
            {
                var toReturn = new VideoWatch_vm();
                int userId = User.GetUserId();

                // Manually map các property từ Entity sang ViewModel
                toReturn.Id = fetchedVideo.Id;
                toReturn.Title = fetchedVideo.Title;
                toReturn.Description = fetchedVideo.Description;
                toReturn.CreatedAt = fetchedVideo.CreatedAt;
                toReturn.ChannelId = fetchedVideo.ChannelId;
                toReturn.ChannelName = fetchedVideo.Channel.Name;

                // Kiểm tra trạng thái của user hiện tại
                toReturn.IsSubscribed = fetchedVideo.Channel.Subscribers.Any(x => x.AppUserId == userId);
                toReturn.IsLiked = fetchedVideo.LikeDislikes.Any(x => x.AppUserId == userId && x.Liked == true);
                toReturn.IsDisiked = fetchedVideo.LikeDislikes.Any(x => x.AppUserId == userId && x.Liked == false);

                // Tính toán các số liệu thống kê
                toReturn.SubscribersCount = fetchedVideo.Channel.Subscribers.Count();
                toReturn.ViewersCount = fetchedVideo.Viewers.Select(X => X.NumberOfVisit).Sum();
                toReturn.LikesCount = fetchedVideo.LikeDislikes.Where(x => x.Liked == true).Count();
                toReturn.DislikesCount = fetchedVideo.LikeDislikes.Where(x => x.Liked == false).Count();

                // Chuẩn bị comment section
                toReturn.CommentVM.PostComment.VideoId = id;
                toReturn.CommentVM.AvailableComments = fetchedVideo.Comments.Select(x => new AvailableComment_vm
                {
                    FromName = x.AppUser.Name,
                    FromChannelId = UnitOfWork.ChannelRepo.GetChannelIdByUserId(x.AppUserId).GetAwaiter().GetResult(),
                    PostedAt = x.PostedAt,
                    Content = x.Content,
                });

                return toReturn;
            }

            return null;
        }

        /// <summary>
        /// Phương pháp HIỆU QUẢ để lấy thông tin video cho trang watch
        /// Sử dụng Projection để chỉ select những column cần thiết -> nhanh hơn nhiều
        /// Đây là best practice nên sử dụng trong production
        /// </summary>
        /// <param name="id">ID của video</param>
        /// <returns>VideoWatch_vm hoặc null nếu không tìm thấy</returns>
        private async Task<VideoWatch_vm> GetVideoWatch_vmWithProjections(int id)
        {
            int userId = User.GetUserId();
            
            // Sử dụng LINQ Projection để chỉ select những field cần thiết
            // Database sẽ chỉ trả về đúng data cần thiết -> hiệu quả hơn nhiều
            var toReturn = await Context.Video
                .Where(x => x.Id == id)
                .Select(x => new VideoWatch_vm
                {
                    // Basic video info
                    Id = x.Id,
                    Title = x.Title,
                    Description = x.Description,
                    CreatedAt = x.CreatedAt,
                    ChannelId = x.ChannelId,
                    ChannelName = x.Channel.Name,
                    
                    // User-specific status (đã subscribe, like, dislike chưa)
                    IsSubscribed = x.Channel.Subscribers.Any(s => s.AppUserId == userId),
                    IsLiked = x.LikeDislikes.Any(l => l.AppUserId == userId && l.Liked == true),
                    IsDisiked = x.LikeDislikes.Any(l => l.AppUserId == userId && l.Liked == false),
                    
                    // Statistics (số subscriber, view, like, dislike)
                    SubscribersCount = x.Channel.Subscribers.Count(),
                    ViewersCount = x.Viewers.Select(v => v.NumberOfVisit).Sum(),
                    LikesCount = x.LikeDislikes.Where(l => l.Liked == true).Count(),
                    DislikesCount = x.LikeDislikes.Where(l => l.Liked == false).Count(),
                    
                    // Comment section
                    CommentVM = new Comment_vm
                    {
                        PostComment = new CommentPost_vm
                        {
                            VideoId = x.Id
                        },
                        AvailableComments = x.Comments.Select(c => new AvailableComment_vm
                        {
                            FromName = c.AppUser.Name,
                            FromChannelId = c.AppUser.Channel.Id,
                            PostedAt = c.PostedAt,
                            Content = c.Content,
                        })
                    }
                })
                .FirstOrDefaultAsync();

            return toReturn;
        }
        #endregion
    }
}
