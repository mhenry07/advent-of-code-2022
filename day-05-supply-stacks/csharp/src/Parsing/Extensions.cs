using System.Buffers;
using System.Text;

namespace Parsing;

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
