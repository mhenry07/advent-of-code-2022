using System.Buffers;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;

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

public static class CharBuffersStacksParser
{
    public static async Task<SupplyStacks> ParseLinesAsync(
        string filename, Encoding encoding, IFormatProvider? provider)
    {
        Stack<CharBuffer> startingStackLines = new();
        IReadOnlyList<SupplyStack>? startingStacks = default;
        List<CraneMove> rearrangementProcedure = new();
        var state = ParseState.StartingStacks;

        using var stream = File.OpenRead(filename);
        var reader = PipeReader.Create(stream);
        while (true)
        {
            var readResult = await reader.ReadAsync().ConfigureAwait(false);
            var buffer = readResult.Buffer;

            while (FilePipelineParser.TryReadLine(ref buffer, out var line))
            {
                switch (state)
                {
                    case ParseState.StartingStacks:
                        if (line.IsBlankLine())
                        {
                            if (!TryParseStartingStackLines(startingStackLines, provider, out startingStacks))
                                throw new FormatException("Failed to parse starting stack lines");
                            state = ParseState.RearrangementProcedure;
                        }
                        else
                        {
                            startingStackLines.Push(encoding.GetCharBuffer(line));
                        }
                        break;

                    case ParseState.RearrangementProcedure:
                        if (!line.IsBlankLine())
                            rearrangementProcedure.Add(FilePipelineParser.Parse<CraneMove>(line, encoding, provider));
                        break;
                }
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);

        return new SupplyStacks(startingStacks ?? Array.Empty<SupplyStack>(), rearrangementProcedure);
    }

    private static bool TryParseStartingStackLines(
        Stack<CharBuffer> startingStackLines, IFormatProvider? provider,
        [MaybeNullWhen(false)] out IReadOnlyList<SupplyStack> result)
    {
        var state = StacksParseState.Ids;
        IReadOnlyList<SupplyStack>? supplyStacks = null;
        while (startingStackLines.TryPop(out var line))
        {
            try
            {
                var span = line.AsSpan();
                switch (state)
                {
                    case StacksParseState.Ids:
                        if (!TryParseStackIds(span, provider, out supplyStacks))
                            return Try.Failed(out result);
                        state = StacksParseState.Crates;
                        break;

                    case StacksParseState.Crates:
                        if (!TryParseAndPushCrates(supplyStacks, span, provider))
                            return Try.Failed(out result);
                        break;
                }
            }
            finally
            {
                line.Dispose();
            }
        }

        result = supplyStacks;
        return result is not null;
    }

    private static bool TryParseStackIds(
        ReadOnlySpan<char> line, IFormatProvider? provider,
        [MaybeNullWhen(false)] out IReadOnlyList<SupplyStack> result)
    {
        var supplyStacks = new List<SupplyStack>();

        for (int i = 0; i < line.Length; i++)
        {
            if (line[i] == ' ')
                continue;

            var idLength = line.Slice(i).IndexOf(' ');
            if (idLength == -1)
                idLength = line.Length - i;

            if (int.TryParse(line.Slice(i, idLength), provider, out var id))
                supplyStacks.Add(new SupplyStack(id, i));
            else if (idLength > 0)
                return Try.Failed(out result);

            i += Math.Max(0, idLength - 1);
        }

        result = supplyStacks;
        return supplyStacks.Count > 0;
    }

    private static bool TryParseAndPushCrates(
        IReadOnlyList<SupplyStack>? supplyStacks, ReadOnlySpan<char> line, IFormatProvider? provider)
    {
        if (supplyStacks is null || supplyStacks.Count == 0)
            return false;

        if (line.IsWhiteSpace())
            return false;

        for (int i = 0; i < supplyStacks.Count; i++)
        {
            var supplyStack = supplyStacks[i];
            var slice = line.Slice(Math.Max(0, supplyStack.Offset - 1));
            var openOffset = slice.IndexOf('[');
            if (openOffset == -1)
                break;
            if (openOffset > 2)
                continue;

            slice = slice.Slice(openOffset + 1);

            var length = slice.IndexOf(']');
            if (length == -1 || length > 1) // only support single character
                return false;

            var crate = slice[0];
            //Console.WriteLine($"Adding {crate} to {supplyStack.Id}");
            supplyStack.Stack.Push(crate);
        }

        return true;
    }
}

public static class StackRowsStacksParser
{
    public static async Task<SupplyStacks> ParseLinesAsync(
        string filename, Encoding encoding, IFormatProvider? provider)
    {
        Stack<SupplyStackRow> stackRows = new();
        IReadOnlyList<SupplyStack>? startingStacks = default;
        List<CraneMove> rearrangementProcedure = new();
        var state = ParseState.StartingStacks;

        using var stream = File.OpenRead(filename);
        var reader = PipeReader.Create(stream);
        while (true)
        {
            var readResult = await reader.ReadAsync().ConfigureAwait(false);
            var buffer = readResult.Buffer;

            while (FilePipelineParser.TryReadLine(ref buffer, out var line))
            {
                switch (state)
                {
                    case ParseState.StartingStacks:
                        if (line.IsBlankLine())
                        {
                            startingStacks = CreateSupplyStacks(stackRows, provider);
                            state = ParseState.RearrangementProcedure;
                        }
                        else
                        {
                            stackRows.Push(
                                FilePipelineParser.Parse<SupplyStackRow>(line, encoding, provider));
                        }
                        break;

                    case ParseState.RearrangementProcedure:
                        if (!line.IsBlankLine())
                            rearrangementProcedure.Add(FilePipelineParser.Parse<CraneMove>(line, encoding, provider));
                        break;
                }
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);

        return new SupplyStacks(startingStacks ?? Array.Empty<SupplyStack>(), rearrangementProcedure);
    }

    private static IReadOnlyList<SupplyStack> CreateSupplyStacks(
        Stack<SupplyStackRow> stackRows, IFormatProvider? provider)
    {
        SupplyStack[]? supplyStacks = null;
        var state = StacksParseState.Ids;
        while (stackRows.TryPop(out var stackRow))
        {
            try
            {
                var span = stackRow.AsSpan();
                switch (state)
                {
                    case StacksParseState.Ids:
                    {
                        if (stackRow.RowType != SupplyStackRow.Type.Ids)
                            throw new ArgumentException($"Expected row type to be Ids but found: {stackRow.RowType}");

                        supplyStacks = new SupplyStack[span.Length];
                        for (int i = 0; i < span.Length; i++)
                        {
                            int id = span[i].Label - '0';
                            supplyStacks[i] = new SupplyStack(id, span[i].Offset);
                        }
                        state = StacksParseState.Crates;
                        continue;
                    }

                    case StacksParseState.Crates:
                    {
                        if (stackRow.RowType != SupplyStackRow.Type.Crates)
                            throw new ArgumentException($"Expected row type to be Crates but found: {stackRow.RowType}");
                        if (supplyStacks is null)
                            throw new InvalidOperationException("Expected supplyStacks to be initialized but it was null");

                        int j = 0;
                        for (int i = 0; i < span.Length; i++)
                        {
                            while (supplyStacks[j].Offset < span[i].Offset)
                                j++;
                            if (supplyStacks[j].Offset == span[i].Offset)
                                supplyStacks[j].Stack.Push(span[i].Label);
                            else
                                throw new InvalidOperationException($"Expected supplyStack {supplyStacks[j].Id} offset {supplyStacks[j].Offset} to match crate [{span[i].Label}] offset {span[i].Offset}");
                        }
                        continue;
                    }
                }
            }
            finally
            {
                stackRow.Dispose();
            }
        }

        if (supplyStacks is null)
            throw new InvalidOperationException("Expected supplyStacks to be initialized but it was null");

        return supplyStacks;
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

    public static IReadOnlyList<char> GetTopCrates(IReadOnlyList<SupplyStack> stacks)
    {
        var topCrates = new char[stacks.Count];
        for (var i = 0; i < stacks.Count; i++)
        {
            var stack = stacks[i].Stack;
            topCrates[i] = stack.TryPeek(out var crate) ? crate : ' ';
        }

        return topCrates;
    }
}

internal enum ParseState
{
    StartingStacks,
    RearrangementProcedure
}

internal enum StacksParseState
{
    Ids,
    Crates
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
    public IReadOnlyList<char> TopCrates { get; set; }
}

internal record struct CratePosition(char Label, int Offset)
{ }

internal record struct SupplyStackRow(SupplyStackRow.Type RowType) : ISpanParsable<SupplyStackRow>, IDisposable
{
    private CratePosition[] _values;
    private int _count;

    public ReadOnlySpan<CratePosition> AsSpan() =>
        _values.AsSpan(0, _count);

    public static SupplyStackRow Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"{s}");
    }

    public static SupplyStackRow Parse(string s, IFormatProvider? provider) =>
        Parse(s.AsSpan(), provider);

    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out SupplyStackRow result)
    {
        CratePosition[]? rented = null;
        var buffer = s.Length <= FilePipelineParser.MaxStackallocLength
            ? stackalloc CratePosition[s.Length]
            : (rented = ArrayPool<CratePosition>.Shared.Rent(s.Length));

        var count = 0;
        // var values = ArrayPool<CratePosition>.Shared.Rent(s.Length);
        var type = Type.Unknown;
        for (int i = 0; i < s.Length; i++)
        {
            switch (s[i])
            {
                case ' ':
                    continue;

                case '\r':
                    break;

                case '[':
                {
                    if (type == Type.Unknown)
                        type = Type.Crates;

                    var crateSlice = s.Slice(i + 1);
                    var length = crateSlice.IndexOf("]");
                    if (length != 1)
                        return tryFailed(rented, out result);

                    buffer[count] = new CratePosition(crateSlice[0], i);
                    count++;
                    i += length + 1;
                    continue;
                }

                case >= '0' and <= '9':
                {
                    if (type == Type.Unknown)
                        type = Type.Ids;

                    var idSlice = s.Slice(i);
                    var length = idSlice.IndexOf(' ');
                    if (idSlice.Length != 1 && length != 1)
                        return tryFailed(rented, out result);

                    buffer[count] = new CratePosition(s[i], i);
                    count++;
                    continue;
                }

                default:
                    return tryFailed(rented, out result);
            }
        }

        if (count == 0)
            return tryFailed(rented, out result);

        if (rented is null)
        {
            rented = ArrayPool<CratePosition>.Shared.Rent(count);
            buffer.Slice(0, count).CopyTo(rented.AsSpan());
        }

        result = new SupplyStackRow(type)
        {
            _values = rented,
            _count = count
        };
        return true;

        bool tryFailed(CratePosition[]? rented, out SupplyStackRow result)
        {
            if (rented is not null)
                ArrayPool<CratePosition>.Shared.Return(rented);

            return Try.Failed(out result);
        }
    }

    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SupplyStackRow result)
    {
        if (s is null)
            return Try.Failed(out result);

        return TryParse(s.AsSpan(), provider, out result);
    }

    public void Dispose() =>
        ArrayPool<CratePosition>.Shared.Return(_values);

    public enum Type
    {
        Unknown,
        Crates,
        Ids
    }
}

public record struct CharBuffer(char[] Buffer, int Length) : IDisposable
{
    public ReadOnlySpan<char> AsSpan() =>
        Buffer.AsSpan(0, Length);

    public void Dispose() =>
        ArrayPool<char>.Shared.Return(Buffer);
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

public static class BufferExtensions
{
    public static bool IsBlankLine(this ReadOnlySequence<byte> line) =>
        line.IsSingleSegment && line.FirstSpan.TrimEnd((byte)'\r').IsEmpty;
}

public static class EncodingExtensions
{
    public static CharBuffer GetCharBuffer(this Encoding encoding, in ReadOnlySequence<byte> bytes)
    {
        var buffer = ArrayPool<char>.Shared.Rent((int)bytes.Length);
        var chars = buffer.AsSpan();
        var length = encoding.GetChars(bytes, chars);
        length = chars.Slice(0, length).TrimEnd('\r').Length;
        return new CharBuffer(buffer, length);
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
