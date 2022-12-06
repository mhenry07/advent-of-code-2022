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
    private const int CharBufferLength = 256; // should be <= 1 KB if using stackalloc

    // based on https://learn.microsoft.com/en-us/dotnet/standard/io/pipelines
    // and https://timiskhakov.github.io/posts/exploring-spans-and-pipelines
    public static async ValueTask<IReadOnlyList<TItem>> ParseLinesAsync<TItem>(
        string filename, Encoding encoding, IFormatProvider? provider)
        where TItem : ISpanParsable<TItem>
    {
        var result = new List<TItem>();
        Memory<char> charBuffer = new char[CharBufferLength]; // allocating once outside loop rather than stackalloc within loop
        using var stream = File.OpenRead(filename);
        var reader = PipeReader.Create(stream);
        while (true)
        {
            var readResult = await reader.ReadAsync().ConfigureAwait(false);
            var buffer = readResult.Buffer;

            while (TryReadLine(ref buffer, out var line))
            {
                var item = Parse<TItem>(line, charBuffer, encoding, provider);
                result.Add(item);
            }

            reader.AdvanceTo(buffer.Start, buffer.End);

            if (readResult.IsCompleted)
                break;
        }

        await reader.CompleteAsync().ConfigureAwait(false);

        return result;
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

    // TODO: benchmark `Span<char> charBuffer = stackalloc char[MaxStackallocLength]` here vs. Memory<T> outside loop
    private static TItem Parse<TItem>(
        ReadOnlySequence<byte> line, Memory<char> charMemory, Encoding encoding, IFormatProvider? provider)
        where TItem : ISpanParsable<TItem>
    {
        char[]? rented = null;
        Span<char> charBuffer = line.Length <= charMemory.Length
            ? charMemory.Span
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
