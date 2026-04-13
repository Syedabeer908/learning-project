using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication1.DTOs;
using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;
using WebApplication1.Settings;

namespace WebApplication1.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly JwtSettings _jwtsettings;
        private readonly RoleSettings _roleSettings;
        private readonly PasswordHasher<User> _hasher;

        public AuthService(IUserRepository userRepo, IRoleRepository roleRepo, IOptions<JwtSettings> jwtOptions, IOptions<RoleSettings> rolesettings)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _jwtsettings = jwtOptions.Value;
            _roleSettings = rolesettings.Value;
            _hasher = new PasswordHasher<User>();
        }

        private User ToEntity(AuthRegisterDto dto, Role ?role)
        {
            if (role == null)
                throw new Exception($"Role '{_roleSettings.User}' not found. Ensure it is seeded in the database.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                RoleId = role.RoleId
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            return user;
        }

        private AuthResponseDto ToDto(string token)
        {
            return new AuthResponseDto
            {
                Token = token
            };
        }

        private async Task CheckEmailUnique(string email, Guid? excludeId = null)
        {
            var exists = await _userRepo.EmailExistsAsync(email, excludeId);

            if (exists)
                throw new Exception($"Invalid Email '{email}'.");
        }

        private string GenerateToken(User user)
        {
            var secretKey = _jwtsettings.Key;

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role.Name),
                new Claim(ClaimTypes.Version, user.TokenVersion.ToString())
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                claims: claims,
                expires: DateTime.UtcNow.AddHours(_jwtsettings.ExpiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task ValidatePassword(User user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new Exception("Invalid username or password");
            }
            if (result == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _hasher.HashPassword(user, password);
                await _userRepo.UpdateAsync(user);
            }
        }

        public async Task<AuthResponseDto> RegisterAsync(AuthRegisterDto dto)
        {
            await CheckEmailUnique(dto.Email);
            var role = await _roleRepo.GetByNameAsync(_roleSettings.User);

            var user = ToEntity(dto, role);

            await _userRepo.AddAsync(user);

            var token = GenerateToken(user);

            return ToDto(token);
        }

        public async Task<AuthResponseDto> LoginAsync(AuthLoginDto authLoginDto)
        {
            var email = authLoginDto.Email;
            var password = authLoginDto.Password;

            var user = await _userRepo.GetByEmailAsync(email);

            if (user == null)
                throw new Exception("Invalid email or password");

            await ValidatePassword(user, password);

            var token = GenerateToken(user);

            return ToDto(token);
        }
    }
}
