using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.Common.Extensions;
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
                [FromQuery] Guid? lastId,
                [FromQuery] int page = 1,
                [FromQuery] int pageSize = 10 )
        {
            var result = await _service.GetAllAsync(search, isActive, lastId, page, pageSize);
            return Ok(result);
        }

        [HttpGet("users/{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var result = await _service.GetByIdAsync(id);
            return Ok(result);
        }
        
        [HttpPost("users")]
        public async Task<IActionResult> AddAsync([FromBody] CreateAdminUserDto dto)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.AddAsync(userId, dto);
            if (result.Result == null)
                return BadRequest(result);

            return CreatedAtRoute(
                "GetUserById",
                new { id = result.Result.UserId },
                result
            );
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateAdminUserDto dto)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.UpdateAsync(id, userId, dto);
            return Ok(result);
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.DeleteAsync(id, userId);
            return Ok(result);
        }

        [HttpPatch("users/{id}/restore")]
        public async Task<IActionResult> RestoreAsync(Guid id)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.RestoreAsync(id, userId);
            return Ok(result);
        }

        [HttpPatch("users/{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchAdminUserDto dto)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.PatchAsync(id, userId, dto);
            return Ok(result);
        }

        [HttpPatch("users/{id}/disable")]
        public async Task<IActionResult> DisableUserAsync(Guid id)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.DisableUserAsync(id, userId);
            return Ok(result);
        }

        [HttpPatch("users/{id}/enable")]
        public async Task<IActionResult> EnableUserAsync(Guid id)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.EnableUserAsync(id, userId);
            return Ok(result);
        }

        [HttpPatch("users/{id}/logout")]
        public async Task<IActionResult> ForceUserLogoutAsync(Guid id)
        {
            var userId = HttpContext.GetUserId();
            var result = await _service.ForceUserLogoutAsync(id, userId);
            return Ok(result);
        }
    }
}
