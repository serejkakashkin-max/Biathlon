using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintAthleteSessionTests
    {
        public static SprintAthleteSession Create(RaceEventLog events, long start = 1, ulong seed = 123)
        {
            var profile = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", profile.ToRouteRequirements()), profile.ToRouteRequirements());
            var ski = TimeTrialTests.Athlete(); var shooting = ShootingSessionTests.Athlete();
            shooting = ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id, ski.Id));
            return new SprintAthleteSession(new SimulationConfig(1, "sprint_test_sim", 20, seed), profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), ski, shooting, track, route,
                new CheckpointLayout(track, route), SnowConditionTests.Conditions(), "ski_neutral", new SimulationTick(start),
                new ShootingLaneAssignment(track.Range.Id, "lane_1", 1), events);
        }
        public static SprintAthleteInput Script(SprintAthleteSession session, int control = 800, double wind = 0)
        {
            var tick = session.Tick.Next();
            return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, session.AthleteId, control), 1, 1, wind),
                new WeaponAimCommand(tick, session.AthleteId, 0, 0), true);
        }
        [Test] public void FullScriptedSprintKeepsPhysiologyAndRequiresActualPenaltyDistance()
        {
            var log = new RaceEventLog(2000); var s = Create(log); double arrivalHr = 0, exitHr = 0; bool onMat = false; int shots = 0;
            while (!s.HasFinished && s.Tick.Value < 100000)
            {
                var previous = s.PhysiologyState; var frame = s.Step(Script(s));
                if (frame.ShootingActive)
                {
                    if (!onMat) arrivalHr = previous.HeartRateBpm;
                    exitHr = s.PhysiologyState.HeartRateBpm;
                    Assert.That(s.Motion.SpeedMetersPerSecond, Is.Zero);
                    Assert.That(frame.Shooting.Input.HeartRateBpm, Is.EqualTo(s.PhysiologyState.HeartRateBpm));
                    Assert.That(frame.Shooting.Input.Fatigue, Is.EqualTo(s.PhysiologyState.MuscularFatigue));
                    if (frame.Shooting.Shot.Lifecycle.ShotAccepted) shots++;
                }
                if (onMat && !frame.ShootingActive) Assert.That(exitHr, Is.LessThan(arrivalHr));
                onMat = frame.ShootingActive;
            }
            Assert.That(s.HasFinished, Is.True); Assert.That(shots, Is.EqualTo(10)); Assert.That(s.Lifecycle.CompletedBouts, Is.EqualTo(2));
            Assert.That(s.TotalPenaltyLoops, Is.GreaterThanOrEqualTo(8));
            Assert.That(s.Motion.DistanceNanometers, Is.EqualTo((3000 + s.TotalPenaltyLoops * 150) * 1000000000L));
            Assert.That(s.RouteNanometers, Is.EqualTo(3000000000000)); Assert.That(s.Lifecycle.CompletedLaps, Is.EqualTo(3));
            Assert.That(s.Checkpoints.Count, Is.EqualTo(9)); Assert.That(s.Checkpoints.Get(8).GlobalTime, Is.EqualTo(s.Lifecycle.GlobalFinishTime));
            int penalties = 0, finishes = 0;
            for (int i = 0; i < log.Count; i++)
            { if (log[i].EventType == RaceEventType.PenaltyLoopCompleted) penalties++; if (log[i].EventType == RaceEventType.RaceFinished) finishes++;
              if (i > 0) Assert.That(log[i].Tick.Value, Is.GreaterThanOrEqualTo(log[i - 1].Tick.Value)); }
            Assert.That(penalties, Is.EqualTo(s.TotalPenaltyLoops)); Assert.That(finishes, Is.EqualTo(1));
            TestContext.WriteLine(FormattableString.Invariant($"Sprint ticks={s.Tick.Value}, finish={s.Lifecycle.ElapsedFinishTime.WholeTicks}:{s.Lifecycle.ElapsedFinishTime.FractionParts}, penalties={s.TotalPenaltyLoops}"));
        }
        [Test] public void RepeatedCompleteSprintFramesAndEventsMatchAndInvalidInputDoesNotAdvance()
        {
            var la = new RaceEventLog(2000); var lb = new RaceEventLog(2000); var a = Create(la); var b = Create(lb);
            a.Step(Script(a)); b.Step(Script(b)); int count = la.Count;
            Assert.Catch(() => a.Step(Script(a, wind: 1000))); Assert.That(a.Tick.Value, Is.EqualTo(1)); Assert.That(la.Count, Is.EqualTo(count));
            while (!a.HasFinished && a.Tick.Value < 100000) Assert.That(a.Step(Script(a)), Is.EqualTo(b.Step(Script(b))));
            Assert.That(a.HasFinished, Is.True); Assert.That(la.Count, Is.EqualTo(lb.Count));
            for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
        }
        [Test] public void LaterIntervalStartPreservesIndividualRaceDurationAndFinalPhysicalState()
        {
            var a = Create(new RaceEventLog(2000)); var b = Create(new RaceEventLog(2000), 601);
            while (!a.HasFinished && a.Tick.Value < 100000) a.Step(Script(a));
            while (!b.HasFinished && b.Tick.Value < 100000) b.Step(Script(b));
            Assert.That(b.HasFinished, Is.True); Assert.That(b.Lifecycle.ElapsedFinishTime, Is.EqualTo(a.Lifecycle.ElapsedFinishTime));
            Assert.That(b.Tick.Value - a.Tick.Value, Is.EqualTo(600)); Assert.That(b.PhysiologyState, Is.EqualTo(a.PhysiologyState));
            Assert.That(b.Motion, Is.EqualTo(a.Motion)); Assert.That(b.TotalPenaltyLoops, Is.EqualTo(a.TotalPenaltyLoops));
        }
        [Test] public void AiFinishesThroughSharedSessionAndActiveSkiTicksDoNotAllocate()
        {
            var s = Create(new RaceEventLog(2000)); var model = ShootingSessionTests.Model(); ShootingAiController ai = null; ShootingSession previous = null;
            s.Step(Script(s)); var warm = Script(s); s.Step(warm);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 100; i++) s.Step(Script(s));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            while (!s.HasFinished && s.Tick.Value < 100000)
            {
                var shot = s.ActiveShooting; var tick = s.Tick.Next();
                if (shot != null && shot != previous)
                {
                    ai = new ShootingAiController(ShootingAiTests.Definition(), model.Targets, model.Aim,
                        model.Stances.Get(s.ActiveBout == 0 ? ShootingStance.Prone : ShootingStance.Standing), s.AthleteId, 20,
                        new SeededRandomSource((ulong)s.ActiveBout + 5), s.Tick); previous = shot;
                }
                if (shot == null) { s.Step(Script(s)); continue; }
                var sight = shot.Sight;
                var c = ai.Step(tick, new ShootingAiObservation(shot.Phase, shot.ClosedMask, shot.ShotsFired, sight.YawRadians, sight.PitchRadians));
                s.Step(new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, s.AthleteId, 800), 1, 1, 0), c.Aim, c.Trigger, c.HoldBreath));
            }
            Assert.That(s.HasFinished, Is.True); Assert.That(s.TotalPenaltyLoops, Is.LessThan(8));
        }
    }
}
