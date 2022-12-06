// Day 4: Camp Cleanup
// https://adventofcode.com/2022/day/4

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var sw = Stopwatch.StartNew();

        var resultTasks = new[]
        {
            Solution.GetResultsPipeReaderAsyncEnumerableAsync(),
            Solution.GetResultsPipeWriterReaderAsync(),
            Solution.GetResultsReadAllBytesSpanAsync(),
            Solution.GetResultsReadAllLinesAsync(),
            Solution.GetResultsReadLinesAsync(),
            Solution.GetResultsStreamReaderAsync()
        };

        await Task.WhenAll(resultTasks);

        sw.Stop();

        var result = await resultTasks[0];
        for (int i = 1; i < resultTasks.Length; i++)
        {
            var resultN = await resultTasks[i];
            if (resultN != result)
                Console.WriteLine($"Mismatch: {resultN} doesn't match {result}");
        }

        // part 1
        Console.WriteLine($"Assignment pairs where one range fully contains the other: {result.FullyContainedCount}");

        // part 2
        Console.WriteLine($"Assignment pairs with overlapping ranges: {result.OverlapCount}");

        Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N2} ms for {result.TotalCount:N0} records");
    }
}

public static class Solution
{
    private const string FilePath = @"input.txt";
    private static Encoding Utf8Encoding = Encoding.UTF8;
    private static IFormatProvider? Provider = CultureInfo.InvariantCulture;

    public static async Task<Results> GetResultsPipeWriterReaderAsync()
    {
        var results = new Results();
        var assignmentPairsTask =
            FilePipelineParser.ParseFileAsync<AssignmentPair>(FilePath, Utf8Encoding, Provider);
        foreach (var assignmentPair in await assignmentPairsTask)
            results.Update(assignmentPair);

        return results;
    }

    public static async Task<Results> GetResultsPipeReaderAsyncEnumerableAsync()
    {
        var results = new Results();
        var assignmentPairsAsyncEnumerable =
            FilePipelineParser.ParseLinesAsync<AssignmentPair>(FilePath, Utf8Encoding, Provider);
        await foreach (var assignmentPair in assignmentPairsAsyncEnumerable)
            results.Update(assignmentPair);

        return results;
    }

    public static async Task<Results> GetResultsReadAllBytesSpanAsync()
    {
        var results = new Results();
        Memory<char> charMemory = new char[256];
        Memory<byte> memory = await File.ReadAllBytesAsync(FilePath);
        int start = 0, end = 0;
        while (start < memory.Length)
        {
            var index = memory.Span.Slice(start).IndexOf((byte)'\n');
            end = index >= 0 ? start + index : memory.Length - 1;

            var assignmentPair = Parse(memory.Slice(start, end - start + 1).Span, charMemory, Provider);
            results.Update(assignmentPair);
            
            start = end + 1;
        }

        return results;

        AssignmentPair Parse(Span<byte> line, Memory<char> charMemory, IFormatProvider? provider)
        {
            var charBuffer = charMemory.Span;
            var length = Utf8Encoding.GetChars(line, charBuffer);
            var chars = charBuffer.Slice(0, length).Trim();
            return AssignmentPair.Parse(chars, provider);
        }
    }

    public static async Task<Results> GetResultsReadAllLinesAsync()
    {
        var results = new Results();
        var allLines = await File.ReadAllLinesAsync(FilePath, Utf8Encoding);
        foreach (var line in allLines)
        {
            var assignmentPair = AssignmentPair.ParseAllocating(line);
            results.Update(assignmentPair);
        }

        return results;
    }

    public static async Task<Results> GetResultsReadLinesAsync()
    {
        var results = new Results();
        var linesAsyncEnumerable = File.ReadLinesAsync(FilePath, Utf8Encoding);
        await foreach (var line in linesAsyncEnumerable)
        {
            var assignmentPair = AssignmentPair.ParseAllocating(line);
            results.Update(assignmentPair);
        }

        return results;
    }

    public static async Task<Results> GetResultsStreamReaderAsync()
    {
        var results = new Results();
        using var streamReader = new StreamReader(FilePath, Utf8Encoding);
        while (true)
        {
            var line = await streamReader.ReadLineAsync();
            if (line == null)
                break;

            var assignmentPair = AssignmentPair.ParseAllocating(line);
            results.Update(assignmentPair);
        }

        return results;
    }
}

public record struct Results
{
    public int TotalCount { get; set; }
    public int FullyContainedCount { get; set; }
    public int OverlapCount { get; set; }

    public void Update(AssignmentPair assignmentPair)
    {
        TotalCount++;

        if (assignmentPair.HasFullyContainedRange())
            FullyContainedCount++;

        if (assignmentPair.HasOverlap())
            OverlapCount++;
    }
}

// implementation
// would it be worthwhile for this to be a record struct? yes - it makes a significant difference in allocations
public record struct AssignmentPair : ISpanParsable<AssignmentPair>
{
    public Range Elf1 { get; init; }
    public Range Elf2 { get; init; }

    public bool HasFullyContainedRange() =>
        Elf1.FullyContains(Elf2) || Elf2.FullyContains(Elf1);

    public bool HasOverlap() =>
        Elf1.Overlaps(Elf2);

    public static AssignmentPair Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"{s}");
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

    public static AssignmentPair ParseAllocating(string line)
    {
        var pair = line.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return new AssignmentPair
        {
            Elf1 = ParseRangeAllocating(pair[0]),
            Elf2 = ParseRangeAllocating(pair[1])
        };
    }

    private static Range ParseRangeAllocating(string input)
    {
        var tokens = input.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var start = int.Parse(tokens[0]);
        var end = int.Parse(tokens[1]);
        return new Range(start, end);
    }
}

public static class RangeExtensions
{
    public static bool FullyContains(this Range source, Range other) =>
        source.Start.Value <= other.Start.Value &&
        source.End.Value >= other.End.Value;

    public static bool Overlaps(this Range source, Range other) =>
        source.Start.Value <= other.End.Value && source.End.Value >= other.Start.Value;
}
