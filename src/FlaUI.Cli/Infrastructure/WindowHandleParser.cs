using System.Globalization;

namespace FlaUI.Cli.Infrastructure;

internal static class WindowHandleParser
{
    internal static long Parse(string value)
    {
        var hex = value.AsSpan();
        if (hex.StartsWith("0x", StringComparison.OrdinalIgnoreCase)) hex = hex[2..];
        if (hex.IsEmpty || !ulong.TryParse(hex, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out var parsed)
            || parsed == 0 || (IntPtr.Size == 4 && parsed > uint.MaxValue))
            throw new FormatException($"Invalid window handle '{value}': expected a nonzero hexadecimal handle, optionally prefixed with 0x.");
        return unchecked((long)parsed);
    }
}
