using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireLearnerOrAdmin")]
    public class ResponsesController : ControllerBase
    {
        private readonly ResponseService _responseService;
        public ResponsesController(ResponseService responseService) => _responseService = responseService;

        [HttpPost("{formId}")]
        public async Task<IActionResult> SubmitResponse(string formId, [FromBody] List<FormResponseAnswer> answers)
        {
            // user id from JWT (we stored Users.Id as string in sub)
            var sub = User.FindFirstValue(ClaimTypes.NameIdentifier) 
                      ?? User.FindFirstValue(ClaimTypes.Name) 
                      ?? User.FindFirstValue("sub");

            if (string.IsNullOrWhiteSpace(sub) || !int.TryParse(sub, out var userId))
                return Unauthorized("Invalid user id in token.");

            var response = new FormResponse
            {
                FormId = formId,
                UserId = userId,
                SubmittedAt = DateTime.UtcNow
            };

            var saved = await _responseService.SaveAsync(response, answers);
            return Ok(saved);
        }

        [HttpGet("{formId}")]
        [Authorize(Policy = "RequireAdmin")] // usually only admins view all responses
        public async Task<IActionResult> GetResponses(string formId)
        {
            var (items, total) = await _responseService.ListAsync(formId);
            return Ok(new { total, items });
        }
    }
}

