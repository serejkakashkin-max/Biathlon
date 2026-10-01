using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathMotionTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private static RacePathMotion Setup(out PhysicalRangePathMap lane)
        {
            PhysicalRangePathMapTests.Setup(out var track,out var route,out _,out lane,out _);
            return new RacePathMotion(new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),TimeTrialTests.Athlete(),track,route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions("packed_neutral").ToJson().Replace("snow_test_packed","packed")),"ski_neutral",20);
        }
        private static TimeTrialInput Input()=>new TimeTrialInput(new SimulationInput(new SimulationTick(1),new AthleteId(TimeTrialTests.Athlete().Id),800),1,1,0);
        [Test] public void NewPathPreviewSplitsAtDistributedForkAndTerrainBoundary()
        {
            var motion=Setup(out var lane);var segments=new RaceMotionSegment[10];
            var frame=motion.Preview(new SkiState(9900000000,10),9900000000,100*M,null,350,0,Input(),segments,physicalRangePath:lane);
            Assert.That(frame.SegmentCount,Is.EqualTo(2));Assert.That(segments[0].RouteAfter,Is.EqualTo(10*M));
            Assert.That(segments[1].StartSeconds,Is.EqualTo(segments[0].EndSeconds));
            Assert.That(lane.MapRoute(segments[0].RouteAfter,PhysicalBoundarySide.Incoming).LoopId,Is.EqualTo("main"));
            Assert.That(lane.MapRoute(segments[1].RouteBefore).LoopId,Is.EqualTo("mat_1"));
            Assert.That(frame.Motion.DistanceNanometers-9900000000,Is.EqualTo(frame.RouteNanometers-9900000000));
            var terrain=motion.Preview(new SkiState(29900000000,10),29900000000,100*M,null,350,0,Input(),segments,physicalRangePath:lane);
            Assert.That(terrain.SegmentCount,Is.EqualTo(2));Assert.That(segments[0].RouteAfter,Is.EqualTo(30*M));
        }
        [Test] public void PrivateCurvatureAffectsSharedSkiForcesInsteadOfUsingFlatMainPhysics()
        {
            var motion=Setup(out var lane);var segments=new RaceMotionSegment[10];var input=Input();var initial=new SkiState(20*M,15);
            var main=motion.Preview(initial,20*M,100*M,null,350,.2,input,segments);
            var physical=motion.Preview(initial,20*M,100*M,null,350,.2,input,segments,physicalRangePath:lane);
            Assert.That(physical.Motion.SpeedMetersPerSecond,Is.LessThan(main.Motion.SpeedMetersPerSecond));
            Assert.That(physical.RouteNanometers,Is.LessThan(main.RouteNanometers));
        }
        [Test] public void OptionalMapPreviewIsRepeatableAllocationFreeAndRejectsForeignBinding()
        {
            var motion=Setup(out var lane);var input=Input();var segments=new RaceMotionSegment[10];var initial=new SkiState(20*M,8);
            var expected=motion.Preview(initial,20*M,100*M,null,350,0,input,segments,physicalRangePath:lane);RacePathMotionFrame actual=default;
            for(int i=0;i<1000;i++)actual=motion.Preview(initial,20*M,100*M,null,350,0,input,segments,physicalRangePath:lane);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)actual=motion.Preview(initial,20*M,100*M,null,350,0,input,segments,physicalRangePath:lane);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
            Setup(out var foreign);Assert.Throws<ArgumentException>(()=>motion.Preview(initial,20*M,100*M,null,350,0,input,segments,physicalRangePath:foreign));
            var unintegratedRange=RangePenaltyTraversalTests.Create(new RaceEventLog(100));
            Assert.Throws<ArgumentException>(()=>motion.Preview(initial,20*M,100*M,unintegratedRange,350,0,input,segments,physicalRangePath:lane));
        }
    }
}
