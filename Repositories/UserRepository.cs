using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using MTU.Data;
using MTU.Models.Entities;

namespace MTU.Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly MTUSocialDbContext _context;

        public UserRepository(MTUSocialDbContext context)
        {
            _context = context;
        }

        public async Task<User?> GetByIdAsync(int userId)
        {
            return await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == userId);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Username == username);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetAllAsync()
        {
            return await _context.Users
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();
        }

        public async Task<User> CreateAsync(User user)
        {
            user.CreatedAt = DateTime.Now;
            user.UpdatedAt = DateTime.Now;
            user.IsActive = true;

            _context.Users.Add(user);
            await _context.SaveChangesAsync();
            
            return user;
        }

        public async Task<User> UpdateAsync(User user)
        {
            user.UpdatedAt = DateTime.Now;
            
            _context.Users.Update(user);
            await _context.SaveChangesAsync();
            
            return user;
        }

        public async Task<bool> ExistsAsync(string username, string email)
        {
            return await _context.Users
                .AnyAsync(u => u.Username == username || u.Email == email);
        }

        public async Task<List<User>> GetSuggestedFriendsAsync(int currentUserId, int limit)
        {
            // ── 1. Lấy danh sách đã kết bạn / đang pending ──────────────────
            var excludedIds = await _context.Friendships
                .Where(f => (f.UserId == currentUserId || f.FriendId == currentUserId)
                            && (f.Status == "accepted" || f.Status == "pending"))
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            excludedIds.Add(currentUserId); // loại bản thân

            // ── 2. Lấy thông tin người dùng hiện tại ─────────────────────────
            var currentUser = await _context.Users
                .Include(u => u.Student)
                .FirstOrDefaultAsync(u => u.UserId == currentUserId);

            var currentClass  = currentUser?.Student?.Class;
            var currentFaculty = currentUser?.Student?.Faculty;

            // ── 3. Danh sách bạn đã accepted ──────────────────────────────────
            var myFriendIds = await _context.Friendships
                .Where(f => (f.UserId == currentUserId || f.FriendId == currentUserId)
                            && f.Status == "accepted")
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();

            // ── 4. Lấy pool ứng viên (lấy nhiều rồi score in-memory) ──────────
            var candidates = await _context.Users
                .Include(u => u.Student)
                .Where(u => !excludedIds.Contains(u.UserId) && u.IsActive)
                .ToListAsync();

            // ── 5. Tính điểm cho từng ứng viên ───────────────────────────────
            // Lấy toàn bộ friend-ids của tất cả candidates trong 1 query
            var candidateIds = candidates.Select(u => u.UserId).ToList();

            var candidateFriendships = await _context.Friendships
                .Where(f => (candidateIds.Contains(f.UserId) || candidateIds.Contains(f.FriendId))
                            && f.Status == "accepted")
                .Select(f => new { f.UserId, f.FriendId })
                .ToListAsync();

            // Build dict: candidateId → set of their friend ids
            var candidateFriendMap = candidateIds.ToDictionary(
                id => id,
                id => candidateFriendships
                    .Where(f => f.UserId == id || f.FriendId == id)
                    .Select(f => f.UserId == id ? f.FriendId : f.UserId)
                    .ToHashSet()
            );

            var scored = candidates.Select(u =>
            {
                int score = 0;

                // +10 per mutual friend (friend-of-friend)
                if (candidateFriendMap.TryGetValue(u.UserId, out var theirFriends))
                {
                    int mutualCount = theirFriends.Intersect(myFriendIds).Count();
                    score += mutualCount * 10;
                }

                // +20 same class (cùng lớp – signal mạnh nhất)
                if (!string.IsNullOrEmpty(currentClass)
                    && u.Student?.Class == currentClass)
                    score += 20;

                // +8 same faculty (cùng khoa)
                if (!string.IsNullOrEmpty(currentFaculty)
                    && u.Student?.Faculty == currentFaculty
                    && u.Student?.Class != currentClass) // tránh cộng đôi
                    score += 8;

                // +2 mới tạo tài khoản trong 7 ngày
                if ((DateTime.Now - u.CreatedAt).TotalDays <= 7)
                    score += 2;

                return new { User = u, Score = score };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.User.CreatedAt)
            .Take(limit)
            .Select(x => x.User)
            .ToList();

            return scored;
        }

        /// <summary>
        /// Tính số bạn chung giữa currentUser và targetUser
        /// </summary>
        public async Task<int> GetMutualFriendCountAsync(int currentUserId, int targetUserId)
        {
            var myFriends = (await _context.Friendships
                .Where(f => (f.UserId == currentUserId || f.FriendId == currentUserId)
                            && f.Status == "accepted")
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync()).ToHashSet();

            var theirFriends = (await _context.Friendships
                .Where(f => (f.UserId == targetUserId || f.FriendId == targetUserId)
                            && f.Status == "accepted")
                .Select(f => f.UserId == targetUserId ? f.FriendId : f.UserId)
                .ToListAsync()).ToHashSet();

            return myFriends.Intersect(theirFriends).Count();
        }

        public async Task<string> SaveAvatarAsync(IFormFile file, int userId)
        {            if (!ValidateImageFile(file))
                throw new ArgumentException("Invalid image file. Only jpg, jpeg, png, gif are allowed with max size of 5MB");            var extension = Path.GetExtension(file.FileName);
            var fileName = $"avatar_{userId}_{Guid.NewGuid()}{extension}";
            var uploadPath = Path.Combine("wwwroot", "uploads", "avatars");            Directory.CreateDirectory(uploadPath);            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }            return $"/uploads/avatars/{fileName}";
        }

        public async Task<string> SaveCoverImageAsync(IFormFile file, int userId)
        {            if (!ValidateImageFile(file))
                throw new ArgumentException("Invalid image file. Only jpg, jpeg, png, gif are allowed with max size of 5MB");            var extension = Path.GetExtension(file.FileName);
            var fileName = $"cover_{userId}_{Guid.NewGuid()}{extension}";
            var uploadPath = Path.Combine("wwwroot", "uploads", "covers");            Directory.CreateDirectory(uploadPath);            var filePath = Path.Combine(uploadPath, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }            return $"/uploads/covers/{fileName}";
        }

        private bool ValidateImageFile(IFormFile file)
        {            if (file.Length > 5 * 1024 * 1024)
                return false;            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            
            return allowedExtensions.Contains(extension);
        }

        public async Task<List<User>> SearchAsync(string query, int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(query))
                return new List<User>();

            var searchTerm = query.ToLower().Trim();

            return await _context.Users
                .Where(u => u.IsActive && (
                    (u.FirstName != null && u.FirstName.ToLower().Contains(searchTerm)) ||
                    (u.LastName != null && u.LastName.ToLower().Contains(searchTerm)) ||
                    u.Username.ToLower().Contains(searchTerm) ||
                    u.Email.ToLower().Contains(searchTerm)
                ))
                .OrderBy(u => u.FirstName)
                .ThenBy(u => u.LastName)
                .Take(limit)
                .ToListAsync();
        }
    }
}

