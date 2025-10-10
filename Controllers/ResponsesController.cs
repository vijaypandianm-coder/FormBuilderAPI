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

        // Submit a response
        [HttpPost("forms/{formId}/responses")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> Submit(string formId, [FromBody] List<FormResponseAnswer> answers)
        {
            if (CurrentUserIdLong is null)
                return Unauthorized(new { message = "Invalid token." });

            var response = new FormResponse
            {
                FormId = formId,
                UserId = CurrentUserIdLong.Value,
                SubmittedAt = DateTime.UtcNow
            };

            var saved = await _responses.SaveAsync(response, answers);

            // return a flat DTO to avoid cycles & over-posting
            var dto = new ResponseDto
            {
                Id = saved.Id,
                FormId = saved.FormId,
                UserId = saved.UserId,
                SubmittedAt = saved.SubmittedAt,
                Answers = answers.Select(a => new AnswerDto
                {
                    FieldId = a.FieldId,
                    AnswerValue = a.AnswerValue
                }).ToList()
            };

            return Ok(dto);
        }

        // List responses (admin can filter; learner only sees their own)
        // GET /api/responses?mine=true&learnerId=123&formId=abc&page=1&pageSize=20
        [HttpGet("responses")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List(
            [FromQuery] bool mine = false,
            [FromQuery] long? learnerId = null,
            [FromQuery] string? formId = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
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

            // project to DTOs (don’t return EF entities)
            var list = items.Select(r => new ResponseDto
            {
                Id = r.Id,
                FormId = r.FormId,
                UserId = r.UserId,
                SubmittedAt = r.SubmittedAt,
                Answers = r.Answers.Select(a => new AnswerDto
                {
                    FieldId = a.FieldId,
                    AnswerValue = a.AnswerValue
                }).ToList()
            }).ToList();

            return Ok(new { total, page, pageSize, items = list });
        }

        // DTOs
        public class ResponseDto
        {
            public long Id { get; set; }
            public string FormId { get; set; } = default!;
            public long UserId { get; set; }
            public DateTime SubmittedAt { get; set; }
            public List<AnswerDto> Answers { get; set; } = new();
        }

        public class AnswerDto
        {
            public string FieldId { get; set; } = default!;
            public string? AnswerValue { get; set; }
        }
    }
}