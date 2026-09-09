using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SkiTechniqueOutput
    {
        public string TechniqueId { get; }
        public double SkillEfficiency { get; }
        public double TerrainAffinity { get; }
        public double ClimbingEfficiency { get; }
        public double FatigueModifier { get; }
        public double Efficiency => SkillEfficiency * TerrainAffinity * ClimbingEfficiency * FatigueModifier;
        public double PropulsivePowerWatts { get; }
        internal SkiTechniqueOutput(string id, double skill, double terrain, double climbing, double fatigue, double watts)
        {
            TechniqueId = id; SkillEfficiency = skill; TerrainAffinity = terrain;
            ClimbingEfficiency = climbing; FatigueModifier = fatigue;
            PropulsivePowerWatts = watts * skill * terrain * climbing * fatigue;
        }
    }

    public sealed class SkiTechniqueSystem
    {
        private readonly SkiTechniqueDefinition definition;
        public SkiTechniqueSystem(SkiTechniqueDefinition definition)
        { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }

        public SkiTechniqueOutput Evaluate(SkiTechniqueStats stats, TrackSample track, double muscularFatigue,
            double deliveredPowerWatts, string selectedTechniqueId = null)
        {
            if (string.IsNullOrEmpty(track.SegmentId)) throw new ArgumentException("A valid track sample is required.", nameof(track));
            PhysiologyNumbers.RequireRange(muscularFatigue, 0, 1, nameof(muscularFatigue));
            PhysiologyNumbers.RequireRange(deliveredPowerWatts, 0, PhysiologyNumbers.MaximumMagnitude, nameof(deliveredPowerWatts));
            double climbWeight = Math.Min(1, Math.Max(0, track.Grade / definition.FullClimbGrade));
            double skill = definition.MinimumSkillEfficiency + (1 - definition.MinimumSkillEfficiency) * stats.SkiTechnique / 100.0;
            double climbing = 1 - climbWeight * (1 - definition.MinimumClimbingEfficiency) * (1 - stats.Climbing / 100.0);
            double fatigue = 1 - muscularFatigue * (1 - definition.ExhaustedTechniqueModifier);
            var best = default(SkiTechniqueOutput);
            bool found = false;
            for (int i = 0; i < definition.ProfileCount; i++)
            {
                var profile = definition.GetProfile(i);
                if (selectedTechniqueId != null && !string.Equals(profile.Id, selectedTechniqueId, StringComparison.Ordinal)) continue;
                double terrain = profile.FlatAffinity + (profile.ClimbAffinity - profile.FlatAffinity) * climbWeight;
                var candidate = new SkiTechniqueOutput(profile.Id, skill, terrain, climbing, fatigue, deliveredPowerWatts);
                // Strict comparison gives serialized profile order a deterministic tie break, even while coasting.
                if (!found || candidate.Efficiency > best.Efficiency) { best = candidate; found = true; }
            }
            if (!found) throw new ArgumentException("Unknown technique ID.", nameof(selectedTechniqueId));
            return best;
        }
    }
}
