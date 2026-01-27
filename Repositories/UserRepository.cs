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
            var friendIds = await _context.Friendships
                .Where(f => (f.UserId == currentUserId || f.FriendId == currentUserId) 
                            && (f.Status == "accepted" || f.Status == "pending"))
                .Select(f => f.UserId == currentUserId ? f.FriendId : f.UserId)
                .ToListAsync();            var suggestions = await _context.Users
                .Where(u => u.UserId != currentUserId 
                            && !friendIds.Contains(u.UserId)
                            && u.IsActive)
                .OrderByDescending(u => u.CreatedAt)
                .Take(limit)
                .ToListAsync();
            
            return suggestions;
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

