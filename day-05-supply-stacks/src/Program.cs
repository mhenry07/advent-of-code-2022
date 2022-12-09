using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO.Pipelines;
using System.Text;

internal class Program
{
    private static async Task Main(string[] args)
    {
        await Solution.GetResultsAsync();
    }
}

public static class Solution
{
    private const string FilePath = @"input.txt";
    private static Encoding Utf8Encoding = Encoding.UTF8;
    private static IFormatProvider? Provider = CultureInfo.InvariantCulture;

    public static async Task GetResultsAsync()
    {
        var x = await SupplyStacksService.ParseLinesAsync(FilePath, Utf8Encoding, Provider);

        Console.WriteLine($"Starting stacks lines: {x.StartingStackLines.Count}, Rearrangement procedure lines: {x.RearrangementProcedure.Count}");

        Console.WriteLine();
        Console.WriteLine("== Starting Stacks ==");
        if (!SupplyStacksService.TryParseStartingStackLines(x.StartingStackLines, Provider, out var initialStacks))
        {
            Console.WriteLine("Failed to parse StartingStackLines");
            return;
        }

        //Console.WriteLine($"Ids: {string.Join(' ', initialStacks.Select(s => s.Id))}");
        foreach (var stack in initialStacks)
            Console.WriteLine($"Stack {stack.Id}: {string.Join(" ", stack.Stack.Reverse())}");

        Console.WriteLine();
        Console.WriteLine("== Rearranged Stacks ==");
        var rearrangedStacks = SupplyStacksService.RearrangeStacks(initialStacks, x.RearrangementProcedure, 9000);
        //var rearrangedStacks = SupplyStacksService.RearrangeStacks(initialStacks, x.RearrangementProcedure, 9001);
        foreach (var stack in initialStacks)
            Console.WriteLine($"Stack {stack.Id}: {string.Join(" ", stack.Stack.Reverse())}");

        Console.WriteLine();
        var topCrates = SupplyStacksService.GetTopCrates(rearrangedStacks);
        Console.WriteLine($"Top crates: {string.Join("", topCrates)}");
    }
}

public static class SupplyStacksService
{
    public static async Task<(Stack<CharBuffer> StartingStackLines, IReadOnlyList<CraneMove> RearrangementProcedure)> ParseLinesAsync(
        string filename, Encoding encoding, IFormatProvider? provider)
    {
        var startingStackLines = new Stack<CharBuffer>();
        var rearrangementProcedure = new List<CraneMove>();
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
                        if (isBlankLine(line))
                            state = ParseState.RearrangementProcedure;
                        else
                            startingStackLines.Push(encoding.GetCharBuffer(line));
                        break;
                    case ParseState.RearrangementProcedure:
                        if (!isBlankLine(line))
                            rearrangementProcedure.Add(FilePipelineParser.Parse<CraneMove>(line, encoding, provider));
                        break;
                }
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);

        return (startingStackLines, rearrangementProcedure);

        bool isBlankLine(ReadOnlySequence<byte> line) =>
            line.IsSingleSegment && line.FirstSpan.TrimEnd((byte)'\r').IsEmpty;
    }

    public static bool TryParseStartingStackLines(
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

    public static IReadOnlyList<SupplyStack> RearrangeStacks(
        IReadOnlyList<SupplyStack> stacks, IReadOnlyList<CraneMove> rearrangementProcedure, int model)
    {
        foreach (var move in rearrangementProcedure)
        {
            var from = stacks[move.From - 1];
            var to = stacks[move.To - 1];
            switch (model)
            {
                case 9000:
                    Move9000(from, to, move.Quantity);
                    break;
                // case 9001:
                //     Move9001(from, to, move.Quantity);
                //     break;
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

    // // part 2: CrateMover 9001: move N crates at a time
    // private static void Move9001(SupplyStack from, SupplyStack to, int quantity)
    // {
    //     Span<char> crates = quantity <= FilePipelineParser.MaxStackallocLength
    //         ? stackalloc char[quantity]
    //         : throw new NotSupportedException($"Too many crates: {quantity}");

    //     for (int i = 0; i < quantity; i++)
    //         crates[i] = from.Stack.Pop();

    //     for (int j = quantity - 1; j >= 0; j--)
    //         to.Stack.Push(crates[j]);
    // }

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

    private enum ParseState
    {
        StartingStacks,
        RearrangementProcedure
    }

    private enum StacksParseState
    {
        Ids,
        Crates
    }
}

public record struct CharBuffer(char[] Buffer, int Length) : IDisposable
{
    public ReadOnlySpan<char> AsSpan() =>
        Buffer.AsSpan(0, Length);

    public void Dispose()
    {
        ArrayPool<char>.Shared.Return(Buffer);
    }
}

public record struct SupplyStack(int Id, int Offset)
{
    public Stack<char> Stack { get; } = new Stack<char>();
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
