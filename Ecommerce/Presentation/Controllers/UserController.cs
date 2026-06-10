using Application.DTOs.User;
using Application.Interfaces.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Presentation.Common.Extensions;

namespace Presentation.Controllers
{
    [ApiController]
    [Authorize]
    [Route("api/profile")]
    public class UserController : ControllerBase
    {
        private readonly IUserService _userService;

        public UserController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public async Task<IActionResult> GetUser(CancellationToken cancellationToken)
        {
            var result = await _userService.GetUserAsync(cancellationToken);
            return this.FromResult(result);
        }

        [HttpPut]
        public async Task<IActionResult> UpdateUser([FromBody] UpdateUserRequest request, CancellationToken cancellationToken)
        {
            var result = await _userService.UpdateUserAsync(request, cancellationToken);
            return this.FromResult(result);
        }

        [HttpPost("avatar")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadAvatar(Microsoft.AspNetCore.Http.IFormFile? image, CancellationToken cancellationToken)
        {
            if (image == null)
                return BadRequest("No file was uploaded.");

            await using var stream = image.OpenReadStream();
            var result = await _userService.UploadAvatarAsync(
                stream,
                image.FileName,
                image.ContentType,
                cancellationToken);

            return this.FromResult(result);
        }
    }
}
