using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using WebApplication1.DTOs.Control;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ControlController : ControllerBase
    {
        private readonly ControlService _service;

        public ControlController(ControlService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var controls = await _service.GetAllAsync();
            return Ok(controls);
        }

        [HttpGet("{id}", Name = "GetControlById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var control = await _service.GetByIdAsync(id);
            return Ok(control);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] CreateControlDto dto)
        {
            var createdControl = await _service.AddAsync(dto);
            return CreatedAtRoute("GetControlById", new { id = createdControl.ControlId }, createdControl);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateControlDto dto)
        {
            var updatedControl = await _service.UpdateAsync(id, dto);
            return Ok(updatedControl);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Control deleted" });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchControlDto dto)
        {
            var patchedControl = await _service.PatchAsync(id, dto);
            return Ok(patchedControl);
        }
    }
}
