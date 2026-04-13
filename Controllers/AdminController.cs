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

        private Task<Guid> GetCurrentUserIdAsync()
        {
            var userid = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUserId = userid != null ? Guid.Parse(userid) : Guid.Empty;
            return Task.FromResult(currentUserId);
        }

        [HttpGet("users")]
        public async Task<IActionResult> GetAllAsync()
        {
            var users = await _service.GetAllAsync();
            return Ok(users);
        }

        [HttpGet("users/{id}", Name = "GetUserById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            try
            {
                var user = await _service.GetByIdAsync(id);
                return Ok(user);
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }
        
        [HttpPost("users")]
        public async Task<IActionResult> AddAsync([FromBody] CreateAdminUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var createdUser = await _service.AddAsync(dto);

                return CreatedAtRoute(
                    "GetUserById",
                    new { id = createdUser.UserId },
                    createdUser
                );
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut("users/{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateAdminUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var updatedUser = await _service.UpdateAsync(id, dto);
                return Ok(updatedUser);
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("users/{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                await _service.DeleteAsync(id, currentUserId);
                return NoContent();
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPatch("users/{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchAdminUserDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                var patchedUser = await _service.PatchAsync(id, dto);
                return Ok(patchedUser);
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPatch("users/{id}/disable")]
        public async Task<IActionResult> DisableUserAsync(Guid id)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                await _service.DisableUserAsync(id, currentUserId);
                return Ok("Disabled");
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPatch("users/{id}/enable")]
        public async Task<IActionResult> EnableUserAsync(Guid id)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                await _service.EnableUserAsync(id, currentUserId);
                return Ok("Enabled");
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpPatch("users/{id}/logout")]
        public async Task<IActionResult> ForceUserLogoutAsync(Guid id)
        {
            try
            {
                var currentUserId = await GetCurrentUserIdAsync();
                await _service.ForceUserLogoutAsync(id, currentUserId);
                return Ok("User logged out");
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }
    }
}
