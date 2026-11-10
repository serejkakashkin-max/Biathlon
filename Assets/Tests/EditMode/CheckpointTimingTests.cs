using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CheckpointTimingTests
    {
        [Test]
        public void AdditionalWoodlandLinesFollowEveryLapWithoutChangingTacticalReference()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var manifest=VenueManifest.FromJson(Read(root+"manifest"));
            var original=new VenueAuthoringPlan(manifest,Read);
            var updated=new VenueAuthoringPlan(manifest,Read,Read(root+"broadcast_checkpoints_v1"));
            Assert.That(original.Track.CheckpointCount,Is.EqualTo(3));
            Assert.That(updated.Track.CheckpointCount,Is.EqualTo(7));
            for(int i=0;i<updated.Formats.Count;i++)
            {
                var binding=updated.Formats.Get(i);
                var route=new RaceRoute(updated.Track,binding.Route,binding.Profile.ToRouteRequirements(),updated.Junctions,updated.Connections);
                var layout=new CheckpointLayout(updated.Track,route);
                var tracker=new CheckpointTracker(layout,new AthleteId("timing"),20,default,new RaceEventLog(64));
                long previous=0;
                int additional=0;
                for(int ordinal=0;ordinal<layout.Count;ordinal++)
                {
                    var occurrence=layout.Get(ordinal);
                    Assert.That(occurrence.RouteNanometers,Is.GreaterThan(previous));
                    int tactical=tracker.LatestTacticalOrdinal;
                    Assert.That(tracker.Observe(new SimulationTick(ordinal+1),previous,occurrence.RouteNanometers),Is.EqualTo(1));
                    if(occurrence.BroadcastOnly)additional++;
                    Assert.That(tracker.LatestTacticalOrdinal,Is.EqualTo(occurrence.BroadcastOnly?tactical:ordinal));
                    Assert.That(tracker.Get(ordinal).GlobalTime.Seconds,Is.EqualTo((ordinal+1)/20d));
                    previous=occurrence.RouteNanometers;
                }
                Assert.That(additional,Is.EqualTo(2*route.LapCount),binding.Id);
            }
        }
        [Test] public void ProductionStadiumSplitUsesRetainedGantryAfterPenaltyWithoutATimeZeroSplit()
        {
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
            TrackCheckpointDefinition stadium=null;
            for(int i=0;i<venue.Track.CheckpointCount;i++)
                if(venue.Track.GetCheckpoint(i).Id=="range_exit_split")stadium=venue.Track.GetCheckpoint(i);
            Assert.That(stadium,Is.Not.Null);
            var loop=venue.Track.GetLoop(venue.StartApproach.RouteEntryLoopId);
            Assert.That(stadium.Anchor.Distance,Is.EqualTo(loop.Length),"Use the existing lap gantry rather than another portal 25 m before it.");
            Assert.That(stadium.Anchor.Distance.Meters,Is.GreaterThan(venue.Track.Penalty.Rejoin.Distance.Meters));
            var gate=venue.Visual.Get(loop.Id).Sample(0);var split=venue.At(stadium.Anchor);
            Assert.That(split.X,Is.EqualTo(gate.X).Within(.001));Assert.That(split.Y,Is.EqualTo(gate.Y).Within(.001));
            Assert.That(split.Z,Is.EqualTo(gate.Z).Within(.001));
            for(int format=0;format<venue.Formats.Count;format++)
            {
                var binding=venue.Formats.Get(format);
                var route=new RaceRoute(venue.Track,binding.Route,binding.Profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
                var layout=new CheckpointLayout(venue.Track,route);int stadiumOccurrences=0,firstGantryOrdinal=-1;long firstGantryRoute=-1;
                for(int i=0;i<layout.Count;i++)
                {
                    if(layout.Get(i).CheckpointId!=stadium.Id)continue;
                    var occurrence=layout.Get(i);var section=route.GetSection(occurrence.SectionIndex);
                    Assert.That(section.LoopEnd,Is.EqualTo(stadium.Anchor.Distance));
                    Assert.That(occurrence.RouteNanometers,Is.GreaterThan(0),"Loop-end timing must not record an arrival at the race start.");
                    if(firstGantryRoute<0){firstGantryRoute=occurrence.RouteNanometers;firstGantryOrdinal=i;}
                    stadiumOccurrences++;
                }
                Assert.That(stadiumOccurrences,Is.GreaterThan(0),binding.Id);
                var tracker=new CheckpointTracker(layout,new AthleteId("gantry_test"),20,default,new RaceEventLog());
                long oldPortal=firstGantryRoute-(stadium.Anchor.Distance.Millimeters-935000)*SkiState.NanometersPerMillimeter;
                tracker.Observe(new SimulationTick(1),0,oldPortal);
                Assert.That(tracker.Count,Is.EqualTo(firstGantryOrdinal),binding.Id+": the removed portal must no longer trigger timing.");
                tracker.Observe(new SimulationTick(2),oldPortal,firstGantryRoute);
                Assert.That(tracker.Count,Is.EqualTo(firstGantryOrdinal+1));Assert.That(tracker.Get(firstGantryOrdinal).CheckpointId,Is.EqualTo(stadium.Id));
                Assert.That(tracker.Get(firstGantryOrdinal).ElapsedTime.Seconds,Is.EqualTo(.1));
            }
        }
        public static CheckpointLayout Layout()
        {
            var p = CompetitionProfileTests.TestProfile(); var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_test_route", track.Id, "main_long", p.ToRouteRequirements()), p.ToRouteRequirements());
            return new CheckpointLayout(track, route);
        }
        [Test] public void AuthoredOccurrencesKeepColocatedLapMarkersAndExactCrossingTimes()
        {
            var layout = Layout(); Assert.That(layout.Count, Is.EqualTo(9));
            Assert.That(layout.Get(2).CheckpointId, Is.EqualTo("finish")); Assert.That(layout.Get(3).CheckpointId, Is.EqualTo("start"));
            Assert.That(layout.Get(2).RouteNanometers, Is.EqualTo(layout.Get(3).RouteNanometers));
            Assert.That(layout.Get(2).SectionIndex, Is.Not.EqualTo(layout.Get(3).SectionIndex));
            var s = new CheckpointTracker(layout, new AthleteId("a"), 20, new SimulationTick(600), new RaceEventLog(20));
            Assert.That(s.Observe(new SimulationTick(601), 0, 1600000000000), Is.EqualTo(4));
            Assert.That(s.Get(0).GlobalTime.WholeTicks, Is.EqualTo(600)); Assert.That(s.Get(0).ElapsedTime.Seconds, Is.Zero);
            Assert.That(s.Get(1).GlobalTime.FractionParts, Is.EqualTo(500000000));
            Assert.That(s.Get(2).GlobalTime.FractionParts, Is.EqualTo(625000000));
            Assert.That(s.Get(3).GlobalTime, Is.EqualTo(s.Get(2).GlobalTime));
            Assert.That(s.Observe(new SimulationTick(602), 1600000000000, 3000000000000), Is.EqualTo(5));
            Assert.That(s.Get(8).GlobalTime.WholeTicks, Is.EqualTo(602)); Assert.That(s.Get(8).GlobalTime.FractionParts, Is.Zero);
        }
        [Test] public void PenaltyAndShootingStopsDoNotRepeatMainRouteSplitsAndInvalidProgressIsAtomic()
        {
            var log = new RaceEventLog(20); var s = new CheckpointTracker(Layout(), new AthleteId("a"), 20, default, log);
            s.Observe(new SimulationTick(1), 0, 900000000000);
            for (int tick = 2; tick <= 100; tick++) Assert.That(s.Observe(new SimulationTick(tick), 900000000000, 900000000000), Is.Zero);
            Assert.That(s.Count, Is.EqualTo(2)); Assert.That(log.Count, Is.EqualTo(2));
            Assert.Catch(() => s.Observe(new SimulationTick(101), 800000000000, 1000000000000));
            Assert.Catch(() => s.Observe(new SimulationTick(101), 900000000000, 800000000000));
            Assert.Catch(() => s.Observe(new SimulationTick(102), 900000000000, 1000000000000));
            Assert.That(s.Tick.Value, Is.EqualTo(100)); Assert.That(log.Count, Is.EqualTo(2));
            s.Observe(new SimulationTick(101), 900000000000, 1000000000000);
            Assert.That(s.Get(2).ElapsedTime.Seconds, Is.EqualTo(101 / 20.0));
        }
        private static CheckpointTracker AtFirstSplit(CheckpointLayout layout, string id, long start, long after)
        {
            var s = new CheckpointTracker(layout, new AthleteId(id), 20, new SimulationTick(start), new RaceEventLog(20));
            s.Observe(new SimulationTick(start + 1), 0, after); return s;
        }
        [Test] public void LaterFasterStarterUpdatesElapsedLeaderWhileGlobalLeaderAndTiesStayStable()
        {
            var layout = Layout(); var a = new AthleteId("a"); var z = new AthleteId("z"); var b = new AthleteId("b");
            var elapsed = new CheckpointTimingBoard(layout, new[] { z, b, a }, 20, CheckpointClock.Elapsed);
            var global = new CheckpointTimingBoard(layout, new[] { a, b, z }, 20, CheckpointClock.Global);
            var first = AtFirstSplit(layout, "z", 1, 1000000000000).Get(1);
            var later = AtFirstSplit(layout, "a", 601, 1600000000000).Get(1);
            var tie = AtFirstSplit(layout, "b", 1201, 1600000000000).Get(1);
            elapsed.Record(first); global.Record(first); Assert.That(elapsed.Leader(1).Athlete, Is.EqualTo(z));
            elapsed.Record(later); elapsed.Record(tie); global.Record(later); global.Record(tie);
            Assert.That(elapsed.Leader(1).Athlete, Is.EqualTo(a)); Assert.That(global.Leader(1).Athlete, Is.EqualTo(z));
            Assert.That(elapsed.GapSeconds(z, 1), Is.EqualTo(0.015)); Assert.That(elapsed.GapSeconds(b, 1), Is.Zero);
            Assert.That(global.GapSeconds(a, 1), Is.EqualTo(29.985));
            Assert.Catch(() => elapsed.Record(later)); Assert.Catch(() => elapsed.Record(default));
            Assert.Catch(() => elapsed.Record(AtFirstSplit(Layout(), "a", 1, 1600000000000).Get(1)));
            Assert.Catch(() => elapsed.Record(AtFirstSplit(layout, "foreign", 1, 1600000000000).Get(1)));
            var wrongClock = new CheckpointTracker(layout, a, 30, default, new RaceEventLog(10));
            wrongClock.Observe(new SimulationTick(1), 0, 800000000000);
            Assert.Catch(() => elapsed.Record(wrongClock.Get(1)));
            Assert.That(elapsed.Leader(1).Athlete, Is.EqualTo(a)); Assert.Catch(() => elapsed.Leader(8));
        }
        [Test] public void CompleteSplitEventsReplayAndTimingHotPathDoesNotAllocate()
        {
            var layout = Layout(); var athlete = new AthleteId("a"); var la = new RaceEventLog(20); var lb = new RaceEventLog(20);
            var a = new CheckpointTracker(layout, athlete, 20, default, la); var b = new CheckpointTracker(layout, athlete, 20, default, lb);
            var board = new CheckpointTimingBoard(layout, new[] { athlete }, 20, CheckpointClock.Elapsed);
            a.Observe(new SimulationTick(1), 0, 0); b.Observe(new SimulationTick(1), 0, 0); board.Record(a.Get(0));
            board.GapSeconds(athlete, 0);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int tick = 2; tick <= 10001; tick++)
            {
                long from = (tick - 2) * 300000000L, to = (tick - 1) * 300000000L;
                int oldCount = a.Count; a.Observe(new SimulationTick(tick), from, to); b.Observe(new SimulationTick(tick), from, to);
                for (int i = oldCount; i < a.Count; i++) board.Record(a.Get(i));
                board.GapSeconds(athlete, 0);
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
            Assert.That(a.Count, Is.EqualTo(9)); Assert.That(la.Count, Is.EqualTo(lb.Count));
            for (int i = 0; i < a.Count; i++) { Assert.That(a.Get(i), Is.EqualTo(b.Get(i))); Assert.That(la[i], Is.EqualTo(lb[i])); }
        }
        [TestCase(CheckpointClock.Elapsed)][TestCase(CheckpointClock.Global)]
        public void ThirtyAthleteLeaderMatchesFullScanAfterEveryArrival(CheckpointClock clock)
        {
            var layout=Layout();var ids=new AthleteId[30];var splits=new CheckpointSplit[30];
            for(int i=0;i<30;i++)ids[i]=new AthleteId("timing_"+i.ToString("D2"));
            var board=new CheckpointTimingBoard(layout,ids,20,clock);
            for(int arrival=0;arrival<30;arrival++)
            {
                int i=(arrival*17)%30;
                splits[i]=AtFirstSplit(layout,ids[i].Value,(i%5)*600,1000000000000+(i%3)*200000000000L).Get(1);
                board.Record(splits[i]);CheckpointSplit best=default;
                foreach(var candidate in splits)
                {
                    if(!candidate.IsValid)continue;
                    var time=clock==CheckpointClock.Elapsed?candidate.ElapsedTime:candidate.GlobalTime;
                    var bestTime=clock==CheckpointClock.Elapsed?best.ElapsedTime:best.GlobalTime;
                    int order=best.IsValid?time.CompareTo(bestTime):-1;
                    if(order<0||order==0&&string.CompareOrdinal(candidate.Athlete.Value,best.Athlete.Value)<0)best=candidate;
                }
                Assert.That(board.Leader(1),Is.EqualTo(best));
            }
        }
        [Test, Explicit("Timing-query CPU diagnostic; no portable timing threshold.")]
        public void ThirtyAthleteRepeatedTimingQueryCpuProfile()
        {
            var layout=Layout();var ids=new AthleteId[30];
            for(int i=0;i<30;i++)ids[i]=new AthleteId("timing_"+i.ToString("D2"));
            var board=new CheckpointTimingBoard(layout,ids,20,CheckpointClock.Elapsed);
            for(int i=0;i<30;i++)board.Record(AtFirstSplit(layout,ids[i].Value,i*600,1000000000000+(i%3)*200000000000L).Get(1));
            for(int i=0;i<3000;i++)board.GapSeconds(ids[i%30],1);
            var timer=new System.Diagnostics.Stopwatch();long before=GC.GetAllocatedBytesForCurrentThread();
            double sum=0;timer.Start();
            for(int i=0;i<300000;i++)sum+=board.GapSeconds(ids[i%30],1);
            timer.Stop();long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            TestContext.WriteLine($"queries=300000 roster=30 wall_ms={timer.Elapsed.TotalMilliseconds:R} allocated_bytes={bytes} gap_checksum={sum:R}");
            Assert.That(bytes,Is.Zero);Assert.That(sum,Is.GreaterThan(0));
        }
    }
}
