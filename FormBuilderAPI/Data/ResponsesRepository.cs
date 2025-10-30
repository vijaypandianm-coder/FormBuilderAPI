// namespace matches your project root namespace
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;
using Dapper;
using MySqlConnector;
using FormBuilderAPI.DTOs;

namespace FormBuilderAPI.Data
{
    public interface IResponsesRepository
    {
        // inserts
        Task<long> InsertFormResponseHeaderAsync(long userId, int formKey, string formId);
        Task<int> InsertFormResponseAnswerAsync(long responseId, long userId, int formKey, string fieldId, string? fieldType, string? answerValue);

        // listings
        Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByFormKeyAsync(int formKey);
        Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByFormKeyAndUserAsync(int formKey, long userId);
        Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByUserAsync(long userId);

        // details
        Task<ResponseHeaderDto?> GetHeaderByIdAsync(long responseId);
        Task<IReadOnlyList<ResponseAnswerRow>> ListAnswersByResponseIdAsync(long responseId);
        Task<long> InsertFileAsync(long responseId, int formKey, string fieldId,
                           string fileName, string contentType, long sizeBytes, byte[] blob);
        Task<(string FileName, string ContentType, byte[] Blob)?> GetFileAsync(long fileId);
        Task<(long ResponseUserId, int FormKey)?> GetResponseOwnerAsync(long responseId);
        Task<(long ResponseId, long ResponseUserId, int FormKey)?> GetFileOwnerByIdAsync(long fileId);
    }

    /// <summary>
    /// Dapper-based repository for formresponses / formresponseanswers.
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

        public async Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByFormKeyAsync(int formKey)
        {
            const string sql = @"
SELECT Id, FormKey, UserId, SubmittedAt
FROM formresponses
WHERE FormKey = @FormKey
ORDER BY SubmittedAt DESC, Id ASC;";

            using var conn = Create();
            var rows = await conn.QueryAsync<ResponseHeaderDto>(sql, new { FormKey = formKey });
            return rows.AsList();
        }

        public async Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByFormKeyAndUserAsync(int formKey, long userId)
        {
            const string sql = @"
SELECT Id, FormKey, UserId, SubmittedAt
FROM formresponses
WHERE FormKey = @FormKey AND UserId = @UserId
ORDER BY SubmittedAt DESC, Id ASC;";

            using var conn = Create();
            var rows = await conn.QueryAsync<ResponseHeaderDto>(sql, new { FormKey = formKey, UserId = userId });
            return rows.AsList();
        }

        public async Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByUserAsync(long userId)
        {
            const string sql = @"
SELECT Id, FormKey, UserId, SubmittedAt
FROM formresponses
WHERE UserId = @UserId
ORDER BY SubmittedAt DESC, Id ASC;";

            using var conn = Create();
            var rows = await conn.QueryAsync<ResponseHeaderDto>(sql, new { UserId = userId });
            return rows.AsList();
        }

        public async Task<ResponseHeaderDto?> GetHeaderByIdAsync(long responseId)
        {
            const string sql = @"
SELECT Id, FormKey, UserId, SubmittedAt
FROM formresponses
WHERE Id = @Id
LIMIT 1;";

            using var conn = Create();
            return await conn.QueryFirstOrDefaultAsync<ResponseHeaderDto>(sql, new { Id = responseId });
        }

        public async Task<IReadOnlyList<ResponseAnswerRow>> ListAnswersByResponseIdAsync(long responseId)
        {
            const string sql = @"
SELECT Id, ResponseId, FieldId, FieldType, AnswerValue, SubmittedAt
FROM formresponseanswers
WHERE ResponseId = @ResponseId
ORDER BY Id ASC;";

            using var conn = Create();
            var rows = await conn.QueryAsync<ResponseAnswerRow>(sql, new { ResponseId = responseId });
            return rows.AsList();
        }
        public async Task<long> InsertFileAsync(long responseId, int formKey, string fieldId,
                                        string fileName, string contentType, long sizeBytes, byte[] blob)
        {
            const string sql = @"
INSERT INTO formresponsefiles (ResponseId, FormKey, FieldId, FileName, ContentType, SizeBytes, `Blob`, CreatedAt)
VALUES (@ResponseId, @FormKey, @FieldId, @FileName, @ContentType, @SizeBytes, @Blob, UTC_TIMESTAMP());
SELECT LAST_INSERT_ID();";

            using var conn = Create();
            return await conn.ExecuteScalarAsync<long>(sql, new
            {
                ResponseId = responseId,
                FormKey = formKey,
                FieldId = fieldId,
                FileName = fileName,
                ContentType = contentType,
                SizeBytes = sizeBytes,
                Blob = blob
            });
        }

        public async Task<(string FileName, string ContentType, byte[] Blob)?> GetFileAsync(long fileId)
        {
            const string sql = @"
SELECT FileName, ContentType, `Blob`
FROM formresponsefiles
WHERE Id = @Id
LIMIT 1;";
            using var conn = Create();
            var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = fileId });
            if (row == null) return null;
            return ((string)row.FileName, (string)row.ContentType, (byte[])row.Blob);
        }

        public async Task<(long ResponseUserId, int FormKey)?> GetResponseOwnerAsync(long responseId)
        {
            const string sql = @"SELECT UserId, FormKey FROM formresponses WHERE Id = @Id LIMIT 1;";
            using var conn = Create();
            var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = responseId });
            if (row == null) return null;
            return ((long)row.UserId, (int)row.FormKey);
        }
        public async Task<(long ResponseId, long ResponseUserId, int FormKey)?> GetFileOwnerByIdAsync(long fileId)
        {
            const string sql = @"
SELECT f.ResponseId, r.UserId AS ResponseUserId, r.FormKey
FROM formresponsefiles f
JOIN formresponses r ON r.Id = f.ResponseId
WHERE f.Id = @Id
LIMIT 1;";
            using var conn = Create();
            var row = await conn.QueryFirstOrDefaultAsync(sql, new { Id = fileId });
            if (row == null) return null;
            return ((long)row.ResponseId, (long)row.ResponseUserId, (int)row.FormKey);
        }
    }
}