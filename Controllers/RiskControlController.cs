using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs.RiskControl;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RiskControlController : ControllerBase
    {
        private readonly RiskControlService _service;

        public RiskControlController(RiskControlService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var riskControls = await _service.GetAllAsync();
            return Ok(riskControls);
        }

        [HttpGet("{id}", Name = "GetRiskControlById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var riskControl = await _service.GetByIdAsync(id);
            return Ok(riskControl);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] CreateRiskControlDto dto)
        {
            var created = await _service.AddAsync(dto);
            return CreatedAtRoute("GetRiskControlById", new { id = created.RiskControlId }, created);
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateRiskControlDto dto)
        {
            var updated = await _service.UpdateAsync(id, dto);
            return Ok(updated);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "RiskControl deleted" });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchRiskControlDto dto)
        {
            var patched = await _service.PatchAsync(id, dto);
            return Ok(patched);
        }
    }
}
