﻿using ClipShare.Core.Entities;
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
    [Authorize(Roles = $"{SD.AdminRole}")]
    public class AdminController : CoreController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly RoleManager<AppRole> _roleManager;

        public AdminController(UserManager<AppUser> userManager,
            RoleManager<AppRole> roleManager)
        {
            _userManager = userManager;
            _roleManager = roleManager;
        }
        // ===================== PHẦN 1: USER 3 PHỤ TRÁCH =====================
        // Quản lý user, role, khóa/mở user, xóa user, các chức năng liên quan đến người dùng

        // --- [USER 3] Quản lý danh sách user ---
        public IActionResult Category()
        {
            return View();
        }

        public async Task<IActionResult> AllUsers()
        {
            var toReturn = new List<UserDisplayGrid_vm>();
            var users = await _userManager.Users
                .Include(x => x.Channel)
                .Where(x => x.UserName != "admin")
                .ToListAsync();

            foreach (var user in users)
            {
                var userDisplayToAdd = new UserDisplayGrid_vm();
                Mapper.Map(user, userDisplayToAdd);
                userDisplayToAdd.IsLocked = _userManager.IsLockedOutAsync(user).GetAwaiter().GetResult();
                userDisplayToAdd.AssignedRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
                toReturn.Add(userDisplayToAdd);
            }

            return View(toReturn);
        }

        // --- [USER 3] Giao diện thêm/sửa user (GET) ---
        public async Task<IActionResult> AddEditUser(int id)
        {
            var toReturn = new UserAddEdit_vm();
            toReturn.ApplicationRoles = await GetApplicationRolesAsync();

            if (id > 0)
            {
                // edit
                var user = await _userManager.FindByIdAsync(id.ToString());
                Mapper.Map(user, toReturn);

                var userRoles = _userManager.GetRolesAsync(user).GetAwaiter().GetResult();
                toReturn.UserRoles = userRoles.ToList();
            }

            return View(toReturn);
        }

        // --- [USER 3] Xử lý thêm/sửa user (POST) ---

        [HttpPost]
        public async Task<IActionResult> AddEditUser(UserAddEdit_vm model)
        {
            if (ModelState.IsValid)
            {
                bool proceed = true;

                if (model.Id == 0)
                {
                    // Creating a user
                    if (string.IsNullOrEmpty(model.Password))
                    {
                        proceed = false;
                        ModelState.AddModelError("Password", "Password is required");
                    }

                    if (proceed && model.UserRoles.Count == 0)
                    {
                        proceed = false;
                        ModelState.AddModelError("UserRoles", "Please select at least one role");
                    }

                    if (proceed && CheckNameExistsAsync(model.Name).GetAwaiter().GetResult())
                    {
                        proceed = false;
                        ModelState.AddModelError("Name", $"The name of '{model.Name} is taken. Please try another name.");
                    }

                    if (proceed && CheckEmailExistsAsync(model.Email).GetAwaiter().GetResult())
                    {
                        proceed = false;
                        ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address.");
                    }

                    if (proceed)
                    {
                        var userToAdd = new AppUser
                        {
                            Name = model.Name,
                            UserName = model.Name.ToLower(),
                            Email = model.Email,
                        };

                        var result = await _userManager.CreateAsync(userToAdd, model.Password);
                        await _userManager.AddToRolesAsync(userToAdd, model.UserRoles);

                        if (result.Succeeded)
                        {
                            return RedirectToAction("AllUsers");
                        }
                        else
                        {
                            foreach (var error in result.Errors)
                            {
                                ModelState.AddModelError(string.Empty, error.Description);
                            }
                        }
                    }
                }
                else
                {
                    // Editing an user
                    var user = await _userManager.FindByIdAsync(model.Id.ToString());

                    if (user == null)
                    {
                        TempData["notification"] = "false;Not Found;The requested user was not found";
                        return RedirectToAction("AllUsers");
                    }

                    if (IsSuperAdminUserId(model.Id))
                    {
                        TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                        return RedirectToAction("AllUsers");
                    }

                    if (model.UserRoles.Count == 0)
                    {
                        proceed = false;
                        ModelState.AddModelError("UserRoles", "Please select at least one role");
                    }

                    if (proceed && !user.Name.Equals(model.Name))
                    {
                        if (CheckNameExistsAsync(model.Name).GetAwaiter().GetResult())
                        {
                            proceed = false;
                            ModelState.AddModelError("Name", $"The name of '{model.Name} is taken. Please try another name.");
                        }
                    }

                    if (proceed && !user.Email.Equals(model.Email))
                    {
                        if (CheckEmailExistsAsync(model.Email).GetAwaiter().GetResult())
                        {
                            proceed = false;
                            ModelState.AddModelError("Email", $"Email address of {model.Email} is taken. Please try using another email address.");
                        }
                    }

                    if (proceed && !string.IsNullOrEmpty(model.Password))
                    {
                        // Changing the user's password
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
                        user.Name = model.Name;
                        user.UserName = model.Name.ToLower();
                        user.Email = model.Email;

                        var userRoles = await _userManager.GetRolesAsync(user);
                        // remove user's existing roles
                        await _userManager.RemoveFromRolesAsync(user, userRoles);

                        // adding the new roles
                        foreach (var role in model.UserRoles)
                        {
                            var roleToAdd = await _roleManager.Roles.FirstOrDefaultAsync(r => r.Name == role);
                            if (roleToAdd != null)
                            {
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

        // --- [USER 3] Khóa user ---

        [HttpPost]
        public async Task<IActionResult> LockUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                TempData["notification"] = "false;Not Found;The requested user was not found";
                return RedirectToAction("AllUsers");
            }

            if (IsSuperAdminUserId(id))
            {
                TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                return RedirectToAction("AllUsers");
            }

            // Lock the user for 5 days
            user.LockoutEnabled = true;
            var result = await _userManager.SetLockoutEndDateAsync(user, DateTime.UtcNow.AddDays(5));

            if (!result.Succeeded)
            {
                TempData["notification"] = "false;Server Error;Server Error";
            }

            return RedirectToAction("AllUsers");
        }

        // --- [USER 3] Mở khóa user ---

        [HttpPost]
        public async Task<IActionResult> UnlockUser(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());

            if (user == null)
            {
                TempData["notification"] = "false;Not Found;The requested user was not found";
                return RedirectToAction("AllUsers");
            }

            if (IsSuperAdminUserId(id))
            {
                TempData["notification"] = "false;Bad Request;Super Admin change is not allowed!";
                return RedirectToAction("AllUsers");
            }


            var result = await _userManager.SetLockoutEndDateAsync(user, null);

            if (!result.Succeeded)
            {
                TempData["notification"] = "false;Server Error;Server Error";
            }

            return RedirectToAction("AllUsers");
        }


        // --- [USER 3] Xóa user (API) ---
        [HttpDelete]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var user = await _userManager.Users
                .Include(x => x.Channel)
                .Where(x => x.Id == id)
                .FirstOrDefaultAsync();

            if (user != null)
            {
                if (IsSuperAdminUserId(id))
                {
                    return Json(new ApiResponse(400, message: "Super admin cannot be deleted"));
                }

                if (user.Channel != null)
                {
                    var userChannelWithVideos = await Context.Channel
                        .Where(x => x.AppUserId == id)
                        .Select(x => new
                        {
                            Videos = x.Videos.Select(x => new
                            {
                                x.Id,
                                x.ThumbnailUrl
                            })
                        }).FirstOrDefaultAsync();

                    foreach (var video in userChannelWithVideos.Videos)
                    {
                        PhotoService.DeletePhotoLocally(video.ThumbnailUrl);
                        await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id);
                        await UnitOfWork.CompleteAsync();
                    }
                }

                var result = await _userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    TempData["notification"] = $"true;Deleted;User of {user.Name} has been permanently removed";
                    return Json(new ApiResponse(200));
                }
                else
                {
                    return Json(new ApiResponse(400, message: result.Errors.Select(x => x.Description).FirstOrDefault()));
                }
            }

            return Json(new ApiResponse(404, message: "The requested user was not found"));
        }

        #region API Endpoints
        // ===================== PHẦN 2: USER 4 PHỤ TRÁCH =====================
        // Quản lý category, video chờ duyệt, duyệt video, xóa video, xem video, các chức năng liên quan đến video & category

        // --- [USER 4] API lấy danh sách category ---
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

        // --- [USER 4] Thêm/sửa category (API) ---
        [HttpPost]
        public async Task<IActionResult> AddEditCategory(CategoryAddEdit_vm model)
        {
            if (ModelState.IsValid)
            {
                if (model.Id == 0)
                {
                    UnitOfWork.CategoryRepo.Add(new Category() { Name = model.Name });
                    await UnitOfWork.CompleteAsync();
                    return Json(new ApiResponse(201, "Created", "New Category was added"));
                }
                else
                {
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

        // --- [USER 4] Xóa category (API) ---
        [HttpDelete]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var category = await UnitOfWork.CategoryRepo.GetByIdAsync(id);
            if (category != null)
            {
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
                    foreach (var video in categoryVideoIdsAndThumbnailUrls)
                    {
                        PhotoService.DeletePhotoLocally(video.ThumbnailUrl);
                        await UnitOfWork.VideoRepo.RemoveVideoAsync(video.Id);
                        await UnitOfWork.CompleteAsync();
                    }
                }

                UnitOfWork.CategoryRepo.Remove(category);
                await UnitOfWork.CompleteAsync();

                return Json(new ApiResponse(200, "Deleted", "Category of " + category.Name + " has been removed"));
            }

            return Json(new ApiResponse(404, message: "The requested category was not found"));
        }


        #endregion



        // --- [USER 4] Xem danh sách video chờ duyệt ---
        [HttpGet]
        public async Task<IActionResult> PendingVideos()
        {
            var videos = await Context.Video
                .Where(x => !x.IsApproved)
                .ToListAsync();
            return View(videos);
        }
        // --- [USER 4] Duyệt video ---
        [HttpPost]
        public async Task<IActionResult> ApproveVideo(int id)
        {
            var video = await Context.Video.FindAsync(id);
            if (video == null) return NotFound();
            video.IsApproved = true;
            await Context.SaveChangesAsync();
            return RedirectToAction("PendingVideos");
        }

        // --- [USER 4] Xem chi tiết video ---
        [HttpGet]
        public async Task<IActionResult> ViewVideo(int id)
        {
            var video = await Context.Video
                .Include(v => v.Channel)
                .FirstOrDefaultAsync(v => v.Id == id);
            if (video == null) return NotFound();
            return View(video);
        }

        // --- [USER 4] Xóa video (POST) ---
        [HttpPost]
        public async Task<IActionResult> DeleteVideo(int id)
        {
            var video = await Context.Video.FindAsync(id);
            if (video == null) return NotFound();
            Context.Video.Remove(video);
            await Context.SaveChangesAsync();
            return RedirectToAction("PendingVideos");
        }

        // ===================== CẢ HAI USER 3 & 4 CÓ THỂ THAM KHẢO =====================
        // Các hàm private/phụ trợ dùng chung cho controller
        #region Private Methods
        public async Task<List<string>> GetApplicationRolesAsync()
        {
            return await _roleManager.Roles
                .Select(x => x.Name)
                .ToListAsync();
        }

        private async Task<bool> CheckEmailExistsAsync(string email)
        {
            return await _userManager.Users.AnyAsync(x => x.Email == email.ToLower());
        }

        private async Task<bool> CheckNameExistsAsync(string name)
        {
            return await _userManager.Users.AnyAsync(x => x.Name.ToLower() == name.ToLower());
        }

        private bool IsSuperAdminUserId(int userId)
        {
            return _userManager.FindByIdAsync(userId.ToString()).GetAwaiter().GetResult().UserName.Equals("admin");
        }
        #endregion
    }
}


