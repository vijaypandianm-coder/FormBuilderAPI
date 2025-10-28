using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Helpers;          // FieldTypeHelper
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Models.SqlModels; // FormResponse, FormResponseAnswer
using FormBuilderAPI.Services;         // FormService
using SqlFormResponseAnswer = FormBuilderAPI.Models.SqlModels.FormResponseAnswer;
using FormBuilderAPI.Application.Interfaces;

namespace FormBuilderAPI.Services
{
    public interface IResponseService
    {
        Task<long> SaveAsync(int formKey, long userId, SubmitResponseDto payload, CancellationToken ct = default);
        Task<IReadOnlyList<ResponseFlatRowDto>> ListAsync(int formKey, long? userId = null, CancellationToken ct = default);
        Task<ResponseFlatRowDto?> GetAsync(long id, CancellationToken ct = default);
    }

    public class ResponseService : IResponseService
    {
        private readonly SqlDbContext _db;
        private readonly IFormService _forms;

        public ResponseService(SqlDbContext db, IFormService forms)
        {
            _db = db;
            _forms = forms;
        }

        /// <summary>
        /// Saves one submission header (formresponses) and one answer per field (formresponseanswers).
        /// Choice answers are collapsed into AnswerValue (single = optionId; multi = JSON array string).
        /// </summary>
        public async Task<long> SaveAsync(int formKey, long userId, SubmitResponseDto payload, CancellationToken ct = default)
        {
            if (payload == null || payload.Answers == null || payload.Answers.Count == 0)
                throw new InvalidOperationException("No answers provided.");

            // 1) Load form + layout
            var form = await _forms.GetByFormKeyAsync(formKey) ?? throw new KeyNotFoundException("Form not found.");
            if (!string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is not published.");

            // Lookup fields by FieldId
            var fieldLookup = (form.Layout ?? new List<FormSection>())
                .SelectMany(s => s.Fields ?? new List<FormField>())
                .ToDictionary(f => f.FieldId, StringComparer.Ordinal);

            // 2) Create header row
            var header = new FormResponse
            {
                FormId = form.Id,                 // string id from Mongo
                FormKey = form.FormKey ?? formKey, // keep numeric for filtering
                UserId = userId,
                SubmittedAt = DateTime.UtcNow
            };
            _db.FormResponses.Add(header);
            await _db.SaveChangesAsync(ct); // header.Id now available

            // 3) Create answer rows
            foreach (var a in payload.Answers)
            {
                if (string.IsNullOrWhiteSpace(a.FieldId))
                    throw new InvalidOperationException("Each answer must include fieldId.");

                if (!fieldLookup.TryGetValue(a.FieldId, out var field))
                    throw new InvalidOperationException($"Unknown field: {a.FieldId}");

                var isChoice = FieldTypeHelper.IsChoice(field.Type);

                // Required validation
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

                // Type-specific validation for non-choice (optional; keep if you already have helpers)
                if (!isChoice && !string.IsNullOrEmpty(field.Type))
                {
                    switch (field.Type.Trim().ToLowerInvariant())
                    {
                        case "shorttext":
                        case "text":
                            if ((a.AnswerValue ?? "").Length > ValidationHelper.ShortTextMax)
                                throw new InvalidOperationException(
                                    $"'{field.Label}' must be ≤ {ValidationHelper.ShortTextMax} characters.");
                            break;
                        case "longtext":
                            if ((a.AnswerValue ?? "").Length > ValidationHelper.LongTextMax)
                                throw new InvalidOperationException(
                                    $"'{field.Label}' must be ≤ {ValidationHelper.LongTextMax} characters.");
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
                        default:
                            break;
                    }
                }

                // Collapse to AnswerValue per your rule
                string? storedValue = a.AnswerValue;
                if (isChoice)
                {
                    var ids = a.OptionIds ?? new List<string>();
                    // validate option ids against layout
                    var valid = new HashSet<string>((field.Options ?? new()).Select(o => o.Id));
                    if (!ids.All(valid.Contains))
                        throw new InvalidOperationException($"One or more OptionIds are invalid for '{field.Label}'.");

                    storedValue = ids.Count <= 1 ? ids.FirstOrDefault() : JsonSerializer.Serialize(ids);
                }

                if (string.IsNullOrWhiteSpace(storedValue))
                    storedValue = string.Empty; // allow empty if not required

                var answer = new FormResponseAnswer
                {
                    ResponseId = header.Id,
                    FormKey = header.FormKey,

                    UserId = userId,
                    FieldId = a.FieldId,
                    FieldType = field.Type,
                    AnswerValue = storedValue,
                    SubmittedAt = DateTime.UtcNow
                };

                _db.Set<FormResponseAnswer>().Add(answer);
            }

            await _db.SaveChangesAsync(ct);
            return header.Id;
        }

        /// <summary>
        /// Flat listing of saved rows (answers) for a form; optionally filter by userId.
        /// </summary>
        public async Task<IReadOnlyList<ResponseFlatRowDto>> ListAsync(int formKey, long? userId = null, CancellationToken ct = default)
        {
            var q = _db.FormResponses.AsNoTracking().Where(r => r.FormKey == formKey);

            if (userId.HasValue)
                q = q.Where(r => r.UserId == userId);

            // join answers for those headers
            var headerIds = await q.Select(r => r.Id).ToListAsync(ct);

            if (headerIds.Count == 0) return Array.Empty<ResponseFlatRowDto>();

            var rows = await _db.Set<FormResponseAnswer>()
                .AsNoTracking()
                .Where(a => headerIds.Contains(a.ResponseId))
                .OrderByDescending(a => a.SubmittedAt)
                .ThenBy(a => a.Id)
                .Select(a => new ResponseFlatRowDto
                {
                    ResponseId = a.ResponseId,
                    FormKey = a.FormKey ?? formKey,
                    UserId = a.UserId,
                    SubmittedAt = a.SubmittedAt,
                    FieldId = a.FieldId,
                    AnswerValue = a.AnswerValue
                })
                .ToListAsync(ct);

            return rows;
        }

        public async Task<ResponseFlatRowDto?> GetAsync(long id, CancellationToken ct = default)
        {
            var a = await _db.Set<FormResponseAnswer>()
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, ct);

            if (a == null) return null;

            return new ResponseFlatRowDto
            {
                ResponseId = a.ResponseId,
                FormKey = a.FormKey ?? 0,
                UserId = a.UserId,
                SubmittedAt = a.SubmittedAt,
                FieldId = a.FieldId,
                AnswerValue = a.AnswerValue
            };
        }
    }

    public class ResponseFlatRowDto
    {
        public long ResponseId { get; set; }
        public int FormKey { get; set; }
        public long UserId { get; set; }
        public DateTime SubmittedAt { get; set; }
        public string FieldId { get; set; } = default!;
        public string? AnswerValue { get; set; }
    }
}