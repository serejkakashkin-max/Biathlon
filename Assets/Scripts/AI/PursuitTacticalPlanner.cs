using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    [Flags] public enum PursuitTacticalReason { None=0, Chase=1, Defend=2, Cooperate=4, LastRange=8 }
    public readonly struct PursuitTacticalPlan
    {
        public PursuitTacticalReason Reason { get; }
        public double GapPressure { get; }
        public double DefensePressure { get; }
        public double DraftStrength { get; }
        public double ShootingUrgency { get; }
        internal PursuitTacticalPlan(PursuitTacticalReason reason,double gap,double defense,double draft,double urgency)
        { Reason=reason;GapPressure=gap;DefensePressure=defense;DraftStrength=draft;ShootingUrgency=urgency; }
    }
    /// <summary>Bounded decision preferences only. Eligibility and physical capability stay with
    /// the shared utility/pace/space controllers. Call at tactical cadence.</summary>
    public sealed class PursuitTacticalPlanner
    {
        private readonly PursuitTacticsDefinition definition;
        public PursuitTacticalPlanner(PursuitTacticsDefinition definition)
        { this.definition=definition??throw new ArgumentNullException(nameof(definition)); }
        public PursuitTacticalPlan Evaluate(double? globalGapSeconds, int? observedPlace, double? nearestBehindMeters,
            double eligibleDraftStrength, bool lastRange, double baseShootingUrgency)
        {
            if(globalGapSeconds.HasValue) PhysiologyNumbers.RequireRange(globalGapSeconds.Value,0,double.MaxValue,nameof(globalGapSeconds));
            if(observedPlace.HasValue && observedPlace.Value<1) throw new ArgumentException("Positive observed place required.");
            if(nearestBehindMeters.HasValue) PhysiologyNumbers.RequireRange(nearestBehindMeters.Value,0,double.MaxValue,nameof(nearestBehindMeters));
            PhysiologyNumbers.RequireRange(eligibleDraftStrength,0,1,nameof(eligibleDraftStrength));
            PhysiologyNumbers.RequireRange(baseShootingUrgency,0,1,nameof(baseShootingUrgency));
            double gap=globalGapSeconds.HasValue?Clamp(globalGapSeconds.Value/definition.FullChaseGap):0;
            bool leader=observedPlace==1 && globalGapSeconds.HasValue && globalGapSeconds.Value==0;
            double defense=leader && nearestBehindMeters.HasValue?Clamp(1-nearestBehindMeters.Value/definition.DefenseRadius):0;
            var reason=(gap>0?PursuitTacticalReason.Chase:0)|(defense>0?PursuitTacticalReason.Defend:0)|
                (eligibleDraftStrength>0?PursuitTacticalReason.Cooperate:0)|(lastRange?PursuitTacticalReason.LastRange:0);
            double urgency=Clamp(baseShootingUrgency+gap*definition.ChaseUrgency+
                (lastRange?gap*definition.LastRangeUrgency-(leader?definition.LeaderCaution:0):0));
            return new PursuitTacticalPlan(reason,PhysiologyNumbers.Quantize(gap),PhysiologyNumbers.Quantize(defense),eligibleDraftStrength,PhysiologyNumbers.Quantize(urgency));
        }
        public void ApplyPreferences(PursuitTacticalPlan plan, Span<double> preferences)
        {
            if(preferences.Length!=8) throw new ArgumentException("Complete tactical preference vector required.");
            for(int i=0;i<preferences.Length;i++) PhysiologyNumbers.RequireRange(preferences[i],0,1,nameof(preferences));
            preferences[(int)TacticalIntentKind.Attack]=Clamp(preferences[(int)TacticalIntentKind.Attack]+plan.GapPressure*definition.ChasePreference+plan.DefensePressure*definition.DefensePreference);
            preferences[(int)TacticalIntentKind.Follow]=Clamp(preferences[(int)TacticalIntentKind.Follow]+plan.DraftStrength*definition.GroupPreference);
            preferences[(int)TacticalIntentKind.Draft]=Clamp(preferences[(int)TacticalIntentKind.Draft]+plan.DraftStrength*definition.GroupPreference);
        }
        private static double Clamp(double value)=>Math.Max(0,Math.Min(1,value));
    }
}
