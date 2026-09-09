using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public sealed class SimulationClock
    {
        public SimulationTick Tick { get; private set; }
        public int Frequency { get; }
        // Authoritative time is the rational pair (Tick.Value, Frequency), never accumulated floating point.
        public double ElapsedSeconds => Tick.Value / (double)Frequency;
        public SimulationClock(int frequency)
        {
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            Frequency = frequency;
        }
        internal void Advance() => Tick = Tick.Next();
    }
}
