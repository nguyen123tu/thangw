using MTU.Models.DTOs;
using Microsoft.AspNetCore.Http;

namespace MTU.Services
{
    public interface IProfileUpdateService
    {
        Task<UpdateProfileResult> UpdateBasicInfoAsync(UpdateProfileDto dto, int userId);
        Task<UpdateProfileResult> UpdateAvatarAsync(IFormFile file, int userId);
        Task<UpdateProfileResult> UpdateCoverImageAsync(IFormFile file, int userId);
        Task<bool> ValidateImageFileAsync(IFormFile file);
        Task MarkProfileAsCompletedAsync(int userId);
    }
}

