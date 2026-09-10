using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PursuitLifecycleTests
    {
        public static SprintAthleteSession Create(RaceEventLog events, long start = 1)
        {
            var profile = PursuitQualificationTests.Profile();
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = profile.ToRouteRequirements();
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("pursuit_test_route", track.Id, "main_long", requirements), requirements);
            var ski = TimeTrialTests.Athlete(); var shooting = ShootingSessionTests.Athlete();
            shooting = ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id, ski.Id));
            return new SprintAthleteSession(new SimulationConfig(1, "pursuit_test_sim", 20, 123), profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), ski, shooting,
                track, route, new CheckpointLayout(track, route), SnowConditionTests.Conditions(), "ski_neutral",
                new SimulationTick(start), new ShootingLaneAssignment(track.Range.Id, "lane_1", 1), events);
        }

        [Test] public void FourBoutsFiveLapsAndPenaltiesRepeatThroughTheSharedPhysicalSession()
        {
            var la = new RaceEventLog(4000); var lb = new RaceEventLog(4000);
            var a = Create(la); var b = Create(lb); var shots = new int[4]; var visited = new bool[4];
            while (!a.HasFinished && a.Tick.Value < 100000)
            {
                int bout = a.ActiveBout;
                var frame = a.Step(SprintAthleteSessionTests.Script(a));
                Assert.That(frame, Is.EqualTo(b.Step(SprintAthleteSessionTests.Script(b))));
                if (frame.ShootingActive)
                {
                    visited[bout] = true;
                    if (frame.Shooting.Shot.Lifecycle.ShotAccepted) shots[bout]++;
                    Assert.That(a.Motion.SpeedMetersPerSecond, Is.Zero);
                    Assert.That(a.Lifecycle.CompletedLaps, Is.EqualTo(bout));
                }
            }
            Assert.That(a.HasFinished, Is.True); Assert.That(a.Lifecycle.CompletedBouts, Is.EqualTo(4));
            Assert.That(a.Lifecycle.CompletedLaps, Is.EqualTo(5)); Assert.That(shots, Is.EqualTo(new[] { 5, 5, 5, 5 }));
            Assert.That(visited, Is.EqualTo(new[] { true, true, true, true }));
            Assert.That(a.TotalPenaltyLoops, Is.GreaterThan(0));
            Assert.That(a.Motion.DistanceNanometers, Is.EqualTo((5000L + a.TotalPenaltyLoops * 150L) * 1000000000L));
            Assert.That(a.RouteNanometers, Is.EqualTo(5000000000000L));
            Assert.That(la.Count, Is.EqualTo(lb.Count));
            int penalties = 0, finishes = 0;
            for (int i = 0; i < la.Count; i++)
            {
                Assert.That(la[i], Is.EqualTo(lb[i]));
                if (la[i].EventType == RaceEventType.PenaltyLoopCompleted) penalties++;
                if (la[i].EventType == RaceEventType.RaceFinished) finishes++;
            }
            Assert.That(penalties, Is.EqualTo(a.TotalPenaltyLoops)); Assert.That(finishes, Is.EqualTo(1));
            var p = PursuitQualificationTests.Profile();
            Assert.That(new[] { p.GetBout(0).Stance, p.GetBout(1).Stance, p.GetBout(2).Stance, p.GetBout(3).Stance },
                Is.EqualTo(new[] { ShootingStance.Prone, ShootingStance.Prone, ShootingStance.Standing, ShootingStance.Standing }));
            TestContext.WriteLine($"Pursuit ticks={a.Tick.Value}; penalties={a.TotalPenaltyLoops}; finish={a.Lifecycle.ElapsedFinishTime.WholeTicks}:{a.Lifecycle.ElapsedFinishTime.FractionParts}");
        }

        [Test] public void GapStartDoesNotChangeSoloPhysicalPerformanceOrSkipObligations()
        {
            var a = Create(new RaceEventLog(4000)); var b = Create(new RaceEventLog(4000), 3);
            while (!a.HasFinished && a.Tick.Value < 100000) a.Step(SprintAthleteSessionTests.Script(a));
            while (!b.HasFinished && b.Tick.Value < 100000) b.Step(SprintAthleteSessionTests.Script(b));
            Assert.That(a.HasFinished && b.HasFinished, Is.True);
            Assert.That(b.Tick.Value - a.Tick.Value, Is.EqualTo(2));
            Assert.That(b.Lifecycle.ElapsedFinishTime, Is.EqualTo(a.Lifecycle.ElapsedFinishTime));
            Assert.That(b.PhysiologyState, Is.EqualTo(a.PhysiologyState)); Assert.That(b.Motion, Is.EqualTo(a.Motion));
            Assert.That(b.TotalPenaltyLoops, Is.EqualTo(a.TotalPenaltyLoops));
        }
    }
}
