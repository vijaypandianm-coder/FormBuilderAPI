// Application/Interfaces/IFormAppService.cs
using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.DTOs;

namespace FormBuilderAPI.Application.Interfaces
{
    public interface IFormAppService
    {
        Task<FormOutDto> CreateMetaAsync(string createdBy, FormMetaDto meta);

        Task<FormOutDto> AddLayoutAsync(int formKey, FormLayoutDto layout);
        Task<FormOutDto> SetLayoutAsync(int formKey, FormLayoutDto layout);

        // Optional: single-field helper (for simple UI)
        //Task<FormOutDto> SetFieldAsync(int formKey, SingleFieldDto dto);

        Task<FormOutDto> GetByKeyAsync(int formKey, bool allowPreview, bool isAdmin);
        Task<(IEnumerable<FormOutDto> Items, long Total)> ListAsync(string? status, bool isAdmin, int page, int pageSize);

        Task<FormOutDto> SetStatusAsync(int formKey, string status);
        Task<FormOutDto> SetAccessAsync(int formKey, string access);
        // ADD THESE:
        Task<FormOutDto> UpdateMetaAsync(int formKey, FormMetaDto meta);
        Task DeleteAsync(int formKey);

        // Assignments:
        Task AssignUserAsync(int formKey, long userId);
        Task UnassignUserAsync(int formKey, long userId);
        Task<IEnumerable<object>> ListAssigneesAsync(int formKey);
        
    }
}