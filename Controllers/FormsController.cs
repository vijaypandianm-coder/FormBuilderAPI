
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Services;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormsController : ControllerBase
    {
        private readonly FormService _forms;
        public FormsController(FormService forms) => _forms = forms;

        private bool IsAdmin => User.IsInRole("Admin");
        private string? CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        // Create
        [HttpPost]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Create([FromBody] Form form)
        {
            form.CreatedBy = CurrentUserId ?? "system";
            var created = await _forms.CreateFormAsync(form);
            return CreatedAtAction(nameof(GetById), new { id = created.Id }, created);
        }

        // Update
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Update(string id, [FromBody] Form form)
        {
            var updated = await _forms.UpdateFormAsync(id, form);
            return updated is null ? NotFound() : Ok(updated);
        }

        // Delete
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(string id)
        {
            var ok = await _forms.DeleteFormAsync(id);
            return ok ? NoContent() : NotFound();
        }

        // Publish / Draft
        [HttpPatch("{id}/status")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SetStatus(string id, [FromBody] SetStatusRequest req)
        {
            if (req is null || string.IsNullOrWhiteSpace(req.Status))
                return BadRequest("Status is required (Published|Draft).");

            var updated = await _forms.SetStatusAsync(id, req.Status);
            return updated is null ? NotFound() : Ok(updated);
        }

        // Get one (+ preview)
        // /api/forms/{id}?mode=preview
        [HttpGet("{id}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> GetById(string id, [FromQuery] string? mode = null)
        {
            var allowPreview = IsAdmin && string.Equals(mode, "preview", StringComparison.OrdinalIgnoreCase);
            var form = await _forms.GetFormByIdAsync(id, allowPreview, IsAdmin);
            return form is null ? NotFound() : Ok(form);
        }

        // List with filters
        // /api/forms?status=Published|Draft|All&mine=true&page=1&pageSize=20
        [HttpGet]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List(
            [FromQuery] string? status = null,
            [FromQuery] bool mine = false,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var createdBy = (IsAdmin && mine) ? CurrentUserId : null;
            var (items, total) = await _forms.ListAsync(
                status: status,
                createdBy: createdBy,
                isAdmin: IsAdmin,
                page: page,
                pageSize: pageSize);

            return Ok(new { total, items, page, pageSize });
        }

        public class SetStatusRequest
        {
            public string Status { get; set; } = "Draft";
        }
    }
}