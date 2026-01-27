using MTU.Models.DTOs;
using MTU.Repositories;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace MTU.Services
{
    public class ProfileUpdateService : IProfileUpdateService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly ILogger<ProfileUpdateService> _logger;

        public ProfileUpdateService(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            ILogger<ProfileUpdateService> logger)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _logger = logger;
        }

        public async Task<UpdateProfileResult> UpdateBasicInfoAsync(UpdateProfileDto dto, int userId)
        {
            try
            {                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new UpdateProfileResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy người dùng"
                    };
                }                if (dto.FirstName != null)
                    user.FirstName = dto.FirstName;
                
                if (dto.LastName != null)
                    user.LastName = dto.LastName;
                
                if (dto.Bio != null)
                    user.Bio = dto.Bio;
                
                if (dto.Location != null)
                    user.Location = dto.Location;
                
                if (dto.Interests != null)
                    user.Interests = dto.Interests;                await _userRepository.UpdateAsync(user);                var student = await _studentRepository.GetByUserIdAsync(userId);
                _logger.LogInformation("Student record for user {UserId}: {StudentExists}", userId, student != null);                bool needsStudentUpdate = dto.PlaceOfBirth != null || dto.Gender != null || 
                                          dto.DateOfBirth.HasValue || dto.Faculty != null || dto.AcademicYear != null;
                
                if (needsStudentUpdate)
                {                    if (student == null)
                    {
                        _logger.LogInformation("Creating new student record for user {UserId}", userId);
                        student = await _studentRepository.CreateForUserAsync(userId);
                    }
                    
                    if (student != null)
                    {
                        _logger.LogInformation("Updating student - Gender: {Gender}, DOB: {DOB}, Faculty: {Faculty}, AcademicYear: {AcademicYear}", 
                            dto.Gender, dto.DateOfBirth, dto.Faculty, dto.AcademicYear);
                        
                        if (dto.PlaceOfBirth != null)
                            student.PlaceOfBirth = dto.PlaceOfBirth;                        if (dto.Gender != null)
                            student.Gender = string.IsNullOrEmpty(dto.Gender) ? null : dto.Gender;                        if (dto.DateOfBirth.HasValue)
                            student.DateOfBirth = dto.DateOfBirth.Value;                        if (dto.Faculty != null)
                            student.Faculty = string.IsNullOrEmpty(dto.Faculty) ? null : dto.Faculty;                        if (dto.AcademicYear != null)
                        {
                            if (string.IsNullOrEmpty(dto.AcademicYear))
                            {                                student.Class = null;
                            }
                            else
                            {                                if (!string.IsNullOrEmpty(student.Class) && student.Class.Length > 4)
                                {                                    student.Class = dto.AcademicYear + student.Class.Substring(4);
                                }
                                else
                                {
                                    student.Class = dto.AcademicYear;
                                }
                            }
                        }

                        await _studentRepository.UpdateAsync(student);
                        _logger.LogInformation("Student updated - Class: {Class}, Faculty: {Faculty}", student.Class, student.Faculty);
                    }
                }

                _logger.LogInformation("User {UserId} updated profile successfully", userId);

                return new UpdateProfileResult
                {
                    Success = true
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to update profile for user {UserId}", userId);
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "Không thể cập nhật thông tin. Vui lòng thử lại"
                };
            }
        }

        public async Task<UpdateProfileResult> UpdateAvatarAsync(IFormFile file, int userId)
        {            if (file == null || file.Length == 0)
            {
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "Vui lòng chọn một file ảnh"
                };
            }

            if (!await ValidateImageFileAsync(file))
            {
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "File không hợp lệ. Chỉ chấp nhận jpg, jpeg, png, gif và kích thước tối đa 5MB"
                };
            }

            try
            {                var filePath = await _userRepository.SaveAvatarAsync(file, userId);                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new UpdateProfileResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy người dùng"
                    };
                }

                user.Avatar = filePath;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {UserId} updated avatar successfully", userId);

                return new UpdateProfileResult
                {
                    Success = true,
                    UpdatedValue = filePath
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid avatar file for user {UserId}", userId);
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "File không hợp lệ. Chỉ chấp nhận jpg, jpeg, png, gif và kích thước tối đa 5MB"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save avatar for user {UserId}", userId);
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "Không thể lưu ảnh. Vui lòng thử lại"
                };
            }
        }

        public async Task<UpdateProfileResult> UpdateCoverImageAsync(IFormFile file, int userId)
        {            if (file == null || file.Length == 0)
            {
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "Vui lòng chọn một file ảnh"
                };
            }

            if (!await ValidateImageFileAsync(file))
            {
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "File không hợp lệ. Chỉ chấp nhận jpg, jpeg, png, gif và kích thước tối đa 5MB"
                };
            }

            try
            {                var filePath = await _userRepository.SaveCoverImageAsync(file, userId);                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new UpdateProfileResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy người dùng"
                    };
                }

                user.CoverImage = filePath;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {UserId} updated cover image successfully", userId);

                return new UpdateProfileResult
                {
                    Success = true,
                    UpdatedValue = filePath
                };
            }
            catch (ArgumentException ex)
            {
                _logger.LogWarning(ex, "Invalid cover image file for user {UserId}", userId);
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "File không hợp lệ. Chỉ chấp nhận jpg, jpeg, png, gif và kích thước tối đa 5MB"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to save cover image for user {UserId}", userId);
                return new UpdateProfileResult
                {
                    Success = false,
                    ErrorMessage = "Không thể lưu ảnh. Vui lòng thử lại"
                };
            }
        }

        public async Task<bool> ValidateImageFileAsync(IFormFile file)
        {            if (file.Length > 5 * 1024 * 1024)
                return false;            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();

            return await Task.FromResult(allowedExtensions.Contains(extension));
        }

        public async Task MarkProfileAsCompletedAsync(int userId)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user != null)
                {
                    user.IsProfileCompleted = true;
                    user.UpdatedAt = DateTime.Now;
                    await _userRepository.UpdateAsync(user);
                    
                    _logger.LogInformation("User {UserId} profile marked as completed", userId);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to mark profile as completed for user {UserId}", userId);
                throw;
            }
        }
    }
}

