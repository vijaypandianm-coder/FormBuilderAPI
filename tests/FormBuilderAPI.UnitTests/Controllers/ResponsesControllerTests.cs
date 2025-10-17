// tests/FormBuilderAPI.UnitTests/Controllers/ResponsesControllerTests.cs
using Xunit;

namespace FormBuilderAPI.UnitTests.Controllers
{
    public class ResponsesControllerTests
    {
        // Skipped: controller depends on concrete ResponseService (non-virtual methods),
        // which cannot be mocked cleanly without spinning real EF/Mongo plumbing.
        // We'll cover this path in ResponseService unit tests and later with integration tests.
        [Fact(Skip = "ResponsesController depends on concrete ResponseService; cannot be unit-mocked without backend changes. Will cover via service tests/integration tests.")]
        public void Placeholder() { }
    }
}