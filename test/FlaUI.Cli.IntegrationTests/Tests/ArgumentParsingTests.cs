using System.CommandLine;
using System.Globalization;
using FlaUI.Cli.Commands.Elem;
using FlaUI.Cli.Infrastructure;

namespace FlaUI.Cli.IntegrationTests.Tests;

public class ArgumentParsingTests
{
    [Theory]
    [InlineData("fi-FI")]
    [InlineData("en-US")]
    public void Scroll_DecimalPointHasSameMeaning(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var command = ScrollCommand.Create(new Option<string?>("--session"));
            var parsed = command.Parse(["--id", "grid", "--vertical", "12.5"]);
            Assert.Empty(parsed.Errors);
            Assert.Equal(12.5, parsed.GetValue(command.Options.OfType<Option<double?>>().Single(o => o.Name == "--vertical")));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("12,5")]
    [InlineData("NaN")]
    [InlineData("Infinity")]
    public void Scroll_RejectsAmbiguousOrNonfiniteValue(string value)
    {
        var command = ScrollCommand.Create(new Option<string?>("--session"));
        var parsed = command.Parse(["--id", "grid", "--vertical", value]);
        Assert.NotEmpty(parsed.Errors);
    }

    [Theory]
    [InlineData("fi-FI")]
    [InlineData("en-US")]
    public void RangeAndTransform_UseInvariantNumbers(string culture)
    {
        var previous = CultureInfo.CurrentCulture;
        try
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(culture);
            var range = SetRangeCommand.Create(new Option<string?>("--session"));
            var parsedRange = range.Parse(["--id", "slider", "--value", "12.5"]);
            Assert.Empty(parsedRange.Errors);
            Assert.Equal(12.5, parsedRange.GetValue(range.Options.OfType<Option<double>>().Single()));
            var transform = TransformCommand.Create(new Option<string?>("--session"));
            var parsedTransform = transform.Parse(["--id", "window", "--x", "-12.5", "--rotate", "12.5"]);
            Assert.Empty(parsedTransform.Errors);
            Assert.Equal(-12.5, parsedTransform.GetValue(transform.Options.OfType<Option<double?>>().Single(o => o.Name == "--x")));
        }
        finally { CultureInfo.CurrentCulture = previous; }
    }

    [Theory]
    [InlineData("38C1D86")]
    [InlineData("0x38C1D86")]
    [InlineData("0X38c1d86")]
    public void Handles_AcceptReturnedHexForms(string value) => Assert.Equal(0x38C1D86, WindowHandleParser.Parse(value));

    [Theory]
    [InlineData("")]
    [InlineData("0x")]
    [InlineData("0")]
    [InlineData("invalid")]
    [InlineData("0x10000000000000000")]
    public void Handles_RejectInvalidForms(string value) => Assert.Throws<FormatException>(() => WindowHandleParser.Parse(value));
}
