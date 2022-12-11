using System.IO.Pipelines;
using System.Text;

namespace Parsing;

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
