using MTU.Models.DTOs;
using MTU.Models.Entities;

namespace MTU.Services
{
    public interface IProfileService
    {
        Task<ProfileDto?> GetProfileAsync(int userId, int? currentUserId = null);
        Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto updateDto);
        Task<string?> SaveUploadedFileAsync(IFormFile file, string folder);
        Task<bool> IsOwnProfileAsync(int profileUserId, int currentUserId);
        Task<User?> GetUserByIdAsync(int userId);
    }
}

