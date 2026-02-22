using MTU.Models.Entities;
using Microsoft.AspNetCore.Http;

namespace MTU.Repositories
{
    public interface IUserRepository
    {
        Task<User?> GetByIdAsync(int userId);
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<List<User>> GetAllAsync();
        Task<User> CreateAsync(User user);
        Task<User> UpdateAsync(User user);
        Task<bool> ExistsAsync(string username, string email);
        Task<List<User>> GetSuggestedFriendsAsync(int currentUserId, int limit);
        Task<int> GetMutualFriendCountAsync(int currentUserId, int targetUserId);
        Task<string> SaveAvatarAsync(IFormFile file, int userId);
        Task<string> SaveCoverImageAsync(IFormFile file, int userId);
        Task<List<User>> SearchAsync(string query, int limit = 10);
    }
}

