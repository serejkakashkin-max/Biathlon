using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Runtime.Serialization.Json;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PhysicalRangePathAthleteTests
    {
        private const long M=SkiState.NanometersPerMeter;
        public static SprintAthleteSession Create(RaceEventLog events,out PhysicalRangePathMap path,int laneIndex=0,bool privateMuscleLoad=false,
            bool wrongLane=false,PhysicalRangePathMap foreignPath=null)
        {
            string trackJson=PhysicalRangePathDefinitionTests.Fixture("track");
            if(privateMuscleLoad)
                trackJson=Regex.Replace(trackJson,"(\"id\"\\s*:\\s*\"inbound\"[\\s\\S]*?\"terrain_muscle_factor\"\\s*:\\s*)1", "${1}2");
            var track=TrackDefinition.FromJson(trackJson);
            var junctions=PhysicalJunctionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("junctions"),track);
            var connections=PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"),junctions);
            var definition=PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"),track,junctions,connections);
            var profile=CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(CompetitionProfileTests.TestProfile().ToJson(),"distance_m","900"));
            var route=new RaceRoute(track,RaceRoutePlan.TimeTrial("range_athlete_route",track.Id,"main",profile.ToRouteRequirements()),profile.ToRouteRequirements());
            path=new PhysicalRangePathMap(new PhysicalTrackMap(track,route),track,route,definition,laneIndex);
            var ski=TimeTrialTests.Athlete();var shooting=ShootingSessionTests.Athlete();
            shooting=ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id,ski.Id));
            return new SprintAthleteSession(new SimulationConfig(1,"range_athlete_sim",20,123),profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),ski,shooting,track,route,
                new CheckpointLayout(track,route),RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed")),
                "ski_neutral",new SimulationTick(1),new ShootingLaneAssignment(track.Range.Id,wrongLane?"lane_2":path.LaneId,wrongLane?2:path.LaneOrdinal),events,physicalRangePath:foreignPath??path);
        }
        [TestCase(0)] [TestCase(1)]
        public void CompleteAthleteStopsAtEachOwnMatAndKeepsActualTravelAndPenaltyCredit(int laneIndex)
        {
            var log=new RaceEventLog(4000);var athlete=Create(log,out var path,laneIndex);int arrivals=0;bool exit=false,penalty=false;
            while(!athlete.HasFinished&&athlete.Tick.Value<30000)
            {
                var preview=athlete.PrepareStep(SprintAthleteSessionTests.Script(athlete));
                if(preview.Movement.AtFiringLine)
                {
                    Assert.That(preview.Movement.RouteNanometers,Is.EqualTo(path.FiringRouteNanometers(arrivals)));
                    Assert.That(path.MapRoute(preview.Movement.RouteNanometers).LocalNanometers,Is.EqualTo(path.MatAnchor.Distance.Millimeters*SkiState.NanometersPerMillimeter));
                    arrivals++;
                }
                athlete.CommitStep(preview);
                if(preview.Movement.AtFiringLine)Assert.That(athlete.Motion.SpeedMetersPerSecond,Is.Zero);
                exit|=athlete.CurrentRangeMotion?.Phase==RangeTraversalPhase.RangeExit;
                penalty|=athlete.CurrentRangeMotion?.Phase==RangeTraversalPhase.PenaltyLoop;
            }
            Assert.That(athlete.HasFinished,Is.True);Assert.That(arrivals,Is.EqualTo(2));Assert.That(exit&&penalty,Is.True);
            Assert.That(athlete.Lifecycle.CompletedBouts,Is.EqualTo(2));Assert.That(athlete.RouteNanometers,Is.EqualTo(900*M));
            Assert.That(athlete.Motion.DistanceNanometers,Is.EqualTo((900+150*athlete.TotalPenaltyLoops)*M));
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(athlete.PhysicalRangePathContextJson)))
            {
                var context=(string[])new DataContractJsonSerializer(typeof(string[])).ReadObject(stream);
                Assert.That(context[1],Is.EqualTo(path.ContextJson));Assert.That(context[3],Is.EqualTo(path.LaneId));
            }
            int completed=0;for(int i=0;i<log.Count;i++)if(log[i].EventType==RaceEventType.PenaltyLoopCompleted)completed++;
            Assert.That(completed,Is.EqualTo(athlete.TotalPenaltyLoops));
        }
        [Test] public void PersonalStopPreparationIsEventFreeAndRejectedTravelDoesNotStartShooting()
        {
            var log=new RaceEventLog(4000);var athlete=Create(log,out var path);SprintStepPreview preview=default;
            while(athlete.Tick.Value<3000)
            {
                preview=athlete.PrepareStep(SprintAthleteSessionTests.Script(athlete));
                if(preview.Movement.AtFiringLine)break;
                athlete.CommitStep(preview);
            }
            Assert.That(preview.Movement.AtFiringLine,Is.True);int oldEvents=log.Count;var oldTick=athlete.Tick;
            var oldMotion=athlete.Motion;var oldState=athlete.PhysiologyState;var oldRange=athlete.CurrentRangeMotion;
            var duplicate=athlete.PrepareStep(SprintAthleteSessionTests.Script(athlete));
            Assert.That(duplicate.Movement,Is.EqualTo(preview.Movement));Assert.That(log.Count,Is.EqualTo(oldEvents));
            Assert.That(athlete.Tick,Is.EqualTo(oldTick));Assert.That(athlete.Motion,Is.EqualTo(oldMotion));Assert.That(athlete.PhysiologyState,Is.EqualTo(oldState));
            Assert.That(athlete.CurrentRangeMotion,Is.EqualTo(oldRange));Assert.That(athlete.ActiveShooting,Is.Null);
            Assert.Throws<ArgumentException>(()=>athlete.CommitStep(preview));
            var stopped=athlete.ConstrainPreparedTravel(duplicate,0);athlete.CommitStep(stopped);
            Assert.That(athlete.Motion.DistanceNanometers,Is.EqualTo(oldMotion.DistanceNanometers));Assert.That(athlete.ActiveShooting,Is.Null);
            Assert.That(athlete.CurrentRangeMotion?.Phase,Is.EqualTo(RangeTraversalPhase.Approach));
            while(athlete.ActiveShooting==null&&athlete.Tick.Value<3000)athlete.Step(SprintAthleteSessionTests.Script(athlete));
            Assert.That(athlete.RouteNanometers,Is.EqualTo(path.FiringRouteNanometers(0)));Assert.That(athlete.ActiveShooting,Is.Not.Null);
        }
        [Test] public void PrivateTerrainDrivesPreparedPhysiologyAndWarmQueriesAllocateNothing()
        {
            var athlete=Create(new RaceEventLog(4000),out var path,privateMuscleLoad:true);
            while(athlete.RouteNanometers<14*M&&athlete.Tick.Value<3000)athlete.Step(SprintAthleteSessionTests.Script(athlete));
            Assert.That(path.SampleRoute(athlete.RouteNanometers).TerrainMuscleFactor,Is.EqualTo(2));
            var ski=TimeTrialTests.Athlete();var model=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var physical=model.StatCurves.Apply(model.Physiology,ski.PhysiologyStats);
            var independent=new PhysiologySession(new SimulationConfig(1,"independent",20,123),physical,athlete.AthleteId,athlete.PhysiologyState,new RaceEventLog(100));
            var expected=independent.Preview(new PhysiologyInput(new SimulationInput(new SimulationTick(1),athlete.AthleteId,800),1,2));
            var wrong=independent.Preview(new PhysiologyInput(new SimulationInput(new SimulationTick(1),athlete.AthleteId,800),1,1));
            var input=SprintAthleteSessionTests.Script(athlete);var preview=athlete.PrepareStep(input);
            Assert.That(preview.Physiology.State,Is.EqualTo(expected.State));Assert.That(preview.Physiology.State.MuscularFatigue,Is.Not.EqualTo(wrong.State.MuscularFatigue));
            for(int i=0;i<1000;i++)preview=athlete.PrepareStep(input);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)preview=athlete.PrepareStep(input);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);athlete.CommitStep(preview);
            Assert.That(athlete.PhysiologyState,Is.EqualTo(expected.State));
        }
        [Test] public void RepeatedPathRacesMatchFramesAndEvents()
        {
            var la=new RaceEventLog(4000);var lb=new RaceEventLog(4000);var a=Create(la,out _);var b=Create(lb,out _);
            while(!a.HasFinished&&a.Tick.Value<30000)Assert.That(a.Step(SprintAthleteSessionTests.Script(a)),Is.EqualTo(b.Step(SprintAthleteSessionTests.Script(b))));
            Assert.That(a.HasFinished&&b.HasFinished,Is.True);Assert.That(la.Count,Is.EqualTo(lb.Count));
            for(int i=0;i<la.Count;i++)Assert.That(la[i],Is.EqualTo(lb[i]));
        }
        [Test] public void LaneAndForeignPathMismatchesFailBeforeAnyEvent()
        {
            var log=new RaceEventLog(100);Create(new RaceEventLog(100),out var foreign);
            Assert.Throws<ArgumentException>(()=>Create(log,out _,wrongLane:true));
            Assert.Throws<ArgumentException>(()=>Create(log,out _,foreignPath:foreign));Assert.That(log.Count,Is.Zero);
        }
    }
}
