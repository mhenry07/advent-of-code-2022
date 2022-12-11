using System.Buffers;

namespace Parsing;

public record struct CharBuffer(char[] Buffer, int Length) : IDisposable
{
    public ReadOnlySpan<char> AsSpan() =>
        Buffer.AsSpan(0, Length);

    public void Dispose() =>
        ArrayPool<char>.Shared.Return(Buffer);
}
