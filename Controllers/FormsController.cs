using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using FormBuilderAPI.Services;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class FormController : ControllerBase
    {
        private readonly FormService _formService;

        public FormController(FormService formService)
        {
            _formService = formService;
        }

        // ✅ Only Admin can create
        [HttpPost("create")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> CreateForm([FromBody] Form form)
        {
            var created = await _formService.CreateFormAsync(form);
            return Ok(created);
        }

        // ✅ Admins only can update
        [HttpPut("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> UpdateForm(string id, [FromBody] Form form)
        {
            var updated = await _formService.UpdateFormAsync(id, form);
            if (updated == null) return NotFound();
            return Ok(updated);
        }

        // ✅ Admins only can delete
        [HttpDelete("{id}")]
        [Authorize(Policy = "RequireAdmin")]
        public async Task<IActionResult> DeleteForm(string id)
        {
            var deleted = await _formService.DeleteFormAsync(id);
            if (!deleted) return NotFound();
            return Ok(new { message = "Form deleted" });
        }

        // ✅ Learners & Admins can view
        [HttpGet("{id}")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> GetForm(string id)
        {
            var form = await _formService.GetFormByIdAsync(id);
            if (form == null) return NotFound();
            return Ok(form);
        }

        [HttpGet("list")]
        [Authorize(Policy = "RequireLearnerOrAdmin")]
        public async Task<IActionResult> GetAllForms()
        {
            var forms = await _formService.GetAllFormsAsync();
            return Ok(forms);
        }
    }
}