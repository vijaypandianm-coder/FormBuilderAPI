using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;
using Microsoft.EntityFrameworkCore;

namespace FormBuilderAPI.Services
{
    public class ResponseService
    {
        private readonly SqlDbContext _db;
        public ResponseService(SqlDbContext db) => _db = db;

        public async Task<FormResponse> SaveAsync(FormResponse response, List<FormResponseAnswer> answers)
        {
            _db.FormResponses.Add(response);
            await _db.SaveChangesAsync();

            foreach (var a in answers)
            {
                a.ResponseId = response.Id;
                _db.FormResponseAnswers.Add(a);
            }
            await _db.SaveChangesAsync();
            return response;
        }

        public async Task<(List<FormResponse> Items, int Total)> ListAsync(
            long? learnerId,
            string? formId,
            int page,
            int pageSize)
        {
            var q = _db.FormResponses
                .Include(r => r.Answers)
                .AsQueryable();

            if (learnerId is not null)
                q = q.Where(r => r.UserId == learnerId.Value);

            if (!string.IsNullOrWhiteSpace(formId))
                q = q.Where(r => r.FormId == formId);

            var total = await q.CountAsync();
            var items = await q
                .OrderByDescending(r => r.SubmittedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}
