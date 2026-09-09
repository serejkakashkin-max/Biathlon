namespace Biathlon.Domain
{
    public interface IRandomSource
    {
        ulong State { get; }
        ulong NextUInt64();
    }
}
