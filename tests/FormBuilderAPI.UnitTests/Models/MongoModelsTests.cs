using System;
using System.Collections.Generic;
using FluentAssertions;
using FormBuilderAPI.Models.MongoModels;
using Xunit;

namespace FormBuilderAPI.UnitTests.Models.MongoModels
{
    public class CounterTests
    {
        [Fact]
        public void Counter_should_set_and_get_properties()
        {
            var c = new Counter
            {
                Id = "FormKey",
                Value = 123
            };

            c.Id.Should().Be("FormKey");
            c.Value.Should().Be(123);
        }
    }

    public class FormModelsTests
    {
        [Fact]
        public void Form_should_have_expected_defaults()
        {
            var f = new Form();

            f.Id.Should().BeNull();                          // string default! (unset until DB)
            f.FormKey.Should().BeNull();                     // not assigned yet
            f.Title.Should().BeNull();                       // default! until you set it
            f.Description.Should().BeNull();
            f.Status.Should().Be("Draft");                   // default set in model
            f.Access.Should().Be("Open");                    // default set in model
            f.CreatedBy.Should().Be("system");               // default set in model
            f.PublishedAt.Should().BeNull();
            f.CreatedAt.Should().Be(default);                // not set until service
            f.UpdatedAt.Should().BeNull();
            f.Layout.Should().NotBeNull().And.BeEmpty();     // initialized list
        }

        [Fact]
        public void Form_should_set_and_get_properties()
        {
            var when = DateTime.UtcNow;

            var f = new Form
            {
                Id = "64f0c7c2f1a3a9a1e0c23abc",
                FormKey = 42,
                Title = "Employee Onboarding",
                Description = "Collect employee details",
                Status = "Published",
                Access = "Open",
                CreatedBy = "admin@site.com",
                PublishedAt = when,
                CreatedAt = when.AddMinutes(-5),
                UpdatedAt = when
            };

            f.Id.Should().Be("64f0c7c2f1a3a9a1e0c23abc");
            f.FormKey.Should().Be(42);
            f.Title.Should().Be("Employee Onboarding");
            f.Description.Should().Be("Collect employee details");
            f.Status.Should().Be("Published");
            f.Access.Should().Be("Open");
            f.CreatedBy.Should().Be("admin@site.com");
            f.PublishedAt.Should().Be(when);
            f.CreatedAt.Should().Be(when.AddMinutes(-5));
            f.UpdatedAt.Should().Be(when);
        }

        [Fact]
        public void Form_layout_should_support_sections_fields_and_options()
        {
            var section = new FormSection
            {
                SectionId = "sec-1",
                Title = "Personal Info",
                Description = "Tell us about you",
                Fields = new List<FormField>
                {
                    new FormField
                    {
                        FieldId = "f-firstname",
                        Label = "First name",
                        Type = "shortText",
                        IsRequired = true
                    },
                    new FormField
                    {
                        FieldId = "f-gender",
                        Label = "Gender",
                        Type = "radio",
                        Options = new List<FieldOption>
                        {
                            new FieldOption { Id = "opt-m", Text = "Male" },
                            new FieldOption { Id = "opt-f", Text = "Female" },
                            new FieldOption { Id = "opt-o", Text = "Other" },
                        }
                    }
                }
            };

            var form = new Form
            {
                Title = "Demo",
                Layout = new List<FormSection> { section }
            };

            form.Layout.Should().HaveCount(1);
            var s = form.Layout[0];

            s.SectionId.Should().Be("sec-1");
            s.Title.Should().Be("Personal Info");
            s.Description.Should().Be("Tell us about you");
            s.Fields.Should().HaveCount(2);

            var firstName = s.Fields[0];
            firstName.FieldId.Should().Be("f-firstname");
            firstName.Label.Should().Be("First name");
            firstName.Type.Should().Be("shortText");
            firstName.IsRequired.Should().BeTrue();
            firstName.Options.Should().BeNull(); // not a choice field

            var gender = s.Fields[1];
            gender.FieldId.Should().Be("f-gender");
            gender.Label.Should().Be("Gender");
            gender.Type.Should().Be("radio");
            gender.IsRequired.Should().BeFalse();
            gender.Options.Should().NotBeNull().And.HaveCount(3);
            gender.Options![0].Should().BeEquivalentTo(new FieldOption { Id = "opt-m", Text = "Male" });
        }

        [Fact]
        public void FormSection_should_set_and_get_properties()
        {
            var s = new FormSection
            {
                SectionId = "abc",
                Title = "Details",
                Description = "desc",
                Fields = new List<FormField>()
            };

            s.SectionId.Should().Be("abc");
            s.Title.Should().Be("Details");
            s.Description.Should().Be("desc");
            s.Fields.Should().NotBeNull();
        }

        [Fact]
        public void FormField_should_set_and_get_properties()
        {
            var f = new FormField
            {
                FieldId = "f1",
                Label = "Age",
                Type = "number",
                IsRequired = true,
                Options = null
            };

            f.FieldId.Should().Be("f1");
            f.Label.Should().Be("Age");
            f.Type.Should().Be("number");
            f.IsRequired.Should().BeTrue();
            f.Options.Should().BeNull();
        }

        [Fact]
        public void FieldOption_should_set_and_get_properties()
        {
            var o = new FieldOption
            {
                Id = "x1",
                Text = "Choice A"
            };

            o.Id.Should().Be("x1");
            o.Text.Should().Be("Choice A");
        }
    }

    public class WorkflowTests
    {
        [Fact]
        public void Workflow_should_have_expected_defaults()
        {
            var w = new Workflow();

            w.Id.Should().BeNull();                  // not set until persisted
            w.FormId.Should().BeNull();              // default! until set
            w.UsageCount.Should().Be(0);             // default in model
            w.Name.Should().Be("Default Workflow");  // default in model
            w.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, precision: TimeSpan.FromSeconds(5));
        }

        [Fact]
        public void Workflow_should_set_and_get_properties()
        {
            var when = DateTime.UtcNow.AddDays(-1);

            var w = new Workflow
            {
                Id = "6500000000000000000000aa",
                FormId = "6500000000000000000000ff",
                UsageCount = 3,
                Name = "WF-1",
                CreatedAt = when
            };

            w.Id.Should().Be("6500000000000000000000aa");
            w.FormId.Should().Be("6500000000000000000000ff");
            w.UsageCount.Should().Be(3);
            w.Name.Should().Be("WF-1");
            w.CreatedAt.Should().Be(when);
        }
    }
}