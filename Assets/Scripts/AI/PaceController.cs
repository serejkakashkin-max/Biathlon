using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public readonly struct PaceControllerFrame
    {
        public SimulationInput Input { get; }
        public TacticalIntentDecision Intent { get; }
        public int NominalEffort { get; }
        public int ControlError { get; }
        public int TargetEffort { get; }
        public int PlannedEffortAdjustment { get; }
        public bool DecisionAccepted { get; }
        internal PaceControllerFrame(SimulationInput input, TacticalIntentDecision intent, int nominal, int error, int target, bool accepted, int adjustment)
        { Input = input; Intent = intent; NominalEffort = nominal; ControlError = error; TargetEffort = target; DecisionAccepted = accepted; PlannedEffortAdjustment = adjustment; }
    }
    public sealed class PaceController
    {
        private readonly PaceControllerDefinition definition;
        private readonly AthleteId athlete;
        private readonly IRandomSource random;
        private readonly double step;
        private double effort;
        private TacticalIntentDecision intent;
        private int error;
        private int adjustment;
        public SimulationTick Tick { get; private set; }
        public PaceControllerFrame LastFrame { get; private set; }
        public PaceController(PaceControllerDefinition definition, AthleteId athlete, int frequency, IRandomSource random,
            SimulationTick initialTick = default, int initialEffort = 0)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.random = random ?? throw new ArgumentNullException(nameof(random));
            if (!athlete.IsValid || frequency <= 0 || frequency > 10000) throw new ArgumentException("Invalid controller actor/clock.");
            AthleteState.ValidateEffort(initialEffort); this.athlete = athlete; step = definition.ResponsePerSecond / frequency;
            Tick = initialTick; effort = initialEffort;
        }
        public PaceControllerFrame Step(SimulationTick tick, TacticalIntentDecision? newDecision = null, int plannedEffortAdjustment = 0)
        {
            if (tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Pace ticks must be consecutive.");
            var active = newDecision ?? intent;
            active.ValidateFor(athlete, tick);
            if (newDecision.HasValue && active.DecidedAt.Value != tick.Value) throw new ArgumentException("A new decision must be issued at this control tick.");
            if (plannedEffortAdjustment < -1000 || plannedEffortAdjustment > 1000 || (!newDecision.HasValue && plannedEffortAdjustment != 0))
                throw new ArgumentException("A bounded effort adjustment may only accompany a new decision.");
            int nextAdjustment = newDecision.HasValue ? plannedEffortAdjustment : adjustment;
            int nextError = error;
            if (newDecision.HasValue)
            {
                // A single fixed draw at decision acceptance, never a physical or hit probability modifier.
                ulong sample = random.NextUInt64(); int width = definition.ControlError * 2 + 1;
                nextError = (int)(sample % (ulong)width) - definition.ControlError;
            }
            int nominal = definition.Effort(active.Kind), target = Math.Max(0, Math.Min(1000, nominal + nextError + nextAdjustment));
            double nextEffort = PhysiologyNumbers.Quantize(effort + Math.Max(-step, Math.Min(step, target - effort)));
            var input = new SimulationInput(tick, athlete, (int)Math.Round(nextEffort, MidpointRounding.ToEven));
            var frame = new PaceControllerFrame(input, active, nominal, nextError, target, newDecision.HasValue, nextAdjustment);
            Tick = tick; intent = active; error = nextError; adjustment = nextAdjustment; effort = nextEffort; LastFrame = frame; return frame;
        }
    }
}
