using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SQLitePCL;
using System.Security.Claims;
using WebApplication1.DTOs.Admin;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize(Policy = "AdminOnly")]
    [ApiController]
    [Route("api/[controller]")]
    public class AdminController : ControllerBase
    {
        private readonly AdminService _service;

        public AdminController(AdminService service)
        {
            _service = service;
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllAsync( [FromQuery] string? search,
                [FromQuery] bool? isActive,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 1 )
        {
            var users = await _service.GetAllAsync(search, isActive, page, pageSize);
            return Ok(users);
        }

        [HttpGet("users/{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var user = await _service.GetByIdAsync(id);
            return Ok(user);
        }
        
        [HttpPost("users")]
        public async Task<IActionResult> AddAsync([FromBody] CreateAdminUserDto dto)
        {
            var createdUser = await _service.AddAsync(dto);
            return CreatedAtRoute(
                "GetUserById",
                new { id = createdUser.UserId },
                createdUser
            );
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateAdminUserDto dto)
        {
            var updatedUser = await _service.UpdateAsync(id, dto);
            return Ok(updatedUser);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "User deleted" });
        }

        [HttpPatch("users/{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchAdminUserDto dto)
        {
            var patchedUser = await _service.PatchAsync(id, dto);
            return Ok(patchedUser);
        }

        [HttpPatch("users/{id}/disable")]
        public async Task<IActionResult> DisableUserAsync(Guid id)
        {
            await _service.DisableUserAsync(id);
            return Ok(new { message = "User disabled" });
        }

        [HttpPatch("users/{id}/enable")]
        public async Task<IActionResult> EnableUserAsync(Guid id)
        {
            await _service.EnableUserAsync(id);
            return Ok(new { message = "User enabled" });
        }

        [HttpPatch("users/{id}/logout")]
        public async Task<IActionResult> ForceUserLogoutAsync(Guid id)
        {
            await _service.ForceUserLogoutAsync(id);
            return Ok(new { message = "User logged out" });
        }
    }
}
