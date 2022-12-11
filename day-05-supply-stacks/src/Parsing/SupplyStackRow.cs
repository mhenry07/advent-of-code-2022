using System.Buffers;
using System.Diagnostics.CodeAnalysis;

namespace Parsing;

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
