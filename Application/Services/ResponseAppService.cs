// File: Application/Services/ResponseAppService.cs

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;

using FormBuilderAPI.Application.Interfaces;  // IResponseAppService
using FormBuilderAPI.Data;                   // SqlDbContext
using FormBuilderAPI.DTOs;                   // SubmitResponseDto
using FormBuilderAPI.Helpers;                // FieldTypeHelper, ValidationHelper
using FormBuilderAPI.Models.MongoModels;     // FormSection, FormField
using FormBuilderAPI.Models.SqlModels;       // FormResponse
using FormBuilderAPI.Services;               // FormService

namespace FormBuilderAPI.Application.Services
{
    /// <summary>
    /// App-layer service that validates payloads against the form layout
    /// and persists flat rows into SQL (formresponses).
    /// Choice answers are collapsed into AnswerValue per your rule.
    /// </summary>
    public sealed class ResponseAppService : IResponseAppService
    {
        private readonly SqlDbContext _db;
        private readonly FormService _forms;

        public ResponseAppService(SqlDbContext db, FormService forms)
        {
            _db = db ?? throw new ArgumentNullException(nameof(db));
            _forms = forms ?? throw new ArgumentNullException(nameof(forms));
        }

        /// <summary>
        /// Submit a full response (one submission). Writes one SQL row per answered field.
        /// - Choice fields: AnswerValue holds optionId (single) or JSON array string (multi)
        /// - Non-choice:   AnswerValue holds the typed value
        /// </summary>
        public async Task SubmitAsync(int formKey, long userId, SubmitResponseDto payload)
        {
            if (payload is null || payload.Answers is null || payload.Answers.Count == 0)
                throw new ArgumentException("No answers provided.", nameof(payload));

            // 1) Load form from Mongo by numeric key
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            if (!string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is not published.");

            // Build field lookup by FieldId
            var fields = (form.Layout ?? new List<FormSection>())
                        .SelectMany(s => s.Fields ?? new List<FormField>())
                        .ToDictionary(f => f.FieldId, StringComparer.Ordinal);

            var now = DateTime.UtcNow;
            var rows = new List<FormResponse>(payload.Answers.Count);

            // 2) Validate and convert each answer to a SQL row
            foreach (var a in payload.Answers)
            {
                if (string.IsNullOrWhiteSpace(a.FieldId))
                    throw new InvalidOperationException("Each answer must include 'fieldId'.");

                if (!fields.TryGetValue(a.FieldId, out var field))
                    throw new InvalidOperationException($"Unknown field: {a.FieldId}");

                bool isChoice = FieldTypeHelper.IsChoice(field.Type);

                // -- Required validation
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

                // -- Type-specific validation (non-choice)
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

                        // file/others: no-op here
                    }
                }

                // -- Collapse to single storage column (AnswerValue) per your rule
                string? storedValue = a.AnswerValue;

                if (isChoice)
                {
                    var ids = a.OptionIds ?? new List<string>();

                    // Validate option IDs belong to the field
                    var valid = new HashSet<string>((field.Options ?? new()).Select(o => o.Id));
                    if (!ids.All(valid.Contains))
                        throw new InvalidOperationException($"One or more OptionIds are invalid for '{field.Label}'.");

                    storedValue = ids.Count <= 1 ? ids.FirstOrDefault() : JsonSerializer.Serialize(ids);
                }

                if (storedValue is null)
                    storedValue = string.Empty; // allow empty for non-required

                rows.Add(new FormResponse
                {
                    FormId      = form.Id,                 // string ID from Mongo
                    FormKey     = form.FormKey ?? formKey, // numeric for filtering
                    UserId      = userId,
                    SubmittedAt = now,
                    
                });
            }

            // 3) Persist in one batch
            _db.FormResponses.AddRange(rows);
            await _db.SaveChangesAsync();
        }

        /// <summary>
        /// Flat list: one row per answered field (from formresponses).
        /// </summary>
        public async Task<IReadOnlyList<FormResponse>> ListAsync(int formKey, long? userId = null)
        {
            var q = _db.FormResponses.AsNoTracking()
                        .Where(r => r.FormKey == formKey);

            if (userId.HasValue)
                q = q.Where(r => r.UserId == userId.Value);

            return await q
                .OrderByDescending(r => r.SubmittedAt)
                .ThenBy(r => r.Id)
                .ToListAsync();
        }

        /// <summary>
        /// Get a single saved row by SQL Id.
        /// </summary>
        public Task<FormResponse?> GetAsync(long id)
            => _db.FormResponses.AsNoTracking().FirstOrDefaultAsync(r => r.Id == id);
    }
}