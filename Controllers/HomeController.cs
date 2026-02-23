using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MTU.Models.DTOs;
using MTU.Models.Entities;
using MTU.Repositories;
using MTU.Services;

namespace MTU.Controllers
{
    public class HomeController : Controller
    {
        private readonly IAuthService _authService;
        private readonly IPostService _postService;
        private readonly IProfileUpdateService _profileUpdateService;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<HomeController> _logger;

        public HomeController(
            IAuthService authService,
            IPostService postService,
            IProfileUpdateService profileUpdateService,
            IStudentRepository studentRepository,
            ILogger<HomeController> logger)
        {
            _authService = authService;
            _postService = postService;
            _profileUpdateService = profileUpdateService;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            if (!currentUser.IsProfileCompleted)
            {
                return RedirectToAction("SetupProfile");
            }

            SetCurrentUserViewBag(currentUser);
            var feed = await _postService.GetFriendFeedAsync(1, 20, currentUser.UserId);
            return View(feed);
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Explore(int page = 1)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            SetCurrentUserViewBag(currentUser);
            var feed = await _postService.GetExploreFeedAsync(page, 20, currentUser.UserId);
            return View(feed);
        }

        [AllowAnonymous]
        public IActionResult Login()
        {
            return View(new LoginDto());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var result = await _authService.LoginAsync(dto);
            if (!result.Success || result.User == null)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Đăng nhập không thành công");
                return View(dto);
            }

            await SignInUserAsync(result.User);

            if (!result.User.IsProfileCompleted)
            {
                return RedirectToAction("SetupProfile");
            }

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public IActionResult Register()
        {
            return View(new RegisterDto());
        }

        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var result = await _authService.RegisterAsync(dto);
            if (!result.Success)
            {
                ModelState.AddModelError(string.Empty, result.ErrorMessage ?? "Không thể đăng ký tài khoản");
                return View(dto);
            }

            TempData["AuthSuccess"] = result.ErrorMessage;
            return RedirectToAction("Login");
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login");
        }

        [Authorize]
        public async Task<IActionResult> Settings()
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            SetCurrentUserViewBag(currentUser);
            ViewBag.FirstName = currentUser.FirstName ?? string.Empty;
            ViewBag.LastName = currentUser.LastName ?? string.Empty;
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeName([FromBody] ChangeNameDto dto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var updateDto = new UpdateProfileDto
            {
                FirstName = dto.FirstName?.Trim(),
                LastName = dto.LastName?.Trim()
            };

            var result = await _profileUpdateService.UpdateBasicInfoAsync(updateDto, currentUser.UserId);
            return Json(new { success = result.Success, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword([FromBody] ChangePasswordDto dto)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return Json(new { success = false, message = "Unauthorized" });
            }

            var result = await _authService.ChangePasswordAsync(currentUser.UserId, dto.CurrentPassword, dto.NewPassword);
            return Json(new { success = result.Success, message = result.ErrorMessage });
        }

        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Search(string q)
        {
            if (string.IsNullOrWhiteSpace(q))
            {
                return Json(new { success = true, results = Array.Empty<object>() });
            }

            var users = await _authService.SearchUsersAsync(q.Trim(), 10);
            var results = users.Select(user => new
            {
                id = user.UserId,
                username = user.Username,
                name = string.IsNullOrWhiteSpace($"{user.FirstName} {user.LastName}".Trim())
                    ? user.Username
                    : $"{user.FirstName} {user.LastName}".Trim(),
                avatar = user.Avatar ?? "/assets/user.png"
            });

            return Json(new { success = true, results });
        }

        [Authorize]
        [HttpGet]
        public IActionResult SetupProfile()
        {
            return View(new SetupProfileDto());
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SetupProfile(SetupProfileDto dto)
        {
            if (!ModelState.IsValid)
            {
                return View(dto);
            }

            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            var updateDto = new UpdateProfileDto
            {
                FirstName = dto.FirstName.Trim(),
                LastName = dto.LastName.Trim(),
                Gender = dto.Gender,
                DateOfBirth = dto.DateOfBirth,
                Location = dto.Location,
                Faculty = dto.Faculty,
                AcademicYear = dto.AcademicYear,
                Interests = dto.Interests,
                Bio = dto.Bio
            };

            var updateResult = await _profileUpdateService.UpdateBasicInfoAsync(updateDto, currentUser.UserId);
            if (!updateResult.Success)
            {
                ModelState.AddModelError(string.Empty, updateResult.ErrorMessage ?? "Không thể cập nhật hồ sơ");
                return View(dto);
            }

            if (dto.AvatarFile != null && dto.AvatarFile.Length > 0)
            {
                await _profileUpdateService.UpdateAvatarAsync(dto.AvatarFile, currentUser.UserId);
            }

            if (dto.CoverImageFile != null && dto.CoverImageFile.Length > 0)
            {
                await _profileUpdateService.UpdateCoverImageAsync(dto.CoverImageFile, currentUser.UserId);
            }

            await _profileUpdateService.MarkProfileAsCompletedAsync(currentUser.UserId);
            return RedirectToAction("Index");
        }

        [Authorize]
        [HttpGet]
        public IActionResult LinkMssv()
        {
            return View();
        }

        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> LinkMssv(string mssv, string portalPassword)
        {
            var currentUser = await GetCurrentUserAsync();
            if (currentUser == null)
            {
                return RedirectToAction("Login");
            }

            if (string.IsNullOrWhiteSpace(mssv))
            {
                TempData["ErrorMessage"] = "Vui lòng nhập MSSV";
                return View();
            }

            var student = await _studentRepository.GetByUserIdAsync(currentUser.UserId) ??
                          await _studentRepository.CreateForUserAsync(currentUser.UserId);

            student.MSSV = mssv.Trim();
            student.IsLinked = true;
            student.LinkedAt = DateTime.Now;
            await _studentRepository.UpdateAsync(student);

            return RedirectToAction("Index");
        }

        [AllowAnonymous]
        public IActionResult Privacy()
        {
            return View();
        }

        [AllowAnonymous]
        public IActionResult Error()
        {
            return View("~/Views/Shared/Error.cshtml");
        }

        private async Task<User?> GetCurrentUserAsync()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return null;
            }

            return await _authService.GetUserByIdAsync(userId);
        }

        private void SetCurrentUserViewBag(User user)
        {
            ViewBag.CurrentUserAvatar = user.Avatar ?? "/assets/user.png";
            var fullName = $"{user.FirstName} {user.LastName}".Trim();
            ViewBag.CurrentUserFullName = string.IsNullOrWhiteSpace(fullName) ? user.Username : fullName;
        }

        private async Task SignInUserAsync(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddDays(7)
                });
        }
    }
}

