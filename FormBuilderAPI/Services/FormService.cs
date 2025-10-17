using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Helpers;


namespace FormBuilderAPI.Services
{
    public class FormService
    {
        private readonly MongoDbContext _mongo;

        public FormService(MongoDbContext mongo)
        {
            _mongo = mongo;
        }

        // Resolve by numeric FormKey
        public async Task<Form?> GetByFormKeyAsync(int formKey)
        {
            return await _mongo.Forms
                .Find(f => f.FormKey == formKey)
                .FirstOrDefaultAsync();
        }

        // Generate next FormKey (max+1) — single writer assumption
        private async Task<int> GetNextFormKeyAsync()
        {
            var latest = await _mongo.Forms
                .Find(_ => true)
                .SortByDescending(f => f.FormKey)
                .Limit(1)
                .FirstOrDefaultAsync();

            var currentMax = latest?.FormKey ?? 0;
            return currentMax + 1;
        }

        public async Task<Form> CreateFormAsync(Form form)
        {
            form.CreatedAt = System.DateTime.UtcNow;
            form.UpdatedAt = System.DateTime.UtcNow;
            form.Status = string.IsNullOrWhiteSpace(form.Status) ? "Draft" : form.Status;
            form.Access = string.IsNullOrWhiteSpace(form.Access) ? "Open" : form.Access;

            if (form.FormKey is null || form.FormKey <= 0)
                form.FormKey = await GetNextFormKeyAsync();

            await _mongo.Forms.InsertOneAsync(form);
            return form;
        }

        public async Task<Form?> UpdateFormAsync(string id, Form updated)
        {
            updated.Id = id;
            updated.UpdatedAt = System.DateTime.UtcNow;

            return await _mongo.Forms.FindOneAndReplaceAsync(
                Builders<Form>.Filter.Eq(f => f.Id, id),
                updated,
                new FindOneAndReplaceOptions<Form> { ReturnDocument = ReturnDocument.After });
        }

        public async Task<bool> DeleteFormAndResponsesAsync(string id)
        {
            // If you also delete SQL responses, do that in a different service before this call.
            var res = await _mongo.Forms.DeleteOneAsync(f => f.Id == id);
            return res.DeletedCount > 0;
        }

        public async Task<Form?> GetFormByIdAsync(string id, bool allowPreview, bool isAdmin)
        {
            var f = await _mongo.Forms.Find(x => x.Id == id).FirstOrDefaultAsync();
            if (f is null) return null;
            if (f.Status == "Draft" && !(allowPreview || isAdmin))
                return null;
            return f;
        }

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
                if (!string.IsNullOrWhiteSpace(status) && !status.Equals("All", System.StringComparison.OrdinalIgnoreCase))
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