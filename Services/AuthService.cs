using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using WebApplication1.Common.Exceptions;
using WebApplication1.Entities;
using WebApplication1.DTOs;
using WebApplication1.Interfaces;
using WebApplication1.Configuration;

namespace WebApplication1.Services
{
    public class AuthService
    {
        private readonly IUserRepository _userRepo;
        private readonly IRoleRepository _roleRepo;
        private readonly JwtConfig _jwtConfig;
        private readonly RoleConfig _roleConfig;
        private readonly PasswordHasher<User> _hasher;

        public AuthService(IUserRepository userRepo, IRoleRepository roleRepo, IOptions<JwtConfig> jwtOptions, IOptions<RoleConfig> rolesettings)
        {
            _userRepo = userRepo;
            _roleRepo = roleRepo;
            _jwtConfig = jwtOptions.Value;
            _roleConfig = rolesettings.Value;
            _hasher = new PasswordHasher<User>();
        }

        private User ToEntity(AuthRegisterDto dto, Role ?role)
        {
            if (role == null)
                throw new NotFoundException($"Role '{_roleConfig.User}' not found.");

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
                throw new BadRequestException($"Invalid Email '{email}'.");
        }

        private string GenerateToken(User user)
        {
            var secretKey = _jwtConfig.Key;

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
                expires: DateTime.UtcNow.AddHours(_jwtConfig.ExpiryHours),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private async Task ValidatePassword(User user, string password)
        {
            var result = _hasher.VerifyHashedPassword(user, user.PasswordHash, password);
            if (result == PasswordVerificationResult.Failed)
            {
                throw new BadRequestException("Invalid email or password");
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
            var role = await _roleRepo.GetByNameAsync(_roleConfig.User);

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
                throw new NotFoundException("Invalid email or password");

            await ValidatePassword(user, password);

            var token = GenerateToken(user);

            return ToDto(token);
        }
    }
}
