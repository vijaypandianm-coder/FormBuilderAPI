using FormBuilderAPI.DTOs;
using FormBuilderAPI.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ResponsesController : ControllerBase
    {
        private readonly ResponseService _responses;

        public ResponsesController(ResponseService responses)
        {
            _responses = responses;
        }

        private long CurrentUserId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0", out var id) ? id : 0;

        [HttpPost("{formKey:int}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> Submit(int formKey, [FromBody] SubmitResponseDto payload)
        {
            await _responses.SaveAsync(formKey, CurrentUserId, payload);
            return Ok(new { message = "Saved." });
        }

        [HttpGet("{formKey:int}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List(int formKey, [FromQuery] long? userId = null)
        {
            var items = await _responses.ListAsync(formKey, userId);
            return Ok(items);
        }
    }
}