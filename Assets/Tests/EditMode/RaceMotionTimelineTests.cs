using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class RaceMotionTimelineTests
    {
        private static SprintAthleteInput Input(SprintAthleteSession s,int effort=800)
        { var tick=s.Tick.Next(); return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,s.AthleteId,effort),1,1,0),new WeaponAimCommand(tick,s.AthleteId,0,0),true); }
        private static RaceMotionSegment[] Segments(SprintAthleteSession s,SprintStepPreview p)
        { var result=new RaceMotionSegment[p.Movement.SegmentCount]; for(int i=0;i<result.Length;i++) result[i]=s.GetPreparedSegment(p,i); return result; }
        private static RaceMotionSegment Receipt(double start, double end) => (RaceMotionSegment)Activator.CreateInstance(
            typeof(RaceMotionSegment), System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic,
            null, new object[] { 0L, 10L, 0L, 10L, start, end, false }, null);

        [Test] public void CollapsedMovementGapSuffixAndDistinctRosterBoundariesAreRejected()
        {
            var tiny = new[] { Receipt(0, 1e-12) }; var output = new long[] { 42, 42, 42, 42, 42, 42 }; var scratch = new double[6];
            Assert.Throws<InvalidOperationException>(() => FractionalMotionTimeline.MergeBoundaries(tiny, 0.05, scratch, output));
            Assert.That(output, Is.All.EqualTo(42));
            Assert.Throws<InvalidOperationException>(() => FractionalMotionTimeline.Sample(tiny, default, 0, 0.05));
            Assert.Throws<InvalidOperationException>(() => FractionalMotionTimeline.Sample(new[] { Receipt(1e-12, 0.05) }, default, 0, 0.05));
            Assert.Throws<InvalidOperationException>(() => FractionalMotionTimeline.Sample(new[] { Receipt(0, 0.05 - 1e-12) }, default, 0, 0.05));
            var roster = new[] { Receipt(0, 1e-10), Receipt(0, 1.01e-10) };
            Assert.Throws<InvalidOperationException>(() => FractionalMotionTimeline.MergeBoundaries(roster, 0.05, scratch, output));
            Assert.That(output, Is.All.EqualTo(42));
        }

        [Test] public void FractionalGridPreservesEveryEndpointThroughoutRealSprint()
        {
            var s = SprintFieldTests.Create(new RaceEventLog(4000), 1).GetAthlete(0);
            var track = SyntheticTrackTests.Generated("synthetic_flat_v1");
            var requirements = CompetitionProfileTests.TestProfile().ToRouteRequirements();
            var map = new PhysicalTrackMap(track, new RaceRoute(track, RaceRoutePlan.TimeTrial("projection", track.Id, "main_long", requirements), requirements));
            var projection = new PhysicalMotionProjection(map, 0.05);
            bool penalty = false, stopped = false;
            while (!s.HasFinished && s.Tick.Value < 30000)
            {
                var preview = s.PrepareStep(Input(s)); var segments = Segments(s, preview);
                var initial = new RaceMotionPoint(s.RouteNanometers, s.Motion.DistanceNanometers);
                var scratch = new double[2 + segments.Length * 2]; var fractions = new long[scratch.Length];
                int count = FractionalMotionTimeline.MergeBoundaries(segments, 0.05, scratch, fractions);
                Assert.That(fractions[0], Is.Zero); Assert.That(fractions[count - 1], Is.EqualTo(JunctionAdmissionSession.FractionParts));
                for (int i = 1; i < count; i++) Assert.That(fractions[i], Is.GreaterThan(fractions[i - 1]));
                long penaltyLocal = s.CurrentRangeMotion.HasValue && s.CurrentRangeMotion.Value.Phase == RangeTraversalPhase.PenaltyLoop
                    ? s.CurrentRangeMotion.Value.PenaltyNanometers : 0;
                foreach (var segment in segments)
                {
                    var projected = projection.Project(segment, segment.OnPenaltyLoop ? map.MapPenalty(penaltyLocal) : map.MapRoute(segment.RouteBefore));
                    if (segment.OnPenaltyLoop) penaltyLocal = projected.End.LocalNanometers;
                    long sum = 0;
                    for (int i = 1; i < count; i++)
                        if (fractions[i - 1] >= projected.StartFraction && fractions[i] <= projected.EndFraction)
                            sum += projection.Slice(projected, s.AthleteId, fractions[i - 1], fractions[i], 0, 0).TravelNanometers;
                    Assert.That(sum, Is.EqualTo(segment.TravelAfter - segment.TravelBefore));
                    long end = FractionalMotionTimeline.Quantize(segment.EndSeconds, 0.05);
                    Assert.That(FractionalMotionTimeline.Sample(segments, initial, end, 0.05),
                        Is.EqualTo(new RaceMotionPoint(segment.RouteAfter, segment.TravelAfter)));
                    penalty |= segment.OnPenaltyLoop;
                }
                Assert.That(FractionalMotionTimeline.Sample(segments, initial, 1000000000, 0.05),
                    Is.EqualTo(RaceMotionTimeline.Sample(segments, initial, 0.05, 0.05)));
                stopped |= segments.Length > 0 && preview.Movement.RemainingSeconds > 0;
                s.CommitStep(preview);
            }
            Assert.That(s.HasFinished && penalty && stopped, Is.True);
        }

        [Test] public void FractionalQuantizationAndInvalidOutputAreExplicitAndAllocationFree()
        {
            Assert.That(FractionalMotionTimeline.Quantize(0, 0.05), Is.Zero);
            Assert.That(FractionalMotionTimeline.Quantize(0.05, 0.05), Is.EqualTo(1000000000));
            Assert.That(FractionalMotionTimeline.Quantize(0.025, 0.05), Is.EqualTo(500000000));
            Assert.That(FractionalMotionTimeline.Quantize(0.5, 1000000000), Is.Zero);
            Assert.That(FractionalMotionTimeline.Quantize(1.5, 1000000000), Is.EqualTo(2));
            Assert.Catch(() => FractionalMotionTimeline.Quantize(double.NaN, 0.05));
            Assert.Catch(() => FractionalMotionTimeline.Quantize(0, double.PositiveInfinity));
            var output = new long[] { 42, 42, 42, 42 }; var scratch = new double[4];
            Assert.Catch(() => FractionalMotionTimeline.MergeBoundaries(new RaceMotionSegment[1], 0.05, scratch, output));
            Assert.That(output, Is.All.EqualTo(42));
            var s = SprintFieldTests.Create(new RaceEventLog(4000), 1).GetAthlete(0);
            for (int i = 0; i < 100; i++) s.Step(Input(s));
            var preview = s.PrepareStep(Input(s)); var segments = Segments(s, preview);
            scratch = new double[2 + segments.Length * 2]; output = new long[scratch.Length];
            var initial = new RaceMotionPoint(s.RouteNanometers, s.Motion.DistanceNanometers);
            FractionalMotionTimeline.MergeBoundaries(segments, 0.05, scratch, output);
            FractionalMotionTimeline.Sample(segments, initial, 500000000, 0.05);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
            { FractionalMotionTimeline.MergeBoundaries(segments, 0.05, scratch, output); FractionalMotionTimeline.Sample(segments, initial, 500000000, 0.05); }
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
        [Test] public void WholeSprintSamplesEveryAuthoredEndpointIncludingPenaltyAndStoppedSuffix()
        {
            var s=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0); bool penalty=false,stopped=false;
            while(!s.HasFinished && s.Tick.Value<30000)
            {
                var p=s.PrepareStep(Input(s)); var segments=Segments(s,p); var initial=new RaceMotionPoint(s.RouteNanometers,s.Motion.DistanceNanometers);
                var grid=new double[2+segments.Length*2]; int count=RaceMotionTimeline.MergeBoundaries(segments,0.05,grid);
                Assert.That(grid[0],Is.Zero); Assert.That(grid[count-1],Is.EqualTo(0.05));
                for(int i=1;i<count;i++) Assert.That(grid[i],Is.GreaterThan(grid[i-1]));
                Assert.That(RaceMotionTimeline.Sample(segments,initial,0,0.05),Is.EqualTo(initial));
                foreach(var segment in segments)
                {
                    Assert.That(RaceMotionTimeline.Sample(segments,initial,segment.EndSeconds,0.05),Is.EqualTo(new RaceMotionPoint(segment.RouteAfter,segment.TravelAfter)));
                    var middle=RaceMotionTimeline.Sample(segments,initial,(segment.StartSeconds+segment.EndSeconds)*0.5,0.05);
                    Assert.That(middle.TravelNanometers,Is.InRange(segment.TravelBefore,segment.TravelAfter));
                    if(segment.OnPenaltyLoop){penalty=true; Assert.That(middle.RouteNanometers,Is.EqualTo(segment.RouteBefore));}
                }
                var final=RaceMotionTimeline.Sample(segments,initial,0.05,0.05);
                if(p.MovementActive)
                {
                    Assert.That(final.RouteNanometers,Is.EqualTo(p.Movement.RouteNanometers));
                    Assert.That(final.TravelNanometers,Is.EqualTo(p.Movement.Motion.DistanceNanometers));
                    stopped|=p.Movement.SegmentCount>0 && p.Movement.RemainingSeconds>0;
                }
                else Assert.That(final,Is.EqualTo(initial));
                s.CommitStep(p);
            }
            Assert.That(s.HasFinished && penalty && stopped,Is.True);
        }
        [Test] public void MergedRosterGridAndInvalidChainsUseCallerStorageWithoutMutation()
        {
            var a=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0);
            var b=SprintFieldTests.Create(new RaceEventLog(4000),1).GetAthlete(0);
            for(int i=0;i<100;i++){a.Step(Input(a));b.Step(Input(b,700));}
            var pa=a.PrepareStep(Input(a));var pb=b.PrepareStep(Input(b,700));var sa=Segments(a,pa);var sb=Segments(b,pb);
            var all=new RaceMotionSegment[sa.Length+sb.Length];sa.CopyTo(all,0);sb.CopyTo(all,sa.Length);
            var grid=new double[2+all.Length*2]; int count=RaceMotionTimeline.MergeBoundaries(all,0.05,grid);
            foreach(var segment in all) Assert.That(Array.IndexOf(grid,segment.EndSeconds,0,count),Is.GreaterThanOrEqualTo(0));
            var origin=new RaceMotionPoint(a.RouteNanometers,a.Motion.DistanceNanometers);
            Assert.Catch(()=>RaceMotionTimeline.Sample(all,origin,0,0.05));
            grid[0]=123;Assert.Catch(()=>RaceMotionTimeline.MergeBoundaries(new RaceMotionSegment[1],0.05,grid));Assert.That(grid[0],Is.EqualTo(123));
            Assert.Catch(()=>RaceMotionTimeline.Sample(sa,origin,double.NaN,0.05));
            Assert.Catch(()=>RaceMotionTimeline.MergeBoundaries(all,0.05,new double[1]));
            RaceMotionTimeline.MergeBoundaries(all,0.05,grid); RaceMotionTimeline.Sample(sa,origin,0.025,0.05);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){RaceMotionTimeline.MergeBoundaries(all,0.05,grid);RaceMotionTimeline.Sample(sa,origin,0.025,0.05);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}
