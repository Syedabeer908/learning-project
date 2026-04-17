using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using System.Text.Json;
using WebApplication1.Common.Exceptions;
using WebApplication1.Common.Results;
using WebApplication1.Common.Constants;
using WebApplication1.Data;
using WebApplication1.Entities;
using WebApplication1.DTOs.Admin;
using WebApplication1.Interfaces;

namespace WebApplication1.Services
{
    public class AdminService
    {
        private readonly IUserRepository _repo;
        private readonly ISoftRepository _softRepo;
        private readonly UserDomainService _userDomainService;
        private readonly RedisService _redis;
        private readonly RoleConstants _roleConstants;
        private readonly PasswordHasher<User> _hasher;

        public AdminService(IUserRepository repo, ISoftRepository softRepo,
            UserDomainService userDomainService, RedisService redis, IOptions<RoleConstants> roleOptions)
        {
            _repo = repo;
            _softRepo = softRepo;
            _userDomainService = userDomainService;
            _redis = redis;
            _roleConstants = roleOptions.Value;
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

        private User ToEntity(Guid userId, CreateAdminUserDto dto)
        {
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Username = dto.Username,
                Email = dto.Email,
                PasswordHash = dto.Password,
                RoleId = dto.RoleId,
                CreatedBy = userId
            };

            user.PasswordHash = _hasher.HashPassword(user, dto.Password);
            return user;
        }

        private void UpdateEntity(User user, Guid userId, UpdateAdminUserDto dto)
        {
            user.Username = dto.Username;
            user.Email = dto.Email;
            
            if (!string.IsNullOrWhiteSpace(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            user.LastUpdatedAt = DateTime.UtcNow;
            user.LastUpdatedBy = userId;
        }

        private void PatchEntity(User user, Guid userId, PatchAdminUserDto dto)
        {
            if (!string.IsNullOrEmpty(dto.Username))
                user.Username = dto.Username;

            if (!string.IsNullOrEmpty(dto.Email))
                user.Email = dto.Email;

            if (!string.IsNullOrEmpty(dto.Password))
                user.PasswordHash = _hasher.HashPassword(user, dto.Password);

            user.LastUpdatedAt = DateTime.UtcNow;
            user.LastUpdatedBy = userId;
        }

        private async Task CheckIsAdmin(Guid RoleId)
        {
            var role = await _userDomainService.GetRoleByIdAsync(RoleId);
            if (role != null && role.Name == _roleConstants.Admin)
                throw new BadRequestException("You are not allowed to update/delete this user.");
        }

        private Task<string> CreateValueForCacheAfterUserUpdate(User user)
        {
            var json = JsonSerializer.Serialize(new
            {
                IsActive = user.IsActive,
                IsDeleted = user.IsDeleted,
                TokenVersion = user.TokenVersion
            });
            return Task.FromResult(json);
        }

        private async Task CreateOrUpdateCacheAfterUserUpdate(Guid userId, User user)
        {
            var prefix = _roleConstants.User;
            var data = await CreateValueForCacheAfterUserUpdate(user);
            await _redis.SetAsync(prefix, userId, data, TimeSpan.FromHours(1));
            
        }
        private async Task RunSoftService(Guid id, bool action, Guid actionBy)
        {
            var soft = new Soft();
            var values = soft.SoftValuesSetter(action, DateTime.UtcNow, actionBy);
            await _softRepo.SoftUserAsync(id, values);
        }

        public async Task<ResultT<List<AdminUserDto>>> GetAllAsync(int pageSize, string? search,
            bool? isActive, Guid? lastId)
        {
            var users = await _repo.GetAllAsync(pageSize, search, isActive, lastId);
            var usersDtos = users.Select(u => ToDto(u)).ToList();
            return ResultT<List<AdminUserDto>>.Success(usersDtos);    
        }

        public async Task<ResultT<AdminUserDto?>> GetByIdAsync(Guid id)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            return ResultT<AdminUserDto?>.Success(ToDto(user));
        }

        public async Task<User?> GetByIdInEntityAsync(Guid id)
        {
            return await _repo.GetByIdAsync(id);
        }

        public async Task<ResultT<AdminUserDto>> AddAsync(Guid userId, CreateAdminUserDto dto)
        {
            await _userDomainService.CheckEmailUnique(dto.Email);

            var role = await _userDomainService.GetRoleByIdAsync(dto.RoleId);

            if (role == null)
                throw new NotFoundException($"RoleId '{dto.RoleId}' not found.");

            var user = ToEntity(userId, dto);

            await _repo.AddAsync(user);

            var data = await _userDomainService.CheckUserExistAndGet(user.UserId);

            return ResultT<AdminUserDto>.Success(ToDto(data));
        }

        public async Task<ResultT<AdminUserDto>> UpdateAsync(Guid id, Guid userId, UpdateAdminUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            UpdateEntity(user, userId, dto);
            
            await _repo.UpdateAsync(user);

            return ResultT<AdminUserDto>.Success(ToDto(user));
        }

        public async Task<ResultT<AdminUserDto>> PatchAsync(Guid id, Guid userId, PatchAdminUserDto dto)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            if (!string.IsNullOrEmpty(dto.Email))
                await _userDomainService.CheckEmailUnique(dto.Email, user.UserId);

            PatchEntity(user, userId, dto);

            await _repo.UpdateAsync(user);

            return ResultT<AdminUserDto>.Success(ToDto(user));
        }

        public async Task<Result> DeleteAsync(Guid id, Guid userId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await CheckIsAdmin(user.RoleId);
            user.IsDeleted = true;

            await RunSoftService(id, user.IsDeleted, userId);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
            return Result.Success();
        }

        public async Task<Result> RestoreAsync(Guid id, Guid userId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);

            await CheckIsAdmin(user.RoleId);
            user.IsDeleted = false;

            await RunSoftService(id, user.IsDeleted, userId);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
            return Result.Success();
        }

        public async Task<Result> DisableUserAsync(Guid id, Guid userId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            await CheckIsAdmin(user.RoleId);

            user.IsActive = false;
            user.LastUpdatedAt = DateTime.UtcNow;
            user.LastUpdatedBy = userId;  
            
            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
            return Result.Success();
        }

        public async Task<Result> EnableUserAsync(Guid id, Guid userId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            await CheckIsAdmin(user.RoleId);

            user.IsActive = true;
            user.LastUpdatedAt = DateTime.UtcNow;
            user.LastUpdatedBy = userId;

            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
            return Result.Success();
        }

        public async Task<Result> ForceUserLogoutAsync(Guid id, Guid userId)
        {
            var user = await _userDomainService.CheckUserExistAndGet(id);
            await CheckIsAdmin(user.RoleId);

            user.TokenVersion += 1;
            user.LastUpdatedAt = DateTime.UtcNow;
            user.LastUpdatedBy = userId;

            await _repo.UpdateAsync(user);
            await CreateOrUpdateCacheAfterUserUpdate(id, user);
            return Result.Success();
        }
    }
}
