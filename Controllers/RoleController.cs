using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Options;
using WebApplication1.DTOs.Role;
using WebApplication1.Services;
using WebApplication1.Settings;

namespace WebApplication1.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class RoleController : ControllerBase
    {
        private readonly RoleService _service;

        public RoleController(RoleService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var roles = await _service.GetAllAsync();
            return Ok(roles);
        }

        [HttpGet("{id}", Name = "GetRoleById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var role = await _service.GetByIdAsync(id);
            return Ok(role);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] CreateRoleDto dto)
        {
            var createdRole = await _service.AddAsync(dto);
            return CreatedAtRoute("GetRoleById", new { id = createdRole.RoleId }, createdRole);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateRoleDto dto)
        {
            var updatedRole = await _service.UpdateAsync(id, dto);
            return Ok(updatedRole);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Role deleted" });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchRoleDto dto)
        {
            var PatchRole = await _service.PatchAsync(id, dto);
            return Ok(PatchRole);
        }
    }
}
