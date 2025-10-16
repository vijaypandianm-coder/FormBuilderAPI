using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.Application.Interfaces;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly IFormAppService _app;
        public AdminController(IFormAppService app) => _app = app;

        // Simple admin listing, matches IFormAppService.ListAsync signature
        [HttpGet("forms")]
        public async Task<IActionResult> ListForms(
            [FromQuery] string? status = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            var (items, total) = await _app.ListAsync(status, isAdmin: true, page, pageSize);
            return Ok(new { total, page, pageSize, items });
        }
    }
}