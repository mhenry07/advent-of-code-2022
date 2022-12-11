using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;

namespace Parsing;

public static class StackRowsStacksParser
{
    public static async Task<SupplyStacks> ParseLinesAsync(
        string filename, Encoding encoding, IFormatProvider? provider)
    {
        Stack<SpanStackRow> stackRows = new();
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
                                FilePipelineParser.Parse<SpanStackRow>(line, encoding, provider));
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
        Stack<SpanStackRow> stackRows, IFormatProvider? provider)
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
                        if (stackRow.RowType != StackRowType.Ids)
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
                        if (stackRow.RowType != StackRowType.Crates)
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

    private record struct SpanStackRow(StackRowType RowType) : ISpanParsable<SpanStackRow>, IDisposable
    {
        private CratePosition[] _values;
        private int _count;

        public ReadOnlySpan<CratePosition> AsSpan() =>
            _values.AsSpan(0, _count);

        public static SpanStackRow Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
        {
            if (TryParse(s, provider, out var result))
                return result;

            throw new FormatException($"{s}");
        }

        public static SpanStackRow Parse(string s, IFormatProvider? provider) =>
            Parse(s.AsSpan(), provider);

        public static bool TryParse(
            ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out SpanStackRow result)
        {
            CratePosition[]? rented = null;
            var buffer = s.Length <= FilePipelineParser.MaxStackallocLength
                ? stackalloc CratePosition[s.Length]
                : (rented = ArrayPool<CratePosition>.Shared.Rent(s.Length));

            var count = 0;
            // var values = ArrayPool<CratePosition>.Shared.Rent(s.Length);
            var type = StackRowType.Unknown;
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
                        if (type == StackRowType.Unknown)
                            type = StackRowType.Crates;

                        var crateSlice = s.Slice(i + 1);
                        var length = crateSlice.IndexOf("]");
                        if (length != 1)
                            return tryFailed(rented, out result);

                        buffer[count] = new CratePosition(crateSlice[0], i + 1);
                        count++;
                        i += length + 1;
                        continue;
                    }

                    case >= '0' and <= '9':
                    {
                        if (type == StackRowType.Unknown)
                            type = StackRowType.Ids;

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

            result = new SpanStackRow(type)
            {
                _values = rented,
                _count = count
            };
            return true;

            bool tryFailed(CratePosition[]? rented, out SpanStackRow result)
            {
                if (rented is not null)
                    ArrayPool<CratePosition>.Shared.Return(rented);

                return Try.Failed(out result);
            }
        }

        public static bool TryParse(
            [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out SpanStackRow result)
        {
            if (s is null)
                return Try.Failed(out result);

            return TryParse(s.AsSpan(), provider, out result);
        }

        public void Dispose() =>
            ArrayPool<CratePosition>.Shared.Return(_values);
    }
}
