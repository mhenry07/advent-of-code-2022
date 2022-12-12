using System.Buffers;
using System.IO.Pipelines;
using System.Text;

/// <summary>
/// A file parser that parses each line of a file and returns a collection of parsed items.
/// </summary>
/// <remarks>
/// Uses System.IO.Pipelines and spans to improve performance and reduce allocations.
/// </remarks>
public static class FilePipelineParser
{
    public const int MaxStackallocLength = 256; // should be <= 1 KB when using stackalloc

    // implementing both halves of the pipe seems comparable to only implementing the reader via PipeReader.FromStream
    // update: the reader/writer allocates more (likely due to the List)
    public static async Task<IReadOnlyList<TItem>> ParseFileAsync<TItem>(
        string filename, Encoding encoding, IFormatProvider? provider, CancellationToken cancellationToken = default)
        where TItem : ISpanParsable<TItem>
    {
        using var stream = File.OpenRead(filename);

        var pipe = new Pipe();
        var writingTask = FillPipeAsync(stream, pipe.Writer, cancellationToken);
        var readingTask = ReadPipeAsync<TItem>(pipe.Reader, encoding, provider, cancellationToken);

        await Task.WhenAll(writingTask, readingTask).ConfigureAwait(false);

        return await readingTask;
    }

    public static async Task FillPipeAsync(
        Stream sourceStream, PipeWriter writer, CancellationToken cancellationToken)
    {
        const int minimumBufferSize = 512;

        while (true)
        {
            var memory = writer.GetMemory(minimumBufferSize);
            try
            {
                var bytesRead = await sourceStream.ReadAsync(memory, cancellationToken).ConfigureAwait(false);
                if (bytesRead == 0)
                    break;

                writer.Advance(bytesRead);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"FillPipeAsync failed: {ex}");
                break;
            }

            var result = await writer.FlushAsync(cancellationToken);

            if (result.IsCompleted)
                break;
        }

        await writer.CompleteAsync();
    }

    public static async Task<IReadOnlyList<TItem>> ReadPipeAsync<TItem>(
        PipeReader reader, Encoding encoding, IFormatProvider? provider, CancellationToken cancellationToken)
        where TItem : ISpanParsable<TItem>
    {
        var items = new List<TItem>();
        while (true)
        {
            var readResult = await reader.ReadAsync(cancellationToken).ConfigureAwait(false);
            var buffer = readResult.Buffer;

            while (TryReadLine(ref buffer, out ReadOnlySequence<byte> line))
            {
                var item = Parse<TItem>(line, encoding, provider);
                items.Add(item);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);

        return items;
    }

    // based on https://learn.microsoft.com/en-us/dotnet/standard/io/pipelines
    // and https://timiskhakov.github.io/posts/exploring-spans-and-pipelines
    // note: I think file read should be a Pipeline "writer" (e.g. FillPipeAsync)
    //       and the parser and/or consumer should be a Pipeline "reader"?
    //      (No: it's comparable, but our writer+reader uses a List which allocates more)
    public static async IAsyncEnumerable<TItem> ParseLinesAsync<TItem>(
        string filename, Encoding encoding, IFormatProvider? provider)
        where TItem : ISpanParsable<TItem>
    {
        using var stream = File.OpenRead(filename);
        var reader = PipeReader.Create(stream);
        while (true)
        {
            var readResult = await reader.ReadAsync().ConfigureAwait(false);
            var buffer = readResult.Buffer;

            while (TryReadLine(ref buffer, out var line))
            {
                var item = Parse<TItem>(line, encoding, provider);
                yield return item;
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);
    }

    private static bool TryReadLine(ref ReadOnlySequence<byte> buffer, out ReadOnlySequence<byte> line)
    {
        var position = buffer.PositionOf((byte)'\n');

        if (position == null)
        {
            line = default;
            return false;
        }

        line = buffer.Slice(0, position.Value);
        buffer = buffer.Slice(buffer.GetPosition(1, position.Value));

        return true;
    }

    // it allocates less to use stackalloc here rather than passing in a Memory<char>
    private static TItem Parse<TItem>(
        ReadOnlySequence<byte> line, Encoding encoding, IFormatProvider? provider)
        where TItem : ISpanParsable<TItem>
    {
        char[]? rented = null;
        Span<char> charBuffer = line.Length <= MaxStackallocLength
            ? stackalloc char[(int)line.Length]
            : (rented = ArrayPool<char>.Shared.Rent((int)line.Length));

        try
        {
            var length = encoding.GetChars(line, charBuffer);
            var chars = charBuffer.Slice(0, length).Trim();

            return TItem.Parse(chars, provider);
        }
        finally
        {
            if (rented != null)
                ArrayPool<char>.Shared.Return(rented);
        }
    }
}
