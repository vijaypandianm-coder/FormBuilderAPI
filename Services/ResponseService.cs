using FormBuilderAPI.Data;
using FormBuilderAPI.Models.SqlModels;
using Microsoft.EntityFrameworkCore;

namespace FormBuilderAPI.Services
{
    public class ResponseService
    {
        private readonly SqlDbContext _db;
        public ResponseService(SqlDbContext db) { _db = db; }

        /// <summary>
        /// Persists a submission with its answers.
        /// </summary>
        public async Task<FormResponse> SaveAsync(FormResponse response, List<FormResponseAnswer> answers)
        {
            _db.FormResponses.Add(response);
            await _db.SaveChangesAsync(); // generates response.Id (INT)

            foreach (var ans in answers)
            {
                ans.ResponseId = response.Id; // must be INT and match parent
                _db.FormResponseAnswers.Add(ans);
            }

            await _db.SaveChangesAsync();
            return response;
        }

        /// <summary>
        /// Returns all responses for a given Mongo form id (string).
        /// </summary>
        public async Task<(List<FormResponse> Items, int Total)> ListAsync(string formId)
        {
            var query = _db.FormResponses
                           .Include(r => r.Answers)
                           .Where(r => r.FormId == formId);

            var items = await query.ToListAsync();
            return (items, items.Count);
        }
    }
}
