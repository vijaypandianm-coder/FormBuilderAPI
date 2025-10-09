using MongoDB.Driver;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Services
{
    public class FormService
    {
        private readonly MongoDbContext _mongo;
        public FormService(MongoDbContext mongo) => _mongo = mongo;

        public async Task<Form> CreateFormAsync(Form form)
        {
            form.CreatedAt = DateTime.UtcNow;
            form.UpdatedAt = DateTime.UtcNow;
            form.Status = string.IsNullOrWhiteSpace(form.Status) ? "Draft" : form.Status;
            await _mongo.Forms.InsertOneAsync(form);
            return form;
        }

        public async Task<Form?> UpdateFormAsync(string id, Form updated)
        {
            updated.UpdatedAt = DateTime.UtcNow;
            var result = await _mongo.Forms.FindOneAndReplaceAsync(
                Builders<Form>.Filter.Eq(f => f.Id, id),
                updated,
                new FindOneAndReplaceOptions<Form> { ReturnDocument = ReturnDocument.After });
            return result;
        }

        public async Task<bool> DeleteFormAsync(string id)
        {
            var res = await _mongo.Forms.DeleteOneAsync(f => f.Id == id);
            return res.DeletedCount > 0;
        }

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

        // Get one with visibility rules
        public async Task<Form?> GetFormByIdAsync(string id, bool allowPreview, bool isAdmin)
        {
            var f = await _mongo.Forms.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (f is null) return null;

            // Draft visibility
            if (f.Status == "Draft" && !(allowPreview || isAdmin))
                return null;

            return f;
        }

        // List with filters
        public async Task<(List<Form> Items, long Total)> ListAsync(
            string? status,
            string? createdBy,
            bool isAdmin,
            int page,
            int pageSize)
        {
            var filter = Builders<Form>.Filter.Empty;

            // Non-admins: default to Published only
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