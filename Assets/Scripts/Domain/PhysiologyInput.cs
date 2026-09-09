using System;

namespace Biathlon.Domain
{
    /// <summary>Complete per-tick workload command; record form/terrain as well as effort for replay.</summary>
    public readonly struct PhysiologyInput
    {
        public SimulationInput Effort { get; }
        public double Form { get; }
        public double TerrainMuscleFactor { get; }
        public PhysiologyInput(SimulationInput effort, double form, double terrainMuscleFactor)
        {
            if (!effort.AthleteId.IsValid) throw new ArgumentException("Invalid effort command.", nameof(effort));
            Effort = effort;
            Form = PhysiologyNumbers.RequireRange(form, 0, 1.10, nameof(form));
            TerrainMuscleFactor = PhysiologyNumbers.RequireRange(terrainMuscleFactor, 0, PhysiologyNumbers.MaximumMagnitude, nameof(terrainMuscleFactor));
        }
    }
}
