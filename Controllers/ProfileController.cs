using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTU.Models.DTOs;
using MTU.Services;

namespace MTU.Controllers
{
    [Authorize]
    public class ProfileController : Controller
    {
        private readonly IProfileService _profileService;
        private readonly IFriendshipService _friendshipService;
        private readonly IProfileUpdateService _profileUpdateService;
        private readonly ILogger<ProfileController> _logger;

        public ProfileController(
            IProfileService profileService,
            IFriendshipService friendshipService,
            IProfileUpdateService profileUpdateService,
            ILogger<ProfileController> logger)
        {
            _profileService = profileService;
            _friendshipService = friendshipService;
            _profileUpdateService = profileUpdateService;
            _logger = logger;
        }

 
        [HttpGet]
        [Route("Profile")]
        [Route("Profile/{id:int}")]
        public async Task<IActionResult> Index(int? id)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    _logger.LogWarning("User ID not found in authentication context");
                    return Unauthorized();
                }

                var currentUser = await _profileService.GetUserByIdAsync(currentUserId);
                if (currentUser != null)
                {
                    ViewBag.CurrentUserAvatar = currentUser.Avatar ?? "/assets/user.png";
                    var fullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
                    ViewBag.CurrentUserFullName = string.IsNullOrEmpty(fullName) ? currentUser.Username : fullName;
                }

                int profileUserId = id ?? currentUserId;
                var profileDto = await _profileService.GetProfileAsync(profileUserId, currentUserId);
                if (profileDto == null)
                {
                    _logger.LogWarning("Profile not found for user ID {UserId}", profileUserId);
                    TempData["ErrorMessage"] = "User profile not found";
                    return RedirectToAction("Index", "Home");
                }

                _logger.LogInformation("Profile loaded for user {UserId}", profileUserId);

                ViewData["IsProfilePage"] = true;
                ViewData["Title"] = profileDto.FullName;
                return View("~/Views/Home/Profile.cshtml", profileDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error loading profile for user {UserId}", id);
                TempData["ErrorMessage"] = "Unable to load profile. Please try again later.";
                return RedirectToAction("Index", "Home");
            }
        }

        /// <summary>
        /// Cập nhật chỉnh sửa hồ sơ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileDto updateDto)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var result = await _profileUpdateService.UpdateBasicInfoAsync(updateDto, currentUserId);

                if (result.Success)
                {
                    _logger.LogInformation("Profile updated successfully for user {UserId}", currentUserId);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Cập nhật ảnh đại diện trong hồ sơ
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateAvatar(IFormFile file)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var result = await _profileUpdateService.UpdateAvatarAsync(file, currentUserId);

                if (result.Success)
                {
                    _logger.LogInformation("Avatar updated successfully for user {UserId}", currentUserId);
                    return Json(new { success = true, avatarPath = result.UpdatedValue });
                }
                else
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating avatar");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Ảnh cover avt
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> UpdateCoverImage(IFormFile file)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var result = await _profileUpdateService.UpdateCoverImageAsync(file, currentUserId);

                if (result.Success)
                {
                    _logger.LogInformation("Cover image updated successfully for user {UserId}", currentUserId);
                    return Json(new { success = true, coverPath = result.UpdatedValue });
                }
                else
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating cover image");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Gửi yêu cầu kết bạn 
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> SendFriendRequest([FromBody] int friendId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var result = await _friendshipService.SendFriendRequestAsync(currentUserId, friendId);

                if (result.Success)
                {
                    _logger.LogInformation("Friend request sent from user {UserId} to {FriendId}", currentUserId, friendId);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error sending friend request");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Chấp nhật kết bạn
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AcceptFriendRequest([FromBody] int friendId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var result = await _friendshipService.AcceptFriendRequestAsync(currentUserId, friendId);

                if (result.Success)
                {
                    _logger.LogInformation("Friend request accepted by user {UserId} from {FriendId}", currentUserId, friendId);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting friend request");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Từ chối kết bạn
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> DeclineFriendRequest([FromBody] int friendId)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var result = await _friendshipService.DeclineFriendRequestAsync(currentUserId, friendId);

                if (result.Success)
                {
                    _logger.LogInformation("Friend request declined by user {UserId} from {FriendId}", currentUserId, friendId);
                    return Json(new { success = true });
                }
                else
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error declining friend request");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Kiểm tra user nếu đã update profile sau khi đăng nhập
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> CheckProfileStatus()
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { needsSetup = false });
                }

                var user = await _profileService.GetUserByIdAsync(currentUserId);
                if (user == null)
                {
                    return Json(new { needsSetup = false });
                }

                bool needsSetup = !user.IsProfileCompleted;

                return Json(new { needsSetup });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error checking profile status");
                return Json(new { needsSetup = false });
            }
        }

        /// <summary>
        /// Hoàn thành lần đầu update 
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CompleteFirstTimeSetup([FromForm] string firstName, [FromForm] string lastName, IFormFile? avatarFile, IFormFile? coverFile)
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                if (string.IsNullOrWhiteSpace(firstName) || string.IsNullOrWhiteSpace(lastName))
                {
                    return Json(new { success = false, errorMessage = "Vui lòng nhập đầy đủ họ và tên" });
                }

                var updateDto = new UpdateProfileDto
                {
                    FirstName = firstName.Trim(),
                    LastName = lastName.Trim()
                };

                var result = await _profileUpdateService.UpdateBasicInfoAsync(updateDto, currentUserId);
                if (!result.Success)
                {
                    return Json(new { success = false, errorMessage = result.ErrorMessage });
                }

                if (avatarFile != null && avatarFile.Length > 0)
                {
                    var avatarResult = await _profileUpdateService.UpdateAvatarAsync(avatarFile, currentUserId);
                    if (!avatarResult.Success)
                    {
                        _logger.LogWarning("Failed to upload avatar during first time setup: {Error}", avatarResult.ErrorMessage);
                    }
                }

                if (coverFile != null && coverFile.Length > 0)
                {
                    var coverResult = await _profileUpdateService.UpdateCoverImageAsync(coverFile, currentUserId);
                    if (!coverResult.Success)
                    {
                        _logger.LogWarning("Failed to upload cover during first time setup: {Error}", coverResult.ErrorMessage);
                    }
                }

                await _profileUpdateService.MarkProfileAsCompletedAsync(currentUserId);

                _logger.LogInformation("User {UserId} completed first time setup", currentUserId);

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error completing first time setup");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Nhận lời mời kb
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFriendSuggestions()
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, errorMessage = "Unauthorized" });
                }

                var suggestions = await _friendshipService.GetFriendSuggestionsAsync(currentUserId);

                _logger.LogInformation("Friend suggestions retrieved for user {UserId}", currentUserId);
                return Json(new { success = true, data = suggestions });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friend suggestions");
                return Json(new { success = false, errorMessage = "Đã xảy ra lỗi. Vui lòng thử lại sau." });
            }
        }

        /// <summary>
        /// Nhận yêu cầu kết bạn đang chờ xử lý cho người dùng hiện tại.
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFriendRequests()
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, requests = new List<object>() });
                }

                var requests = await _friendshipService.GetPendingFriendRequestsAsync(currentUserId);

                return Json(new { success = true, requests });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friend requests");
                return Json(new { success = false, requests = new List<object>() });
            }
        }

        /// <summary>
        /// Lấy danh sách bạn bè của người dùng hiện tại
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetFriends()
        {
            try
            {
                var currentUserIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (currentUserIdClaim == null || !int.TryParse(currentUserIdClaim.Value, out int currentUserId))
                {
                    return Json(new { success = false, friends = new List<object>() });
                }

                var friends = await _friendshipService.GetFriendsAsync(currentUserId);

                return Json(new { success = true, friends });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting friends list");
                return Json(new { success = false, friends = new List<object>() });
            }
        }
    }
}

