using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Text;
using Parsing;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var resultsCharBuffers = await Solution.GetResultsCharBuffersAsync();
        WriteResults(resultsCharBuffers, "CharBuffers");

        var resultsStackRows = await Solution.GetResultsStackRowsAsync();
        WriteResults(resultsCharBuffers, "StackRows");
    }

    public static void WriteResults(Results results, string label)
    {
        Console.WriteLine($"= Strategy: {label} =");

        var supplyStacks = results.SupplyStacks;
        Console.WriteLine($"Starting stacks lines: {supplyStacks.Stacks.Count}, Rearrangement procedure lines: {supplyStacks.RearrangementProcedure.Count}");
        Console.WriteLine();
        Console.WriteLine("== Starting Stacks ==");
        //Console.WriteLine($"Ids: {string.Join(' ', initialStacks.Select(s => s.Id))}");
        foreach (var stack in supplyStacks.Stacks)
            Console.WriteLine($"Stack {stack.Id}: {string.Join(" ", stack.Stack.Reverse())}");

        WriteRearrangedStacks(results.Results9000);
        WriteRearrangedStacks(results.Results9001);

        Console.WriteLine();
        Console.WriteLine($"Elapsed: {results.ElapsedMs} ms");
        Console.WriteLine();
    }

    private static void WriteRearrangedStacks(ModelResults modelResults)
    {
        Console.WriteLine();
        Console.WriteLine($"== Rearranged Stacks: {modelResults.Model} ==");
        foreach (var stack in modelResults.RearrangedStacks)
            Console.WriteLine($"Stack {stack.Id}: {string.Join(" ", stack.Stack.Reverse())}");
        Console.WriteLine();
        Console.WriteLine($"Top crates {modelResults.Model}: {string.Join("", modelResults.TopCrates)}");
    }
}

public static class Solution
{
    private const string FilePath = @"input.txt";
    private static Encoding Utf8Encoding = Encoding.UTF8;
    private static IFormatProvider? Provider = CultureInfo.InvariantCulture;

    public static async Task<Results> GetResultsCharBuffersAsync()
    {
        var sw = Stopwatch.StartNew();

        var supplyStacks = await CharBuffersStacksParser.ParseLinesAsync(FilePath, Utf8Encoding, Provider);
        var results = new Results
        {
            SupplyStacks = supplyStacks,
            Results9000 = RearrangeStacks(supplyStacks, 9000),
            Results9001 = RearrangeStacks(supplyStacks, 9001)
        };

        results.ElapsedMs = sw.ElapsedMilliseconds;

        return results;
    }

    public static async Task<Results> GetResultsStackRowsAsync()
    {
        var sw = Stopwatch.StartNew();

        var supplyStacks = await StackRowsStacksParser.ParseLinesAsync(FilePath, Utf8Encoding, Provider);
        var results = new Results
        {
            SupplyStacks = supplyStacks,
            Results9000 = RearrangeStacks(supplyStacks, 9000),
            Results9001 = RearrangeStacks(supplyStacks, 9001)
        };

        results.ElapsedMs = sw.ElapsedMilliseconds;

        return results;
    }

    private static ModelResults RearrangeStacks(SupplyStacks supplyStacks, int model)
    {
        var rearrangedStacks = SupplyStacksService.RearrangeStacks(supplyStacks, model);
        var topCrates = SupplyStacksService.GetTopCrates(rearrangedStacks);
        return new ModelResults(model)
        {
            RearrangedStacks = rearrangedStacks,
            TopCrates = topCrates
        };
    }
}

public static class SupplyStacksService
{
    public static IReadOnlyList<SupplyStack> RearrangeStacks(SupplyStacks supplyStacks, int model)
    {
        if (supplyStacks.Stacks is null)
            return Array.Empty<SupplyStack>();

        var stacks = supplyStacks.Stacks
            .Select(s => s with { Stack = new Stack<char>(s.Stack.Reverse()) })
            .ToArray();

        foreach (var move in supplyStacks.RearrangementProcedure)
        {
            var from = stacks[move.From - 1];
            var to = stacks[move.To - 1];
            switch (model)
            {
                case 9000:
                    Move9000(from, to, move.Quantity);
                    break;
                case 9001:
                    Move9001(from, to, move.Quantity);
                    break;
            }
        }

        return stacks;
    }

    // part 1: CrateMover 9000: move one crate at a time
    private static void Move9000(SupplyStack from, SupplyStack to, int quantity)
    {
        for (int i = 0; i < quantity; i++)
        {
            var crate = from.Stack.Pop();
            to.Stack.Push(crate);
        }
    }

    // part 2: CrateMover 9001: move N crates at a time
    private static void Move9001(SupplyStack from, SupplyStack to, int quantity)
    {
        Span<char> crates = quantity <= FilePipelineParser.MaxStackallocLength
            ? stackalloc char[quantity]
            : throw new NotSupportedException($"Too many crates: {quantity}");

        for (int i = 0; i < quantity; i++)
            crates[i] = from.Stack.Pop();

        for (int j = quantity - 1; j >= 0; j--)
            to.Stack.Push(crates[j]);
    }

    public static IEnumerable<char> GetTopCrates(IEnumerable<SupplyStack> stacks)
    {
        foreach (var stack in stacks)
        {
            yield return stack.Stack.TryPeek(out var crate)
                ? crate
                : ' ';
        }
    }
}

public record struct Results
{
    public SupplyStacks SupplyStacks { get; set; }
    public ModelResults Results9000 { get; set; }
    public ModelResults Results9001 { get; set; }
    public double ElapsedMs { get; set; }
}

public record struct ModelResults(int Model)
{
    public IReadOnlyList<SupplyStack> RearrangedStacks { get; set; }
    public IEnumerable<char> TopCrates { get; set; }
}

public record struct SupplyStacks(
    IReadOnlyList<SupplyStack> Stacks, IReadOnlyList<CraneMove> RearrangementProcedure)
{ }

public record struct SupplyStack(int Id, int Offset)
{
    public Stack<char> Stack { get; init; } = new Stack<char>();
}

public record struct CraneMove : ISpanParsable<CraneMove>
{
    public int Quantity { get; set; }
    public int From { get; set; }
    public int To { get; set; }

    public static CraneMove Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"{s}");
    }

    public static CraneMove Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException(s);
    }

    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out CraneMove result)
    {
        var craneMove = new CraneMove();

        var state = ParseState.Move;
        while (state < ParseState.End)
        {
            var nextWord = state switch
            {
                ParseState.Move => "move ",
                ParseState.From => "from ",
                ParseState.To => "to ",
                _ => null
            };

            if (nextWord is null || !s.StartsWith(nextWord))
                return Try.Failed(out result);

            s = s.Slice(nextWord.Length);
            var numLength = state < ParseState.To
                ? s.IndexOf(' ')
                : s.Length;

            if (numLength == -1)
                return Try.Failed(out result);

            if (!int.TryParse(s.Slice(0, numLength), provider, out int number))
                return Try.Failed(out result);

            s = s.Slice(Math.Min(numLength + 1, s.Length - 1));

            switch (state)
            {
                case ParseState.Move:
                    craneMove.Quantity = number;
                    state = ParseState.From;
                    break;
                case ParseState.From:
                    craneMove.From = number;
                    state = ParseState.To;
                    break;
                case ParseState.To:
                    craneMove.To = number;
                    state = ParseState.End;
                    break;
            }
        }

        result = craneMove;
        return true;
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out CraneMove result)
    {
        if (s is null)
            return Try.Failed(out result);
        
        return TryParse(s.AsSpan(), provider, out result);
    }

    private enum ParseState
    {
        Move,
        From,
        To,
        End
    }
}

public static class Try
{
    public static bool Failed<T>([MaybeNullWhen(false)] out T result)
    {
        result = default;
        return false;
    }
}
