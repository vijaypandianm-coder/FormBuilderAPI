// Application/Services/FormAppService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FormBuilderAPI.Helpers;                 // FieldTypeHelper (your Helpers folder)
using FormBuilderAPI.Application.Interfaces;
using FormBuilderAPI.Data;
using FormBuilderAPI.DTOs;
using FormBuilderAPI.Models.MongoModels;
using FormBuilderAPI.Services;               // FormService, AssignmentService
using Microsoft.EntityFrameworkCore;

namespace FormBuilderAPI.Application.Services
{
    public class FormAppService : IFormAppService
    {
        private readonly FormService _forms;
        private readonly SqlDbContext _db;
        private readonly AssignmentService _assignments;   // ✅ add this

        public FormAppService(FormService forms, SqlDbContext db, AssignmentService assignments) // ✅ inject it
        {
            _forms = forms;
            _db = db;
            _assignments = assignments; // ✅ set it
        }

        // ------------ helpers ------------
        private static List<FieldOption>? BuildOptions(string type, List<string>? incoming)
        {
            if (!FieldTypeHelper.IsChoice(type)) return null;
            if (incoming == null) return new();

            return incoming
                .Where(s => !string.IsNullOrWhiteSpace(s))
                .Select(s => new FieldOption
                {
                    Id   = Guid.NewGuid().ToString("N"),
                    Text = s.Trim()
                })
                .ToList();
        }

        private static FormFieldDto ToFieldDto(FormField f)
        {
            var dto = new FormFieldDto
            {
                FieldId    = f.FieldId,
                Label      = f.Label,
                Type       = f.Type,
                IsRequired = f.IsRequired
            };

            if (FieldTypeHelper.IsChoice(f.Type) && f.Options is { Count: > 0 })
            {
                dto.Options = f.Options
                    .Select(o => new FieldOptionDto { Id = o.Id, Text = o.Text })
                    .ToList();
            }
            else
            {
                dto.Options = null;
            }

            return dto;
        }

        private static FormOutDto MapToOut(Form f, bool includeLayout)
        {
            return new FormOutDto
            {
                Id          = f.Id,
                FormKey     = f.FormKey,
                Title       = f.Title,
                Description = f.Description,
                Status      = f.Status,
                Access      = f.Access,
                CreatedBy   = f.CreatedBy,
                PublishedAt = f.PublishedAt,
                CreatedAt   = f.CreatedAt,
                UpdatedAt   = f.UpdatedAt,
                Layout      = includeLayout
                    ? f.Layout.Select(s => new FormSectionDto
                      {
                          SectionId   = s.SectionId,
                          Title       = s.Title,
                          Description = s.Description,
                          Fields      = s.Fields.Select(ToFieldDto).ToList()
                      }).ToList()
                    : null
            };
        }

        // ------------ use-cases ------------

        public async Task<FormOutDto> CreateMetaAsync(string createdBy, FormMetaDto meta)
        {
            var form = new Form
            {
                Title       = meta.Title,
                Description = meta.Description,
                Status      = "Draft",
                Access      = "Open",
                CreatedBy   = createdBy ?? "system",
                CreatedAt   = DateTime.UtcNow,
                UpdatedAt   = DateTime.UtcNow,
                Layout      = new List<FormSection>
                {
                    new FormSection { SectionId = Guid.NewGuid().ToString("N"), Title = "Questions", Fields = new() }
                }
            };

            var created = await _forms.CreateFormAsync(form);
            return MapToOut(created, includeLayout: false);
        }

        public async Task<FormOutDto> UpdateMetaAsync(int formKey, FormMetaDto meta)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            if (string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is Published and cannot be edited.");

            form.Title       = meta.Title ?? form.Title;
            form.Description = meta.Description ?? form.Description;
            form.UpdatedAt   = DateTime.UtcNow;

            var updated = await _forms.UpdateFormAsync(form.Id, form);
            return new FormOutDto
            {
                Id          = updated!.Id,
                FormKey     = updated.FormKey,
                Title       = updated.Title,
                Description = updated.Description,
                Status      = updated.Status,
                Access      = updated.Access,
                CreatedBy   = updated.CreatedBy,
                PublishedAt = updated.PublishedAt,
                CreatedAt   = updated.CreatedAt,
                UpdatedAt   = updated.UpdatedAt,
                Layout      = null
            };
        }

        public async Task<FormOutDto> AddLayoutAsync(int formKey, FormLayoutDto layout)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");
            if (string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is Published and cannot be edited.");

            foreach (var s in layout.Sections)
            {
                var newSection = new FormSection
                {
                    SectionId   = string.IsNullOrWhiteSpace(s.SectionId) ? Guid.NewGuid().ToString("N") : s.SectionId!,
                    Title       = s.Title,
                    Description = s.Description,
                    Fields      = (s.Fields ?? new()).Select(f => new FormField
                    {
                        FieldId    = string.IsNullOrWhiteSpace(f.FieldId) ? Guid.NewGuid().ToString("N") : f.FieldId!,
                        Label      = f.Label,
                        Type       = f.Type,
                        IsRequired = f.IsRequired,
                        Options    = BuildOptions(f.Type, f.Options)
                    }).ToList()
                };

                form.Layout.Add(newSection);
            }

            form.UpdatedAt = DateTime.UtcNow;
            var updated = await _forms.UpdateFormAsync(form.Id, form);
            return MapToOut(updated!, includeLayout: true);
        }

        public async Task<FormOutDto> SetLayoutAsync(int formKey, FormLayoutDto layout)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");
            if (string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is Published and cannot be edited.");

            form.Layout = (layout.Sections ?? new()).Select(s => new FormSection
            {
                SectionId   = string.IsNullOrWhiteSpace(s.SectionId) ? Guid.NewGuid().ToString("N") : s.SectionId!,
                Title       = s.Title,
                Description = s.Description,
                Fields      = (s.Fields ?? new()).Select(f => new FormField
                {
                    FieldId    = string.IsNullOrWhiteSpace(f.FieldId) ? Guid.NewGuid().ToString("N") : f.FieldId!,
                    Label      = f.Label,
                    Type       = f.Type,
                    IsRequired = f.IsRequired,
                    Options    = BuildOptions(f.Type, f.Options)
                }).ToList()
            }).ToList();

            form.UpdatedAt = DateTime.UtcNow;
            var updated = await _forms.UpdateFormAsync(form.Id, form);
            return MapToOut(updated!, includeLayout: true);
        }

        /*public async Task<FormOutDto> SetFieldAsync(int formKey, SingleFieldDto dto)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");
            if (string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is Published and cannot be edited.");

            if (form.Layout.Count == 0)
                form.Layout.Add(new FormSection { SectionId = Guid.NewGuid().ToString("N"), Title = "Questions" });

            var section = form.Layout[0];

            if (section.Fields.Count == 0 && string.IsNullOrWhiteSpace(dto.FieldId))
            {
                section.Fields.Add(new FormField
                {
                    FieldId    = Guid.NewGuid().ToString("N"),
                    Label      = dto.Label,
                    Type       = dto.Type,
                    IsRequired = dto.IsRequired,
                    Options    = BuildOptions(dto.Type, dto.Options)
                });
            }
            else
            {
                var target = string.IsNullOrWhiteSpace(dto.FieldId)
                    ? section.Fields[0]
                    : section.Fields.FirstOrDefault(f => f.FieldId == dto.FieldId)
                      ?? section.Fields[0];

                target.Label      = dto.Label;
                target.Type       = dto.Type;
                target.IsRequired = dto.IsRequired;
                target.Options    = BuildOptions(dto.Type, dto.Options);
            }

            form.UpdatedAt = DateTime.UtcNow;
            var updated = await _forms.UpdateFormAsync(form.Id, form);
            return MapToOut(updated!, includeLayout: true);
        }*/

        public async Task<FormOutDto> GetByKeyAsync(int formKey, bool allowPreview, bool isAdmin)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");
            if (form.Status == "Draft" && !(allowPreview || isAdmin))
                throw new KeyNotFoundException("Form not visible.");

            return MapToOut(form, includeLayout: true);
        }

        public async Task<(IEnumerable<FormOutDto> Items, long Total)> ListAsync(string? status, bool isAdmin, int page, int pageSize)
        {
            var (items, total) = await _forms.ListAsync(status, null, isAdmin, page, pageSize);
            var dto = items.Select(i => MapToOut(i, includeLayout: false));
            return (dto, total);
        }

        public async Task<FormOutDto> SetStatusAsync(int formKey, string status)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            if (string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is already Published and cannot be changed.");

            if (!string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(status, "Draft", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Status must be Draft or Published.");

            if (string.Equals(status, "Published", StringComparison.OrdinalIgnoreCase))
            {
                form.Status      = "Published";
                form.PublishedAt = DateTime.UtcNow;
            }
            else
            {
                form.Status = "Draft";
            }

            form.UpdatedAt = DateTime.UtcNow;
            var updated = await _forms.UpdateFormAsync(form.Id, form);
            return MapToOut(updated!, includeLayout: false);
        }

        public async Task<FormOutDto> SetAccessAsync(int formKey, string access)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            if (string.Equals(form.Status, "Published", StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Form is Published and cannot change access.");

            if (!string.Equals(access, "Open", StringComparison.OrdinalIgnoreCase) &&
                !string.Equals(access, "Restricted", StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Access must be Open or Restricted.");

            form.Access = access;
            form.UpdatedAt = DateTime.UtcNow;

            var updated = await _forms.UpdateFormAsync(form.Id, form);
            return MapToOut(updated!, includeLayout: false);
        }

        public async Task DeleteAsync(int formKey)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            var ok = await _forms.DeleteFormAndResponsesAsync(form.Id);
            if (!ok) throw new KeyNotFoundException("Form not found.");
        }

        // ---- Assignments ----
        public async Task AssignUserAsync(int formKey, long userId)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");
            await _assignments.AssignAsync(form.Id, userId);
        }

        public async Task UnassignUserAsync(int formKey, long userId)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");
            var ok = await _assignments.UnassignAsync(form.Id, userId);
            if (!ok) throw new KeyNotFoundException("Assignment not found.");
        }

        public async Task<IEnumerable<object>> ListAssigneesAsync(int formKey)
        {
            var form = await _forms.GetByFormKeyAsync(formKey)
                       ?? throw new KeyNotFoundException("Form not found.");

            var list = await _assignments.ListAssigneesAsync(form.Id);

            return list.Select(a => new
            {
                assignmentId = a.SequenceNo > 0 ? (long)a.SequenceNo : a.Id,
                formId       = a.FormId,
                userId       = a.UserId,
                assignedAt   = a.AssignedAt
            });
        }
    }
}