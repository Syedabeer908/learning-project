using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using StackExchange.Redis;
using System.Text.Json;
using WebApplication1.Exceptions;
using WebApplication1.Entities;
using WebApplication1.DTOs.Admin;
using WebApplication1.Interfaces;
using WebApplication1.Configuration;

namespace WebApplication1.Services
{
    public class AdminService
    {
        private readonly IUserRepository _repo;
        
        private readonly UserDomainService _userDomainService;
        private readonly RedisService _redis;
        private readonly RoleConfig _roleConfig;
        private readonly PasswordHasher<User> _hasher;

        public AdminService(IUserRepository repo, UserDomainService userDomainService, RedisService redis, IOptions<RoleConfig> roleOptions)
        {
            _repo = repo;
            _userDomainService = userDomainService;
            _redis = redis;
            _roleConfig = roleOptions.Value;
            _hasher = new PasswordHasher<User>();
        }

        private AdminUserDto ToDto(User user)
        {
            return new AdminUserDto
            {
                UserId = user.UserId,
                Username = user.Username,
                Email = user.Email,
                RoleId = user.RoleId,
                RoleName = user.Role.Name,
                IsActive = user.IsActive
            };
        }

        private User ToEntity(CreateAdminUserDto dto, Guid roleId)
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password,
                RoleId = roleId
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            return user;
        }

        private void UpdateEntity(User user, UpdateAdminUserDto dto)
        {
            user.Username = dto.Username;
            user.Email = dto.Email;

            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        private void PatchEntity(User user, PatchAdminUserDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Username))
                user.Username = dto.Username;

            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);
        }

        private async Task CheckIsAdmin(Guid RoleId)
        {
            var role = await _userDomainService.GetRoleByIdAsync(RoleId);
            if (role != null && role.Name == _roleConfig.Admin)
                throw new BadRequestException("You are not allowed to update/delete this user.");
        }

        private Task<string> CreateValueForCacheAfterUserUpdate(User user)
        {
            var json = JsonSerializer.Serialize(new
            {
                IsActive = user.IsActive,
                TokenVersion = user.TokenVersion
            });
            return Task.FromResult(json);
        }

        private async Task  CreateOrUpdateCacheAfterUserUpdate(Guid userId, User user)
        {
            var prefix = _roleConfig.User;
            var data = await CreateValueForCacheAfterUserUpdate(user);
            await _redis.SetAsync(prefix, userId, data, TimeSpan.FromHours(1));
            
        }

        public async Task<List<AdminUserDto>> GetAllAsync(string? search, bool? isActive, int page, int pageSize)
        {
            var users = await _repo.GetAllAsync(search, isActive, page, pageSize);
            return users.Select(u => ToDto(u)).ToList();
        }

        public async Task<AdminUserDto?> GetByIdAsync(Guid id)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            return ToDto(user);
        }

        public async Task<User?> GetByIdInEntityAsync(Guid id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<AdminUserDto> AddAsync(CreateAdminUserDto dto)
        {
            await _userDomainService.CheckEmailUnique(dto.Email);

            var role = await _userDomainService.GetRoleByNameAsync(_roleConfig.Admin);

            if (role == null)
                throw new NotFoundException($"Role '{_roleConfig.Admin}' not found.");

            var user = ToEntity(dto, role.RoleId);

            await _repo.AddAsync(user);

            return ToDto(user);
        }

        public async Task<AdminUserDto> UpdateAsync(Guid id, UpdateAdminUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            UpdateEntity(user, dto);

            await _repo.UpdateAsync(user);

            return ToDto(user);
        }

        public async Task<AdminUserDto> PatchAsync(Guid id, PatchAdminUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            if (!string.IsNullOrEmpty(dto.Email))
                await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            PatchEntity(user, dto);

            await _repo.UpdateAsync(user);

            return ToDto(user);
        }

        public async Task DeleteAsync(Guid id)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await CheckIsAdmin(user.RoleId);
            await _repo.DeleteAsync(user);
            var prefix = _roleConfig.User;
            await _redis.RemoveAsync(prefix, id);
        }

        public async Task DisableUserAsync(Guid id)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            await CheckIsAdmin(user.RoleId);
            user.IsActive = false;
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
        }

        public async Task EnableUserAsync(Guid id)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            await CheckIsAdmin(user.RoleId);
            user.IsActive = true;
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
        }

        public async Task ForceUserLogoutAsync(Guid id)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            await CheckIsAdmin(user.RoleId);
            user.TokenVersion += 1;
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
        }
    }
}
