// namespace matches your project root namespace
using System.Data;
using System.Threading.Tasks;
using Dapper;
using MySqlConnector;

namespace FormBuilderAPI.Data
{
    public interface IResponsesRepository
    {
        // Minimal methods so Program.cs can resolve the type.
        Task<long> InsertFormResponseHeaderAsync(long userId, int formKey, string formId);
        Task<int> InsertFormResponseAnswerAsync(long responseId, long userId, int formKey, string fieldId, string? fieldType, string? answerValue);
    }

    /// <summary>
    /// Dapper-based repository for formresponses / formresponseanswers.
    /// This is deliberately minimal so the project compiles; expand as needed.
    /// </summary>
    public sealed class ResponsesRepository : IResponsesRepository
    {
        private readonly string _connectionString;

        public ResponsesRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        private IDbConnection Create() => new MySqlConnection(_connectionString);

        public async Task<long> InsertFormResponseHeaderAsync(long userId, int formKey, string formId)
        {
            const string sql = @"
INSERT INTO formresponses (UserId, FormKey, FormId, SubmittedAt)
VALUES (@UserId, @FormKey, @FormId, UTC_TIMESTAMP());
SELECT LAST_INSERT_ID();";

            using var conn = Create();
            var id = await conn.ExecuteScalarAsync<long>(sql, new { UserId = userId, FormKey = formKey, FormId = formId });
            return id;
        }

        public async Task<int> InsertFormResponseAnswerAsync(long responseId, long userId, int formKey, string fieldId, string? fieldType, string? answerValue)
        {
            const string sql = @"
INSERT INTO formresponseanswers (ResponseId, UserId, FormKey, FieldId, FieldType, AnswerValue, SubmittedAt)
VALUES (@ResponseId, @UserId, @FormKey, @FieldId, @FieldType, @AnswerValue, UTC_TIMESTAMP());";

            using var conn = Create();
            return await conn.ExecuteAsync(sql, new
            {
                ResponseId = responseId,
                UserId = userId,
                FormKey = formKey,
                FieldId = fieldId,
                FieldType = fieldType,
                AnswerValue = answerValue
            });
        }
    }
}