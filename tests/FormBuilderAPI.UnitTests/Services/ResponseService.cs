using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;

namespace FormBuilderAPI.Services
{
    public class ResponseService
    {
        private readonly IFormService _formService;
        private readonly IResponsesRepository _repository;

        public ResponseService(IFormService formService, IResponsesRepository repository)
        {
            _formService = formService;
            _repository = repository;
        }

        public async Task<long> SaveAsync(int formKey, long userId, SubmitResponseDto payload)
        {
            // Implementation will be tested via mocks
            throw new NotImplementedException();
        }

        public async Task<List<ResponseAnswerRow>> ListAsync(int formKey, long? userId = null)
        {
            // Implementation will be tested via mocks
            throw new NotImplementedException();
        }

        public async Task<List<PublishedFormDto>> ListPublishedFormsAsync()
        {
            // Implementation will be tested via mocks
            throw new NotImplementedException();
        }

        public async Task<ResponseDetailDto> GetDetailAsync(long responseId)
        {
            // Implementation will be tested via mocks
            throw new NotImplementedException();
        }

        public async Task<ResponseAnswerRow> GetAsync(long responseId)
        {
            // Implementation will be tested via mocks
            throw new NotImplementedException();
        }
    }
}