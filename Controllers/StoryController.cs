using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Controllers
{
    [Authorize]
    public class StoryController : Controller
    {
        private readonly MTUSocialDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<StoryController> _logger;

        public StoryController(
            MTUSocialDbContext context,
            IWebHostEnvironment environment,
            ILogger<StoryController> logger)
        {
            _context = context;
            _environment = environment;
            _logger = logger;
        }

        /// <summary>
        /// Xem tin ở form khác
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> View(int id)
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
            {
                return RedirectToAction("Login", "Home");
            }

            var story = await _context.Set<Story>()
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StoryId == id && s.IsActive && s.ExpiresAt > DateTime.Now);

            if (story == null)
            {
                TempData["ErrorMessage"] = "Story không tồn tại hoặc đã hết hạn";
                return RedirectToAction("Index", "Home");
            }

            bool isOwnStory = story.UserId == userId;
            if (!isOwnStory)
            {
                // Fix: Check if user already viewed this story
                var hasViewed = await _context.Set<StoryView>()
                    .AnyAsync(sv => sv.StoryId == id && sv.ViewerId == userId);

                if (!hasViewed)
                {
                    story.ViewCount++;
                    
                    var storyView = new StoryView
                    {
                        StoryId = id,
                        ViewerId = userId,
                        ViewedAt = DateTime.Now
                    };
                    _context.Set<StoryView>().Add(storyView);

                    await _context.SaveChangesAsync();
                }
            }

            var currentUser = await _context.Users.FindAsync(userId);
            if (currentUser != null)
            {
                ViewBag.CurrentUserAvatar = currentUser.Avatar ?? "/assets/user.png";
                ViewBag.CurrentUserFullName = $"{currentUser.FirstName} {currentUser.LastName}".Trim();
            }

            // Đếm lượt xem thực tế từ bảng StoryViews
            var actualViewCount = await _context.Set<StoryView>()
                .CountAsync(sv => sv.StoryId == id);
            ViewBag.ActualViewCount = actualViewCount;

            var userStories = await _context.Set<Story>()
                .Where(s => s.UserId == story.UserId && s.IsActive && s.ExpiresAt > DateTime.Now)
                .OrderBy(s => s.CreatedAt)
                .Select(s => s.StoryId)
                .ToListAsync();

            ViewBag.UserStories = userStories;
            ViewBag.CurrentIndex = userStories.IndexOf(id);
            ViewBag.IsOwnStory = isOwnStory;
            ViewBag.CurrentUserId = userId;

            return View(story);
        }

        /// <summary>
        /// Nhận tin từ bạn bè và bản thân để show lên feed
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStories()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, stories = new List<object>() });
                }

            
                var friendIds = await _context.Friendships
                    .Where(f => (f.UserId == userId || f.FriendId == userId) && f.Status == "accepted")
                    .Select(f => f.UserId == userId ? f.FriendId : f.UserId)
                    .ToListAsync();

                friendIds.Add(userId);

                var stories = await _context.Set<Story>()
                    .Include(s => s.User)
                    .Where(s => friendIds.Contains(s.UserId) && s.IsActive && s.ExpiresAt > DateTime.Now)
                    .OrderByDescending(s => s.CreatedAt)
                    .GroupBy(s => s.UserId)
                    .Select(g => g.First())
                    .Take(20)
                    .ToListAsync();

                var result = stories.Select(s => new
                {
                    storyId = s.StoryId,
                    userId = s.UserId,
                    userName = s.User != null 
                        ? $"{s.User.FirstName} {s.User.LastName}".Trim() 
                        : "User",
                    avatar = s.User?.Avatar ?? "/assets/user.png",
                    mediaUrl = s.MediaUrl,
                    mediaType = s.MediaType,
                    isOwn = s.UserId == userId
                }).ToList();

                return Json(new { success = true, stories = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting stories");
                return Json(new { success = false, stories = new List<object>() });
            }
        }

        /// <summary>
        /// Tạo 1 tin mới
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Create(IFormFile file, string? caption)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                if (file == null || file.Length == 0)
                {
                    return Json(new { success = false, message = "Vui lòng chọn file" });
                }

                var allowedImageTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/gif" };
                var allowedVideoTypes = new[] { "video/mp4", "video/webm", "video/quicktime" };
                var isImage = allowedImageTypes.Contains(file.ContentType.ToLower());
                var isVideo = allowedVideoTypes.Contains(file.ContentType.ToLower());

                if (!isImage && !isVideo)
                {
                    return Json(new { success = false, message = "Chỉ chấp nhận file ảnh hoặc video" });
                }

                var maxSize = isVideo ? 50 * 1024 * 1024 : 10 * 1024 * 1024;
                if (file.Length > maxSize)
                {   
                    // tránh load ảnh 4k tránh quá tải
                    return Json(new { success = false, message = isVideo ? "Video không được vượt quá 50MB" : "Ảnh không được vượt quá 10MB" });
                }
                    
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "stories");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
                var filePath = Path.Combine(uploadsFolder, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }

                // Tạo tin
                var story = new Story
                {
                    UserId = userId,
                    MediaUrl = $"/uploads/stories/{fileName}",
                    MediaType = isVideo ? "video" : "image",
                    Caption = caption?.Trim(),
                    CreatedAt = DateTime.Now,
                    ExpiresAt = DateTime.Now.AddHours(24)
                };

                _context.Set<Story>().Add(story);
                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} created story {StoryId}", userId, story.StoryId);

                return Json(new { success = true, storyId = story.StoryId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating story");
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        /// <summary>
        /// Xóa tin
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> Delete([FromBody] int storyId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var story = await _context.Set<Story>().FindAsync(storyId);
                if (story == null || story.UserId != userId)
                {
                    return Json(new { success = false, message = "Story không tồn tại" });
                }

                story.IsActive = false;
                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting story");
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }
        /// <summary>
        /// Lấy danh sách người đã xem tin
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetStoryViewers(int storyId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                {
                    return Json(new { success = false, message = "Unauthorized" });
                }

                var story = await _context.Set<Story>()
                    .FirstOrDefaultAsync(s => s.StoryId == storyId);

                if (story == null)
                {
                    return Json(new { success = false, message = "Story không tồn tại" });
                }

                if (story.UserId != userId)
                {
                    return Json(new { success = false, message = "Bạn không có quyền xem danh sách này" });
                }

                // Lấy viewers kèm emoji reaction nếu có
                var viewers = await _context.Set<StoryView>()
                    .Where(sv => sv.StoryId == storyId)
                    .Include(sv => sv.Viewer)
                    .OrderByDescending(sv => sv.ViewedAt)
                    .Select(sv => new
                    {
                        userId      = sv.ViewerId,
                        fullName    = sv.Viewer != null ? $"{sv.Viewer.FirstName} {sv.Viewer.LastName}".Trim() : "Người dùng ẩn danh",
                        avatar      = sv.Viewer != null ? (sv.Viewer.Avatar ?? "/assets/user.png") : "/assets/user.png",
                        viewedAt    = sv.ViewedAt.ToString("HH:mm dd/MM"),
                        emoji       = _context.StoryReactions
                                        .Where(r => r.StoryId == storyId && r.UserId == sv.ViewerId)
                                        .Select(r => r.Emoji)
                                        .FirstOrDefault()
                    })
                    .ToListAsync();

                return Json(new { success = true, viewers });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting story viewers");
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        /// <summary>
        /// Thả/đổi biểu tượng cảm xúc vào story của bạn bè
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> React([FromBody] StoryReactDto dto)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int userId))
                    return Json(new { success = false, message = "Unauthorized" });

                var story = await _context.Stories
                    .FirstOrDefaultAsync(s => s.StoryId == dto.StoryId && s.IsActive && s.ExpiresAt > DateTime.Now);

                if (story == null)
                    return Json(new { success = false, message = "Story không tồn tại hoặc đã hết hạn" });

                if (story.UserId == userId)
                    return Json(new { success = false, message = "Không thể react story của chính mình" });

                // Emoji mapping
                var emojiMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    { "heart", "❤️" }, { "fire", "🔥" }, { "laugh", "😂" },
                    { "wow",   "😮" }, { "sad",  "😢" }, { "clap",  "👏" }
                };

                if (!emojiMap.TryGetValue(dto.ReactionType, out var emoji))
                    return Json(new { success = false, message = "Loại reaction không hợp lệ" });

                // Upsert: đã react thì cập nhật, chưa thì tạo mới
                var existing = await _context.StoryReactions
                    .FirstOrDefaultAsync(r => r.StoryId == dto.StoryId && r.UserId == userId);

                bool isNew = existing == null;
                if (isNew)
                {
                    existing = new StoryReaction
                    {
                        StoryId      = dto.StoryId,
                        UserId       = userId,
                        ReactionType = dto.ReactionType,
                        Emoji        = emoji,
                        CreatedAt    = DateTime.Now
                    };
                    _context.StoryReactions.Add(existing);
                }
                else
                {
                    existing.ReactionType = dto.ReactionType;
                    existing.Emoji        = emoji;
                    existing.CreatedAt    = DateTime.Now;
                }

                await _context.SaveChangesAsync();

                _logger.LogInformation("User {UserId} reacted {Emoji} to story {StoryId}", userId, emoji, dto.StoryId);

                return Json(new { success = true, emoji, isNew });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reacting to story");
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }
    }

    // DTO cho React endpoint
    public class StoryReactDto
    {
        public int StoryId { get; set; }
        public string ReactionType { get; set; } = string.Empty;
    }
}

