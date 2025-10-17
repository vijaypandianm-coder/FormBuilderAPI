using System;
using FluentAssertions;
using FormBuilderAPI.Helpers;
using Xunit;

namespace FormBuilderAPI.UnitTests.Helpers
{
    public class ValidationHelperTests
    {
        [Theory]
        [InlineData("123", true)]
        [InlineData("-42", true)]
        [InlineData("0", true)]
        [InlineData("999999", true)]
        [InlineData("12.34", false)]
        [InlineData("abc", false)]
        [InlineData("", false)]
        [InlineData(null, false)]
        public void IsInteger_ReturnsExpected(string? input, bool expected)
        {
            ValidationHelper.IsInteger(input).Should().Be(expected);
        }

        [Theory]
        [InlineData("25/12/2025", true, 2025, 12, 25)]   // valid Christmas date
        [InlineData("01/01/2000", true, 2000, 1, 1)]     // Y2K date
        [InlineData("31/02/2024", false, 0, 0, 0)]       // invalid day
        [InlineData("2024-12-25", false, 0, 0, 0)]       // wrong format
        [InlineData("", false, 0, 0, 0)]                 // empty string
        [InlineData(null, false, 0, 0, 0)]               // null
        public void TryParseDateDdMmYyyy_ReturnsExpected(
            string? input, bool expectedSuccess, int year, int month, int day)
        {
            var success = ValidationHelper.TryParseDateDdMmYyyy(input, out var parsed);

            success.Should().Be(expectedSuccess);

            if (expectedSuccess)
            {
                parsed.Year.Should().Be(year);
                parsed.Month.Should().Be(month);
                parsed.Day.Should().Be(day);
            }
        }

        [Fact]
        public void Constants_AreExpectedValues()
        {
            ValidationHelper.ShortTextMax.Should().Be(100);
            ValidationHelper.LongTextMax.Should().Be(500);
            ValidationHelper.DateFormat.Should().Be("dd/MM/yyyy");
        }
    }
}