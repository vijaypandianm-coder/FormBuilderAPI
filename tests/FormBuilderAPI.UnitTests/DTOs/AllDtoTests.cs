using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text.Json;
using FluentAssertions;
using Xunit;

// project DTOs
using FormBuilderAPI.DTOs;
// controller-local request types (AuthController defines these too)
using ControllerRegisterRequest = FormBuilderAPI.Controllers.RegisterRequest;
using ControllerLoginRequest    = FormBuilderAPI.Controllers.LoginRequest;

namespace FormBuilderAPI.UnitTests.DTOs
{
    public class AllDtoTests
    {
        // ---------- helpers ----------
        private static List<ValidationResult> Validate(object instance)
        {
            var ctx = new ValidationContext(instance);
            var results = new List<ValidationResult>();
            Validator.TryValidateObject(instance, ctx, results, validateAllProperties: true);
            return results;
        }

        // ---------- Auth DTOs (namespace FormBuilderAPI.DTOs) ----------
        [Fact]
        public void RegisterRequest_Defaults_Are_Empty()
        {
            var dto = new RegisterRequest();
            dto.Username.Should().BeEmpty();
            dto.Email.Should().BeEmpty();
            dto.Password.Should().BeEmpty();
            dto.Role.Should().BeNull();           // optional here
        }

        [Fact]
        public void LoginRequest_Defaults_Are_Empty()
        {
            var dto = new LoginRequest();
            dto.Email.Should().BeEmpty();
            dto.Password.Should().BeEmpty();
        }

        [Fact]
        public void AuthResultDto_Defaults_Are_Correct()
        {
            var dto = new AuthResultDto();
            dto.Token.Should().BeEmpty();
            dto.Scheme.Should().Be("Bearer");
            dto.ExpiresUtc.Should().Be(default);
            dto.Role.Should().BeEmpty();
            dto.Email.Should().BeEmpty();
            dto.UserId.Should().Be(0);
        }

        // ---------- Field DTOs ----------
        [Fact]
        public void FieldCreateDto_Honors_DataAnnotations()
        {
            var valid = new FieldCreateDto { Label = "Q1", Type = "text", IsRequired = true };
            Validate(valid).Should().BeEmpty();

            var invalid = new FieldCreateDto
            {
                Label = new string('x', 101),   // > 100
                Type  = new string('y', 31)     // > 30
            };
            var results = Validate(invalid);

            results.Should().Contain(r => r.MemberNames.Contains(nameof(FieldCreateDto.Label)));
            results.Should().Contain(r => r.MemberNames.Contains(nameof(FieldCreateDto.Type)));
        }

        [Fact]
        public void FieldUpdateDto_Inherits_FieldCreateDto()
        {
            var dto = new FieldUpdateDto { Label = "L", Type = "text" };
            Validate(dto).Should().BeEmpty();
        }

        // ---------- Form creation meta ----------
        [Fact]
        public void FormCreateDto_Can_Be_Constructed()
        {
            var dto = new FormCreateDto { Title = "T" };
            dto.Title.Should().Be("T");
            dto.Description.Should().BeNull();
        }

        // ---------- Form OUT / layout chain ----------
        [Fact]
        public void FormOutDto_Defaults_And_Layout_Null_By_Default()
        {
            var dto = new FormOutDto();
            dto.Status.Should().Be("Draft");
            dto.Access.Should().Be("Open");
            dto.CreatedBy.Should().Be("system");
            dto.Layout.Should().BeNull();
        }

        [Fact]
        public void FormSectionDto_Defaults_And_Fields_List_Init()
        {
            var dto = new FormSectionDto { Title = "Section A" };
            dto.Fields.Should().NotBeNull().And.BeEmpty();
            dto.Description.Should().BeNull();
        }

        [Fact]
        public void FormFieldDto_Defaults_And_Options_Null()
        {
            var dto = new FormFieldDto { Label = "Name" };
            dto.Type.Should().Be("text");
            dto.Options.Should().BeNull();
        }

        [Fact]
        public void FieldOptionDto_Can_Hold_Id_And_Text()
        {
            var dto = new FieldOptionDto { Id = "o1", Text = "Yes" };
            dto.Id.Should().Be("o1");
            dto.Text.Should().Be("Yes");
        }

        [Fact]
        public void FormMetaDto_Basic_Assignment()
        {
            var dto = new FormMetaDto { Title = "New", Description = "Desc" };
            dto.Title.Should().Be("New");
            dto.Description.Should().Be("Desc");
        }

        [Fact]
        public void FormLayoutDto_And_FormSectionCreateDto_Default_Collections()
        {
            var layout = new FormLayoutDto();
            layout.Sections.Should().NotBeNull().And.BeEmpty();

            var section = new FormSectionCreateDto { Title = "S1" };
            section.Fields.Should().NotBeNull().And.BeEmpty();
        }

        [Fact]
        public void SingleFieldDto_Defaults_And_Options_Null()
        {
            var dto = new SingleFieldDto { Label = "Age", Type = "number", IsRequired = true };
            dto.Options.Should().BeNull();
        }

        // ---------- Patch DTOs / JSON names ----------
        [Fact]
        public void AccessPatchDto_Serializes_With_access_Lowercase()
        {
            var dto = new AccessPatchDto { Access = "Restricted" };
            var json = JsonSerializer.Serialize(dto);
            json.Should().Contain("\"access\":\"Restricted\"");
        }

        [Fact]
        public void StatusPatchDto_Serializes_With_status_Lowercase()
        {
            var dto = new StatusPatchDto { Status = "Published" };
            var json = JsonSerializer.Serialize(dto);
            json.Should().Contain("\"status\":\"Published\"");
        }

        // ---------- Responses ----------
        [Fact]
        public void SubmitAnswerDto_Can_Hold_Either_Value_Or_Options()
        {
            var a1 = new SubmitAnswerDto { FieldId = "f1", AnswerValue = "hello" };
            a1.OptionIds.Should().BeNull();

            var a2 = new SubmitAnswerDto { FieldId = "f2", OptionIds = new List<string> { "o1", "o2" } };
            a2.OptionIds.Should().ContainInOrder("o1", "o2");
            a2.AnswerValue.Should().BeNull();
        }

        [Fact]
        public void SubmitResponseDto_Answers_List_Initialized()
        {
            var r = new SubmitResponseDto();
            r.Answers.Should().NotBeNull().And.BeEmpty();

            r.Answers.Add(new SubmitAnswerDto { FieldId = "f1", AnswerValue = "x" });
            r.Answers.Should().HaveCount(1);
        }

        [Fact]
        public void Nested_Layout_RoundTrip_Json_Should_Work()
        {
            var layout = new FormLayoutDto
            {
                Sections = new List<FormSectionCreateDto>
                {
                    new()
                    {
                        SectionId = "s1",
                        Title = "Questions",
                        Fields = new List<FieldCreateDto>
                        {
                            new() { FieldId = "f1", Label = "Name", Type = "text", IsRequired = true },
                            new() { FieldId = "f2", Label = "Choice", Type = "dropdown", Options = new() { "A", "B" } }
                        }
                    }
                }
            };

            var json = JsonSerializer.Serialize(layout);
            var back = JsonSerializer.Deserialize<FormLayoutDto>(json);
            back.Should().NotBeNull();
            back!.Sections.Should().HaveCount(1);
            back.Sections[0].Fields.Should().HaveCount(2);
            back.Sections[0].Fields[0].Label.Should().Be("Name");
            back.Sections[0].Fields[1].Type.Should().Be("dropdown");
        }

        // ---------- Controller-local Auth request types ----------
        [Fact]
        public void Controller_RegisterRequest_Defaults_Are_Empty()
        {
            var dto = new ControllerRegisterRequest();
            dto.Username.Should().BeEmpty();
            dto.Email.Should().BeEmpty();
            dto.Password.Should().BeEmpty();
            dto.Role.Should().Be("Learner"); // controller class defaults to Learner
        }

        [Fact]
        public void Controller_LoginRequest_Defaults_Are_Empty()
        {
            var dto = new ControllerLoginRequest();
            dto.Email.Should().BeEmpty();
            dto.Password.Should().BeEmpty();
        }
    }
}