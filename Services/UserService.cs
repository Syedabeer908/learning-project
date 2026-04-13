using Microsoft.AspNetCore.Identity;
using WebApplication1.DTOs.User;
using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;

namespace WebApplication1.Services
{
    public class UserService
    {
        private readonly IUserRepository _repo;
        private readonly UserDomainService _userDomainService;
        private readonly PasswordHasher<User> _hasher;

        public UserService(IUserRepository repo, UserDomainService userDomainService)
        {
            _repo = repo;
            _userDomainService = userDomainService;
            _hasher = new PasswordHasher<User>();
        }

        private UserDto ToDto(User user)
        {
            return new UserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email
            };
        }

        private void UpdateEntity(User user, UpdateUserDto dto)
        {
            user.Username = dto.Username;
            user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        private void PatchEntity(User user, PatchUserDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Username))
                user.Username = dto.Username;
            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;
            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        public async Task<UserDto> UpdateAsync(Guid id, UpdateUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            UpdateEntity(user, dto);

            await _repo.UpdateAsync(user);

            return ToDto(user);
        }

        public async Task<UserDto> PatchAsync(Guid id, PatchUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            if (!string.IsNullOrEmpty(dto.Email))
                await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            PatchEntity(user, dto);

            await _repo.UpdateAsync(user);

            return ToDto(user);
        }
    }
}
