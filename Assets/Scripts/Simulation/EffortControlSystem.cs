using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct EffortControlOutput
    {
        public int TargetControl { get; }
        public int AppliedControl { get; }
        public bool FinalSprintRequested { get; }
        internal double ContinuousControl { get; }
        internal EffortControlOutput(int target, int applied, bool sprint, double continuous)
        { TargetControl = target; AppliedControl = applied; FinalSprintRequested = sprint; ContinuousControl = continuous; }
    }
    public sealed class EffortControlSystem
    {
        private readonly EffortControlDefinition definition;
        private readonly AthleteId athleteId;
        private readonly double riseStep, fallStep;
        public SimulationTick Tick { get; private set; }
        public double ContinuousControl { get; private set; }
        public EffortControlSystem(EffortControlDefinition definition, AthleteId athleteId, int frequency, int initialControl = 0)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (!athleteId.IsValid || frequency <= 0) throw new ArgumentException("Valid athlete and frequency required.");
            AthleteState.ValidateEffort(initialControl); this.athleteId = athleteId; ContinuousControl = initialControl;
            riseStep = definition.AssistedRiseRate / frequency; fallStep = definition.AssistedFallRate / frequency;
            if (riseStep < PhysiologyNumbers.MinimumPositive || fallStep < PhysiologyNumbers.MinimumPositive)
                throw new ArgumentException("Assisted rate is below control precision.");
        }
        public int AnalogTarget(int rawControl)
        {
            AthleteState.ValidateEffort(rawControl);
            double value = (rawControl - definition.AnalogDeadzone) / (double)(definition.AnalogSaturation - definition.AnalogDeadzone);
            return (int)Math.Round(Math.Min(1, Math.Max(0, value)) * AthleteState.EffortUnits, MidpointRounding.ToEven);
        }
        public EffortControlOutput Step(PlayerCommand command, bool assistedResponse)
        {
            var result = Preview(command, assistedResponse);
            Commit(command, result);
            return result;
        }
        internal void Commit(PlayerCommand command, EffortControlOutput output)
        { Tick = command.Tick; ContinuousControl = output.ContinuousControl; }
        public EffortControlOutput Preview(PlayerCommand command, bool assistedResponse)
        {
            if (!command.IsValid || !command.AthleteId.Equals(athleteId) || !command.Tick.Equals(Tick.Next()))
                throw new ArgumentException("Effort command must target this athlete and next tick.");
            int target;
            switch (command.Preset)
            {
                case EffortPreset.Analog: target = AnalogTarget(command.AnalogEffort); break;
                case EffortPreset.Recovery: target = definition.RecoveryControl; break;
                case EffortPreset.Race: target = definition.RaceControl; break;
                case EffortPreset.Attack: target = definition.AttackControl; break;
                case EffortPreset.FinalSprint: target = definition.RaceControl; break;
                default: throw new ArgumentOutOfRangeException(nameof(command));
            }
            double next = assistedResponse ? ContinuousControl + Math.Max(-fallStep, Math.Min(riseStep, target - ContinuousControl)) : target;
            next = PhysiologyNumbers.Quantize(next);
            int applied = (int)Math.Round(next, MidpointRounding.ToEven);
            return new EffortControlOutput(target, applied, command.Preset == EffortPreset.FinalSprint, next);
        }
    }
}
