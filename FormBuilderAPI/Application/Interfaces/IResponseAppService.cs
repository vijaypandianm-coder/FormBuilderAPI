using System.Collections.Generic;
using System.Threading.Tasks;
using FormBuilderAPI.DTOs;              // SubmitResponseDto
using FormBuilderAPI.Models.SqlModels; // FormResponse

namespace FormBuilderAPI.Application.Interfaces
{
    /// <summary>
    /// Application-layer contract for working with form responses.
    /// Implement this in your ResponseAppService (which can use ResponseService + EF).
    /// </summary>
    public interface IResponseAppService
    {
        /// <summary>
        /// Submit a full response (one submission) for a form.
        /// Choice fields must send option IDs; non-choice sends AnswerValue.
        /// </summary>
        Task SubmitAsync(int formKey, long userId, SubmitResponseDto payload);

        /// <summary>
        /// List saved answers (flat: one row per field answer) for a form.
        /// Optionally filter by userId.
        /// </summary>
        Task<IReadOnlyList<FormResponse>> ListAsync(int formKey, long? userId = null);

        /// <summary>
        /// Get a single saved answer row by its SQL Id.
        /// </summary>
        Task<FormResponse?> GetAsync(long id);
    }
}