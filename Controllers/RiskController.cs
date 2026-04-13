using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApplication1.DTOs.Risk;
using WebApplication1.Services;

namespace WebApplication1.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class RiskController : ControllerBase
    {
        private readonly RiskService _service;

        public RiskController(RiskService service)
        {
            _service = service;
        }

        [HttpGet]
        public async Task<IActionResult> GetAllAsync()
        {
            var risks = await _service.GetAllAsync();
            return Ok(risks);
        }

        [HttpGet("{id}", Name = "GetRiskById")]
        public async Task<IActionResult> GetByIdAsync(Guid id)
        {
            var risk = await _service.GetByIdAsync(id);
            return Ok(risk);
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] CreateRiskDto dto)
        {
            var createdRisk = await _service.AddAsync(dto);
            return CreatedAtRoute("GetRiskById", new { id = createdRisk.RiskId }, createdRisk);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateRiskDto dto)
        {
            var updatedRisk = await _service.UpdateAsync(id, dto);
            return Ok(updatedRisk);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            await _service.DeleteAsync(id);
            return Ok(new { message = "Risk deleted" });
        }

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchRiskDto dto)
        {
            var PatchRisk = await _service.PatchAsync(id, dto);
            return Ok(PatchRisk);
        }
    }
}
