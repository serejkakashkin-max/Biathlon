using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathProjectionTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private static RaceMotionSegment Segment(long before,long after,long travelBefore=0,double start=0,double end=.05)
            =>(RaceMotionSegment)Activator.CreateInstance(typeof(RaceMotionSegment),BindingFlags.Instance|BindingFlags.NonPublic,
                null,new object[]{before,after,travelBefore,travelBefore+after-before,start,end,false},null);

        [TestCase(0)] [TestCase(1)]
        public void FullPreparedRaceReceiptsPreservePhysicalLoopsAndEveryNanometer(int laneIndex)
        {
            var athlete=PhysicalRangePathAthleteTests.Create(new RaceEventLog(4000),out var path,laneIndex);
            var map=path.PhysicalMap;var projection=new PhysicalMotionProjection(map,.05,rangePath:path);long total=0;bool penalty=false,sharedExit=false,mat=false;
            while(!athlete.HasFinished&&athlete.Tick.Value<30000)
            {
                var preview=athlete.PrepareStep(SprintAthleteSessionTests.Script(athlete));
                long penaltyAt=athlete.CurrentRangeMotion?.PenaltyNanometers??0;
                for(int i=0;i<preview.Movement.SegmentCount;i++)
                {
                    var segment=athlete.GetPreparedSegment(preview,i);
                    var origin=segment.OnPenaltyLoop ? map.MapPenalty(penaltyAt) : path.MapRoute(segment.RouteBefore);
                    var receipt=projection.Project(segment,origin);
                    Assert.That(receipt.TravelNanometers,Is.EqualTo(segment.TravelAfter-segment.TravelBefore));
                    if(receipt.EndFraction>receipt.StartFraction)
                    {
                        long mid=receipt.StartFraction+(receipt.EndFraction-receipt.StartFraction)/2;
                        var a=projection.Slice(receipt,athlete.AthleteId,receipt.StartFraction,mid,0,0);
                        var b=projection.Slice(receipt,athlete.AthleteId,mid,receipt.EndFraction,0,0);
                        Assert.That(a.TravelNanometers+b.TravelNanometers,Is.EqualTo(receipt.TravelNanometers));
                    }
                    total+=receipt.TravelNanometers;
                    if(segment.OnPenaltyLoop){penalty=true;penaltyAt=(penaltyAt+receipt.TravelNanometers)%origin.LoopLengthNanometers;}
                    sharedExit|=origin.LoopId=="exit_aisle";mat|=origin.LoopId==path.MatAnchor.LoopId;
                }
                athlete.CommitStep(preview);
            }
            Assert.That(athlete.HasFinished,Is.True);Assert.That(penalty&&sharedExit&&mat,Is.True);
            Assert.That(total,Is.EqualTo(athlete.Motion.DistanceNanometers));
        }
        [Test] public void ReceiptsCannotHidePrivateSeamsOrAnEntireRangeOverlay()
        {
            PhysicalRangePathMapTests.Setup(out _,out _,out var map,out var path,out _);
            var projection=new PhysicalMotionProjection(map,.05,rangePath:path);
            Assert.Throws<ArgumentException>(()=>projection.Project(Segment(9*M,11*M),path.MapRoute(9*M)));
            Assert.Throws<ArgumentException>(()=>projection.Project(Segment(49*M,51*M),path.MapRoute(49*M)));
            Assert.Throws<ArgumentException>(()=>projection.Project(Segment(0,110*M),map.MapRoute(0)));
            var inbound=projection.Project(Segment(9*M,10*M),path.MapRoute(9*M));
            var outbound=projection.Project(Segment(10*M,11*M),path.MapRoute(10*M));
            Assert.That(inbound.End.LoopId,Is.EqualTo("main"));Assert.That(outbound.Start.LoopId,Is.EqualTo("mat_1"));
            Assert.That(outbound.Start.LocalNanometers,Is.Zero);
        }
        [Test] public void StationarySeamReceiptKeepsOutgoingIdentityAndSharedExitOccupancy()
        {
            PhysicalRangePathMapTests.Setup(out _,out _,out var map,out var a,out var b);
            var pa=new PhysicalMotionProjection(map,.05,rangePath:a);var pb=new PhysicalMotionProjection(map,.05,rangePath:b);
            var zero=pa.Project(Segment(10*M,10*M),a.MapRoute(10*M));
            Assert.That(zero.Start,Is.EqualTo(zero.End));Assert.That(zero.End.LoopId,Is.EqualTo("mat_1"));
            var one=pa.Project(Segment(80*M,81*M),a.MapRoute(80*M));var two=pb.Project(Segment(80*M,81*M),b.MapRoute(80*M));
            Assert.That(one.Start,Is.EqualTo(two.Start));
            var query=new PhysicalOccupancySystem(map,OccupancyTests.Definition()).QueryRelativeMove(
                pa.Slice(one,new AthleteId("a"),0,1000000000,0,0),pb.Slice(two,new AthleteId("b"),0,1000000000,0,0));
            Assert.That(query.Blocked,Is.True);
        }
        [Test] public void ForeignProjectionBindingsAndReceiptOwnersAreRejected()
        {
            PhysicalRangePathMapTests.Setup(out _,out _,out var map,out var path,out _);
            PhysicalRangePathMapTests.Setup(out _,out _,out _,out var foreign,out _);
            Assert.Throws<ArgumentException>(()=>new PhysicalMotionProjection(map,.05,rangePath:foreign));
            var projection=new PhysicalMotionProjection(map,.05,rangePath:path);var other=new PhysicalMotionProjection(map,.05,rangePath:path);
            var receipt=projection.Project(Segment(20*M,21*M),path.MapRoute(20*M));
            Assert.Throws<ArgumentException>(()=>other.Slice(receipt,new AthleteId("a"),0,1000000000,0,0));
            Assert.Throws<ArgumentException>(()=>projection.Project(Segment(20*M,21*M),map.MapRoute(20*M)));
        }
        [Test] public void WarmProjectionAndFractionSlicesAllocateNothing()
        {
            PhysicalRangePathMapTests.Setup(out _,out _,out var map,out var path,out _);
            var projection=new PhysicalMotionProjection(map,.05,rangePath:path);var segment=Segment(20*M,21*M);var origin=path.MapRoute(20*M);
            var athlete=new AthleteId("a");PhysicalMotionReceipt receipt=default;PhysicalOccupancySweep sweep=default;
            for(int i=0;i<1000;i++){receipt=projection.Project(segment,origin);sweep=projection.Slice(receipt,athlete,0,500000000,0,0);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){receipt=projection.Project(segment,origin);sweep=projection.Slice(receipt,athlete,0,500000000,0,0);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(sweep.TravelNanometers,Is.EqualTo(M/2));
        }
    }
}
