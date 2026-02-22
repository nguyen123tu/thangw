using Microsoft.Extensions.Logging;
using MTU.Models.DTOs;
using MTU.Models.Entities;
using MTU.Repositories;

namespace MTU.Services
{
    public class ProfileService : IProfileService
    {
        private readonly IUserRepository _userRepository;
        private readonly IStudentRepository _studentRepository;
        private readonly IPostService _postService;
        private readonly IFriendshipService _friendshipService;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(
            IUserRepository userRepository,
            IStudentRepository studentRepository,
            IPostService postService,
            IFriendshipService friendshipService,
            IWebHostEnvironment environment,
            ILogger<ProfileService> logger)
        {
            _userRepository = userRepository;
            _studentRepository = studentRepository;
            _postService = postService;
            _friendshipService = friendshipService;
            _environment = environment;
            _logger = logger;
        }

        public async Task<ProfileDto?> GetProfileAsync(int userId, int? currentUserId = null)
        {
            try
            {                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return null;
                }                StudentDto? studentDto = null;
                var student = await _studentRepository.GetByUserIdAsync(userId);
                if (student != null && student.IsLinked)
                {
                    studentDto = new StudentDto
                    {
                        MSSV         = student.MSSV ?? string.Empty,
                        Class        = student.Class ?? string.Empty,
                        Faculty      = student.Faculty ?? string.Empty,
                        GPA          = student.GPA,
                        DateOfBirth  = student.DateOfBirth,
                        PlaceOfBirth = student.PlaceOfBirth,
                        Gender       = student.Gender,
                        Course       = student.Course,
                        TotalCredits = student.TotalCredits
                    };
                }                var posts = await _postService.GetUserPostsAsync(userId, currentUserId);                var fullName = $"{user.FirstName} {user.LastName}".Trim();
                if (string.IsNullOrWhiteSpace(fullName))
                {
                    fullName = user.Username;
                }                var friends = await _friendshipService.GetFriendsAsync(userId);
                var friendsToShow = friends.Take(6).ToList();                var suggestedFriends = new List<FriendDto>();
                if (currentUserId.HasValue && currentUserId.Value == userId)
                {
                    suggestedFriends = await _friendshipService.GetFriendSuggestionsAsync(userId);
                }                string? friendshipStatus = null;
                bool isOwnProfile = currentUserId.HasValue && currentUserId.Value == userId;
                
                if (!isOwnProfile && currentUserId.HasValue)
                {                    var areFriends = friends.Any(f => f.UserId == currentUserId.Value);
                    if (areFriends)
                    {
                        friendshipStatus = "accepted";
                    }
                    else
                    {                        var pendingRequests = await _friendshipService.GetPendingRequestsAsync(userId);
                        var hasPendingRequest = pendingRequests.Any(r => r.RequesterId == currentUserId.Value);
                        if (hasPendingRequest)
                        {
                            friendshipStatus = "pending";
                        }
                    }
                }                var friendCount = await _friendshipService.GetFriendCountAsync(userId);                var profileDto = new ProfileDto
                {
                    UserId = user.UserId,
                    FullName = fullName,
                    Avatar = user.Avatar ?? "/assets/user.png",
                    CoverImage = user.CoverImage ?? "/assets/bg-makima.png",
                    Bio = user.Bio,
                    Location = user.Location,
                    Interests = user.Interests,
                    Student = studentDto,
                    Posts = posts,
                    Friends = friendsToShow,
                    SuggestedFriends = suggestedFriends,
                    IsOwnProfile = isOwnProfile,
                    FriendshipStatus = friendshipStatus,
                    FriendCount = friendCount
                };

                return profileDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving profile for user {UserId}", userId);
                return null;
            }
        }

        public async Task<bool> UpdateProfileAsync(int userId, UpdateProfileDto updateDto)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    _logger.LogWarning("User with ID {UserId} not found", userId);
                    return false;
                }                if (!string.IsNullOrWhiteSpace(updateDto.FirstName))
                    user.FirstName = updateDto.FirstName;
                
                if (!string.IsNullOrWhiteSpace(updateDto.LastName))
                    user.LastName = updateDto.LastName;
                
                user.Bio = updateDto.Bio;
                user.Location = updateDto.Location;
                user.Interests = updateDto.Interests;                if (updateDto.AvatarFile != null)
                {
                    var avatarPath = await SaveUploadedFileAsync(updateDto.AvatarFile, "avatars");
                    if (avatarPath != null)
                    {
                        user.Avatar = avatarPath;
                    }
                }                if (updateDto.CoverImageFile != null)
                {
                    var coverPath = await SaveUploadedFileAsync(updateDto.CoverImageFile, "covers");
                    if (coverPath != null)
                    {
                        user.CoverImage = coverPath;
                    }
                }

                user.UpdatedAt = DateTime.Now;
                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("Profile updated successfully for user {UserId}", userId);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error updating profile for user {UserId}", userId);
                return false;
            }
        }

        public async Task<string?> SaveUploadedFileAsync(IFormFile file, string folder)
        {
            try
            {
                if (file == null || file.Length == 0)
                    return null;                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!allowedExtensions.Contains(extension))
                {
                    _logger.LogWarning("Invalid file extension: {Extension}", extension);
                    return null;
                }                var fileName = $"{Guid.NewGuid()}{extension}";
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", folder);                Directory.CreateDirectory(uploadsFolder);

                var filePath = Path.Combine(uploadsFolder, fileName);                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    await file.CopyToAsync(stream);
                }                return $"/uploads/{folder}/{fileName}";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error saving uploaded file");
                return null;
            }
        }

        public async Task<bool> IsOwnProfileAsync(int profileUserId, int currentUserId)
        {            return await Task.FromResult(profileUserId == currentUserId);
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _userRepository.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting user by ID: {UserId}", userId);
                return null;
            }
        }
    }
}

