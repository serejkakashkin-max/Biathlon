using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class MassStartFieldTests
    {
        public static PackAiRaceRunner Create(int count,RaceEventLog events,IAiDecisionSink sink,ulong seed=123,string difficulty="expert",string prefix="synthetic_mass_stadium",bool orderedLanes=false)
        {
            string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
            string Fixture(string name)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+name);
            var profile=StartApproachTests.Profile();var track=TrackDefinition.FromJson(Fixture(prefix+"_v1.json"));
            var requirements=profile.ToRouteRequirements();var route=new RaceRoute(track,RaceRoutePlan.TimeTrial("mass_start_route",track.Id,"main_long",requirements),requirements);
            var ski=TimeTrialTests.Athlete();var shot=ShootingSessionTests.Athlete();var ids=MassStartGridTests.Roster(count);
            var entrants=new SprintEntrant[count];var personalities=new AiPersonalityDefinition[count];
            for(int i=0;i<count;i++)
            {
                entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,ids[i].Value)),ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,ids[i].Value)),"ski_neutral");
                personalities[i]=AiPersonalityTests.Definition(i%2==0?"attacker":"patient");
            }
            var junctions=PhysicalJunctionDefinition.FromJson(Fixture(prefix+"_junctions_v1.json"),track);
            var connections=PhysicalConnectionDefinition.FromJson(Fixture(prefix+"_connections_v1.json"),junctions);
            var lanes=PhysicalRangeLaneDefinition.FromJson(Fixture(prefix+"_lanes_v1.json"),track);
            var body=OccupancyTests.Definition();var grid=new MassStartGrid(profile,MassStartGridTests.Definition(),body,ids,6,30);
            var approach=new StartApproachMap(track,route,StartApproachDefinition.FromJson(Read("start_approach_test_v1.json").Replace("synthetic_mass_start_v1",track.Id)));
            var field=new PackSprintFieldSession(new SimulationConfig(1,"mass_start_field_v1",20,seed),profile,new SkiModelDefinitions(Read),new ShootingModelDefinitions(Read),track,route,
                SnowConditionTests.Conditions(),SprintResultsTests.Timing(),entrants,new SimulationTick(1),"mass_start_result_v1",body,DraftingTests.Definition(),OvertakeTests.Definition(),
                junctions,connections,events,lanes,massGrid:grid,startApproach:approach,rangePolicy:orderedLanes?MassStartRangePolicyDefinition.FromJson(Read("mass_start_range_policy_v1.json")):null);
            return new PackAiRaceRunner(field,Read,personalities,AiDifficultyTests.Definition(difficulty),seed,events,sink);
        }
        public static void AssertClear(PackSprintFieldSession field)
        {
            var body=OccupancyTests.Definition();
            for(int i=0;i<field.Count;i++)if(field.IsOnCourse(i))for(int j=0;j<i;j++)if(field.IsOnCourse(j))
            {
                var a=field.GetPhysicalPoint(i);var b=field.GetPhysicalPoint(j);if(a.LoopId!=b.LoopId)continue;
                long delta=Math.Abs(a.LocalNanometers-b.LocalNanometers);if(a.Closed)delta=Math.Min(delta,a.LoopLengthNanometers-delta);
                Assert.That(delta/1e9>=body.LongitudinalSeparation || Math.Abs(field.GetLateral(i)-field.GetLateral(j))>=body.LateralSeparation,Is.True,"Body overlap at tick "+field.Tick.Value);
            }
        }
        [Test] public void ThirtyPhysiologicalGridBodiesCrossLineWithOneClockAndRealTravel()
        {
            var logs=new RaceEventLog(10000);var repeat=new RaceEventLog(10000);
            var a=Create(30,logs,new AiDecisionCounts()).Field;var b=Create(30,repeat,new AiDecisionCounts()).Field;
            var input=new SprintAthleteInput[30];var lanes=new double[30];
            for(int i=0;i<30;i++)
            {
                lanes[i]=a.GetLateral(i);Assert.That(a.IsOnCourse(i),Is.True);
                Assert.That(a.GetAthlete(i).Motion.DistanceNanometers,Is.Zero);
                Assert.That(a.GetPhysicalPoint(i).LocalNanometers,Is.EqualTo(i<3?0:1000000000000L+a.StartGrid.Get(i).LongitudinalNanometers));
            }
            AssertClear(a);
            for(int tick=1;tick<=1200;tick++)
            {
                for(int i=0;i<30;i++)input[i]=SprintAthleteSessionTests.Script(a.GetAthlete(i));
                a.Step(input,lanes);b.Step(input,lanes);AssertClear(a);
                for(int i=0;i<30;i++)
                {
                    var actor=a.GetAthlete(i);Assert.That(actor.LastFrame,Is.EqualTo(b.GetAthlete(i).LastFrame));
                    Assert.That(actor.Lifecycle.StartTick.Value,Is.EqualTo(1));
                    Assert.That(actor.RouteNanometers,Is.EqualTo(Math.Max(0,actor.Motion.DistanceNanometers-actor.InitialStartApproachNanometers)));
                }
            }
            long minimum=long.MaxValue;
            for(int i=0;i<30;i++){Assert.That(a.GetAthlete(i).RemainingStartApproachNanometers,Is.Zero);minimum=Math.Min(minimum,a.GetAthlete(i).RouteNanometers);}
            Assert.That(minimum,Is.GreaterThan(100L*SkiState.NanometersPerMeter));Assert.That(a.PrecisionFallbackTicks,Is.Zero);
            Assert.That(logs.Count,Is.EqualTo(repeat.Count));int starts=0,exits=0;
            for(int i=0;i<logs.Count;i++){Assert.That(logs[i],Is.EqualTo(repeat[i]));if(logs[i].EventType==RaceEventType.ScheduledStartDue)starts++;if(logs[i].EventType==RaceEventType.StartApproachCompleted)exits++;}
            Assert.That(starts,Is.EqualTo(30));Assert.That(exits,Is.EqualTo(27));
            TestContext.Out.WriteLine("Mass launch1200ticks; minimum_route_nm="+minimum+"; events="+logs.Count);
        }
        [Test] public void PhysicalOverlayRejectsDifferentTerrainAndForeignSnapshots()
        {
            var track=StartApproachTests.Track();var route=StartApproachTests.Route(track);var approach=StartApproachTests.Map(track,route);
            // The older approach copied the first (flat) segment; the terminal stadium metadata differs.
            Assert.Catch(()=>new PhysicalStartApproachMap(new PhysicalTrackMap(track,route),track,route,approach));
            var valid=Create(3,new RaceEventLog(100),new AiDecisionCounts()).Field;
            Assert.That(valid.ContextJson,Does.Contain("pre_start_main_loop_overlay_v1"));
        }
        [Test] public void SixAiAthletesFinishPpssWithExactGridBoundResults()
        {
            var log=new RaceEventLog(100000);var a=Create(6,log,new AiDecisionCounts());
            while(!a.Field.IsComplete && a.Field.Tick.Value<40000){a.Step();AssertClear(a.Field);}
            Assert.That(a.Field.IsComplete,Is.True);var result=a.Field.MassStartResult;
            Assert.That(result.Count,Is.EqualTo(6));Assert.That(a.Field.Result,Is.Null);Assert.That(a.Field.PursuitResult,Is.Null);
            Assert.That(MassStartResultSnapshot.FromJson(result.ToJson(),a.Field.StartGrid,20,new SimulationTick(1)).ToJson(),Is.EqualTo(result.ToJson()));
            for(int i=0;i<6;i++)
            {
                var actor=a.Field.GetAthlete(i);Assert.That(actor.Lifecycle.CompletedLaps,Is.EqualTo(5));Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(4));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((5000L+150L*actor.TotalPenaltyLoops)*SkiState.NanometersPerMeter+actor.InitialStartApproachNanometers));
                Assert.That(actor.ScheduledStart.Value,Is.EqualTo(1));
            }
            TestContext.Out.WriteLine("Mass six AI ticks="+a.Field.Tick.Value+"; result="+result.ToJson());
        }
    }
}
