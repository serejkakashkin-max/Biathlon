using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintFieldTests
    {
        public static SprintFieldSession Create(RaceEventLog log, int count = 3, ulong seed = 123, string trackId = "synthetic_flat_v1")
        {
            var profile = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated(trackId);
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", profile.ToRouteRequirements()), profile.ToRouteRequirements());
            var entrants = new SprintEntrant[count]; var ski = TimeTrialTests.Athlete(); var shot = ShootingSessionTests.Athlete();
            for (int i = 0; i < count; i++)
            {
                string id = "athlete_" + i;
                entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id, id)),
                    ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id, id)), "ski_neutral");
            }
            return new SprintFieldSession(new SimulationConfig(1, "sprint_field_v1", 20, seed), profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson), ShootingSessionTests.Model(), track, route,
                SnowConditionTests.Conditions(), SprintResultsTests.Timing(), entrants, new SimulationTick(1), "sprint_result_v1", log);
        }
        public static void Fill(SprintFieldSession s, SprintAthleteInput[] inputs)
        {
            var tick = s.Tick.Next();
            for (int i = 0; i < s.Count; i++)
            {
                var athlete = s.GetAthlete(i).AthleteId;
                inputs[i] = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, athlete, 700 + i % 3 * 100), 1, 1, 0),
                    new WeaponAimCommand(tick, athlete, 0, 0), true);
            }
        }
        [Test] public void CompleteFieldUsesIntervalClocksSplitsAndOneResultPerAthlete()
        {
            var log = new RaceEventLog(4000); var s = Create(log); var input = new SprintAthleteInput[s.Count];
            Assert.That(s.GetStart(0).Tick.Value, Is.EqualTo(1)); Assert.That(s.GetStart(2).Tick.Value, Is.EqualTo(1201));
            while (!s.IsComplete && s.Tick.Value < 100000) { Fill(s, input); s.Step(input); }
            Assert.That(s.IsComplete, Is.True); Assert.That(s.FinishedCount, Is.EqualTo(3)); Assert.That(s.Result.Count, Is.EqualTo(3));
            for (int i = 0; i < s.Count; i++)
            {
                var athlete = s.GetAthlete(i);
                Assert.That(athlete.HasFinished, Is.True); Assert.That(athlete.Lifecycle.StartTick, Is.EqualTo(s.GetStart(i).Tick));
                Assert.That(s.Timing.Get(athlete.AthleteId, 8).ElapsedTime, Is.EqualTo(athlete.Lifecycle.ElapsedFinishTime));
                if (i > 0) Assert.That(s.Result.Get(i).RegisteredUnits, Is.GreaterThanOrEqualTo(s.Result.Get(i - 1).RegisteredUnits));
            }
            Assert.That(SprintResultSnapshot.FromJson(s.Result.ToJson()).ToJson(), Is.EqualTo(s.Result.ToJson()));
            int starts = 0, results = 0;
            for (int i = 0; i < log.Count; i++)
            { if (log[i].EventType == RaceEventType.ScheduledStartDue) starts++; if (log[i].EventType == RaceEventType.CompetitionResultRecorded) results++;
              if (i > 0) Assert.That(log[i].Tick.Value, Is.GreaterThanOrEqualTo(log[i - 1].Tick.Value)); }
            Assert.That(starts, Is.EqualTo(3)); Assert.That(results, Is.EqualTo(3)); Assert.Catch(() => s.Step(input));
        }
        [Test] public void InvalidLastCommandCannotAdvanceEarlierAthletesOrScheduler()
        {
            var log = new RaceEventLog(4000); var s = Create(log); var inputs = new SprintAthleteInput[s.Count]; Fill(s, inputs);
            var last = inputs[2]; inputs[2] = new SprintAthleteInput(new TimeTrialInput(last.Ski.Effort, 1, 1, 1000), last.Aim);
            Assert.Catch(() => s.Step(inputs)); Assert.That(s.Tick.Value, Is.Zero); Assert.That(log.Count, Is.Zero);
            for (int i = 0; i < s.Count; i++) Assert.That(s.GetAthlete(i).Tick.Value, Is.Zero);
            Fill(s, inputs); s.Step(inputs); Assert.That(s.Tick.Value, Is.EqualTo(1));
            Fill(s, inputs); last = inputs[2]; inputs[2] = new SprintAthleteInput(new TimeTrialInput(last.Ski.Effort, 1, 1, 0, techniqueId: "missing"), last.Aim);
            int count = log.Count; Assert.Catch(() => s.Step(inputs)); Assert.That(log.Count, Is.EqualTo(count)); Assert.That(s.GetAthlete(0).Tick.Value, Is.EqualTo(1));
        }
        [Test] public void CompleteFieldReplayKeepsResultsAndEventsAndFrozenFinishers()
        {
            var la = new RaceEventLog(4000); var lb = new RaceEventLog(4000); var a = Create(la, 2); var b = Create(lb, 2);
            var inputs = new SprintAthleteInput[2]; long frozenTick = 0; SkiState frozenMotion = default;
            while (!a.IsComplete && a.Tick.Value < 100000)
            {
                Fill(a, inputs); a.Step(inputs); b.Step(inputs);
                for (int i = 0; i < a.Count; i++) Assert.That(a.GetAthlete(i).LastFrame, Is.EqualTo(b.GetAthlete(i).LastFrame));
                if (a.GetAthlete(1).HasFinished)
                {
                    if (frozenTick == 0) { frozenTick = a.GetAthlete(1).Tick.Value; frozenMotion = a.GetAthlete(1).Motion; }
                    Assert.That(a.GetAthlete(1).Tick.Value, Is.EqualTo(frozenTick)); Assert.That(a.GetAthlete(1).Motion, Is.EqualTo(frozenMotion));
                }
            }
            Assert.That(a.IsComplete, Is.True); Assert.That(b.Result.ToJson(), Is.EqualTo(a.Result.ToJson()));
            Assert.That(la.Count, Is.EqualTo(lb.Count)); for (int i = 0; i < la.Count; i++) Assert.That(la[i], Is.EqualTo(lb[i]));
        }
    }
}
