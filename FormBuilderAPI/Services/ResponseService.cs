using System.Text.Json;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Helpers;              // FieldTypeHelper, ValidationHelper
using FormBuilderAPI.Models.MongoModels;   // Form, FormSection, FormField
using FormBuilderAPI.Models.SqlModels;     // (for model names only)
using FormBuilderAPI.Application.Interfaces;
using System;
using System.Linq;
using System.Collections.Generic;


namespace FormBuilderAPI.Services
{
    public interface IResponseService
    {
        Task<long> SaveAsync(int formKey, long userId, SubmitResponseDto payload, CancellationToken ct = default);

        // existing flat list used by your old GET /api/Responses/{formKey}
        Task<IReadOnlyList<ResponseFlatRowDto>> ListAsync(int formKey, long? userId = null, CancellationToken ct = default);

        // new admin/learner listings
        Task<IReadOnlyList<PublishedFormDto>> ListPublishedFormsAsync(CancellationToken ct = default);
        Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByFormKeyAsync(int formKey, CancellationToken ct = default);
        Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByUserAsync(long userId, CancellationToken ct = default);
        Task<ResponseDetailDto?> GetDetailAsync(long responseId, CancellationToken ct = default);

        Task<ResponseFlatRowDto?> GetAsync(long id, CancellationToken ct = default);
    }

    public class ResponseService : IResponseService
    {
        private readonly IFormService _forms;
        private readonly IResponsesRepository _repo;

        public ResponseService(IFormService forms, IResponsesRepository repo)
        {
            _forms = forms;
            _repo  = repo;
        }

        // ───────────────── Save (Dapper) ─────────────────
        public async Task<long> SaveAsync(int formKey, long userId, SubmitResponseDto payload, CancellationToken ct = default)
        {
            if (payload == null || payload.Answers == null || payload.Answers.Count == 0)
                throw new InvalidOperationException("No answers provided.");

            // 1) Load form + layout (must be Published)
            var form = await _forms.GetByFormKeyAsync(formKey) ?? throw new KeyNotFoundException("Form not found.");
            if (!string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is not published.");

            var fieldLookup = (form.Layout ?? new List<FormSection>())
                .SelectMany(s => s.Fields ?? new List<FormField>())
                .ToDictionary(f => f.FieldId, StringComparer.Ordinal);

            // 2) Insert header via Dapper
            var headerId = await _repo.InsertFormResponseHeaderAsync(userId, form.FormKey ?? formKey, form.Id);

            // 3) Validate + insert each answer via Dapper
            foreach (var a in payload.Answers)
            {
                if (string.IsNullOrWhiteSpace(a.FieldId))
                    throw new InvalidOperationException("Each answer must include fieldId.");

                if (!fieldLookup.TryGetValue(a.FieldId, out var field))
                    throw new InvalidOperationException($"Unknown field: {a.FieldId}");

                var isChoice = FieldTypeHelper.IsChoice(field.Type);

                // Required checks
                if (field.IsRequired)
                {
                    if (isChoice)
                    {
                        if (a.OptionIds is null || a.OptionIds.Count == 0)
                            throw new InvalidOperationException($"'{field.Label}' is required.");
                    }
                    else if (string.IsNullOrWhiteSpace(a.AnswerValue))
                    {
                        throw new InvalidOperationException($"'{field.Label}' is required.");
                    }
                }

                // Type checks (non-choice)
                if (!isChoice && !string.IsNullOrEmpty(field.Type))
                {
                    switch (field.Type.Trim().ToLowerInvariant())
                    {
                        case "shorttext":
                        case "text":
                            if ((a.AnswerValue ?? "").Length > ValidationHelper.ShortTextMax)
                                throw new InvalidOperationException($"'{field.Label}' must be ≤ {ValidationHelper.ShortTextMax} characters.");
                            break;
                        case "longtext":
                        case "textarea":
                            if ((a.AnswerValue ?? "").Length > ValidationHelper.LongTextMax)
                                throw new InvalidOperationException($"'{field.Label}' must be ≤ {ValidationHelper.LongTextMax} characters.");
                            break;
                        case "number":
                            if (!ValidationHelper.IsInteger(a.AnswerValue))
                                throw new InvalidOperationException($"'{field.Label}' must be an integer.");
                            break;
                        case "date":
                            if (!ValidationHelper.TryParseDateDdMmYyyy(a.AnswerValue, out _))
                                throw new InvalidOperationException($"'{field.Label}' must be in {ValidationHelper.DateFormat} format.");
                            break;
                    }
                }

                // Collapse choice(s)
                string? stored = a.AnswerValue;
                if (isChoice)
                {
                    var ids = a.OptionIds ?? new List<string>();
                    var valid = new HashSet<string>((field.Options ?? new()).Select(o => o.Id));
                    if (!ids.All(valid.Contains))
                        throw new InvalidOperationException($"One or more OptionIds are invalid for '{field.Label}'.");

                    stored = ids.Count <= 1 ? ids.FirstOrDefault() : JsonSerializer.Serialize(ids);
                }
                stored ??= string.Empty;

                var enumType = MapToSqlFieldType(field.Type);

                var isFile = string.Equals(field.Type?.Trim(), "file", StringComparison.OrdinalIgnoreCase);
                if (isFile)
                {
                    // Required check
                    if (field.IsRequired && string.IsNullOrWhiteSpace(a.FileBase64))
                        throw new InvalidOperationException($"'{field.Label}' is required.");
                    // If no file provided and not required, store empty answer and continue
                    if (string.IsNullOrWhiteSpace(a.FileBase64))
                    {
                        await _repo.InsertFormResponseAnswerAsync(
                            responseId: headerId,
                            userId: userId,
                            formKey: form.FormKey ?? formKey,
                            fieldId: a.FieldId,
                            fieldType: "file",
                            answerValue: ""
                        );
                        continue;
                    }
                    // Decode base64 (strip data URL if present)
                    string b64 = a.FileBase64!;
                    var comma = b64.IndexOf(',');
                    if (comma >= 0) b64 = b64[(comma + 1)..];
                    byte[] bytes;
                    try { bytes = Convert.FromBase64String(b64); }
                    catch { throw new InvalidOperationException($"Invalid base64 for '{field.Label}'."); }
                    // (Optional) size limit
                    const long MAX = 10L * 1024 * 1024; // 10 MB
                    if (bytes.LongLength > MAX)
                        throw new InvalidOperationException($"'{field.Label}' exceeds 10 MB.");
                    var fileId = await _repo.InsertFileAsync(
                        responseId: headerId,
                        formKey: form.FormKey ?? formKey,
                        fieldId: a.FieldId,
                        fileName: string.IsNullOrWhiteSpace(a.FileName) ? "upload.bin" : a.FileName!,
                        contentType: string.IsNullOrWhiteSpace(a.ContentType) ? "application/octet-stream" : a.ContentType!,
                        sizeBytes: bytes.LongLength,
                        blob: bytes
                    );
                    var token = $"file:{fileId}";
                    await _repo.InsertFormResponseAnswerAsync(
                        responseId: headerId,
                        userId: userId,
                        formKey: form.FormKey ?? formKey,
                        fieldId: a.FieldId,
                        fieldType: "file",
                        answerValue: token
                    );
                    continue; // important
                }

                await _repo.InsertFormResponseAnswerAsync(
                    responseId: headerId,
                    userId: userId,
                    formKey: form.FormKey ?? formKey,
                    fieldId: a.FieldId,
                    fieldType: enumType,
                    answerValue: stored
                );
            }

            return headerId;
        }

        // ───────────────── Existing flat list (kept) ─────────────────
        // This builds a flat view from header+answers using new repo methods.
        public async Task<IReadOnlyList<ResponseFlatRowDto>> ListAsync(int formKey, long? userId = null, CancellationToken ct = default)
        {
            var headers = userId.HasValue
                ? await _repo.ListHeadersByFormKeyAndUserAsync(formKey, userId.Value)
                : await _repo.ListHeadersByFormKeyAsync(formKey);

            if (headers.Count == 0) return Array.Empty<ResponseFlatRowDto>();

            var rows = new List<ResponseFlatRowDto>();
            foreach (var h in headers)
            {
                var answers = await _repo.ListAnswersByResponseIdAsync(h.Id);
                foreach (var a in answers)
                {
                    rows.Add(new ResponseFlatRowDto
                    {
                        ResponseId = h.Id,
                        FormKey = h.FormKey,
                        UserId = h.UserId,
                        SubmittedAt = h.SubmittedAt,
                        FieldId = a.FieldId,
                        AnswerValue = a.AnswerValue
                    });
                }
            }
            return rows;
        }

        // ───────────────── New admin/learner listings ─────────────────
        public async Task<IReadOnlyList<PublishedFormDto>> ListPublishedFormsAsync(CancellationToken ct = default)
        {
            var (items, _) = await _forms.ListAsync("Published", null, isAdmin: true, page: 1, pageSize: 200);
            return items.Select(f => new PublishedFormDto
            {
                FormKey = f.FormKey ?? 0,
                Title = f.Title,
                Description = f.Description,
                PublishedAt = f.PublishedAt
            }).ToList();
        }

        public Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByFormKeyAsync(int formKey, CancellationToken ct = default)
            => _repo.ListHeadersByFormKeyAsync(formKey);

        public Task<IReadOnlyList<ResponseHeaderDto>> ListHeadersByUserAsync(long userId, CancellationToken ct = default)
            => _repo.ListHeadersByUserAsync(userId);

        public async Task<ResponseDetailDto?> GetDetailAsync(long responseId, CancellationToken ct = default)
        {
            var header = await _repo.GetHeaderByIdAsync(responseId);
            if (header is null) return null;
            var answers = await _repo.ListAnswersByResponseIdAsync(responseId);
            return new ResponseDetailDto
            {
                Header = header,
                Answers = answers
                    .Select(a => new ResponseAnswerDto
                    {
                        FieldId = a.FieldId,
                        FieldType = a.FieldType,
                        AnswerValue = a.AnswerValue
                    })
                    .ToList()
            };
        }

        public async Task<ResponseFlatRowDto?> GetAsync(long id, CancellationToken ct = default)
        {
            var h = await _repo.GetHeaderByIdAsync(id);
            if (h is null) return null;
            // return a synthetic single-row summary (first answer) to keep old signature
            var a = (await _repo.ListAnswersByResponseIdAsync(id)).FirstOrDefault();
            return new ResponseFlatRowDto
            {
                ResponseId = h.Id,
                FormKey = h.FormKey,
                UserId = h.UserId,
                SubmittedAt = h.SubmittedAt,
                FieldId = a?.FieldId ?? string.Empty,
                AnswerValue = a?.AnswerValue
            };
        }

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
                "file" => "file",
                _ => null
            };
        }
    }

    // ───────── DTOs used by this service ─────────
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