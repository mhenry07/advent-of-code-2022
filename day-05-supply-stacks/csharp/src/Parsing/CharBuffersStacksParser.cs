using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;

namespace Parsing;

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
