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
            try
            {
                var risk = await _service.GetByIdAsync(id);
                return Ok(risk);
            }
            catch (Exception ex) 
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
            
        }

        [HttpPost]
        public async Task<IActionResult> AddAsync([FromBody] CreateRiskDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var createdRisk = await _service.AddAsync(dto);
                return CreatedAtRoute("GetRiskById", new { id = createdRisk.RiskId }, createdRisk);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }

        [HttpPut]
        public async Task<IActionResult> UpdateAsync(Guid id, [FromBody] UpdateRiskDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var updatedRisk = await _service.UpdateAsync(id, dto);
                return Ok(updatedRisk);
            }
            catch (Exception ex)
            {
                return NotFound(new
                {
                    message = ex.Message
                });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id)
        {
            try
            {
                await _service.DeleteAsync(id);
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

        [HttpPatch("{id}")]
        public async Task<IActionResult> PatchAsync(Guid id, [FromBody] PatchRiskDto dto)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }
            try
            {
                var PatchRisk = await _service.PatchAsync(id, dto);
                return Ok(PatchRisk);
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
