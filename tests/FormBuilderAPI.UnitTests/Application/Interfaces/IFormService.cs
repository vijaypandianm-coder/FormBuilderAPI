using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.UnitTests.Application.Interfaces
{
    public interface IFormService
    {
        Task<Form?> GetByFormKeyAsync(int formKey);
        Task<(List<Form>, int)> ListAsync(string? status, string? search, bool includeLayout, int page, int pageSize);
    }
}
