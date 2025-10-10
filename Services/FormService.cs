using MongoDB.Driver;
using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Models.SqlModels;

namespace FormBuilderAPI.Services
{
    public class FormService
    {
        private readonly MongoDbContext _mongo;
        private readonly SqlDbContext _sql;

        public FormService(MongoDbContext mongo, SqlDbContext sql)
        {
            _mongo = mongo;
            _sql = sql;
        }

        // CREATE
        public async Task<Form> CreateFormAsync(Form form)
        {
            form.CreatedAt = DateTime.UtcNow;
            form.UpdatedAt = DateTime.UtcNow;
            form.Status = string.IsNullOrWhiteSpace(form.Status) ? "Draft" : form.Status;
            await _mongo.Forms.InsertOneAsync(form);
            return form;
        }

        // UPDATE (full replace)
        public async Task<Form?> UpdateFormAsync(string id, Form updated)
        {
            updated.Id = id; // ensure id is preserved on replace
            updated.UpdatedAt = DateTime.UtcNow;

            var result = await _mongo.Forms.FindOneAndReplaceAsync(
                Builders<Form>.Filter.Eq(f => f.Id, id),
                updated,
                new FindOneAndReplaceOptions<Form> { ReturnDocument = ReturnDocument.After });

            return result;
        }

        // DELETE (form only)
        public async Task<bool> DeleteFormAsync(string id)
        {
            var res = await _mongo.Forms.DeleteOneAsync(f => f.Id == id);
            return res.DeletedCount > 0;
        }

        // DELETE (form + SQL responses)
        public async Task<bool> DeleteFormAndResponsesAsync(string formId)
        {
            // 1) Delete SQL responses & answers
            // Pull minimal set: Ids + answers
            var responses = await _sql.FormResponses
                .Where(r => r.FormId == formId)
                .Include(r => r.Answers)
                .ToListAsync();

            if (responses.Count > 0)
            {
                // Remove children then parents
                var allAnswers = responses.SelectMany(r => r.Answers).ToList();
                if (allAnswers.Count > 0)
                    _sql.FormResponseAnswers.RemoveRange(allAnswers);

                _sql.FormResponses.RemoveRange(responses);
                await _sql.SaveChangesAsync();
            }

            // 2) Delete Mongo form
            var res = await _mongo.Forms.DeleteOneAsync(f => f.Id == formId);
            return res.DeletedCount > 0;
        }

        // STATUS
        public async Task<Form?> SetStatusAsync(string id, string status)
        {
            status = (status?.Equals("Published", StringComparison.OrdinalIgnoreCase) ?? false)
                ? "Published" : "Draft";

            var update = Builders<Form>.Update
                .Set(f => f.Status, status)
                .Set(f => f.PublishedAt, status == "Published" ? DateTime.UtcNow : (DateTime?)null)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            return await _mongo.Forms.FindOneAndUpdateAsync(
                Builders<Form>.Filter.Eq(f => f.Id, id),
                update,
                new FindOneAndUpdateOptions<Form> { ReturnDocument = ReturnDocument.After });
        }

        // GET (with preview rules)
        public async Task<Form?> GetFormByIdAsync(string id, bool allowPreview, bool isAdmin)
        {
            var f = await _mongo.Forms.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (f is null) return null;

            // Draft hidden from non-admins unless preview=true for admin
            if (f.Status == "Draft" && !(allowPreview || isAdmin))
                return null;

            return f;
        }

        // LIST (filters + visibility)
        public async Task<(List<Form> Items, long Total)> ListAsync(
            string? status,
            string? createdBy,
            bool isAdmin,
            int page,
            int pageSize)
        {
            var filter = Builders<Form>.Filter.Empty;

            if (!isAdmin)
            {
                filter &= Builders<Form>.Filter.Eq(f => f.Status, "Published");
            }
            else
            {
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", StringComparison.OrdinalIgnoreCase))
                    filter &= Builders<Form>.Filter.Eq(f => f.Status, status);

                if (!string.IsNullOrWhiteSpace(createdBy))
                    filter &= Builders<Form>.Filter.Eq(f => f.CreatedBy, createdBy);
            }

            var total = await _mongo.Forms.CountDocumentsAsync(filter);
            var items = await _mongo.Forms
                .Find(filter)
                .SortByDescending(f => f.UpdatedAt)
                .Skip((page - 1) * pageSize)
                .Limit(pageSize)
                .ToListAsync();

            return (items, total);
        }
    }
}