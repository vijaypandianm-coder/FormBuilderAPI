// Controllers/FormsController.cs
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.DTOs;

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
        // POST /api/forms/meta
        [HttpPost("meta")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateMeta([FromBody] FormMetaDto meta)
        {
            var dto = await _app.CreateMetaAsync(CurrentUserId ?? "system", meta);
            // return Location header to GET by key
            return CreatedAtAction(nameof(GetByKey), new { formKey = dto.FormKey!.Value }, dto);
        }
        /// <summary>Update title/description (Draft only)</summary>
        [HttpPut("{formKey:int}/meta")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateMeta(int formKey, [FromBody] FormMetaDto meta)
        {
            var dto = await _app.UpdateMetaAsync(formKey, meta);
            return Ok(dto);
        }



        // -------- LAYOUT --------
        // POST /api/forms/{formKey}/layout (append)
        [HttpPost("{formKey:int}/layout")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> AddLayout(int formKey, [FromBody] FormLayoutDto layout)
        {
            var dto = await _app.AddLayoutAsync(formKey, layout);
            return Ok(dto);
        }

        // PUT /api/forms/{formKey}/layout (replace)
        [HttpPut("{formKey:int}/layout")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> SetLayout(int formKey, [FromBody] FormLayoutDto layout)
        {
            var dto = await _app.SetLayoutAsync(formKey, layout);
            return Ok(dto);
        }

        // (optional) single-field upsert helper
        // POST /api/forms/{formKey}/field
       // [HttpPost("{formKey:int}/field")]
        //[Authorize(Policy = "RequireAdmin")]
        //public async Task<IActionResult> SetField(int formKey, [FromBody] SingleFieldDto dto)
        //{
          //  var result = await _app.SetFieldAsync(formKey, dto);
            //return Ok(result);
        //}

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
        /// <summary>Delete a form</summary>
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

        /*[HttpGet]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> List([FromQuery] string? status = null, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _app.ListAsync(status, IsAdmin, page, pageSize);
            return Ok(new { total, page, pageSize, items });
        }*/
        // -------- ASSIGNMENTS --------

        /// <summary>Assign a user to a form</summary>
        [HttpPost("{formKey:int}/assignments")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Assign(int formKey, [FromBody] AssignRequest body)
        {
            await _app.AssignUserAsync(formKey, body.UserId);
            return NoContent();
        }

        /// <summary>Unassign a user from a form</summary>
        [HttpDelete("{formKey:int}/assignments/{userId:long}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> Unassign(int formKey, long userId)
        {
            await _app.UnassignUserAsync(formKey, userId);
            return NoContent();
        }

        /// <summary>List assignees for a form</summary>
        [HttpGet("{formKey:int}/assignments")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> ListAssignees(int formKey)
        {
            var items = await _app.ListAssigneesAsync(formKey);
            return Ok(items);
        }
    }
    public class AssignRequest
    {
        public long UserId { get; set; }
    }
}