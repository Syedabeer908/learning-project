using Microsoft.AspNetCore.Identity;
using WebApplication1.Common.Exceptions;
using WebApplication1.DTOs;
using WebApplication1.Entities;

namespace WebApplication1.Mappers
{
    public class AuthMapper
    {
        private readonly PasswordHasher<User> _hasher;
        public AuthMapper()
        {
            _hasher = new PasswordHasher<User>();
        }

        public User ToEntity(AuthRegisterDto dto, Role? role)
        {
            if (role == null)
                throw new NotFoundException($"Role '{role?.Name}' not found.");

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                RoleId = role.RoleId,
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            return user;
        }

        public RefreshToken ToRefreshTokenEntity(string token, Guid userId)
        {
            var refreshToken = new RefreshToken
            {
                RefreshTokenId = Guid.NewGuid(),
                Token = token,
                ExpiryDate = DateTime.UtcNow.AddDays(7),
                UserId = userId,
            };
            return refreshToken;
        }

        public AuthResponseDto ToDto(string token, string refreshToken)
        {
            return new AuthResponseDto
            {
                Token = token,
                RefreshToken = refreshToken
            };
        }
    }
}
