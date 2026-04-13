using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebApplication1.DTOs.Admin;
using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;
using WebApplication1.Settings;

namespace WebApplication1.Services
{
    public class AdminService
    {
        private readonly IUserRepository _repo;
        private readonly IRoleRepository _roleRepo;
        private readonly UserDomainService _userDomainService;
        private readonly RedisService _redis;
        private readonly RoleSettings _roleSettings;
        private readonly PasswordHasher<User> _hasher;

        public AdminService(IUserRepository repo, IRoleRepository roleRepo, UserDomainService userDomainService, RedisService redis, IOptions<RoleSettings> roleOptions)
        {
            _repo = repo;
            _roleRepo = roleRepo;
            _userDomainService = userDomainService;
            _redis = redis;
            _roleSettings = roleOptions.Value;
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

        private User ToEntity(CreateAdminUserDto dto)
        {

            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password,
                RoleId = dto.RoleId
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

            user.RoleId = dto.RoleId;
        }

        private void PatchEntity(User user, PatchAdminUserDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Username))
                user.Username = dto.Username;

            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            if (dto.RoleId.HasValue && dto.RoleId.Value != Guid.Empty)
                user.RoleId = dto.RoleId.Value;
        }

        private async Task CheckRoleExist(Guid roleId)
        {
            var role = await _roleRepo.GetByIdAsync(roleId);
            if (role == null)
                throw new Exception($"Role with id {roleId} not found.");
        }

        private async Task CheckLastAdmin(User user)
        {
            var adminCount = await _repo.CountAsync(_roleSettings.Admin);
            if (adminCount <= 1)
                throw new Exception("Cannot delete the last admin.");
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
            var prefix = _roleSettings.User;
            var data = await CreateValueForCacheAfterUserUpdate(user);
            await _redis.SetAsync(prefix, userId, data, TimeSpan.FromHours(1));
            
        }

        public async Task<List<AdminUserDto>> GetAllAsync()
        {
            var users = await _repo.GetAllAsync();
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

            await CheckRoleExist(dto.RoleId);

            var user = ToEntity(dto);

            await _repo.AddAsync(user);

            return ToDto(user);
        }

        public async Task<AdminUserDto> UpdateAsync(Guid id, UpdateAdminUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            await CheckRoleExist(dto.RoleId);

            UpdateEntity(user, dto);

            await _repo.UpdateAsync(user);

            return ToDto(user);
        }

        public async Task<AdminUserDto> PatchAsync(Guid id, PatchAdminUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            if (!string.IsNullOrEmpty(dto.Email))
                await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            if (dto.RoleId.HasValue && dto.RoleId.Value != Guid.Empty)
                await CheckRoleExist(dto.RoleId.Value);

            PatchEntity(user, dto);

            await _repo.UpdateAsync(user);

            return ToDto(user);
        }

        public async Task DeleteAsync(Guid targetUserId, Guid currentUserId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(targetUserId, currentUserId);

            await CheckLastAdmin(user);
            await _repo.DeleteAsync(user);

            var prefix = _roleSettings.User;
            await _redis.RemoveAsync(prefix, targetUserId);
        }

        public async Task DisableUserAsync(Guid targetUserId, Guid currentUserId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(targetUserId, currentUserId);

            user.IsActive = false;
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(targetUserId, user);
        }

        public async Task EnableUserAsync(Guid targetUserId, Guid currentUserId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(targetUserId, currentUserId);

            user.IsActive = true;
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(targetUserId, user);
        }

        public async Task ForceUserLogoutAsync(Guid targetUserId, Guid currentUserId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(targetUserId, currentUserId);

            user.TokenVersion += 1;
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(targetUserId, user);
        }
    }
}
