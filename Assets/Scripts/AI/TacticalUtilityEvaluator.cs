using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public readonly struct TacticalCandidate
    {
        public TacticalIntentKind Kind { get; }
        public bool Eligible { get; }
        public AthleteId Target { get; }
        public TacticalCandidate(TacticalIntentKind kind, bool eligible, AthleteId target = default)
        {
            if (kind < TacticalIntentKind.Recover || kind > TacticalIntentKind.PreRangeRecovery ||
                (eligible && TacticalIntentDecision.RequiresTarget(kind) != target.IsValid) || (!eligible && target.IsValid))
                throw new ArgumentException("Candidate kind, eligibility and target mismatch.");
            Kind = kind; Eligible = eligible; Target = target;
        }
    }
    public readonly struct TacticalUtilityScore
    {
        public TacticalIntentKind Kind { get; }
        public bool Eligible { get; }
        public double Bias { get; }
        public double Features { get; }
        public double Personality { get; }
        public double Total { get; }
        internal TacticalUtilityScore(TacticalIntentKind kind, bool eligible, double bias, double features, double personality)
        { Kind = kind; Eligible = eligible; Bias = bias; Features = features; Personality = personality;
          Total = PhysiologyNumbers.Quantize(bias + features + personality); }
    }
    /// <summary>Pure utility arithmetic. Feature values and personality preferences are normalized [0,1].
    /// Preferences use 0.5 as neutral. Eligibility comes from real race/space constraints, not utility scores.</summary>
    public sealed class TacticalUtilityEvaluator
    {
        private readonly TacticalUtilityDefinition definition;
        public TacticalUtilityEvaluator(TacticalUtilityDefinition definition)
        { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public TacticalIntentDecision Evaluate(AthleteId athlete, SimulationTick tick, SimulationTick expires,
            ReadOnlySpan<double> features, ReadOnlySpan<double> preferences, ReadOnlySpan<TacticalCandidate> candidates,
            Span<TacticalUtilityScore> scores)
        {
            if (!athlete.IsValid || expires.Value <= tick.Value || features.Length != 8 || preferences.Length != 8 || candidates.Length != 8 || scores.Length < 8)
                throw new ArgumentException("Valid actor, lifetime and complete utility buffers required.");
            // Validate all input before publishing any output contribution.
            for (int i = 0; i < 8; i++)
            {
                PhysiologyNumbers.RequireRange(features[i], 0, 1, nameof(features));
                PhysiologyNumbers.RequireRange(preferences[i], 0, 1, nameof(preferences));
                var candidate = candidates[i];
                if ((int)candidate.Kind != i || candidate.Target.Equals(athlete) ||
                    (candidate.Eligible && TacticalIntentDecision.RequiresTarget(candidate.Kind) != candidate.Target.IsValid) ||
                    (!candidate.Eligible && candidate.Target.IsValid)) throw new ArgumentException("Invalid ordered candidate.");
            }
            if (!candidates[(int)TacticalIntentKind.Hold].Eligible) throw new ArgumentException("Hold must remain an eligible fallback.");
            int winner = -1; double best = double.NegativeInfinity;
            for (int i = 0; i < 8; i++)
            {
                var candidate = candidates[i]; var row = definition.Get(candidate.Kind); double contribution = 0;
                for (int j = 0; j < 8; j++) contribution += row.Weight((TacticalUtilityFeature)j) * features[j];
                var score = new TacticalUtilityScore(candidate.Kind, candidate.Eligible, row.Bias,
                    PhysiologyNumbers.Quantize(contribution), PhysiologyNumbers.Quantize(row.PersonalityWeight * (2 * preferences[i] - 1)));
                scores[i] = score;
                if (candidate.Eligible && score.Total > best) { winner = i; best = score.Total; }
            }
            // Equal canonical totals keep the first approved enum value; no random tie-break or order dependence.
            return new TacticalIntentDecision(definition.Id, athlete, candidates[winner].Kind, tick, expires, candidates[winner].Target);
        }
    }
}
