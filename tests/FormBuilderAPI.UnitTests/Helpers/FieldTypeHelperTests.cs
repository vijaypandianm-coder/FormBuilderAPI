using FluentAssertions;
using FormBuilderAPI.Helpers;
using Xunit;

namespace FormBuilderAPI.UnitTests.Helpers;

public class FieldTypeHelperTests
{
    [Theory]
    [InlineData("radio", true)]
    [InlineData("dropdown", true)]
    [InlineData("checkbox", true)]
    [InlineData("multiselect", true)]
    [InlineData("mcq", true)]
    [InlineData("multiple", true)]
    [InlineData("shortText", false)]
    [InlineData("number", false)]
    [InlineData(null, false)]
    public void IsChoice_ReturnsExpected(string? type, bool expected)
        => FieldTypeHelper.IsChoice(type).Should().Be(expected);
}
