using MTU.Models.DTOs;
using MTU.Models.Entities;

namespace MTU.Services
{
    public interface IAuthService
    {
        Task<AuthResult> RegisterAsync(RegisterDto dto);
        Task<AuthResult> LoginAsync(LoginDto dto);
        Task<User?> GetUserByIdAsync(int userId);
        Task<List<User>> SearchUsersAsync(string query, int limit = 10);
        Task<AuthResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword);
    }
}

