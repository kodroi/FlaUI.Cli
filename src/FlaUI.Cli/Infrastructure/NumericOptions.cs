using System.CommandLine;
using System.CommandLine.Parsing;
using System.Globalization;

namespace FlaUI.Cli.Infrastructure;

internal static class NumericOptions
{
    internal static Option<double?> Optional(string name, string description) => new(name)
    {
        Description = description,
        CustomParser = result => Parse(result)
    };

    internal static Option<double> Required(string name, string description) => new(name)
    {
        Description = description,
        CustomParser = result => Parse(result) ?? 0
    };

    private static double? Parse(ArgumentResult result)
    {
        if (result.Tokens.Count == 0) return null;
        var text = result.Tokens[0].Value;
        if (double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out var value) && double.IsFinite(value))
            return value;
        result.AddError($"Invalid number '{text}': use a finite number with a decimal point and no grouping separators.");
        return null;
    }
}
