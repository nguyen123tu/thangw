using Microsoft.EntityFrameworkCore;
using System.Text.Json;
using MTU.Models.DTOs;
using MTU.Models.Entities;
using MTU.Repositories;

namespace MTU.Services
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly IWebHostEnvironment _environment;
        private readonly ILogger<AuthService> _logger;

        public AuthService(IUserRepository userRepository, IWebHostEnvironment environment, ILogger<AuthService> logger)
        {
            _userRepository = userRepository;
            _environment = environment;
            _logger = logger;
        }

        public async Task<AuthResult> RegisterAsync(RegisterDto dto)
        {
            try
            {
                var existingUser = await _userRepository.GetByEmailAsync(dto.Email);
                if (existingUser != null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Email này đã được đăng ký"
                    };
                }

                var randomPassword = GenerateRandomPassword();

                var passwordHash = BCrypt.Net.BCrypt.HashPassword(randomPassword);

                var username = dto.Email.Split('@')[0];

                var nameParts = username.Split('.');
                var firstName = nameParts.Length > 0 ? CapitalizeFirstLetter(nameParts[0]) : "User";
                var lastName = nameParts.Length > 1 ? CapitalizeFirstLetter(nameParts[^1]) : "";

                var user = new User
                {
                    Username = username,
                    Email = dto.Email,
                    PasswordHash = passwordHash,
                    FirstName = firstName,
                    LastName = lastName,
                    Avatar = "/assets/user.png", 
                    CreatedAt = DateTime.Now,
                    UpdatedAt = DateTime.Now,
                    IsActive = true
                };

                
                var createdUser = await _userRepository.CreateAsync(user);

                await SaveUserCredentialsToFileAsync(dto.Email, randomPassword);

                _logger.LogInformation("User registered successfully: {Email}. Generated password: {Password}", dto.Email, randomPassword);

                return new AuthResult
                {
                    Success = true,
                    User = createdUser,
                    ErrorMessage = $"Đã gửi mật khẩu đến mail {dto.Email}. Vui lòng kiểm tra để đăng nhập. (Mock: Mật khẩu của bạn là: {randomPassword})"
                };
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Database error during user registration");
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Không thể tạo tài khoản. Vui lòng thử lại."
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user registration");
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi không mong muốn. Vui lòng thử lại sau."
                };
            }
        }

        private async Task SaveUserCredentialsToFileAsync(string email, string password)
        {
            try
            {
                var userFolder = Path.Combine(_environment.WebRootPath, "user");
                if (!Directory.Exists(userFolder))
                {
                    Directory.CreateDirectory(userFolder);
                }

                var filePath = Path.Combine(userFolder, "tkmk.json");
                var credentials = new List<object>();

                if (File.Exists(filePath))
                {
                    var existingJson = await File.ReadAllTextAsync(filePath);
                    credentials = JsonSerializer.Deserialize<List<object>>(existingJson) ?? new List<object>();
                }

                credentials.Add(new { tk = email, mk = password, ngayTao = DateTime.Now.ToString("dd/MM/yyyy HH:mm:ss") });

                var json = JsonSerializer.Serialize(credentials, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(filePath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Lỗi khi lưu thông tin tài khoản vào file");
            }
        }

        private string GenerateRandomPassword()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            var length = random.Next(8, 13); 
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        private string CapitalizeFirstLetter(string text)
        {
            if (string.IsNullOrEmpty(text))
                return text;
            return char.ToUpper(text[0]) + text.Substring(1).ToLower();
        }

        public async Task<AuthResult> LoginAsync(LoginDto dto)
        {
            try
            {
                User? user = null;
                if (dto.UsernameOrEmail.Contains("@"))
                {
                    user = await _userRepository.GetByEmailAsync(dto.UsernameOrEmail);
                }
                else
                {
                    user = await _userRepository.GetByUsernameAsync(dto.UsernameOrEmail);
                }

                if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
                {
                    _logger.LogWarning("Failed login attempt for: {UsernameOrEmail}", dto.UsernameOrEmail);
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Invalid username/email or password"
                    };
                }

                if (!user.IsActive)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Your account has been deactivated"
                    };
                }

                _logger.LogInformation("User logged in successfully: {Username}", user.Username);

                return new AuthResult
                {
                    Success = true,
                    User = user
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during user login");
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "An unexpected error occurred. Please try again later."
                };
            }
        }

        public async Task<User?> GetUserByIdAsync(int userId)
        {
            try
            {
                return await _userRepository.GetByIdAsync(userId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user by ID: {UserId}", userId);
                return null;
            }
        }

        public async Task<List<User>> SearchUsersAsync(string query, int limit = 10)
        {
            try
            {
                return await _userRepository.SearchAsync(query, limit);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error searching users with query: {Query}", query);
                return new List<User>();
            }
        }

        public async Task<AuthResult> ChangePasswordAsync(int userId, string currentPassword, string newPassword)
        {
            try
            {
                var user = await _userRepository.GetByIdAsync(userId);
                if (user == null)
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Không tìm thấy người dùng"
                    };
                }

                if (!BCrypt.Net.BCrypt.Verify(currentPassword, user.PasswordHash))
                {
                    return new AuthResult
                    {
                        Success = false,
                        ErrorMessage = "Mật khẩu hiện tại không đúng"
                    };
                }

                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
                user.UpdatedAt = DateTime.Now;

                await _userRepository.UpdateAsync(user);

                _logger.LogInformation("User {UserId} changed password successfully", userId);

                return new AuthResult { Success = true };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error changing password for user {UserId}", userId);
                return new AuthResult
                {
                    Success = false,
                    ErrorMessage = "Đã xảy ra lỗi khi đổi mật khẩu"
                };
            }
        }
    }
}

