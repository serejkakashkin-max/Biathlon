using System;
using System.Collections.Generic;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public sealed class SimulationWorld
    {
        private readonly AthleteState[] athletes;
        private readonly Dictionary<AthleteId, int> indices;
        private readonly IRaceEventSink events;
        public SimulationConfig Config { get; }
        public SimulationClock Clock { get; }
        public IRandomSource Random { get; }
        public int AthleteCount => athletes.Length;
        public AthleteState GetAthlete(int index) => athletes[index];

        public SimulationWorld(SimulationConfig config, IReadOnlyList<AthleteState> initialState, IRaceEventSink eventSink)
        {
            Config = config ?? throw new ArgumentNullException(nameof(config));
            if (initialState == null) throw new ArgumentNullException(nameof(initialState));
            events = eventSink ?? throw new ArgumentNullException(nameof(eventSink));
            Clock = new SimulationClock(config.TickFrequency);
            Random = new SeededRandomSource(config.Seed);
            athletes = new AthleteState[initialState.Count];
            indices = new Dictionary<AthleteId, int>(initialState.Count);
            for (int i = 0; i < athletes.Length; i++)
            {
                var state = initialState[i];
                if (!state.Id.IsValid || indices.ContainsKey(state.Id))
                    throw new ArgumentException("Athlete IDs must be valid and unique.", nameof(initialState));
                athletes[i] = state;
                indices.Add(state.Id, i);
            }
        }
        /// <summary>Inputs target the next tick. Same-tick inputs apply in caller order, including repeated IDs.</summary>
        public void Step(IReadOnlyList<SimulationInput> inputs)
        {
            if (inputs == null) throw new ArgumentNullException(nameof(inputs));
            var nextTick = Clock.Tick.Next();
            // Validate the whole batch before any mutation.
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                if (!input.Tick.Equals(nextTick) || !input.AthleteId.IsValid || !indices.ContainsKey(input.AthleteId))
                    throw new ArgumentException("Input must reference a known athlete and the next tick.", nameof(inputs));
                AthleteState.ValidateEffort(input.RequestedEffort);
            }
            Clock.Advance();
            for (int i = 0; i < inputs.Count; i++)
            {
                var input = inputs[i];
                int index = indices[input.AthleteId];
                if (athletes[index].RequestedEffort == input.RequestedEffort) continue;
                athletes[index] = new AthleteState(input.AthleteId, input.RequestedEffort);
                events.Record(new RaceEvent(nextTick, Clock.Frequency, input.AthleteId, RaceEventType.EffortChanged, input.RequestedEffort));
            }
        }
        public void Step() => Step(Array.Empty<SimulationInput>());
    }
}
