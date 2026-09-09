using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class SprintPerceptionTests
    {
        private static readonly TrackDistance Radius = TrackDistance.FromMeters(10000);
        private static readonly TrackDistance[] Offsets = { default, TrackDistance.FromMeters(200), TrackDistance.FromMeters(10000) };
        [Test] public void PrestartCaptureIsReadOnlyAndRejectsInvalidQueriesBeforeWriting()
        {
            var log = new RaceEventLog(4000); var field = SprintFieldTests.Create(log);
            var source = new SprintPerceptionSource(field); var ahead = new AiRouteLookahead[3]; var neighbors = new AiRouteNeighbor[2];
            var state = source.Capture(0, Radius, Offsets, ahead, neighbors);
            Assert.That(state.Tick.Value, Is.Zero); Assert.That(state.Phase, Is.EqualTo(CompetitionPhase.Prestart));
            Assert.That(state.RemainingBaseRouteNanometers, Is.EqualTo(3000L * SkiState.NanometersPerMeter));
            Assert.That(state.NextRangeEntryRouteDistanceNanometers, Is.EqualTo(820L * SkiState.NanometersPerMeter));
            Assert.That(state.NeighborCount, Is.Zero); Assert.That(state.PhysicalDraftAvailable, Is.False);
            Assert.That(state.ElapsedGapToCheckpointLeaderSeconds, Is.Null); Assert.That(state.ObservedCheckpointPlace, Is.Null);
            Assert.That(ahead[2].Sample.RouteDistance.Millimeters, Is.EqualTo(3000000));
            var saved = ahead[0]; Assert.Catch(() => source.Capture(0, Radius, new[] { Offsets[1], Offsets[0] }, ahead, neighbors));
            Assert.That(ahead[0], Is.EqualTo(saved)); Assert.Catch(() => source.Capture(0, Radius, Offsets, new AiRouteLookahead[1], neighbors));
            Assert.That(log.Count, Is.Zero); Assert.That(field.Tick.Value, Is.Zero);
        }
        [Test] public void FullRacePerceptionMatchesAuthorityAndDoesNotChangeReplay()
        {
            var log = new RaceEventLog(4000); var otherLog = new RaceEventLog(4000);
            var field = SprintFieldTests.Create(log, 2); var replay = SprintFieldTests.Create(otherLog, 2);
            var source = new SprintPerceptionSource(field); var ahead = new AiRouteLookahead[3]; var neighbors = new AiRouteNeighbor[1];
            var inputs = new SprintAthleteInput[2]; bool shooting = false, penalty = false, neighborsSeen = false;
            var initial = source.Capture(0, Radius, Offsets, ahead, neighbors);
            while (!field.IsComplete && field.Tick.Value < 20000)
            {
                SprintFieldTests.Fill(field, inputs); field.Step(inputs); replay.Step(inputs);
                for (int i = 0; i < 2; i++)
                {
                    var state = source.Capture(i, Radius, Offsets, ahead, neighbors); var athlete = field.GetAthlete(i);
                    Assert.That(state.Tick, Is.EqualTo(field.Tick)); Assert.That(state.Physiology, Is.EqualTo(athlete.PhysiologyState));
                    Assert.That(state.RouteNanometers, Is.EqualTo(athlete.RouteNanometers));
                    Assert.That(state.RemainingPenaltyLoops, Is.EqualTo(athlete.Lifecycle.PendingPenaltyLoops));
                    if (state.NeighborCount > 0) { neighborsSeen = true; Assert.That(neighbors[0].RouteGapNanometers,
                        Is.EqualTo(field.GetAthlete(1 - i).RouteNanometers - athlete.RouteNanometers)); }
                    if (state.LatestCheckpointOrdinal >= 0) Assert.That(state.ElapsedGapToCheckpointLeaderSeconds,
                        Is.EqualTo(field.Timing.GapSeconds(athlete.AthleteId, state.LatestCheckpointOrdinal)));
                    if (state.ShootingPhase.HasValue) { shooting = true; Assert.That(state.NextRangeEntryRouteDistanceNanometers, Is.Zero); }
                    if (athlete.CurrentRangeMotion?.Phase == RangeTraversalPhase.PenaltyLoop)
                    { penalty = true; Assert.That(state.CurrentTrack.LoopId, Is.EqualTo(field.Track.Penalty.LoopId)); }
                }
            }
            Assert.That(field.IsComplete, Is.True); Assert.That(shooting, Is.True, "shooting observed"); Assert.That(penalty, Is.True, "penalty observed"); Assert.That(neighborsSeen, Is.True, "neighbors observed");
            Assert.That(initial.Tick.Value, Is.Zero); Assert.That(initial.Phase, Is.EqualTo(CompetitionPhase.Prestart));
            var finish = source.Capture(0, Radius, Offsets, ahead, neighbors);
            Assert.That(finish.RemainingBaseRouteNanometers, Is.Zero); Assert.That(finish.NextRangeEntryRouteDistanceNanometers, Is.Null);
            Assert.That(field.Result.ToJson(), Is.EqualTo(replay.Result.ToJson())); Assert.That(log.Count, Is.EqualTo(otherLog.Count));
            for (int i = 0; i < log.Count; i++) Assert.That(log[i], Is.EqualTo(otherLog[i]));
        }
        [Test] public void ActiveCaptureUsesCallerStorageWithoutAllocations()
        {
            var field = SprintFieldTests.Create(new RaceEventLog(4000)); var inputs = new SprintAthleteInput[3];
            while (field.Tick.Value < 1500) { SprintFieldTests.Fill(field, inputs); field.Step(inputs); }
            var source = new SprintPerceptionSource(field); var ahead = new AiRouteLookahead[3]; var neighbors = new AiRouteNeighbor[2];
            source.Capture(0, Radius, Offsets, ahead, neighbors); long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++) source.Capture(i % 3, Radius, Offsets, ahead, neighbors);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}


