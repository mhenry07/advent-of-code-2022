// Day 4: Camp Cleanup
// https://adventofcode.com/2022/day/4

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

internal class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<Solution>();
    }

    // private static async Task Main(string[] args)
    // {
    //     var sw = Stopwatch.StartNew();

    //     var solution = new Solution();
    //     var resultTasks = new[]
    //     {
    //         solution.GetResultsPipeReaderAsyncEnumerableAsync(),
    //         solution.GetResultsPipeWriterReaderAsync(),
    //         solution.GetResultsReadAllBytesSpanAsync(),
    //         solution.GetResultsReadAllLinesAsync(),
    //         solution.GetResultsReadLinesAsync(),
    //         solution.GetResultsStreamReaderAsync()
    //     };

    //     await Task.WhenAll(resultTasks);

    //     sw.Stop();

    //     var result = await resultTasks[0];
    //     for (int i = 1; i < resultTasks.Length; i++)
    //     {
    //         var resultN = await resultTasks[i];
    //         if (resultN != result)
    //             Console.WriteLine($"Mismatch: {resultN} doesn't match {result}");
    //     }

    //     // part 1
    //     Console.WriteLine($"Assignment pairs where one range fully contains the other: {result.FullyContainedCount}");

    //     // part 2
    //     Console.WriteLine($"Assignment pairs with overlapping ranges: {result.OverlapCount}");

    //     Console.WriteLine($"Elapsed: {sw.ElapsedMilliseconds:N2} ms for {result.TotalCount:N0} records");
    // }
}

[InProcessAttribute]
[MemoryDiagnoser]
public class Solution
{
    [Benchmark]
    public async Task<Results> GetResultsPipeWriterReaderAsync()
    {
        var totalCount = 0;
        var fullyContainedCount = 0;
        var overlapCount = 0;
        var assignmentPairsTask =
            FilePipelineParser.ParseFileAsync<AssignmentPair>("input.txt", Encoding.UTF8, CultureInfo.InvariantCulture);
        foreach (var assignmentPair in await assignmentPairsTask)
        {
            totalCount++;

            if (assignmentPair.HasFullyContainedRange())
                fullyContainedCount++;

            if (assignmentPair.HasOverlap())
                overlapCount++;
        }

        return new Results(totalCount, fullyContainedCount, overlapCount);
    }

    [Benchmark]
    public async Task<Results> GetResultsPipeReaderAsyncEnumerableAsync()
    {
        var totalCount = 0;
        var fullyContainedCount = 0;
        var overlapCount = 0;
        var assignmentPairsAsyncEnumerable =
            FilePipelineParser.ParseLinesAsync<AssignmentPair>("input.txt", Encoding.UTF8, CultureInfo.InvariantCulture);
        await foreach (var assignmentPair in assignmentPairsAsyncEnumerable)
        {
            totalCount++;

            if (assignmentPair.HasFullyContainedRange())
                fullyContainedCount++;

            if (assignmentPair.HasOverlap())
                overlapCount++;
        }

        return new Results(totalCount, fullyContainedCount, overlapCount);
    }

    [Benchmark]
    public async Task<Results> GetResultsReadAllBytesSpanAsync()
    {
        var totalCount = 0;
        var fullyContainedCount = 0;
        var overlapCount = 0;
        Memory<char> charMemory = new char[256];
        Memory<byte> memory = await File.ReadAllBytesAsync("input.txt");
        int start = 0, end = 0;
        while (start < memory.Length)
        {
            var index = memory.Span.Slice(start).IndexOf((byte)'\n');
            end = index >= 0 ? start + index : memory.Length - 1;

            totalCount++;

            var assignmentPair = Parse(memory.Slice(start, end - start + 1).Span, charMemory);
            if (assignmentPair.HasFullyContainedRange())
                fullyContainedCount++;

            if (assignmentPair.HasOverlap())
                overlapCount++;
            
            start = end + 1;
        }

        return new Results(totalCount, fullyContainedCount, overlapCount);

        AssignmentPair Parse(Span<byte> line, Memory<char> charMemory)
        {
            var charBuffer = charMemory.Span;
            var length = Encoding.UTF8.GetChars(line, charBuffer);
            var chars = charBuffer.Slice(0, length).Trim();
            return AssignmentPair.Parse(chars, CultureInfo.InvariantCulture);
        }
    }

    [Benchmark]
    public async Task<Results> GetResultsReadAllLinesAsync()
    {
        var totalCount = 0;
        var fullyContainedCount = 0;
        var overlapCount = 0;
        var allLines = await File.ReadAllLinesAsync("input.txt", Encoding.UTF8);
        foreach (var line in allLines)
        {
            totalCount++;

            var assignmentPair = AssignmentPair.ParseAllocating(line);
            if (assignmentPair.HasFullyContainedRange())
                fullyContainedCount++;

            if (assignmentPair.HasOverlap())
                overlapCount++;
        }

        return new Results(totalCount, fullyContainedCount, overlapCount);
    }

    [Benchmark]
    public async Task<Results> GetResultsReadLinesAsync()
    {
        var totalCount = 0;
        var fullyContainedCount = 0;
        var overlapCount = 0;
        var linesAsyncEnumerable = File.ReadLinesAsync("input.txt", Encoding.UTF8);
        await foreach (var line in linesAsyncEnumerable)
        {
            totalCount++;

            var assignmentPair = AssignmentPair.ParseAllocating(line);
            if (assignmentPair.HasFullyContainedRange())
                fullyContainedCount++;

            if (assignmentPair.HasOverlap())
                overlapCount++;
        }

        return new Results(totalCount, fullyContainedCount, overlapCount);
    }

    [Benchmark]
    public async Task<Results> GetResultsStreamReaderAsync()
    {
        var totalCount = 0;
        var fullyContainedCount = 0;
        var overlapCount = 0;
        using var streamReader = new StreamReader("input.txt", Encoding.UTF8);
        while (true)
        {
            var line = await streamReader.ReadLineAsync();
            if (line == null)
                break;

            totalCount++;

            var assignmentPair = AssignmentPair.ParseAllocating(line);
            if (assignmentPair.HasFullyContainedRange())
                fullyContainedCount++;

            if (assignmentPair.HasOverlap())
                overlapCount++;
        }

        return new Results(totalCount, fullyContainedCount, overlapCount);
    }
}

public record Results(int TotalCount, int FullyContainedCount, int OverlapCount);

// implementation
// would it be worthwhile for this to be a record struct?
public record AssignmentPair : ISpanParsable<AssignmentPair>
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
