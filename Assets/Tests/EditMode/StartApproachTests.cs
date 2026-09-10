using System;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class StartApproachTests
    {
        public static CompetitionProfile Profile()=>CompetitionProfile.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_test_v1.json"));
        public static TrackDefinition Track()=>TrackDefinition.FromJson(PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_mass_start_v1.json"));
        public static RaceRoute Route(TrackDefinition track)=>new RaceRoute(track,RaceRoutePlan.TimeTrial("mass_start_route",track.Id,"main_long",Profile().ToRouteRequirements()),Profile().ToRouteRequirements());
        public static StartApproachMap Map(TrackDefinition track,RaceRoute route)=>new StartApproachMap(track,route,StartApproachDefinition.FromJson(PhysiologyContractTests.DefinitionJson("start_approach_test_v1.json")));
        public static SprintAthleteSession Create(RaceEventLog log,long behind=27000000000L)
        {
            var track=Track();var route=Route(track);var ski=TimeTrialTests.Athlete();var shot=ShootingSessionTests.Athlete();
            shot=ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,ski.Id));
            return new SprintAthleteSession(new SimulationConfig(1,"approach_test",20,123),Profile(),new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),
                ShootingSessionTests.Model(),ski,shot,track,route,new CheckpointLayout(track,route),SnowConditionTests.Conditions(),"ski_neutral",
                new SimulationTick(1),new ShootingLaneAssignment(track.Range.Id,"lane_1",1),log,Map(track,route),behind);
        }
        [Test] public void SameTickHandoffPreservesPhysiologyVelocityAndUncreditedApproachTravel()
        {
            var a=Create(new RaceEventLog(5000));var independent=new PhysiologySession(new SimulationConfig(1,"approach_test",20,123),
                a.PhysiologyDefinition,a.AthleteId,PhysiologyState.Rested(a.PhysiologyDefinition),new RaceEventLog(1000));
            var segments=new RaceMotionSegment[16];bool handoff=false;
            var track=Track();var baseRoute=Route(track);var map=Map(track,baseRoute);var physicalMap=new PhysicalTrackMap(track,baseRoute);
            var projection=new PhysicalMotionProjection(physicalMap,0.05,approach:map,startBehindLineNanometers:a.InitialStartApproachNanometers);
            for(int i=0;i<400;i++)
            {
                long travel=a.Motion.DistanceNanometers;long route=a.RouteNanometers;
                var input=SprintAthleteSessionTests.Script(a);var preview=a.PrepareStep(input);
                for(int j=0;j<preview.Movement.SegmentCount;j++)segments[j]=a.GetPreparedSegment(preview,j);
                for(int j=0;j<preview.Movement.SegmentCount;j++)
                {
                    var segment=segments[j];var start=segment.OnStartApproach?
                        physicalMap.MapLoop(map.LoopId,map.LengthNanometers-a.InitialStartApproachNanometers+segment.TravelBefore):physicalMap.MapRoute(segment.RouteBefore);
                    var receipt=projection.Project(segment,start);
                    Assert.That(receipt.TravelNanometers,Is.EqualTo(segment.TravelAfter-segment.TravelBefore));
                    if(segment.OnStartApproach)Assert.That(receipt.End.LoopId,Is.EqualTo(map.LoopId));
                }
                if(preview.Movement.SegmentCount>0)
                {
                    var span=segments.AsSpan(0,preview.Movement.SegmentCount);
                    var end=RaceMotionTimeline.Sample(span,new RaceMotionPoint(route,travel),0.05,0.05);
                    Assert.That(end.RouteNanometers,Is.EqualTo(preview.Movement.RouteNanometers));
                    var fractional=FractionalMotionTimeline.Sample(span,new RaceMotionPoint(route,travel),1000000000,0.05);
                    Assert.That(fractional.TravelNanometers,Is.EqualTo(end.TravelNanometers));
                    for(int j=1;j<span.Length;j++)if(span[j-1].OnStartApproach && !span[j].OnStartApproach)
                    {
                        handoff=true;Assert.That(span[j-1].RouteAfter,Is.Zero);Assert.That(span[j].RouteBefore,Is.Zero);
                        Assert.That(span[j-1].TravelAfter,Is.EqualTo(a.InitialStartApproachNanometers));
                        Assert.That(span[j-1].EndSeconds,Is.EqualTo(span[j].StartSeconds));
                        Assert.That(span[j].EndSeconds,Is.GreaterThan(span[j].StartSeconds));
                        Assert.That(preview.Movement.Motion.SpeedMetersPerSecond,Is.GreaterThan(0));
                    }
                }
                a.CommitStep(preview);
                var expected=independent.Step(new PhysiologyInput(new SimulationInput(a.Tick,a.AthleteId,a.Tick.Value>1?800:0),1,1));
                Assert.That(a.PhysiologyState,Is.EqualTo(expected.State));
                Assert.That(a.RouteNanometers,Is.EqualTo(Math.Max(0,a.Motion.DistanceNanometers-a.InitialStartApproachNanometers)));
            }
            Assert.That(handoff,Is.True);Assert.That(a.RemainingStartApproachNanometers,Is.Zero);
            Assert.That(a.StartApproachExitTime.FractionParts,Is.GreaterThan(0));Assert.That(a.Lifecycle.StartTick.Value,Is.EqualTo(1));
        }
        [Test] public void FullRearRowRaceRepeatsWithoutMissingTravelBoutsOrStartEvents()
        {
            var la=new RaceEventLog(5000);var lb=new RaceEventLog(5000);var a=Create(la);var b=Create(lb);
            while(!a.HasFinished && a.Tick.Value<100000)Assert.That(a.Step(SprintAthleteSessionTests.Script(a)),Is.EqualTo(b.Step(SprintAthleteSessionTests.Script(b))));
            Assert.That(a.HasFinished && b.HasFinished,Is.True);Assert.That(a.Lifecycle.CompletedLaps,Is.EqualTo(5));Assert.That(a.Lifecycle.CompletedBouts,Is.EqualTo(4));
            Assert.That(a.Motion.DistanceNanometers,Is.EqualTo((5027L+150L*a.TotalPenaltyLoops)*SkiState.NanometersPerMeter));
            Assert.That(a.RouteNanometers,Is.EqualTo(5000L*SkiState.NanometersPerMeter));
            Assert.That(la.Count,Is.EqualTo(lb.Count));int exits=0;
            for(int i=0;i<la.Count;i++){Assert.That(la[i],Is.EqualTo(lb[i]));if(la[i].EventType==RaceEventType.StartApproachCompleted)exits++;}
            Assert.That(exits,Is.EqualTo(1));
            TestContext.Out.WriteLine($"approach finish={a.Lifecycle.GlobalFinishTime.WholeTicks}:{a.Lifecycle.GlobalFinishTime.FractionParts}; exit={a.StartApproachExitTime.WholeTicks}:{a.StartApproachExitTime.FractionParts}; penalties={a.TotalPenaltyLoops}; travel={a.Motion.DistanceNanometers}");
        }
        [Test] public void InvalidApproachBindingsAndRearPositionsReject()
        {
            Assert.Catch(()=>Create(new RaceEventLog(100),-1));Assert.Catch(()=>Create(new RaceEventLog(100),31000000000L));
            var track=Track();var route=Route(track);var json=PhysiologyContractTests.DefinitionJson("start_approach_test_v1.json");
            Assert.Catch(()=>new StartApproachMap(track,route,StartApproachDefinition.FromJson(json.Replace("synthetic_mass_start_v1","wrong"))));
            Assert.Catch(()=>new StartApproachMap(track,route,StartApproachDefinition.FromJson(json.Replace("\"loop_id\":\"start_approach\"","\"loop_id\":\"main_long\""))));
            var map=Map(track,route);Assert.Catch(()=>map.Sample(0));Assert.Catch(()=>map.Sample(map.LengthNanometers+1));
            var front=Create(new RaceEventLog(100),0);front.Step(SprintAthleteSessionTests.Script(front));front.Step(SprintAthleteSessionTests.Script(front));
            Assert.That(front.RouteNanometers,Is.EqualTo(front.Motion.DistanceNanometers));
        }
    }
}
