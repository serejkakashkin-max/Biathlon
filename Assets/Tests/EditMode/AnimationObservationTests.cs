using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AnimationObservationTests
    {
        [Test]
        public void DetachedAnimationObservationsFollowCommittedRaceWithoutChangingEventsOrResult()
        {
            string Read(string name) => PhysiologyContractTests.DefinitionJson(name);
            string Fixture(string name)
            {
                string json = PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/" + name)
                    .Replace("\"curvature_per_m\": 0.04", "\"curvature_per_m\": -0.04");
                // The production greybox main loop is flat. Author test-only climb/descent
                // metadata while preserving all route lengths, range lanes and connections.
                foreach (var segment in new[] { ("generated_0000", "-0.1"), ("generated_0001", "0.1") })
                    json = System.Text.RegularExpressions.Regex.Replace(json,
                        "\\{[^{}]*\"id\": \"" + segment.Item1 + "\"[^{}]*\\}", match =>
                            PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.ReplaceNumber(match.Value,
                                "start_grade", segment.Item2), "end_grade", segment.Item2));
                return json;
            }
            var events = new RaceEventLog(30000);
            var controlEvents = new RaceEventLog(30000);
            var observed = ChampionshipRaceFactory.Create(Read, Fixture, CompetitionFormat.Sprint, events, new AiDecisionCounts(), count: 1);
            var control = ChampionshipRaceFactory.Create(Read, Fixture, CompetitionFormat.Sprint, controlEvents, new AiDecisionCounts(), count: 1);
            var initial = RacePresentationSnapshot.Capture(observed.Field);
            var athlete = observed.Field.GetAthlete(0);
            AthletePresentationState retained = default;
            bool retainedSetup = false, tuck = false, sprint = false, penalty = false, climb = false, heldBreath = false;
            bool eventTuck = false, eventSprint = false, checkedPreparedIntent = false;
            int eventCursor = 0, phases = 0, stances = 0;
            while (!observed.Field.IsComplete && observed.Field.Tick.Value < 50000)
            {
                var tick = observed.Field.Tick.Next();
                var command = new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, athlete.AthleteId, 800),
                    1, 1, 0, tuckRequested: true, sprintRequested: true),
                    new WeaponAimCommand(tick, athlete.AthleteId, 1000, 1000), tick.Value % 3 == 0, holdBreath:tick.Value % 60 < 30);
                observed.StepWithPlayer(0, command, 0);
                control.StepWithPlayer(0, command, 0);
                for (; eventCursor < events.Count; eventCursor++)
                {
                    switch (events[eventCursor].EventType)
                    {
                        case RaceEventType.TuckStarted: eventTuck = true; break;
                        case RaceEventType.TuckEnded: eventTuck = false; break;
                        case RaceEventType.FinalSprintStarted: eventSprint = true; break;
                        case RaceEventType.FinalSprintEnded: eventSprint = false; break;
                    }
                }
                var state = RacePresentationSnapshot.Capture(observed.Field).Get(0);
                Assert.That(state.TuckActive, Is.EqualTo(eventTuck));
                Assert.That(state.FinalSprintActive, Is.EqualTo(eventSprint));
                if (tick.Value == 1)
                {
                    Assert.That(state.TuckActive, Is.False, "Held input does not imply accepted tuck.");
                    Assert.That(state.FinalSprintActive, Is.False, "Held input does not imply accepted final sprint.");
                }
                tuck |= state.TuckActive;
                sprint |= state.FinalSprintActive;
                climb |= state.CourseGrade > 0;
                if (athlete.CurrentRangeMotion?.Phase == RangeTraversalPhase.PenaltyLoop)
                {
                    penalty = true;
                    Assert.That(state.CourseCurvaturePerMeter, Is.EqualTo(-0.04), "Preserve signed penalty-branch curvature.");
                }
                else
                {
                    var track = observed.Field.Route.Sample(new TrackDistance(athlete.RouteNanometers / SkiState.NanometersPerMillimeter)).Track;
                    Assert.That(state.CourseGrade, Is.EqualTo(track.Grade));
                    Assert.That(state.CourseCurvaturePerMeter, Is.EqualTo(track.CurvaturePerMeter));
                }
                var shooting = athlete.ActiveShooting;
                if (shooting != null)
                {
                    phases |= 1 << (int)state.ShootingPhase;
                    stances |= 1 << (int)state.Stance;
                    Assert.That(state.ShootingPhase, Is.EqualTo(shooting.Phase));
                    Assert.That(state.Sight, Is.EqualTo(shooting.Sight));
                    Assert.That(state.ShootingSway, Is.EqualTo(shooting.LastFrame.Sway));
                    Assert.That(state.BreathHolding, Is.EqualTo(shooting.LastFrame.BreathControl.Holding));
                    heldBreath |= state.BreathHolding;
                    Assert.That(state.BreathingPhaseCycles, Is.EqualTo(shooting.LastFrame.Breathing.PhaseCycles));
                    Assert.That(state.ShootingPhaseDurationTicks, Is.EqualTo(shooting.PhaseDurationTicks));
                    Assert.That(state.ShootingPhaseElapsedTicks + state.ShootingPhaseRemainingTicks,
                        Is.EqualTo(state.ShootingPhaseDurationTicks));
                    if (state.ShootingPhaseDurationTicks > 0)
                        Assert.That(state.ShootingPhaseRemainingTicks, Is.EqualTo(shooting.LastFrame.Shot.Lifecycle.ReadyTick - shooting.Tick.Value));
                    if (!retainedSetup && state.ShootingPhase == ShootingPhase.Setup)
                    {
                        retained = state;
                        retainedSetup = true;
                    }
                }
                else
                {
                    Assert.That(state.ShootingPhase, Is.EqualTo(ShootingPhase.Outside));
                    Assert.That(state.ShootingPhaseDurationTicks, Is.Zero);
                    Assert.That(state.ShootingPhaseRemainingTicks, Is.Zero);
                    Assert.That(state.ShootingPhaseElapsedTicks, Is.Zero);
                    Assert.That(state.BreathHolding, Is.False);
                    Assert.That(state.BreathingPhaseCycles, Is.Zero);
                }
                if (!checkedPreparedIntent && state.TuckActive && !athlete.HasFinished)
                {
                    var next = athlete.Tick.Next();
                    athlete.PrepareStep(new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next, athlete.AthleteId, 800), 1, 1, 0),
                        new WeaponAimCommand(next, athlete.AthleteId, 0, 0)));
                    Assert.That(athlete.TuckActive, Is.True, "Uncommitted intent must not replace the accepted observation.");
                    checkedPreparedIntent = true;
                }
            }
            Assert.That(observed.Field.IsComplete, Is.True);
            Assert.That(tuck, Is.True, "Observe at least one accepted tuck.");
            Assert.That(sprint, Is.True, "Observe at least one accepted final sprint.");
            Assert.That(penalty, Is.True, "Observe penalty branch.");
            Assert.That(climb, Is.True, "Observe climbing grade.");
            Assert.That(heldBreath, Is.True, "Observe an accepted breath hold.");
            Assert.That(checkedPreparedIntent, Is.True, "Check prepared intent separation.");
            foreach (var phase in new[] { ShootingPhase.Setup, ShootingPhase.Aiming, ShootingPhase.BoltCycle, ShootingPhase.Exiting })
                Assert.That(phases & (1 << (int)phase), Is.Not.Zero, "Missing range phase " + phase);
            Assert.That(stances & (1 << (int)ShootingStance.Prone), Is.Not.Zero);
            Assert.That(stances & (1 << (int)ShootingStance.Standing), Is.Not.Zero);
            Assert.That(retainedSetup, Is.True);
            Assert.That(retained.ShootingPhase, Is.EqualTo(ShootingPhase.Setup));
            Assert.That(retained.ShootingPhaseElapsedTicks, Is.Zero);
            Assert.That(retained.ShootingPhaseRemainingTicks, Is.EqualTo(retained.ShootingPhaseDurationTicks));
            Assert.That(initial.Get(0).Phase, Is.EqualTo(CompetitionPhase.Prestart));
            Assert.That(initial.Get(0).TuckActive || initial.Get(0).FinalSprintActive, Is.False);
            Assert.That(initial.Get(0).ShootingPhaseDurationTicks, Is.Zero);
            Assert.That(observed.Field.Result.ToJson(), Is.EqualTo(control.Field.Result.ToJson()));
            Assert.That(events.Count, Is.EqualTo(controlEvents.Count));
            for (int i = 0; i < events.Count; i++) Assert.That(events[i], Is.EqualTo(controlEvents[i]));
        }

        [TestCase(ShootingStance.Prone, 20)]
        [TestCase(ShootingStance.Standing, 30)]
        public void TimedShootingObservationsUseExactConfiguredDurationsAndGlobalClock(ShootingStance stance, int frequency)
        {
            var model = ShootingSessionTests.Model();
            var athlete = ShootingSessionTests.Athlete();
            var session = new ShootingSession(new SimulationConfig(1, "animation_timing", frequency, 123), model, athlete,
                stance, "range", new RaceEventLog(1000), new SimulationTick(12345));
            var stanceProfile = model.Stances.Get(stance);
            var handling = new ShootingHandlingProfile(model.Handling, model.Equipment, athlete.ShootingSpeed);
            int setup = (int)Math.Ceiling(model.Lifecycle.SetupSeconds * frequency * stanceProfile.SetupMultiplier * handling.TimeMultiplier);
            int bolt = (int)Math.Ceiling(model.Lifecycle.BoltSeconds * frequency * handling.TimeMultiplier);
            int exit = (int)Math.Ceiling(model.Lifecycle.ExitSeconds * frequency * stanceProfile.ExitMultiplier);
            int previousRemaining = 0, previousShots = 0;
            ShootingPhase previousPhase = ShootingPhase.Outside;
            while (session.Phase != ShootingPhase.Complete && session.Tick.Value < 15000)
            {
                var tick = session.Tick.Next();
                var action = session.Phase == ShootingPhase.Outside ? ShootingAction.EnterRange :
                    session.Phase == ShootingPhase.AwaitingLane ? ShootingAction.AssignLane :
                    tick.Value % 3 == 0 ? ShootingAction.Trigger : ShootingAction.None;
                session.Step(new ShootingSessionInput(new WeaponAimCommand(tick, session.AthleteId, 0, 0), action,
                    action == ShootingAction.AssignLane ? new ShootingLaneAssignment("range", "lane", 1) : default,
                    false, 150, 0.3, 0.2));
                int expected = session.Phase == ShootingPhase.Setup ? setup : session.Phase == ShootingPhase.BoltCycle ? bolt :
                    session.Phase == ShootingPhase.Exiting ? exit : 0;
                Assert.That(session.PhaseDurationTicks, Is.EqualTo(expected));
                Assert.That(session.PhaseElapsedTicks + session.PhaseRemainingTicks, Is.EqualTo(expected));
                Assert.That(session.PhaseRemainingTicks, Is.InRange(0, expected));
                if (expected > 0)
                {
                    if (previousPhase == session.Phase && previousShots == session.ShotsFired)
                        Assert.That(session.PhaseRemainingTicks, Is.EqualTo(previousRemaining - 1));
                    else Assert.That(session.PhaseRemainingTicks, Is.EqualTo(expected));
                }
                previousPhase = session.Phase;
                previousRemaining = session.PhaseRemainingTicks;
                previousShots = session.ShotsFired;
            }
            Assert.That(session.Phase, Is.EqualTo(ShootingPhase.Complete));
            Assert.That(session.PhaseDurationTicks, Is.Zero);
        }

        [Test]
        public void MassStartRearRowsSampleTheApproachInsteadOfBaseRoute()
        {
            string Read(string name) => PhysiologyContractTests.DefinitionJson(name);
            string Fixture(string name) => PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/" + name);
            var runner = ChampionshipRaceFactory.Create(Read, Fixture, CompetitionFormat.MassStart,
                new RaceEventLog(30000), new AiDecisionCounts(), count: 6);
            var snapshot = RacePresentationSnapshot.Capture(runner.Field);
            bool checkedApproach = false;
            for (int i = 0; i < runner.Field.Count; i++)
            {
                var athlete = runner.Field.GetAthlete(i);
                if (athlete.RemainingStartApproachNanometers == 0) continue;
                checkedApproach = true;
                var sample = runner.Field.StartApproach.Sample(athlete.RemainingStartApproachNanometers);
                Assert.That(snapshot.Get(i).CourseGrade, Is.EqualTo(sample.Grade));
                Assert.That(snapshot.Get(i).CourseCurvaturePerMeter, Is.EqualTo(sample.CurvaturePerMeter));
            }
            Assert.That(checkedApproach, Is.True);
        }
    }
}
