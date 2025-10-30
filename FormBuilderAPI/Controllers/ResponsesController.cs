using System.Security.Claims;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Services;
using FormBuilderAPI.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FormBuilderAPI.Controllers
{
    /// <summary>
    /// Manages form responses for both Admin and Learner views.
    /// Notes:
    ///  - Keeps existing plural route base for legacy endpoints: /api/Responses/{formKey}
    ///  - Adds singular absolute routes for new use-cases under /api/Response/...
    /// </summary>
    [ApiController]
    [Route("api/[controller]")] // -> /api/Responses for legacy submit/list-by-formKey
    public class ResponsesController : ControllerBase
    {
        private readonly ResponseService _responses;
        private readonly IResponsesRepository _repo;


        public ResponsesController(ResponseService responses, IResponsesRepository repo)
        {
            _responses = responses;
            _repo = repo;
        }

        private long CurrentUserId =>
            long.TryParse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0", out var id) ? id : 0;

        // ────────────────────────────────────────────────────────────
        // Legacy endpoints (kept intact)
        // ────────────────────────────────────────────────────────────

        /// <summary>POST /api/Responses/{formKey} - Save a submission (Learner/Admin)</summary>
        [HttpPost("{formKey:int}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> Submit(int formKey, [FromBody] SubmitResponseDto payload)
        {
            await _responses.SaveAsync(formKey, CurrentUserId, payload);
            return Ok(new { message = "Saved." });
        }

        /// <summary>GET /api/Responses/{formKey} - Flat list of rows for a form (optionally ?userId=)</summary>
        [HttpGet("{formKey:int}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List(int formKey, [FromQuery] long? userId = null)
        {
            var items = await _responses.ListAsync(formKey, userId);
            return Ok(items);
        }

        // ────────────────────────────────────────────────────────────
        // New absolute routes (singular base) used by Admin & Learner
        // ────────────────────────────────────────────────────────────

        /// <summary>GET /api/Response/published - Admin: list published forms (cards/source for admin pages)</summary>
        [HttpGet("/api/Response/published")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Published()
        {
            var items = await _responses.ListPublishedFormsAsync();
            return Ok(items);
        }

        /// <summary>GET /api/Response/form/{formKey}/responses - Admin: list submission headers for a form</summary>
        [HttpGet("/api/Response/form/{formKey:int}/responses")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ResponsesByFormKey(int formKey)
        {
            var items = await _responses.ListHeadersByFormKeyAsync(formKey);
            return Ok(items);
        }

        /// <summary>GET /api/Response/my-submissions - Learner/Admin: list current user's submission headers</summary>
        [HttpGet("/api/Response/my-submissions")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> MySubmissions()
        {
            var items = await _responses.ListHeadersByUserAsync(CurrentUserId);
            return Ok(items);
        }

        /// <summary>
        /// GET /api/Response/{responseId} - View a single submission (header + answers).
        /// Admins can view all; Learners can only view their own.
        /// </summary>
        [HttpGet("/api/Response/{responseId:long}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> GetByResponseId(long responseId)
        {
            var dto = await _responses.GetDetailAsync(responseId);
            if (dto is null)
                return NotFound(new { message = "Response not found" });

            // Ownership check for learners; admins bypass.
            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && dto.Header.UserId != CurrentUserId)
                return Forbid();

            return Ok(dto);
        }
        /// <summary>GET /api/Response/file/{fileId} - download uploaded file</summary>
        [HttpGet("/api/Response/file/{fileId:long}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> DownloadFile(long fileId)
        {
            // 1) Ownership / access check
            var owner = await _repo.GetFileOwnerByIdAsync(fileId);
            if (owner is null) return NotFound(new { message = "File not found" });

            var (responseId, responseUserId, _) = owner.Value;

            var isAdmin = User.IsInRole("Admin");
            if (!isAdmin && responseUserId != CurrentUserId)
                return Forbid();

            // 2) Fetch file payload
            var file = await _repo.GetFileAsync(fileId);
            if (file is null) return NotFound(new { message = "File not found" });

            var (name, type, blob) = file.Value;
            return File(blob, string.IsNullOrWhiteSpace(type) ? "application/octet-stream" : type, fileDownloadName: name);
        }

    }
}