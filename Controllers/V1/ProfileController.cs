using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using WebApplication1.Common.Extensions;
using WebApplication1.Common.Results;
using WebApplication1.DTOs;
using WebApplication1.Mappers;
using WebApplication1.Services;

namespace WebApplication1.Controllers.V1
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class ProfileController : ControllerBase
    {
        private readonly ProfileService _service;
        private readonly ProfileImageMapper _mapper;
        private readonly ErrorHelper _errorHelper;
        private readonly ResultHelper _resultHelper;

        public ProfileController(ProfileService service) 
        { 
            _service = service;
            _mapper = new ProfileImageMapper();
            _errorHelper = new ErrorHelper();
            _resultHelper = new ResultHelper();
        }

        [HttpGet]
        public async Task<IActionResult> GetProfileImageAsync()
        {
            var userId = HttpContext.GetUserId();
            var data = await _service.GetProfile(userId);
            if (data.IsNullOrEmpty())
            {
                var errors = _errorHelper.CreateErrors("NulldDataException", "No content found");
                return NotFound(_resultHelper.Failure<GetProfileImageDto>(404, errors));
            }

            var result = _mapper.ToDto(data);
            return Ok(_resultHelper.Success<GetProfileImageDto>(result));
        }

        [HttpPost("upload-image")]
        public async Task<IActionResult> UploadProfileImageAsync(IFormFile file)
        {
            var userId = HttpContext.GetUserId();
            var data = await _service.UpdateProfileImage(userId, file);
            if (data == null)
            {
                var errors = _errorHelper.CreateErrors("NulldDataException", "No content found");
                return NotFound(_resultHelper.Failure<GetProfileImageDto>(404, errors));
            }

            var result = _mapper.ToDto(data);
            return Ok(_resultHelper.Success<GetProfileImageDto>(result));
        }

        [HttpPost("delete-image")]
        public async Task<IActionResult> DeleteProfileImageAsync()
        {
            var userId = HttpContext.GetUserId();
            var user = await _service.CheckUserExistAndGet(userId);

            await _service.UpdateProfile(user);

            return Ok(_resultHelper.Success());
        }

    }
}
