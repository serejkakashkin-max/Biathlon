using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    public readonly struct TacticalDecisionWindow
    {
        public SimulationTick Tick { get; }
        public bool DecisionDue { get; }
        public SimulationTick NextDecisionTick { get; }
        public SimulationTick PlanningEndTick { get; }
        public long DecisionCount { get; }
        internal TacticalDecisionWindow(SimulationTick tick, bool due, SimulationTick next, SimulationTick horizon, long count)
        { Tick = tick; DecisionDue = due; NextDecisionTick = next; PlanningEndTick = horizon; DecisionCount = count; }
    }
    public sealed class TacticalDecisionClock
    {
        private readonly long origin;
        private readonly int frequency, decisionHz;
        public SimulationTick Tick { get; private set; }
        public SimulationTick NextDecisionTick { get; private set; }
        public SimulationTick PlanningEndTick { get; private set; }
        public long DecisionCount { get; private set; }
        public long PlanningHorizonTicks { get; }
        public TacticalDecisionClock(TacticalCadenceDefinition definition, int simulationFrequency, SimulationTick initialTick = default)
        {
            if (definition == null) throw new ArgumentNullException(nameof(definition));
            if (simulationFrequency < definition.DecisionHz) throw new ArgumentException("Simulation must support at least one tactical decision per tick.");
            frequency = simulationFrequency; decisionHz = definition.DecisionHz; origin = initialTick.Value;
            PlanningHorizonTicks = checked((long)Math.Ceiling((decimal)definition.PlanningHorizonSeconds * frequency));
            NextDecisionTick = initialTick.Next(); _ = checked(NextDecisionTick.Value + PlanningHorizonTicks);
            Tick = initialTick; PlanningEndTick = initialTick;
        }
        public TacticalDecisionWindow Step(SimulationTick tick)
        {
            if (tick.Value != checked(Tick.Value + 1)) throw new ArgumentException("Tactical clock ticks must be consecutive.");
            bool due = tick.Value == NextDecisionTick.Value; long count = DecisionCount;
            var next = NextDecisionTick; var horizon = PlanningEndTick;
            if (due)
            {
                count = checked(count + 1);
                long numerator = checked(count * frequency);
                long offset = checked(numerator / decisionHz + (numerator % decisionHz == 0 ? 0 : 1));
                next = new SimulationTick(checked(origin + 1 + offset));
                horizon = new SimulationTick(checked(tick.Value + PlanningHorizonTicks));
            }
            Tick = tick; DecisionCount = count; NextDecisionTick = next; PlanningEndTick = horizon;
            return new TacticalDecisionWindow(tick, due, next, horizon, count);
        }
    }
}
