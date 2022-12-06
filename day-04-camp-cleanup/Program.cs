// Day 4: Camp Cleanup
// https://adventofcode.com/2022/day/4

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

var fullyContainedCount = 0;
var parseLinesEnumberable =
    FilePipelineParser.ParseLinesAsync<AssignmentPair>("input.txt", Encoding.UTF8, CultureInfo.InvariantCulture);
await foreach (var assignmentPair in parseLinesEnumberable)
{
    if (assignmentPair.HasFullyContainedRange())
        fullyContainedCount++;
}

// part 1
Console.WriteLine($"Assignment pairs where one range fully contains the other: {fullyContainedCount}");

public record AssignmentPair : ISpanParsable<AssignmentPair>
{
    public Range Elf1 { get; init; }
    public Range Elf2 { get; init; }

    public bool HasFullyContainedRange() =>
        Elf1.FullyContains(Elf2) || Elf2.FullyContains(Elf1);

    public static AssignmentPair Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException();
    }

    public static AssignmentPair Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out AssignmentPair result)
    {
        var separatorIndex = s.IndexOf(',');
        if (separatorIndex >= 0 &&
            TryParseRange(s.Slice(0, separatorIndex), out var elf1) &&
            TryParseRange(s.Slice(separatorIndex + 1), out var elf2))
        {
            result = new AssignmentPair
            {
                Elf1 = elf1,
                Elf2 = elf2
            };
            return true;
        }

        result = default;
        return false;
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider,
        [MaybeNullWhen(false)] out AssignmentPair result)
    {
        if (s != null)
            return TryParse(s.AsSpan(), provider, out result);

        result = default;
        return false;
    }

    private static bool TryParseRange(ReadOnlySpan<char> s, out Range result)
    {
        var separatorIndex = s.IndexOf('-');
        if (separatorIndex >= 0 &&
            int.TryParse(s.Slice(0, separatorIndex), out var start) &&
            int.TryParse(s.Slice(separatorIndex + 1), out var end))
        {
            result = new Range(start, end);
            return true;
        }

        result = default;
        return false;
    }
}

public static class RangeExtensions
{
    public static bool FullyContains(this Range source, Range other) =>
        source.Start.Value <= other.Start.Value &&
        source.End.Value >= other.End.Value;
}
