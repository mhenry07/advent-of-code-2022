using System.Diagnostics.CodeAnalysis;
using System.IO.Pipelines;
using System.Text;
using System.Text.RegularExpressions;

namespace Parsing;

public static class RegexAllLinesStacksParser
{
    public static async Task<SupplyStacks> ParseLinesAsync(
        string filename, Encoding encoding, IFormatProvider? provider)
    {
        Stack<RegexStackRow> stackRows = new();
        IReadOnlyList<SupplyStack>? startingStacks = default;
        List<CraneMove> rearrangementProcedure = new();
        var state = ParseState.StartingStacks;

        var allLines = await File.ReadAllLinesAsync(filename, encoding).ConfigureAwait(false);
        foreach (var line in allLines)
        {
            switch (state)
            {
                case ParseState.StartingStacks:
                    if (string.IsNullOrEmpty(line))
                    {
                        startingStacks = RegexStacksParserService.CreateSupplyStacks(stackRows, provider);
                        state = ParseState.RearrangementProcedure;
                    }
                    else
                    {
                        stackRows.Push(RegexStackRow.Parse(line, provider));
                    }
                    break;

                case ParseState.RearrangementProcedure:
                    if (!string.IsNullOrEmpty(line))
                    {
                        rearrangementProcedure.Add(
                            RegexCraneMove.Parse(line, provider).ToCraneMove());
                    }
                    break;
            }
        }

        return new SupplyStacks(startingStacks ?? Array.Empty<SupplyStack>(), rearrangementProcedure);
    }
}

public static class RegexPipelineStacksParser
{
    private const int MaxStackallocLength = FilePipelineParser.MaxStackallocLength;

    public static async Task<SupplyStacks> ParseLinesAsync(
        string filename, Encoding encoding, IFormatProvider? provider)
    {
        Stack<RegexStackRow> stackRows = new();
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
                            startingStacks = RegexStacksParserService.CreateSupplyStacks(stackRows, provider);
                            state = ParseState.RearrangementProcedure;
                        }
                        else
                        {
                            stackRows.Push(
                                FilePipelineParser.Parse<RegexStackRow>(line, encoding, provider));
                        }
                        break;

                    case ParseState.RearrangementProcedure:
                        if (!line.IsBlankLine())
                        {
                            rearrangementProcedure.Add(
                                FilePipelineParser.Parse<RegexCraneMove>(line, encoding, provider).ToCraneMove());
                        }
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
}

internal static class RegexStacksParserService
{
    public static IReadOnlyList<SupplyStack> CreateSupplyStacks(
        Stack<RegexStackRow> stackRows, IFormatProvider? provider)
    {
        SupplyStack[]? supplyStacks = null;
        var state = StacksParseState.Ids;
        while (stackRows.TryPop(out var stackRow))
        {
            switch (state)
            {
                case StacksParseState.Ids:
                {
                    if (stackRow.RowType != StackRowType.Ids)
                        throw new ArgumentException($"Expected row type to be Ids but found: {stackRow.RowType}. Values: {string.Join(' ', stackRow.Values.Select(p => p.Label))}");

                    var ids = stackRow.Values;
                    supplyStacks = new SupplyStack[ids.Length];
                    for (int i = 0; i < ids.Length; i++)
                    {
                        int id = ids[i].Label - '0';
                        supplyStacks[i] = new SupplyStack(id, ids[i].Offset);
                    }
                    state = StacksParseState.Crates;
                    continue;
                }

                case StacksParseState.Crates:
                {
                    if (stackRow.RowType != StackRowType.Crates)
                        throw new ArgumentException($"Expected row type to be Crates but found: {stackRow.RowType}. Values: {string.Join(' ', stackRow.Values.Select(p => p.Label))}");
                    if (supplyStacks is null)
                        throw new InvalidOperationException("Expected supplyStacks to be initialized but it was null");

                    var crates = stackRow.Values;
                    int j = 0;
                    for (int i = 0; i < crates.Length; i++)
                    {
                        var crate = crates[i];
                        while (supplyStacks[j].Offset < crate.Offset)
                            j++;
                        if (supplyStacks[j].Offset == crate.Offset)
                            supplyStacks[j].Stack.Push(crate.Label);
                        else
                            throw new InvalidOperationException($"Expected supplyStack {supplyStacks[j].Id} offset {supplyStacks[j].Offset} to match crate [{crate.Label}] offset {crate.Offset}");
                    }
                    continue;
                }
            }
        }

        if (supplyStacks is null)
            throw new InvalidOperationException("Expected supplyStacks to be initialized but it was null");

        return supplyStacks;
    }
}

internal record struct RegexStackRow(StackRowType RowType) : ISpanParsable<RegexStackRow>
{
    public CratePosition[] Values;

    public static RegexStackRow Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"{s}");
    }

    public static RegexStackRow Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException(s);
    }

    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out RegexStackRow result) =>
        TryParse(s.ToString(), provider, out result);

    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out RegexStackRow result)
    {
        if (s is null)
            return Try.Failed(out result);

        Console.WriteLine();

        var crateMatches = Regex.Matches(s, @"\[([A-Z])\]", RegexOptions.Compiled);
        if (crateMatches.Any())
        {
            result = new RegexStackRow(StackRowType.Crates);
            var crates = new CratePosition[crateMatches.Count];
            for (int i = 0; i < crateMatches.Count; i++)
            {
                var group = crateMatches[i].Groups[1];
                crates[i] = new CratePosition(group.Value.Single(), group.Index);
                // Console.WriteLine($"Added crate: {crates[i].Label} with offset: {crates[i].Offset}");
            }
            result.Values = crates;
            return true;
        }

        var idMatches = Regex.Matches(s, @"(?:^|\s)([0-9])(?:\s|$)", RegexOptions.Compiled);
        if (idMatches.Any())
        {
            result = new RegexStackRow(StackRowType.Ids);
            var ids = new CratePosition[idMatches.Count];
            for (int i = 0; i < idMatches.Count; i++)
            {
                var group = idMatches[i].Groups[1];
                ids[i] = new CratePosition(group.Value.Single(), group.Index);
                // Console.WriteLine($"Added id: {ids[i].Label} with offset: {ids[i].Offset}");
            }
            result.Values = ids;
            return true;
        }

        return Try.Failed(out result);
    }
}

internal record struct RegexCraneMove : ISpanParsable<RegexCraneMove>
{
    public int Quantity { get; set; }
    public int From { get; set; }
    public int To { get; set; }

    public CraneMove ToCraneMove() =>
        new CraneMove { Quantity = Quantity, From = From, To = To };

    public static RegexCraneMove Parse(ReadOnlySpan<char> s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException($"{s}");
    }

    public static RegexCraneMove Parse(string s, IFormatProvider? provider)
    {
        if (TryParse(s, provider, out var result))
            return result;

        throw new FormatException(s);
    }

    public static bool TryParse(
        ReadOnlySpan<char> s, IFormatProvider? provider, [MaybeNullWhen(false)] out RegexCraneMove result) =>
        TryParse(s.ToString(), provider, out result);

    public static bool TryParse(
        [NotNullWhen(true)] string? s, IFormatProvider? provider, [MaybeNullWhen(false)] out RegexCraneMove result)
    {
        if (s is null)
            return Try.Failed(out result);

        var match = Regex.Match(s, @"^move ([0-9]+) from ([0-9]) to ([0-9])$", RegexOptions.Compiled);
        if (!match.Success)
            return Try.Failed(out result);

        if (!int.TryParse(match.Groups[1].Value, out var quantity))
            return Try.Failed(out result);

        if (!int.TryParse(match.Groups[2].Value, out var from))
            return Try.Failed(out result);

        if (!int.TryParse(match.Groups[3].Value, out var to))
            return Try.Failed(out result);

        result = new RegexCraneMove
        {
            Quantity = quantity,
            From = from,
            To = to
        };
        return true;
    }
}
