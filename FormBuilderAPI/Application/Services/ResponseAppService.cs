// File: Application/Services/ResponseAppService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using FormBuilderAPI.Application.Interfaces;   // IResponseAppService, IFormService
using FormBuilderAPI.Data;                    // SqlDbContext
using FormBuilderAPI.DTOs;                    // SubmitResponseDto
using FormBuilderAPI.Helpers;                 // FieldTypeHelper, ValidationHelper
using FormBuilderAPI.Models.MongoModels;      // FormSection, FormField
using FormBuilderAPI.Models.SqlModels;        // FormResponse, FormResponseAnswer

namespace FormBuilderAPI.Application.Services
{
    /// <summary>
    /// Validates the payload against the form layout and persists:
    /// - 1 row in formresponses (submission header)
    /// - N rows in formresponseanswers (one per field answer)
    /// </summary>
    public sealed class ResponseAppService : IResponseAppService
    {
        private readonly SqlDbContext _db;
        private readonly IFormService _forms;

        public ResponseAppService(SqlDbContext db, IFormService forms)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _forms = forms ?? throw new ArgumentNullException(nameof(forms));
        }

        public async Task SubmitAsync(int formKey, long userId, SubmitResponseDto payload)
        {
            if (payload is null || payload.Answers is null || payload.Answers.Count == 0)
                throw new ArgumentException("No answers provided");

            // 1) Load form (must be Published)
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            if (!string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is not published.");

            // Build quick lookup of fields by FieldId
            var fields = (form.Layout ?? new List<FormSection>())
                .SelectMany(s => s.Fields ?? new List<FormField>())
                .ToDictionary(f => f.FieldId, StringComparer.Ordinal);

            var now = DateTime.UtcNow;

            // Use a transaction so header + answers are atomic
            await using var tx = await _db.Database.BeginTransactionAsync();

            // --- MySQL-only precondition (NO-OP on SQLite tests)
            var provider = _db.Database.ProviderName ?? string.Empty;
            var isMySql = provider.IndexOf("mysql", StringComparison.OrdinalIgnoreCase) >= 0;

            if (isMySql)
            {
                // Ensure a matching parent key exists (for FK FormResponses.FormKey -> FormKeys.FormKey)
                var sql = $"INSERT INTO formkeys (FormKey) VALUES ({formKey}) ON DUPLICATE KEY UPDATE FormKey = FormKey;";
                await _db.Database.ExecuteSqlRawAsync(sql);
            }

            // 2) Create ONE submission header in formresponses
            var header = new FormResponse
            {
                // columns: Id (auto), UserId, FormKey, FormId, SubmittedAt
                UserId      = userId,
                FormKey     = form.FormKey ?? formKey,
                FormId      = form.Id,
                SubmittedAt = now
            };

            _db.FormResponses.Add(header);
            await _db.SaveChangesAsync(); // ensures header.Id is generated

            // 3) Build answer rows for formresponseanswers
            var answerRows = new List<FormResponseAnswer>(payload.Answers.Count);

            foreach (var a in payload.Answers)
            {
                if (string.IsNullOrWhiteSpace(a.FieldId))
                    throw new InvalidOperationException("Each answer must include 'fieldId'.");

                if (!fields.TryGetValue(a.FieldId, out var field))
                    throw new InvalidOperationException($"Unknown field: {a.FieldId}");

                var isChoice = FieldTypeHelper.IsChoice(field.Type);

                // -- Required checks
                if (field.IsRequired)
                {
                    if (isChoice)
                    {
                        if (a.OptionIds is null || a.OptionIds.Count == 0)
                            throw new InvalidOperationException($"'{field.Label}' is required.");
                    }
                    else
                    {
                        if (string.IsNullOrWhiteSpace(a.AnswerValue))
                            throw new InvalidOperationException($"'{field.Label}' is required.");
                    }
                }

                // -- Type checks for non-choice fields
                if (!isChoice && !string.IsNullOrEmpty(field.Type))
                {
                    switch ((field.Type ?? string.Empty).Trim().ToLowerInvariant())
                    {
                        case "shorttext":
                        case "short_text":
                        case "text":
                            if ((a.AnswerValue ?? "").Length > ValidationHelper.ShortTextMax)
                                throw new InvalidOperationException(
                                    $"'{field.Label}' must be ≤ {ValidationHelper.ShortTextMax} characters.");
                            break;

                        case "longtext":
                        case "long_text":
                        case "textarea":
                            if ((a.AnswerValue ?? "").Length > ValidationHelper.LongTextMax)
                                throw new InvalidOperationException(
                                    $"'{field.Label}' must be ≤ {ValidationHelper.LongTextMax} characters.");
                            break;

                        case "email":
                            // (Optional email format validation could go here)
                            break;

                        case "number":
                            if (!ValidationHelper.IsInteger(a.AnswerValue))
                                throw new InvalidOperationException($"'{field.Label}' must be an integer.");
                            break;

                        case "date":
                            if (!ValidationHelper.TryParseDateDdMmYyyy(a.AnswerValue, out _))
                                throw new InvalidOperationException(
                                    $"'{field.Label}' must be in {ValidationHelper.DateFormat} format.");
                            break;
                    }
                }

                // -- Collapse choices into a single stored string
                string? storedValue = a.AnswerValue;
                if (isChoice)
                {
                    var ids = a.OptionIds ?? new List<string>();
                    var valid = new HashSet<string>((field.Options ?? new()).Select(o => o.Id));
                    if (!ids.All(valid.Contains))
                        throw new InvalidOperationException($"One or more OptionIds are invalid for '{field.Label}'.");

                    storedValue = ids.Count <= 1 ? ids.FirstOrDefault() : JsonSerializer.Serialize(ids);
                }
                storedValue ??= string.Empty;

                var enumFieldType = MapToSqlFieldType(field.Type);

                answerRows.Add(new FormResponseAnswer
                {
                    UserId      = userId,
                    FormKey     = header.FormKey,
                    ResponseId  = header.Id,
                    FieldId     = a.FieldId,
                    FieldType   = enumFieldType,
                    AnswerValue = storedValue,
                    SubmittedAt = now
                });
            }

            // 4) Save all answers, commit txn
            _db.FormResponseAnswers.AddRange(answerRows);
            await _db.SaveChangesAsync();
            await tx.CommitAsync();
        }

        public async Task<IReadOnlyList<FormResponse>> ListAsync(int formKey, long? userId = null)
        {
            var q = _db.FormResponses.AsNoTracking().Where(r => r.FormKey == formKey);
            if (userId.HasValue) q = q.Where(r => r.UserId == userId.Value);

            return await q
                .OrderByDescending(r => r.SubmittedAt)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        public Task<FormResponse?> GetAsync(long id) =>
            _db.FormResponses.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);

        /// <summary>
        /// Maps various form field types to the MySQL ENUM used by formresponseanswers.FieldType.
        /// Allowed ENUM values:
        ///   shortText, textarea, email, number, date, radio, dropdown, checkbox, multiselect, mcq
        /// </summary>
        private static string? MapToSqlFieldType(string? formFieldType)
        {
            if (string.IsNullOrWhiteSpace(formFieldType)) return null;

            var t = formFieldType.Trim().ToLowerInvariant();

            return t switch
            {
                "shorttext" or "short_text" or "text" => "shortText",
                "longtext" or "long_text" or "textarea" => "textarea",
                "email" => "email",
                "number" => "number",
                "date" => "date",
                "radio" => "radio",
                "dropdown" => "dropdown",
                "checkbox" => "checkbox",
                "multiselect" or "multi-select" => "multiselect",
                "mcq" or "multiple" => "mcq",
                _ => null
            };
        }
    }
}