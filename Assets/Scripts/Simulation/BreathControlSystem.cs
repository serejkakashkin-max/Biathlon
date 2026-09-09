using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct BreathControlFrame
    {
        public bool Holding { get; } public bool Blocked { get; }
        public double Debt { get; } public double BreathingMultiplier { get; }
        public BreathControlFrame(bool holding, bool blocked, double debt, double multiplier)
        { Holding = holding; Blocked = blocked; Debt = debt; BreathingMultiplier = multiplier; }
    }
    public sealed class BreathControlSystem
    {
        private readonly BreathControlDefinition definition;
        private readonly AthleteId athlete;
        private readonly int frequency;
        private readonly double recovery;
        private readonly IRaceEventSink events;
        private long tick;
        public BreathControlFrame LastFrame { get; private set; } = new BreathControlFrame(false, false, 0, 1);
        public BreathControlSystem(BreathControlDefinition definition, AthleteId athlete, int frequency, IRaceEventSink events,
            ShootingStanceProfile stance = null, SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!athlete.IsValid || frequency <= 0) throw new ArgumentException("Invalid breath identity/clock.");
            this.events = events ?? throw new ArgumentNullException(nameof(events)); this.athlete = athlete;
            this.frequency = frequency; tick = initialTick.Value; recovery = definition.RecoveryRate * (stance?.RecoveryMultiplier ?? 1) / frequency;
        }
        public BreathControlFrame Step(SimulationTick target, bool holdRequested, int breathControl)
        {
            if (target.Value != checked(tick + 1)) throw new ArgumentException("Breath control ticks must be consecutive.");
            if (breathControl < 0 || breathControl > 100) throw new ArgumentOutOfRangeException(nameof(breathControl));
            bool blocked = LastFrame.Blocked, holding = holdRequested && !blocked;
            double debt;
            if (holding)
            {
                double rate = definition.DebtRate * (1 + (definition.ExpertDebtMultiplier - 1) * breathControl / 100.0);
                debt = PhysiologyNumbers.Quantize(Math.Min(1, LastFrame.Debt + rate / frequency));
                if (debt >= 1) { holding = false; blocked = true; }
            }
            else debt = PhysiologyNumbers.Quantize(Math.Max(0, LastFrame.Debt - recovery));
            if (!holdRequested && debt <= definition.RearmDebt) blocked = false;
            double multiplier = holding ? definition.HoldMultiplier + definition.DiscomfortGain * debt * debt : 1 + definition.ReboundGain * debt;
            var next = new BreathControlFrame(holding, blocked, debt, PhysiologyNumbers.Quantize(multiplier));
            bool changed = holding != LastFrame.Holding;
            LastFrame = next; tick = target.Value;
            if (changed) events.Record(new RaceEvent(target, frequency, athlete, holding ? RaceEventType.BreathHoldStarted : RaceEventType.BreathHoldEnded,
                blocked ? 1 : 0));
            return next;
        }
    }
}
