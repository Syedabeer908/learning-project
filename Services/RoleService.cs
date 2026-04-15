using Microsoft.Extensions.Options;
using WebApplication1.Common.Exceptions;
using WebApplication1.Common.Results;
using WebApplication1.Common.Constants;
using WebApplication1.Entities;
using WebApplication1.DTOs.Role;
using WebApplication1.Interfaces;
using WebApplication1.Data.SoftDelete.Services;

namespace WebApplication1.Services
{
    public class RoleService
    {
        private readonly IRoleRepository _repo;
        private readonly IUserRepository _userRepo;
        private readonly RoleConstants _roleConstants;
        private readonly SoftDeleteService _softDeleteService;  

        public RoleService(IRoleRepository repo, IUserRepository userRepo,
            IOptions<RoleConstants> roleOptions, SoftDeleteService softDeleteService)
        {
            _repo = repo;
            _userRepo = userRepo;
            _roleConstants = roleOptions.Value;
            _softDeleteService = softDeleteService;
        }

        private RoleDto ToDto(Role role)
        {
            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.Name
            };
        }

        private Role ToEntity(Guid userId, CreateRoleDto dto)
        {
            return new Role
            {
                RoleId = Guid.NewGuid(),
                Name = dto.RoleName,
                CreatedBy = userId
            };
        }

        private void UpdateEntity(Role role, Guid userId, UpdateRoleDto dto)
        {
            role.Name = dto.RoleName;
            role.LastUpdatedAt = DateTime.UtcNow;
            role.LastUpdatedBy = userId;
        }

        private void PatchEntity(Role role, Guid userId, PatchRoleDto dto)
        {
            if (!string.IsNullOrEmpty(dto.RoleName))
                role.Name = dto.RoleName;
            role.LastUpdatedAt = DateTime.UtcNow;
            role.LastUpdatedBy = userId;
        }

        private async Task<Role> CheckRoleExistAndGet(Guid id)
        {
            var role = await _repo.GetByIdAsync(id);

            if (role == null)
                throw new NotFoundException($"Role with id {id} not found.");

            return role;
        }

        private async Task CheckRoleNameUnique(string roleName, Guid? excludeId = null)
        {
            var exists = await _repo.RoleNameExistsAsync(roleName, excludeId);
            if (exists)
                throw new BadRequestException($"Role Name '{roleName}' is already in use.");
        }

        private async Task CheckRoleInUse(Guid roleId)
        {
            var userWithRole = await _userRepo.GetByRoleIdAsync(roleId);
            if(userWithRole != null)
            {
                throw new BadRequestException($"Cannot delete role with id {roleId} because it is assigned to one or more users.");
            }
        }

        private async Task CheckRoleNotStartupRole(Role role)
        {
            if (role.Name.Equals(_roleConstants.Admin, StringComparison.OrdinalIgnoreCase) ||
                role.Name.Equals(_roleConstants.User, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException($"Cannot delete startup role '{role.Name}'.");
            }
            await CheckRoleInUse(role.RoleId);
        }

        public async Task<ResultT<List<RoleDto>>> GetAllAsync()
        {
            var roles = await _repo.GetAllAsync();
            var roleDtos = roles.Select(r => ToDto(r)).ToList();
            return ResultT<List<RoleDto>>.Success(roleDtos);
        }

        public async Task<ResultT<RoleDto>> GetByIdAsync(Guid id)
        {
            var role = await CheckRoleExistAndGet(id);
            return ResultT<RoleDto>.Success(ToDto(role));
        }

        public async Task<ResultT<RoleDto>> AddAsync(Guid userId, CreateRoleDto dto)
        {
            await CheckRoleNameUnique(dto.RoleName);
            var role = ToEntity(userId, dto);

            await _repo.AddAsync(role);

            return ResultT<RoleDto>.Success(ToDto(role));
        }

        public async Task<ResultT<RoleDto>> UpdateAsync(Guid id, Guid userId, UpdateRoleDto dto)
        {
            var role = await CheckRoleExistAndGet(id);

            await CheckRoleNameUnique(dto.RoleName, role.RoleId);

            UpdateEntity(role, userId, dto);

            await _repo.UpdateAsync(role);

            return ResultT<RoleDto>.Success(ToDto(role));
        }

        public async Task<Result> DeleteAsync(Guid id, Guid userId)
        {
            var role = await CheckRoleExistAndGet(id);

            await CheckRoleNotStartupRole(role);

            await _softDeleteService.DeleteAsync<Role>(id, userId);

            return Result.Success();
        }

        public async Task<Result> RestoreAsync(Guid id, Guid userId)
        {
            var role = await CheckRoleExistAndGet(id);
            await _softDeleteService.RestoreAsync<Role>(id, userId);
            return Result.Success();
        }

        public async Task<ResultT<RoleDto>> PatchAsync(Guid id, Guid userId, PatchRoleDto dto)
        {
            var role = await CheckRoleExistAndGet(id);

            if (!string.IsNullOrEmpty(dto.RoleName))
                await CheckRoleNameUnique(dto.RoleName, role.RoleId);

            PatchEntity(role, userId, dto);

            await _repo.UpdateAsync(role);
            return ResultT<RoleDto>.Success(ToDto(role));
        }
    }
}
