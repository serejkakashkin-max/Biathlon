using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PlayerReplayTests
    {
        public static PlayerControlSettings Settings() => PlayerControlSettings.FromJson(PhysiologyContractTests.DefinitionJson("player_control_settings_v1.json"));
        public static PlayerTimeTrialSession Create(RaceEventLog events, PlayerControlSettings settings = null)
        {
            var track = SyntheticTrackTests.Generated("synthetic_rolling_v1");
            var requirements = CompetitionRouteRequirements.TimeTrial("player_trial", track.GetLoop("main_long").Length, 1);
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("player_route", track.Id, "main_long", requirements), requirements);
            return new PlayerTimeTrialSession(new SimulationConfig(1, "player_sim", 20, 12345), new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),
                TimeTrialTests.Athlete(), track, route, SnowConditionTests.Conditions(), "ski_neutral", settings ?? Settings(),
                EffortControlTests.Definition(), SteeringTests.Definition(), OccupancyTests.Definition(), events);
        }
        private static PlayerCommand Command(PlayerTimeTrialSession s, string technique = null) =>
            new PlayerCommand(s.Clock.Tick.Next(), s.AthleteId, 800, (s.Clock.Tick.Value / 200) % 2 == 0 ? -500 : 500,
                EffortPreset.Race, tuckHeld: true, techniqueId: technique);
        [Test]
        public void SerializedCommandsReproduceCompleteMotionResourcesLateralPositionAndEvents()
        {
            var logA = new RaceEventLog(1000); var a = Create(logA);
            var tape = new PlayerCommandTape("test_recording", a.ContextJson, a.AthleteId, 100000);
            while (!a.HasFinished && a.Clock.Tick.Value < 100000) tape.StepAndRecord(a, Command(a));
            Assert.That(a.HasFinished, Is.True);
            var restored = PlayerCommandTape.FromJson(tape.ToJson()); var logB = new RaceEventLog(1000); var b = Create(logB);
            for (int i = 0; i < restored.Count; i++) restored.ReplayNext(b);
            Assert.That(b.Placement, Is.EqualTo(a.Placement)); Assert.That(b.PhysiologyState, Is.EqualTo(a.PhysiologyState));
            Assert.That(b.FinishTime, Is.EqualTo(a.FinishTime)); Assert.That(logB.Count, Is.EqualTo(logA.Count));
            for (int i = 0; i < logA.Count; i++) Assert.That(logB[i], Is.EqualTo(logA[i]));
            Assert.Catch(() => restored.ReplayNext(b));
        }
        [Test]
        public void InvalidCommandsCapacityAndChangedSettingsCannotPartlyAdvanceRaceOrTape()
        {
            var events = new RaceEventLog(1000); var s = Create(events); var tape = new PlayerCommandTape("record", s.ContextJson, s.AthleteId, 1);
            Assert.Catch(() => tape.StepAndRecord(s, Command(s, "unknown")));
            Assert.That(s.Clock.Tick.Value, Is.Zero); Assert.That(tape.Count, Is.Zero); Assert.That(events.Count, Is.Zero);
            tape.StepAndRecord(s, Command(s)); Assert.Catch(() => tape.StepAndRecord(s, Command(s)));
            Assert.That(s.Clock.Tick.Value, Is.EqualTo(1)); Assert.That(tape.Count, Is.EqualTo(1));
            var changed = PlayerControlSettings.FromJson(Settings().ToJson().Replace("\"assisted_center\":true", "\"assisted_center\":false"));
            Assert.Catch(() => tape.ReplayNext(Create(new RaceEventLog(1000), changed)));
            Assert.Catch(() => tape.Append(tape.GetCommand(0)));
            Assert.Catch(() => PlayerCommandTape.FromJson(PhysiologyContractTests.ReplaceNumber(tape.ToJson(), "schema_version", "2")));
        }
        [Test]
        public void RecordingAndControlledTicksDoNotAllocate()
        {
            var s = Create(new RaceEventLog(1000)); var tape = new PlayerCommandTape("record", s.ContextJson, s.AthleteId, 2000);
            tape.StepAndRecord(s, Command(s)); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) tape.StepAndRecord(s, Command(s));
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(s.Placement.Motion.DistanceMeters, Is.GreaterThan(0));
        }
    }
}
