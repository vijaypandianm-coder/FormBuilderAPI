using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.DTOs;

namespace FormBuilderAPI.Application.Interfaces
{
    public interface IResponsesRepository
    {
        Task<long> InsertFormResponseHeaderAsync(long userId, int formKey, string formId);
        Task InsertFormResponseAnswerAsync(long responseId, long userId, int formKey, string fieldId, string fieldType, string answerValue);
        Task<long> InsertFileAsync(long responseId, int formKey, string fieldId, string fileName, string contentType, long fileSize, byte[] fileData);
        Task<List<ResponseHeaderDto>> ListHeadersByFormKeyAsync(int formKey);
        Task<List<ResponseHeaderDto>> ListHeadersByFormKeyAndUserAsync(int formKey, long userId);
        Task<List<ResponseAnswerRow>> ListAnswersByResponseIdAsync(long responseId);
        Task<ResponseHeaderDto> GetHeaderByIdAsync(long responseId);
    }
}
