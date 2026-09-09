using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct AiPhysicalNeighbor
    {
        public AthleteId AthleteId { get; }
        public long GapNanometers { get; }
        public double LateralOffsetMeters { get; }
        public double SpeedMetersPerSecond { get; }
        internal AiPhysicalNeighbor(SkiPlacement self, SkiPlacement other)
        { AthleteId = other.AthleteId; GapNanometers = other.Motion.DistanceNanometers - self.Motion.DistanceNanometers;
          LateralOffsetMeters = other.LateralMeters - self.LateralMeters; SpeedMetersPerSecond = other.Motion.SpeedMetersPerSecond; }
    }
    public readonly struct AiPhysicalPerception
    {
        public bool Available { get; }
        public SimulationTick Tick { get; }
        public SkiPlacement Self { get; }
        public DraftOutput Draft { get; }
        public int NeighborCount { get; }
        internal AiPhysicalPerception(SimulationTick tick, SkiPlacement self, DraftOutput draft, int count)
        { Available = true; Tick = tick; Self = self; Draft = draft; NeighborCount = count; }
        public void ValidateAgainst(AiSprintPerception race)
        {
            if (!Available || Tick.Value != race.Tick.Value || !Self.AthleteId.Equals(race.AthleteId))
                throw new ArgumentException("Physical and race perception must refer to the same athlete and tick.");
        }
    }
    /// <summary>Adapter for authoritative physical placements. The caller supplies one coherent tick,
    /// common corridor origins and the current physical track sample; never infer these from race progress.</summary>
    public sealed class PhysicalPerceptionSource
    {
        private readonly DraftingSystem drafting;
        public PhysicalPerceptionSource(DraftDefinition definition) { drafting = new DraftingSystem(definition); }
        public AiPhysicalPerception Capture(SimulationTick tick, int athleteIndex, ReadOnlySpan<SkiPlacement> placements,
            TrackSample currentTrack, TrackDistance radius, Span<AiPhysicalNeighbor> neighbors,
            double headwindMetersPerSecond = 0, double crosswindMetersPerSecond = 0)
        {
            if (athleteIndex < 0 || athleteIndex >= placements.Length || neighbors.Length < placements.Length - 1)
                throw new ArgumentException("Valid athlete and worst-case neighbor capacity required.");
            for (int i = 0; i < placements.Length; i++)
            {
                if (!placements[i].IsValid) throw new ArgumentException("Invalid physical placement.");
                for (int j = 0; j < i; j++)
                    if (placements[i].AthleteId.Equals(placements[j].AthleteId)) throw new ArgumentException("Duplicate physical athlete.");
            }
            long limit = checked(radius.Millimeters * SkiState.NanometersPerMillimeter);
            var self = placements[athleteIndex];
            // Shared evaluation also validates track and wind before writing caller storage.
            var draft = drafting.Evaluate(self, placements, currentTrack, headwindMetersPerSecond, crosswindMetersPerSecond);
            int count = 0;
            for (int i = 0; i < placements.Length; i++)
            {
                var other = placements[i];
                if (i == athleteIndex || !string.Equals(self.CorridorId, other.CorridorId, StringComparison.Ordinal)) continue;
                long delta = other.Motion.DistanceNanometers - self.Motion.DistanceNanometers;
                if (delta < -limit || delta > limit) continue;
                var item = new AiPhysicalNeighbor(self, other); int slot = count++;
                while (slot > 0 && string.CompareOrdinal(neighbors[slot - 1].AthleteId.Value, item.AthleteId.Value) > 0)
                { neighbors[slot] = neighbors[slot - 1]; slot--; }
                neighbors[slot] = item;
            }
            return new AiPhysicalPerception(tick, self, draft, count);
        }
    }
}
