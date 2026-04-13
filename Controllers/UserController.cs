using Microsoft.AspNetCore.Mvc;
using WebApplication1.Services;
using WebApplication1.DTOs.User;
using Microsoft.AspNetCore.Authorization;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly UserService _service;

        public UserController(UserService service)
        {
            _service = service;
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateUserDto dto)
        {
            var updatedUser = await _service.UpdateAsync(id, dto);
            return Ok(updatedUser);
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchUserDto dto)
        {
            var patchedUser = await _service.PatchAsync(id, dto);
            return Ok(patchedUser);
        }
    }
}
