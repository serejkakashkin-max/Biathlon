using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Single-athlete headless M1 composition. Sinks obey the Foundation nonthrow/nonreentrant contract.</summary>
    public sealed class PhysiologySession
    {
        private readonly SimulationWorld kernel;
        private readonly SimulationInput[] batch = new SimulationInput[1];
        private readonly IRaceEventSink events;
        private readonly EffortSystem effort;
        private readonly AnaerobicSystem anaerobic;
        private readonly FatigueSystem fatigue;
        private readonly CardiacDriftSystem drift;
        private readonly HeartRateSystem heartRate;
        public PhysiologyDefinition Definition { get; }
        public SimulationConfig Config => kernel.Config;
        public SimulationClock Clock => kernel.Clock;
        public ulong RandomState => kernel.Random.State;
        public int RequestedControl => kernel.GetAthlete(0).RequestedEffort;
        public AthleteId AthleteId { get; }
        public PhysiologyState State { get; private set; }
        public PhysiologyFrame LastFrame { get; private set; }

        public PhysiologySession(SimulationConfig config, PhysiologyDefinition definition, AthleteId athleteId,
            PhysiologyState initialState, IRaceEventSink eventSink)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));
            Definition = definition ?? throw new ArgumentNullException(nameof(definition));
            initialState.Validate(definition);
            events = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
            kernel = new SimulationWorld(config, new[] { new AthleteState(athleteId) }, events);
            AthleteId = athleteId;
            State = initialState;
            effort = new EffortSystem(definition);
            anaerobic = new AnaerobicSystem(definition, config.TickFrequency);
            fatigue = new FatigueSystem(definition, config.TickFrequency);
            drift = new CardiacDriftSystem(definition, config.TickFrequency);
            heartRate = new HeartRateSystem(definition, config.TickFrequency);
            LastFrame = new PhysiologyFrame(Clock.Tick, athleteId, initialState, default,
                new HeartRateOutput(initialState.HeartRateBpm, initialState.HeartRateBpm, 0), 0, 0, 0);
        }

        public PhysiologyFrame Step(PhysiologyInput input)
        {
            var preview = Preview(input);
            CommitPreview(input, preview);
            return LastFrame;
        }

        public PhysiologyFrame Preview(PhysiologyInput input)
        {
            if (!input.Effort.Tick.Equals(Clock.Tick.Next()) || !input.Effort.AthleteId.Equals(AthleteId))
                throw new ArgumentException("Input must target this athlete and the next tick.", nameof(input));
            PhysiologyNumbers.RequireRange(input.TerrainMuscleFactor, 0, Definition.MaximumTerrainMuscleFactor, nameof(input));
            var previous = State;
            // Evaluate the full transition in locals before mutating any authoritative state.
            var power = effort.Evaluate(input.Effort.RequestedEffort, previous, input.Form);
            var reserve = anaerobic.Step(previous.AnaerobicReserve, power.DeliveredRelativeEffort, previous.MuscularFatigue);
            var muscle = fatigue.Step(previous.MuscularFatigue, power.DeliveredRelativeEffort, input.TerrainMuscleFactor);
            var exposure = drift.Step(previous.HighLoadSeconds, power.DeliveredRelativeEffort, muscle.Value);
            var hr = heartRate.Step(previous.HeartRateBpm, power.DeliveredRelativeEffort, muscle.Value, exposure.DriftBpm);
            var next = new PhysiologyState(hr.HeartRateBpm, reserve.Value, muscle.Value, exposure.HighLoadSeconds);
            next.Validate(Definition);
            return new PhysiologyFrame(input.Effort.Tick, AthleteId, next, power, hr, exposure.DriftBpm, reserve.ChangePerSecond, muscle.ChangePerSecond);
        }

        internal void CommitPreview(PhysiologyInput input, PhysiologyFrame preview)
        {
            var previous = State;
            var next = preview.State;
            batch[0] = input.Effort;
            kernel.Step(batch);
            State = next;
            LastFrame = preview;
            if (previous.AnaerobicReserve > 0 && next.AnaerobicReserve == 0) Record(RaceEventType.AnaerobicDepleted, 0);
            if (previous.AnaerobicReserve < 1 && next.AnaerobicReserve == 1) Record(RaceEventType.AnaerobicRefilled, 1);
            if (previous.MuscularFatigue < 1 && next.MuscularFatigue == 1) Record(RaceEventType.MusclesExhausted, 1);
            if (previous.MuscularFatigue > 0 && next.MuscularFatigue == 0) Record(RaceEventType.MusclesFullyRecovered, 0);
        }
        private void Record(RaceEventType type, int value) => events.Record(new RaceEvent(Clock.Tick, Clock.Frequency, AthleteId, type, value));
    }
}
