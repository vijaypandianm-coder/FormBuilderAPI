using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize(Policy = "RequireAdmin")]
    public class AdminController : ControllerBase
    {
        private readonly SqlDbContext _db;
        public AdminController(SqlDbContext db) => _db = db;

        [HttpGet("responses/{formId}")]
        public async Task<IActionResult> GetFormResponses(string formId)
        {
            var responses = await _db.FormResponses
                .Include(r => r.Answers)
                .Where(r => r.FormId == formId)
                .ToListAsync();

            var result = responses.Select(r => new
            {
                r.Id,
                r.UserId,
                r.SubmittedAt,
                Answers = r.Answers.Select(a => new { a.FieldId, a.AnswerValue })
            });

            return Ok(result);
        }
    }
}