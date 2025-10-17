using FormBuilderAPI.Models.MongoModels;
using System;
using System.Collections.Generic;

namespace FormBuilderAPI.UnitTests.Helpers
{
    public static class TestDataFactory
    {
        public static Form CreateSampleForm(string id = "507f1f77bcf86cd799439011", int formKey = 1, string status = "Draft")
        {
            return new Form
            {
                Id = id,
                FormKey = formKey,
                Title = "Sample Form",
                Description = "This is a test form",
                Status = status,
                Access = "Open",
                CreatedBy = "tester",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                Layout = new List<FormSection>
                {
                    new FormSection
                    {
                        SectionId = Guid.NewGuid().ToString("N"),
                        Title = "Section 1",
                        Description = "Sample Section",
                        Fields = new List<FormField>
                        {
                            new FormField
                            {
                                FieldId = Guid.NewGuid().ToString("N"),
                                Label = "Name",
                                Type = "shorttext",
                                IsRequired = true
                            }
                        }
                    }
                }
            };
        }
    }
}