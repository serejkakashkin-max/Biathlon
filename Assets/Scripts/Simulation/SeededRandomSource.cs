using Biathlon.Domain;
namespace Biathlon.Simulation
{
    /// <summary>SplitMix64 with explicitly wrapping integer arithmetic; state can be restored as a seed.</summary>
    public sealed class SeededRandomSource : IRandomSource
    {
        public ulong State { get; private set; }
        public SeededRandomSource(ulong seed) { State = seed; }
        public ulong NextUInt64()
        {
            unchecked
            {
                State += 0x9E3779B97F4A7C15UL;
                ulong z = State;
                z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
                z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
                return z ^ (z >> 31);
            }
        }
    }
}
