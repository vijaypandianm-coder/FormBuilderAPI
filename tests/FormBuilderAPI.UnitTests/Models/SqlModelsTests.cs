using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json.Serialization;
using FluentAssertions;
using FormBuilderAPI.Models.SqlModels;
using Xunit;

namespace FormBuilderAPI.UnitTests.Models.SqlModels
{
    public class FormKeyTests
    {
        [Fact]
        public void Attributes_should_match_schema_Contract()
        {
            typeof(FormKey).GetCustomAttributes(typeof(TableAttribute), inherit: false)
                .Cast<TableAttribute>().Single().Name.Should().Be("formkeys");

            var idProp = typeof(FormKey).GetProperty(nameof(FormKey.Id))!;
            idProp.GetCustomAttributes(typeof(KeyAttribute), false).Should().HaveCount(1);
            idProp.GetCustomAttributes(typeof(ColumnAttribute), false)
                .Cast<ColumnAttribute>().Single().Name.Should().Be("FormKey");

            var formIdProp = typeof(FormKey).GetProperty(nameof(FormKey.FormId))!;
            formIdProp.GetCustomAttributes(typeof(RequiredAttribute), false).Should().HaveCount(1);
            formIdProp.GetCustomAttributes(typeof(ColumnAttribute), false)
                .Cast<ColumnAttribute>().Single().TypeName.Should().Be("varchar(24)");
        }

        [Fact]
        public void Properties_should_set_and_get()
        {
            var model = new FormKey
            {
                Id = 7,
                FormId = "6500000000000000000000ab"
            };

            model.Id.Should().Be(7);
            model.FormId.Should().Be("6500000000000000000000ab");
        }
    }

    public class FormAssignmentTests
    {
        [Fact]
        public void Attributes_should_match_schema_Contract()
        {
            typeof(FormAssignment).GetCustomAttributes(typeof(TableAttribute), false)
                .Cast<TableAttribute>().Single().Name.Should().Be("formassignments");

            var formIdProp = typeof(FormAssignment).GetProperty(nameof(FormAssignment.FormId))!;
            formIdProp.GetCustomAttributes(typeof(RequiredAttribute), false).Should().HaveCount(1);
            formIdProp.GetCustomAttributes(typeof(ColumnAttribute), false)
                .Cast<ColumnAttribute>().Single().TypeName.Should().Be("varchar(24)");

            var userIdProp = typeof(FormAssignment).GetProperty(nameof(FormAssignment.UserId))!;
            userIdProp.GetCustomAttributes(typeof(RequiredAttribute), false).Should().HaveCount(1);
        }

        [Fact]
        public void Defaults_should_be_initialized()
        {
            var before = DateTime.UtcNow;
            var model = new FormAssignment();
            var after = DateTime.UtcNow;

            model.AssignedAt.Should().BeOnOrAfter(before).And.BeOnOrBefore(after);
            model.AssignedBy.Should().BeNull();
            model.SequenceNo.Should().BeNull();
        }

        [Fact]
        public void Properties_should_set_and_get()
        {
            var when = DateTime.UtcNow.AddMinutes(-10);

            var model = new FormAssignment
            {
                Id = 123,
                FormId = "6500000000000000000000ff",
                UserId = 9876,
                AssignedAt = when,
                AssignedBy = 42,
                SequenceNo = 3
            };

            model.Id.Should().Be(123);
            model.FormId.Should().Be("6500000000000000000000ff");
            model.UserId.Should().Be(9876);
            model.AssignedAt.Should().Be(when);
            model.AssignedBy.Should().Be(42);
            model.SequenceNo.Should().Be(3);
        }
    }

    public class FormResponseAnswerTests
    {
        [Fact]
        public void Attributes_should_match_schema_Contract()
        {
            typeof(FormResponseAnswer).GetCustomAttributes(typeof(TableAttribute), false)
                .Cast<TableAttribute>().Single().Name.Should().Be("formresponseanswers");

            var idProp = typeof(FormResponseAnswer).GetProperty(nameof(FormResponseAnswer.Id))!;
            idProp.GetCustomAttributes(typeof(KeyAttribute), false).Should().HaveCount(1);

            var responseIdProp = typeof(FormResponseAnswer).GetProperty(nameof(FormResponseAnswer.ResponseId))!;
            responseIdProp.GetCustomAttributes(typeof(ForeignKeyAttribute), false)
                .Cast<ForeignKeyAttribute>().Single().Name.Should().Be(nameof(FormResponseAnswer.FormResponse));

            var formIdProp = typeof(FormResponseAnswer).GetProperty(nameof(FormResponseAnswer.FormId))!;
            formIdProp.GetCustomAttributes(typeof(ColumnAttribute), false)
                .Cast<ColumnAttribute>().Single().TypeName.Should().Be("varchar(255)");

            var fieldIdProp = typeof(FormResponseAnswer).GetProperty(nameof(FormResponseAnswer.FieldId))!;
            fieldIdProp.GetCustomAttributes(typeof(RequiredAttribute), false).Should().HaveCount(1);
            fieldIdProp.GetCustomAttributes(typeof(ColumnAttribute), false)
                .Cast<ColumnAttribute>().Single().TypeName.Should().Be("varchar(255)");

            var fieldTypeProp = typeof(FormResponseAnswer).GetProperty(nameof(FormResponseAnswer.FieldType))!;
            fieldTypeProp.GetCustomAttributes(typeof(ColumnAttribute), false)
                .Cast<ColumnAttribute>().Single().TypeName.Should().Be("varchar(32)");

            var navProp = typeof(FormResponseAnswer).GetProperty(nameof(FormResponseAnswer.FormResponse))!;
            navProp.GetCustomAttributes(typeof(JsonIgnoreAttribute), false).Should().HaveCount(1);
        }

        [Fact]
        public void Defaults_should_be_unset_until_assigned()
        {
            var model = new FormResponseAnswer();

            model.Id.Should().Be(0);
            model.ResponseId.Should().Be(0);
            model.FormResponse.Should().BeNull();
            model.FormKey.Should().BeNull();
            model.FormId.Should().BeNull();
            model.UserId.Should().Be(0);
            model.FieldId.Should().BeNull();   // default! not set yet
            model.FieldType.Should().BeNull();
            model.AnswerValue.Should().BeNull();
            model.SubmittedAt.Should().Be(default);
        }

        [Fact]
        public void Properties_should_set_and_get()
        {
            var when = DateTime.UtcNow;

            var model = new FormResponseAnswer
            {
                Id = 11,
                ResponseId = 22,
                FormKey = 123,
                FormId = "650000000000000000000099",
                UserId = 999,
                FieldId = "field-1",
                FieldType = "number",
                AnswerValue = "42",
                SubmittedAt = when
            };

            model.Id.Should().Be(11);
            model.ResponseId.Should().Be(22);
            model.FormKey.Should().Be(123);
            model.FormId.Should().Be("650000000000000000000099");
            model.UserId.Should().Be(999);
            model.FieldId.Should().Be("field-1");
            model.FieldType.Should().Be("number");
            model.AnswerValue.Should().Be("42");
            model.SubmittedAt.Should().Be(when);
        }

        [Fact]
        public void AnswerValue_may_be_null()
        {
            var model = new FormResponseAnswer
            {
                FieldId = "x"
            };

            model.AnswerValue.Should().BeNull();
        }
    }
}