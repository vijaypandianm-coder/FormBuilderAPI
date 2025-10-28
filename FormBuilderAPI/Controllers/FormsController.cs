// Controllers/FormsController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.DTOs;
using System.Linq; // for Any()

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormsController : ControllerBase
    {
        private readonly IFormAppService _app;

        public FormsController(IFormAppService app)
        {
            _app = app;
        }

        private bool IsAdmin => User.IsInRole("Admin");
        private string? CurrentUserId =>
            User.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
            User.FindFirst("sub")?.Value;

        // -------- META --------
        [HttpPost("meta")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateMeta([FromBody] FormMetaDto meta)
        {
            var dto = await _app.CreateMetaAsync(CurrentUserId ?? "system", meta);
            return CreatedAtAction(nameof(GetByKey), new { formKey = dto.FormKey!.Value }, dto);
        }

        [HttpPut("{formKey:int}/meta")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateMeta(int formKey, [FromBody] FormMetaDto meta)
        {
            var dto = await _app.UpdateMetaAsync(formKey, meta);
            return Ok(dto);
        }

        // -------- LAYOUT --------
        [HttpPost("{formKey:int}/layout")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> AddLayout(int formKey, [FromBody] FormLayoutDto layout)
        {
            var dto = await _app.AddLayoutAsync(formKey, layout);
            return Ok(dto);
        }

        [HttpPut("{formKey:int}/layout")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SetLayout(int formKey, [FromBody] FormLayoutDto layout)
        {
            var dto = await _app.SetLayoutAsync(formKey, layout);
            return Ok(dto);
        }

        // -------- STATUS / ACCESS --------
        [HttpPatch("{formKey:int}/status")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SetStatus(int formKey, [FromBody] StatusPatchDto body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.Status))
                return BadRequest(new { message = "status is required" });
            var dto = await _app.SetStatusAsync(formKey, body.Status);
            return Ok(dto);
        }

        [HttpPatch("{formKey:int}/access")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SetAccess(int formKey, [FromBody] AccessPatchDto body)
        {
            if (body is null || string.IsNullOrWhiteSpace(body.Access))
                return BadRequest(new { message = "access is required" });
            var dto = await _app.SetAccessAsync(formKey, body.Access);
            return Ok(dto);
        }

        [HttpDelete("{formKey:int}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Delete(int formKey)
        {
            await _app.DeleteAsync(formKey);
            return NoContent();
        }

        // -------- READ --------
        [HttpGet("{formKey:int}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> GetByKey(int formKey, [FromQuery] string? mode = null)
        {
            var allowPreview = IsAdmin && string.Equals(mode, "preview", StringComparison.OrdinalIgnoreCase);
            var dto = await _app.GetByKeyAsync(formKey, allowPreview, IsAdmin);
            return Ok(dto);
        }

        [HttpGet]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List([FromQuery] string? status = null,
                                              [FromQuery] int page = 1,
                                              [FromQuery] int pageSize = 20)
        {
            // _app.ListAsync currently returns (IEnumerable<FormOutDto> Items, long Total)
            var result = await _app.ListAsync(status, IsAdmin, page, pageSize);

            if (IsAdmin)
            {
                // Use the local page/pageSize variables, not properties on result
                return Ok(new
                {
                    Total = result.Total,
                    Page = page,
                    PageSize = pageSize,
                    Items = result.Items
                });
            }

            // Learner branch: Published only; Restricted requires assignment
            var userId = CurrentUserId;
            if (string.IsNullOrEmpty(userId))
                return Unauthorized();

            var userIdLong = long.TryParse(userId, out var parsed) ? parsed : -1;

            var filtered = new List<FormOutDto>();
            foreach (var form in result.Items)
            {
                if (!string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                    continue;

                if (string.Equals(form.Access, "Open", StringComparison.OrdinalIgnoreCase))
                {
                    filtered.Add(form);
                    continue;
                }

                if (string.Equals(form.Access, "Restricted", StringComparison.OrdinalIgnoreCase) && form.FormKey.HasValue)
                {
                    var assignees = await _app.ListAssigneesAsync(form.FormKey.Value);
                    var isAssigned = assignees.Any(a => HasUserId(a, userIdLong));
                    if (isAssigned) filtered.Add(form);
                }
            }

            return Ok(new
            {
                Total = filtered.Count,
                Page = page,
                PageSize = pageSize,
                Items = filtered
            });
        }

        // -------- ASSIGNMENTS --------
        [HttpPost("{formKey:int}/assignments")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Assign(int formKey, [FromBody] AssignRequest body)
        {
            await _app.AssignUserAsync(formKey, body.UserId);
            return NoContent();
        }

        [HttpDelete("{formKey:int}/assignments/{userId:long}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Unassign(int formKey, long userId)
        {
            await _app.UnassignUserAsync(formKey, userId);
            return NoContent();
        }

        [HttpGet("{formKey:int}/assignments")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ListAssignees(int formKey)
        {
            var items = await _app.ListAssigneesAsync(formKey);
            return Ok(items);
        }

        // -------- HELPER --------
        private static bool HasUserId(object? obj, long userId)
        {
            if (obj is null) return false;
            if (obj is long l) return l == userId;

            var prop = obj.GetType().GetProperty("UserId");
            if (prop?.GetValue(obj) is null) return false;

            try
            {
                var value = Convert.ToInt64(prop.GetValue(obj));
                return value == userId;
            }
            catch
            {
                return false;
            }
        }
    }

    public class AssignRequest
    {
        public long UserId { get; set; }
    }
}