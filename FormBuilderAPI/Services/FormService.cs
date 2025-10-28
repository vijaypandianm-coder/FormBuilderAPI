using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MongoDB.Driver;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Services
{
    public class FormService : IFormService
    {
        private readonly MongoDbContext _mongo;

        public FormService(MongoDbContext mongo) => _mongo = mongo;

        public async Task<Form?> GetByFormKeyAsync(int formKey)
        {
            return await _mongo.Forms
                .Find(f => f.FormKey == formKey)
                .FirstOrDefaultAsync();
        }

        public async Task<Form> CreateFormAsync(Form form)
        {
            form.CreatedAt = System.DateTime.UtcNow;
            form.UpdatedAt = System.DateTime.UtcNow;
            form.Status = string.IsNullOrWhiteSpace(form.Status) ? "Draft" : form.Status;
            form.Access = string.IsNullOrWhiteSpace(form.Access) ? "Open" : form.Access;

            if (form.FormKey is null || form.FormKey <= 0)
            {
                // your existing GetNextFormKeyAsync logic (kept private)
                var latest = await _mongo.Forms
                    .Find(_ => true)
                    .SortByDescending(f => f.FormKey)
                    .Limit(1)
                    .FirstOrDefaultAsync();

                form.FormKey = (latest?.FormKey ?? 0) + 1;
            }

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

        public async Task<(List<Form> Items, long Total)> ListAsync(
            string? status, string? createdBy, bool isAdmin, int page, int pageSize)
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

        public async Task<bool> DeleteFormAndResponsesAsync(string id)
        {
            var res = await _mongo.Forms.DeleteOneAsync(f => f.Id == id);
            return res.DeletedCount > 0;
        }
    }
}