using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CheckpointTimingTests
    {
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
