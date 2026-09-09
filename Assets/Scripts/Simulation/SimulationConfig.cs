using System;
namespace Biathlon.Simulation
{
    public sealed class SimulationConfig
    {
        public int SchemaVersion { get; }
        public string Id { get; }
        public int TickFrequency { get; }
        public ulong Seed { get; }
        public SimulationConfig(int schemaVersion, string id, int tickFrequency, ulong seed)
        {
            if (schemaVersion != 1) throw new ArgumentOutOfRangeException(nameof(schemaVersion), "Unsupported schema.");
            if (string.IsNullOrWhiteSpace(id)) throw new ArgumentException("A stable config ID is required.", nameof(id));
            if (tickFrequency <= 0) throw new ArgumentOutOfRangeException(nameof(tickFrequency));
            SchemaVersion = schemaVersion;
            Id = id;
            TickFrequency = tickFrequency;
            Seed = seed;
        }
    }
}
