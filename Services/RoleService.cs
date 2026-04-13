using Microsoft.Extensions.Options;
using WebApplication1.DTOs.Role;
using WebApplication1.Entities;
using WebApplication1.Repository.Interfaces;
using WebApplication1.Settings;
using WebApplication1.Exceptions;

namespace WebApplication1.Services
{
    public class RoleService
    {
        private readonly IRoleRepository _repo;
        private readonly IUserRepository _userRepo;
        private readonly RoleSettings _roleSettings;

        public RoleService(IRoleRepository repo, IUserRepository userRepo, IOptions<RoleSettings> roleOptions)
        {
            _repo = repo;
            _userRepo = userRepo;
            _roleSettings = roleOptions.Value;
        }

        private RoleDto ToDto(Role role)
        {
            return new RoleDto
            {
                RoleId = role.RoleId,
                RoleName = role.Name
            };
        }

        private Role ToEntity(CreateRoleDto dto)
        {
            return new Role
            {
                RoleId = Guid.NewGuid(),
                Name = dto.RoleName
            };
        }

        private void UpdateEntity(Role role, UpdateRoleDto dto)
        {
            role.Name = dto.RoleName;
        }

        private void PatchEntity(Role role, PatchRoleDto dto)
        {
            if (!string.IsNullOrEmpty(dto.RoleName))
                role.Name = dto.RoleName;
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
            if (role.Name.Equals(_roleSettings.Admin, StringComparison.OrdinalIgnoreCase) ||
                role.Name.Equals(_roleSettings.User, StringComparison.OrdinalIgnoreCase))
            {
                throw new BadRequestException($"Cannot delete startup role '{role.Name}'.");
            }
            await CheckRoleInUse(role.RoleId);
        }

        public async Task<List<RoleDto>> GetAllAsync()
        {
            var roles = await _repo.GetAllAsync();
            return roles.Select(r => ToDto(r)).ToList();
        }

        public async Task<RoleDto> GetByIdAsync(Guid id)
        {
            var role = await CheckRoleExistAndGet(id);
            return ToDto(role);
        }

        public async Task<RoleDto> AddAsync(CreateRoleDto dto)
        {
            await CheckRoleNameUnique(dto.RoleName);
            var role = ToEntity(dto);

            await _repo.AddAsync(role);

            return ToDto(role);
        }

        public async Task<RoleDto> UpdateAsync(Guid id, UpdateRoleDto dto)
        {
            var role = await CheckRoleExistAndGet(id);

            await CheckRoleNameUnique(dto.RoleName, role.RoleId);

            UpdateEntity(role, dto);

            await _repo.UpdateAsync(role);

            return ToDto(role);
        }

        public async Task DeleteAsync(Guid id)
        {
            var role = await CheckRoleExistAndGet(id);

            await CheckRoleNotStartupRole(role);

            await _repo.DeleteAsync(role);
        }

        public async Task<RoleDto> PatchAsync(Guid id, PatchRoleDto dto)
        {
            var role = await CheckRoleExistAndGet(id);

            if (!string.IsNullOrEmpty(dto.RoleName))
                await CheckRoleNameUnique(dto.RoleName, role.RoleId);

            PatchEntity(role, dto);

            await _repo.UpdateAsync(role);
            return ToDto(role);
        }
    }
}
