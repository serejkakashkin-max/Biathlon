using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum TechniqueAssistMode { Automatic = 0, Manual = 1 }
    public readonly struct TechniqueAssistOutput
    {
        public TechniqueAssistMode Mode { get; }
        public string RequestedTechniqueId { get; }
        public SkiTechniqueOutput Applied { get; }
        public string BestTechniqueId { get; }
        public double EfficiencyLoss { get; }
        public bool RequestOverridden => RequestedTechniqueId != null && RequestedTechniqueId != Applied.TechniqueId;
        internal TechniqueAssistOutput(TechniqueAssistMode mode, string requested, SkiTechniqueOutput applied, SkiTechniqueOutput best)
        { Mode = mode; RequestedTechniqueId = requested; Applied = applied; BestTechniqueId = best.TechniqueId; EfficiencyLoss = Math.Max(0, best.Efficiency - applied.Efficiency); }
    }
    public sealed class TechniqueAssistSystem
    {
        private readonly SkiTechniqueSystem technique;
        public TechniqueAssistSystem(SkiTechniqueDefinition definition) { technique = new SkiTechniqueSystem(definition); }
        public TechniqueAssistOutput Evaluate(TechniqueAssistMode mode, PlayerCommand command, SkiTechniqueStats stats,
            TrackSample track, double muscularFatigue, double deliveredWatts)
        {
            if (!command.IsValid) throw new ArgumentException("A valid player command is required.");
            if (mode != TechniqueAssistMode.Automatic && mode != TechniqueAssistMode.Manual) throw new ArgumentOutOfRangeException(nameof(mode));
            if (mode == TechniqueAssistMode.Manual && command.TechniqueId == null) throw new ArgumentException("Manual technique requires an explicit stable ID.");
            var best = technique.Evaluate(stats, track, muscularFatigue, deliveredWatts);
            // Validate any requested ID even when automatic mode takes precedence; stale recordings must fail.
            var requested = command.TechniqueId == null ? best : technique.Evaluate(stats, track, muscularFatigue, deliveredWatts, command.TechniqueId);
            return new TechniqueAssistOutput(mode, command.TechniqueId, mode == TechniqueAssistMode.Automatic ? best : requested, best);
        }
    }
}
