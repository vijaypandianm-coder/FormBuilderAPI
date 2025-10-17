using System;
using System.ComponentModel.DataAnnotations;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using FormBuilderAPI.DTOs;
using Xunit;

namespace FormBuilderAPI.UnitTests.DTOs
{
    public class DtoTests
    {
        [Fact]
        public void RegisterRequest_Defaults_ShouldBeCorrect()
        {
            var dto = new RegisterRequest();
            dto.Username.Should().BeEmpty();
            dto.Email.Should().BeEmpty();
            dto.Password.Should().BeEmpty();
            dto.Role.Should().BeNull();
        }

        [Fact]
        public void LoginRequest_Defaults_ShouldBeCorrect()
        {
            var dto = new LoginRequest();
            dto.Email.Should().BeEmpty();
            dto.Password.Should().BeEmpty();
        }

        [Fact]
        public void AuthResultDto_Defaults_ShouldBeCorrect()
        {
            var dto = new AuthResultDto();
            dto.Token.Should().BeEmpty();
            dto.Scheme.Should().Be("Bearer");
            dto.ExpiresUtc.Should().Be(default);
            dto.Role.Should().BeEmpty();
            dto.Email.Should().BeEmpty();
            dto.UserId.Should().Be(0);
        }

        [Fact]
        public void FieldCreateDto_Should_Honor_DataAnnotations()
        {
            var dto = new FieldCreateDto
            {
                Label = new string('a', 101), // invalid, >100
                Type = new string('b', 31)    // invalid, >30
            };

            var ctx = new ValidationContext(dto);
            var results = new System.Collections.Generic.List<ValidationResult>();
            Validator.TryValidateObject(dto, ctx, results, validateAllProperties: true);

            results.Should().Contain(r => r.ErrorMessage!.Contains("Label max length"));
            results.Should().Contain(r => r.MemberNames.Contains("Type"));
        }

        [Fact]
        public void AccessPatchDto_Should_Serialize_With_LowercaseName()
        {
            var dto = new AccessPatchDto { Access = "Restricted" };
            var json = JsonSerializer.Serialize(dto);
            json.Should().Contain("\"access\"");
            json.Should().Contain("Restricted");
        }

        [Fact]
        public void StatusPatchDto_Should_Serialize_With_LowercaseName()
        {
            var dto = new StatusPatchDto { Status = "Published" };
            var json = JsonSerializer.Serialize(dto);
            json.Should().Contain("\"status\"");
            json.Should().Contain("Published");
        }

        [Fact]
        public void FormOutDto_Defaults_ShouldBeCorrect()
        {
            var dto = new FormOutDto();
            dto.Status.Should().Be("Draft");
            dto.Access.Should().Be("Open");
            dto.CreatedBy.Should().Be("system");
        }

        [Fact]
        public void SubmitResponseDto_Can_Hold_Answers()
        {
            var dto = new SubmitResponseDto();
            dto.Answers.Should().NotBeNull();
            dto.Answers.Add(new SubmitAnswerDto
            {
                FieldId = "f1",
                AnswerValue = "val"
            });
            dto.Answers.Should().ContainSingle(a => a.FieldId == "f1" && a.AnswerValue == "val");
        }
    }
}