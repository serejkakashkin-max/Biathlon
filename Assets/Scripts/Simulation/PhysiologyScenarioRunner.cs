using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class PhysiologyScenarioRunner
    {
        public PhysiologyScenario Scenario { get; }
        public PhysiologySession Session { get; }
        public bool IsComplete => Session.Clock.Tick.Value == Scenario.TotalTicks;
        public PhysiologyInput LastInput { get; private set; }
        public PhysiologyScenarioRunner(PhysiologyScenarioSuite suite, PhysiologyScenario scenario,
            PhysiologyDefinition template, PhysiologyStatCurves curves, IRaceEventSink events)
        {
            if (suite == null || curves == null) throw new ArgumentNullException();
            Scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            var definition = curves.Apply(template, scenario.Stats);
            for (int i = 0; i < scenario.SegmentCount; i++)
                PhysiologyNumbers.RequireRange(scenario.GetSegment(i).TerrainMuscleFactor, 0, definition.MaximumTerrainMuscleFactor, "scenario terrain");
            var initial = new PhysiologyState(definition.RestingHrBpm + (definition.MaxHrBpm - definition.RestingHrBpm) * scenario.InitialHrFraction,
                scenario.InitialReserve, scenario.InitialFatigue, 0);
            Session = new PhysiologySession(new SimulationConfig(1, suite.Id + ":" + scenario.Id, suite.TickFrequency, suite.Seed),
                definition, new AthleteId("scenario_athlete"), initial, events);
        }
        public PhysiologyFrame Step()
        {
            if (IsComplete) throw new InvalidOperationException("Workload has completed.");
            var tick = Session.Clock.Tick.Next();
            var segment = Scenario.AtTick(tick.Value);
            var input = new PhysiologyInput(new SimulationInput(tick, Session.AthleteId, segment.Control), segment.Form, segment.TerrainMuscleFactor);
            var frame = Session.Step(input);
            LastInput = input;
            return frame;
        }
    }
}
