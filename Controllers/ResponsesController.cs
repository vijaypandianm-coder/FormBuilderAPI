using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FormBuilderAPI.Models.SqlModels;
using FormBuilderAPI.Services;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api")]
    public class ResponsesController : ControllerBase
    {
        private readonly ResponseService _responses;
        public ResponsesController(ResponseService responses) => _responses = responses;

        private bool IsAdmin => User.IsInRole("Admin");
        private long? CurrentUserIdLong =>
            long.TryParse(
                User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                User.FindFirst("sub")?.Value, out var id) ? id : null;

        // Submit
        [HttpPost("forms/{formId}/responses")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> Submit(string formId, [FromBody] List<FormResponseAnswer> answers)
        {
            if (CurrentUserIdLong is null) return Unauthorized("Invalid token.");

            var response = new FormResponse
            {
                FormId = formId,
                UserId = CurrentUserIdLong.Value,
                SubmittedAt = DateTime.UtcNow
            };

            var saved = await _responses.SaveAsync(response, answers);
            return Ok(saved);
        }

        // List (admin & learners)
        // /api/responses?mine=true&learnerId=123&formId=abc&page=1&pageSize=20
        [HttpGet("responses")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List(
            [FromQuery] bool mine = false,
            [FromQuery] long? learnerId = null,
            [FromQuery] string? formId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            // Learner cannot query others
            if (!IsAdmin)
            {
                learnerId = CurrentUserIdLong;
                mine = true;
            }

            var (items, total) = await _responses.ListAsync(
                learnerId: learnerId,
                formId: formId,
                page: page,
                pageSize: pageSize);

            return Ok(new { total, items, page, pageSize });
        }
    }
}
