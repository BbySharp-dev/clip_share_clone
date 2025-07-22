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
    [Authorize(Roles = $"{SD.UserRole}")]
    public class ChannelController : CoreController
        // --- [USER 5] Trang chủ kênh, hiển thị thông tin kênh ---
    {
        public async Task<IActionResult> Index(string stringModel)
        {
            var model = new ChannelAddEdit_vm();
            stringModel = HttpContext.Session.GetString("ChannelModelFromSession");

            if (!string.IsNullOrEmpty(stringModel))
            {
                model = JsonConvert.DeserializeObject<ChannelAddEdit_vm>(stringModel);
                if (model.Errors.Count > 0)
                {
                    foreach (var error in model.Errors)
                    {
                        ModelState.AddModelError(error.Key, error.ErrorMessage);
                    }

                    HttpContext.Session.Remove("ChannelModelFromSession");

                    return View(model);
                }
            }

            var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.AppUserId == User.GetUserId(), includeProperties: "Subscribers");

            if (channel != null)
            {
                model.Name = channel.Name;
                model.About = channel.About;
                model.SubscribersCount = channel.Subscribers.Count();
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> CreateChannel(ChannelAddEdit_vm model)
        // --- [USER 5] Tạo kênh mới ---
        {
            if (!ModelState.IsValid)
            {
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

                HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));

                return RedirectToAction("Index");
            }

            var channelNameExists = await UnitOfWork.ChannelRepo.AnyAsync(x => x.Name.ToLower() == model.Name.ToLower());
            if (channelNameExists)
            {
                model.Errors.Add(new ModelError_vm
                {
                    Key = "Name",
                    ErrorMessage = $"Channel name of {model.Name} is taken. Please try other name"
                });

                HttpContext.Session.SetString("ChannelModelFromSession", JsonConvert.SerializeObject(model));
                //return RedirectToAction("Index", new { stringModel = JsonConvert.SerializeObject(model) });
                return RedirectToAction("Index");
            }

            var channelToAdd = new Channel
            {
                AppUserId = User.GetUserId(),
                Name = model.Name,
                About = model.About,
            };

            UnitOfWork.ChannelRepo.Add(channelToAdd);
            await UnitOfWork.CompleteAsync();

            TempData["notification"] = "true;Channel Created;Your channel has been created and you can upload clips now";

            return RedirectToAction("Index");
        }

        [HttpPost]
        public async Task<IActionResult> EditChannel(ChannelAddEdit_vm model)
        // --- [USER 5] Sửa thông tin kênh ---
        {
            if (ModelState.IsValid)
            {
                var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.AppUserId == User.GetUserId());
                if (channel != null)
                {
                    channel.Name = model.Name;
                    channel.About = model.About;
                    await UnitOfWork.CompleteAsync();

                    TempData["notification"] = "true;Channel updated;Your channel is updated";
                    return RedirectToAction("Index");
                }
            }

            TempData["notification"] = "false;Not Found;Your channel was not found";
            return RedirectToAction("Index");
        }





        //
          [HttpGet]
        public async Task<IActionResult> Analytics()
        // --- [USER 5] Thống kê/analytics kênh ---
        {
            var userId = User.GetUserId();
            var channel = await UnitOfWork.ChannelRepo.GetFirstOrDefaultAsync(x => x.AppUserId == userId, includeProperties: "Videos,Subscribers");
            if (channel == null)
            {
                TempData["notification"] = "false;Not Found;Your channel was not found";
                return RedirectToAction("Index");
            }

            // Tổng số video
            var totalVideos = channel.Videos?.Count() ?? 0;
            // Tổng lượt xem
            var totalViews = channel.Videos?.SelectMany(v => v.Viewers ?? new List<VideoView>()).Count() ?? 0;
            // Tổng subscribe
            var totalSubscribers = channel.Subscribers?.Count() ?? 0;

            // Top 5 video nhiều lượt xem nhất
            var topVideos = (channel.Videos ?? new List<Video>())
                .OrderByDescending(v => (v.Viewers?.Count() ?? 0))
                .Take(5)
                .Select(v => new { v.Title, Views = v.Viewers?.Count() ?? 0 })
                .ToList();

            // Dữ liệu cho biểu đồ lượt xem từng video
            var chartLabels = topVideos.Select(v => v.Title).ToArray();
            var chartData = topVideos.Select(v => v.Views).ToArray();

            ViewBag.TotalVideos = totalVideos;
            ViewBag.TotalViews = totalViews;
            ViewBag.TotalSubscribers = totalSubscribers;
            ViewBag.ChartLabels = Newtonsoft.Json.JsonConvert.SerializeObject(chartLabels);
            ViewBag.ChartData = Newtonsoft.Json.JsonConvert.SerializeObject(chartData);

            return View();
        }
    }
}
