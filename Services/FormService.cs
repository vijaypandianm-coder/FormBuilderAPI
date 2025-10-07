using MongoDB.Driver;
using FormBuilderAPI.Data;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Services
{
    public class FormService
    {
        private readonly MongoDbContext _mongoContext;

        public FormService(MongoDbContext mongoContext)
        {
            _mongoContext = mongoContext;
        }

        // ✅ Create new form
        public async Task<Form> CreateFormAsync(Form form)
        {
            form.CreatedAt = DateTime.UtcNow;
            await _mongoContext.Forms.InsertOneAsync(form);
            return form;
        }

        // ✅ Update existing form
        public async Task<Form?> UpdateFormAsync(string id, Form updatedForm)
        {
            var filter = Builders<Form>.Filter.Eq(f => f.Id, id);
            var update = Builders<Form>.Update
                .Set(f => f.Title, updatedForm.Title)
                .Set(f => f.Layout, updatedForm.Layout)
                .Set(f => f.UpdatedAt, DateTime.UtcNow);

            var result = await _mongoContext.Forms.FindOneAndUpdateAsync(filter, update, new FindOneAndUpdateOptions<Form>
            {
                ReturnDocument = ReturnDocument.After
            });

            return result;
        }

        // ✅ Delete form
        public async Task<bool> DeleteFormAsync(string id)
        {
            var result = await _mongoContext.Forms.DeleteOneAsync(f => f.Id == id);
            return result.DeletedCount > 0;
        }

        // ✅ Get form by ID
        public async Task<Form?> GetFormByIdAsync(string id)
        {
            return await _mongoContext.Forms.Find(f => f.Id == id).FirstOrDefaultAsync();
        }

        // ✅ Get all forms
        public async Task<List<Form>> GetAllFormsAsync()
        {
            return await _mongoContext.Forms.Find(_ => true).ToListAsync();
        }
    }
}