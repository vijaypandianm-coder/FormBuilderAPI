using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Application.Interfaces
{
    public interface IFormService
    {
        Task<Form?> GetByFormKeyAsync(int formKey);

        Task<Form> CreateFormAsync(Form form);

        Task<Form?> UpdateFormAsync(string id, Form form);

        // IMPORTANT: Use List<Form> to match the concrete implementation exactly
        Task<(List<Form> Items, long Total)> ListAsync(
            string? status, string? createdBy, bool isAdmin, int page, int pageSize);

        Task<bool> DeleteFormAndResponsesAsync(string id);
    }
}