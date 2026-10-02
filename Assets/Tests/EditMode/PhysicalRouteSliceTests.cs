using System;
using System.Globalization;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    /// <summary>Contract fixtures, not production world geometry or dense-field acceptance.</summary>
    public class PhysicalRouteSliceTests
    {
        private const long M=SkiState.NanometersPerMeter;
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        private static CompetitionProfile Profile()=>CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(
            CompetitionProfileTests.TestProfile().ToJson(),"distance_m","900"));
        private static string PlanJson(TrackDefinition track,CompetitionProfile profile)
        {
            string[] laps=new string[3];
            for(int i=0;i<laps.Length;i++)laps[i]="{\"id\":\"lap_"+(i+1)+"\",\"slices\":["+
                "{\"loop_id\":\"main\",\"start_m\":110,\"end_m\":300},"+
                "{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":110}]}";
            return "{\"schema_version\":2,\"id\":\"rotated_physical_route_v2\",\"track_id\":\""+track.Id+
                "\",\"competition_profile_id\":\""+profile.Id+"\",\"laps\":["+string.Join(",",laps)+"]}";
        }
        private static PhysicalRangeFieldLayout Layout(out TrackDefinition track,out RaceRoute route,
            out PhysicalJunctionDefinition junctions,out PhysicalConnectionDefinition connections,out PhysicalRangePathDefinition paths)
        {
            string json=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("track"),"\"checkpoints\"\\s*:\\s*\\[\\s*\\]",
                "\"checkpoints\":[{\"id\":\"hill_split\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":150}},"+
                "{\"id\":\"range_split\",\"anchor\":{\"loop_id\":\"main\",\"distance_m\":50}}]");
            track=TrackDefinition.FromJson(json);
            junctions=PhysicalJunctionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("junctions"),track);
            connections=PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"),junctions);
            paths=PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"),track,junctions,connections);
            var profile=Profile();route=new RaceRoute(track,RaceRoutePlan.FromJson(PlanJson(track,profile)),profile.ToRouteRequirements(),junctions,connections);
            return new PhysicalRangeFieldLayout(new PhysicalTrackMap(track,route),track,route,paths);
        }
        private static PackSprintFieldSession Field(RaceEventLog events)
        {
            var layout=Layout(out var track,out var route,out var junctions,out var connections,out _);
            var ski=TimeTrialTests.Athlete();var shot=ShootingSessionTests.Athlete();
            shot=ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,ski.Id));
            return new PackSprintFieldSession(new SimulationConfig(1,"physical_slice_contract_v2",20,123),Profile(),
                new SkiModelDefinitions(Read),ShootingSessionTests.Model(),track,route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed")),
                SprintResultsTests.Timing(),new[]{new SprintEntrant(ski,shot,"ski_neutral")},new SimulationTick(1),
                "physical_slice_result",OccupancyTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),junctions,connections,events,rangeLayout:layout);
        }

        [Test] public void PhysicalOffsetsRangeStopsAndCheckpointOccurrencesUseTheSameSlices()
        {
            var layout=Layout(out var track,out var route,out _,out _,out _);var path=layout.GetPath(0);var map=path.PhysicalMap;
            Assert.That(route.SchemaVersion,Is.EqualTo(2));Assert.That(route.Length.Meters,Is.EqualTo(900));
            Assert.That(route.GetSection(0).LoopStart.Meters,Is.EqualTo(110));Assert.That(route.GetSection(0).HasRange,Is.False);
            Assert.That(route.GetSection(1).HasRange,Is.True);Assert.That(route.GetSection(1).RouteDistanceOf(track.Range.Entry.Distance).Meters,Is.EqualTo(195));
            Assert.That(route.Sample(TrackDistance.FromMeters(25)).Track.Distance.Meters,Is.EqualTo(135));
            Assert.That(map.MapRoute(25*M).LocalNanometers,Is.EqualTo(135*M));
            Assert.That(map.MapRoute(190*M).LocalNanometers,Is.Zero);Assert.That(map.MapRoute(190*M,PhysicalBoundarySide.Incoming).LocalNanometers,Is.Zero);
            Assert.That(map.MapRoute(300*M).LocalNanometers,Is.EqualTo(110*M));
            Assert.That(path.FiringRouteNanometers(0),Is.EqualTo(218*M));Assert.That(path.FiringRouteNanometers(1),Is.EqualTo(518*M));
            Assert.That(path.FirstForkRouteNanometers(0),Is.EqualTo(200*M));
            Assert.That(path.MapRoute(218*M).LoopId,Is.EqualTo("mat_1"));Assert.That(path.MapRoute(218*M).LocalNanometers,Is.EqualTo(18*M));
            Assert.That(path.DistanceToNextBoundary(189*M),Is.EqualTo(M));Assert.That(path.DistanceToNextBoundary(299*M),Is.EqualTo(M));
            var traversal=new RangePenaltyTraversal(Profile(),track,route,0,new AthleteId("slice_actor"),20,new RaceEventLog(),rangePath:path);
            Assert.That(traversal.RangeEntryNanometers,Is.EqualTo(195*M));Assert.That(traversal.RangeRejoinNanometers,Is.EqualTo(290*M));
            var checkpoints=new CheckpointLayout(track,route);Assert.That(checkpoints.Count,Is.EqualTo(6));
            for(int i=0;i<3;i++)
            {Assert.That(checkpoints.Get(i*2).RouteNanometers,Is.EqualTo((i*300+40)*M));Assert.That(checkpoints.Get(i*2+1).RouteNanometers,Is.EqualTo((i*300+240)*M));}
        }

        [Test] public void ExplicitOpenLoopSeamsPreserveBothBoundarySidesAndRejectWrongEdges()
        {
            Layout(out var track,out _,out var junctions,out var connections,out _);
            var requirements=CompetitionRouteRequirements.FromJson("{\"schema_version\":1,\"id\":\"slice_seam_profile\",\"lap_count\":1,\"distance_m\":300,\"range_required_on_laps\":[]}");
            string plan="{\"schema_version\":2,\"id\":\"slice_seam_route\",\"track_id\":\""+track.Id+"\",\"competition_profile_id\":\"slice_seam_profile\",\"laps\":[{\"id\":\"lap\",\"slices\":["+
                "{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":10},"+
                "{\"loop_id\":\"mat_1\",\"start_m\":0,\"end_m\":40,\"enter_connection_id\":\"enter_1\"},"+
                "{\"loop_id\":\"exit_aisle\",\"start_m\":10,\"end_m\":60,\"enter_connection_id\":\"leave_1\"},"+
                "{\"loop_id\":\"main\",\"start_m\":100,\"end_m\":300,\"enter_connection_id\":\"return_main\"}]}]}";
            var route=new RaceRoute(track,RaceRoutePlan.FromJson(plan),requirements,junctions,connections);var map=new PhysicalTrackMap(track,route);
            Assert.That(map.MapRoute(10*M,PhysicalBoundarySide.Incoming).LoopId,Is.EqualTo("main"));
            Assert.That(map.MapRoute(10*M).LoopId,Is.EqualTo("mat_1"));
            Assert.That(map.MapRoute(50*M,PhysicalBoundarySide.Incoming).LocalNanometers,Is.EqualTo(40*M));
            Assert.That(map.MapRoute(50*M).LoopId,Is.EqualTo("exit_aisle"));Assert.That(map.MapRoute(50*M).LocalNanometers,Is.EqualTo(10*M));
            Assert.Throws<ArgumentException>(()=>new RaceRoute(track,RaceRoutePlan.FromJson(plan),requirements));
            Assert.Throws<ArgumentException>(()=>new RaceRoute(track,RaceRoutePlan.FromJson(plan.Replace("\"enter_connection_id\":\"enter_1\"","\"enter_connection_id\":\"enter_2\"")),requirements,junctions,connections));
        }

        [Test] public void SchemaBranchesBoundsMissingRangeAndSameLoopTeleportAreRejected()
        {
            Layout(out var track,out _,out var junctions,out var connections,out _);var profile=Profile();string plan=PlanJson(track,profile);
            Assert.That(RaceRoutePlan.FromJson(RaceRoutePlan.FromJson(plan).ToJson()).ToJson(),Is.EqualTo(RaceRoutePlan.FromJson(plan).ToJson()));
            Assert.Catch(()=>RaceRoutePlan.FromJson(plan.Replace("\"schema_version\":2","\"schema_version\":1")));
            Assert.Catch(()=>RaceRoutePlan.FromJson(plan.Replace("\"slices\":[","\"loop_ids\":[\"main\"],\"slices\":[")));
            Assert.Catch(()=>RaceRoutePlan.FromJson(plan.Replace("\"start_m\":110","\"start_m\":110.0001")));
            Assert.Catch(()=>RaceRoutePlan.FromJson(plan.Replace("\"end_m\":110","\"end_m\":0")));
            Assert.Catch(()=>new RaceRoute(track,RaceRoutePlan.FromJson(plan.Replace("\"end_m\":300","\"end_m\":301")),profile.ToRouteRequirements(),junctions,connections));
            string skip=plan.Replace("\"end_m\":300","\"end_m\":299").Replace("\"end_m\":110","\"end_m\":111");
            Assert.That(Assert.Catch(()=>new RaceRoute(track,RaceRoutePlan.FromJson(skip),profile.ToRouteRequirements(),junctions,connections)).Message,Does.Contain("teleport"));
            string noRange=plan.Replace("\"start_m\":110","\"start_m\":111").Replace("\"end_m\":110","\"end_m\":99");
            var req=CompetitionRouteRequirements.FromJson(PhysiologyContractTests.ReplaceNumber(profile.ToRouteRequirements().ToJson(),"distance_m","864"));
            Assert.That(Assert.Catch(()=>new RaceRoute(track,RaceRoutePlan.FromJson(noRange),req,junctions,connections)).Message,Does.Contain("no range access"));
            string noFinish=plan.Replace("\"start_m\":110","\"start_m\":100").Replace("\"end_m\":110","\"end_m\":100");
            Assert.That(Assert.Catch(()=>new RaceRoute(track,RaceRoutePlan.FromJson(noFinish),profile.ToRouteRequirements(),junctions,connections)).Message,Does.Contain("penalty rejoin"));
            var legacy=RaceRoutePlan.TimeTrial("legacy_route","track", "main",profile.ToRouteRequirements()).ToJson();
            Assert.That(legacy,Does.Not.Contain("slices"));Assert.That(legacy,Does.Contain("loop_ids"));
        }
        [Test] public void ClosedTerminalSeamUsesTheSameCanonicalPhysicalEndpointAsTheRuntimeMap()
        {
            var track=TrackDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("track"));
            string junctionJson=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("junctions"),
                "(\"id\"\\s*:\\s*\"enter_1_from\"[\\s\\S]*?\"distance_m\"\\s*:\\s*)10", "${1}0");
            var junctions=PhysicalJunctionDefinition.FromJson(junctionJson,track);var connections=PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"),junctions);
            var requirements=CompetitionRouteRequirements.FromJson("{\"schema_version\":1,\"id\":\"terminal_seam_contract\",\"lap_count\":1,\"distance_m\":590,\"range_required_on_laps\":[]}");
            var plan=RaceRoutePlan.FromJson("{\"schema_version\":2,\"id\":\"terminal_seam_route\",\"track_id\":\""+track.Id+"\",\"competition_profile_id\":\"terminal_seam_contract\",\"laps\":[{\"id\":\"lap\",\"slices\":["+
                "{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":300},"+
                "{\"loop_id\":\"mat_1\",\"start_m\":0,\"end_m\":40,\"enter_connection_id\":\"enter_1\"},"+
                "{\"loop_id\":\"exit_aisle\",\"start_m\":10,\"end_m\":60,\"enter_connection_id\":\"leave_1\"},"+
                "{\"loop_id\":\"main\",\"start_m\":100,\"end_m\":300,\"enter_connection_id\":\"return_main\"}]}]}");
            var route=new RaceRoute(track,plan,requirements,junctions,connections);var map=new PhysicalTrackMap(track,route);
            var incoming=map.MapRoute(300*M,PhysicalBoundarySide.Incoming);Assert.That(incoming.LocalNanometers,Is.Zero);
            var transition=new PhysicalConnectionMap(map,junctions,connections).Transition("enter_1",incoming,300*M);
            Assert.That(transition.Outgoing.LoopId,Is.EqualTo(map.MapRoute(300*M).LoopId));Assert.That(transition.Outgoing.LocalNanometers,Is.Zero);
        }

        [Test] public void CompletePhysicalSliceRacePersistsAndReplaysFramesEventsAndResults()
        {
            var log=new RaceEventLog(10000);var field=Field(log);var mirrorLog=new RaceEventLog(10000);var mirror=Field(mirrorLog);
            var tape=new PackSprintRaceCommandTape("physical_slice_tape",mirror,30000);var inputs=new SprintAthleteInput[1];var lines=new double[1];
            bool atMat=false,wrapped=false;
            while(!field.IsComplete&&field.Tick.Value<30000)
            {
                PackSprintFieldTests.Fill(field,inputs);field.Step(inputs,lines);tape.StepAndRecord(mirror,inputs,lines);
                Assert.That(mirror.GetAthlete(0).LastFrame,Is.EqualTo(field.GetAthlete(0).LastFrame));
                var actual=field.GetPhysicalPoint(0);var copied=mirror.GetPhysicalPoint(0);
                Assert.That(copied.LoopId,Is.EqualTo(actual.LoopId));Assert.That(copied.LocalNanometers,Is.EqualTo(actual.LocalNanometers));
                Assert.That(copied.Closed,Is.EqualTo(actual.Closed));Assert.That(copied.LoopLengthNanometers,Is.EqualTo(actual.LoopLengthNanometers));
                var p=field.GetPhysicalPoint(0);atMat|=p.LoopId=="mat_1";wrapped|=p.LoopId=="main"&&p.LocalNanometers<100*M;
            }
            Assert.That(field.IsComplete&&atMat&&wrapped,Is.True);Assert.That(mirror.Result.ToJson(),Is.EqualTo(field.Result.ToJson()));
            Assert.That(mirrorLog.Count,Is.EqualTo(log.Count));for(int i=0;i<log.Count;i++)Assert.That(mirrorLog[i],Is.EqualTo(log[i]));
            var replay=Field(new RaceEventLog(10000));var cursor=PackSprintRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(replay);
            while(cursor.HasNext)cursor.Step();Assert.That(replay.IsComplete,Is.True);Assert.That(replay.Result.ToJson(),Is.EqualTo(field.Result.ToJson()));
            Assert.That(new CheckpointLayout(field.Track,field.Route).Count,Is.EqualTo(6));
        }

        [Test] public void WarmSliceSamplesMappingsAndPathBoundariesAllocateNothing()
        {
            var layout=Layout(out _,out var route,out _,out _,out _);var path=layout.GetPath(0);var map=path.PhysicalMap;long sum=0;
            void Query(){sum+=route.Sample(TrackDistance.FromMeters(125)).Track.Distance.Millimeters;sum+=map.MapRoute(199*M).LocalNanometers;sum+=path.DistanceToNextBoundary(220*M);}
            for(int i=0;i<100;i++)Query();long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10000;i++)Query();
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);Assert.That(sum,Is.GreaterThan(0));
        }

        [Test] public void ThreeWholeMillimeterSprintLapsPayTheFinalMillimeterOnTheSamePhysicalCourse()
        {
            // Scalar distance/boundary proof only; this fixture has no production world geometry.
            string json=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("track"),
                "(\"id\"\\s*:\\s*\"return\"[\\s\\S]*?\"length_m\"\\s*:\\s*)200", "${1}3233.333");
            var track=TrackDefinition.FromJson(json);var junctions=PhysicalJunctionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("junctions"),track);
            var connections=PhysicalConnectionDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("connections"),junctions);
            var profile=CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(Profile().ToJson(),"distance_m","10000"));
            string lap="{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":3333.333}";
            string plan="{\"schema_version\":2,\"id\":\"paid_sprint_remainder\",\"track_id\":\""+track.Id+"\",\"competition_profile_id\":\""+profile.Id+
                "\",\"laps\":[{\"id\":\"lap_1\",\"slices\":["+lap+"]},{\"id\":\"lap_2\",\"slices\":["+lap+"]},{\"id\":\"lap_3\",\"slices\":["+lap+
                ",{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":0.001}]}]}";
            var route=new RaceRoute(track,RaceRoutePlan.FromJson(plan),profile.ToRouteRequirements(),junctions,connections);var map=new PhysicalTrackMap(track,route);
            Assert.That(route.Length.Millimeters,Is.EqualTo(10000000));Assert.That(route.GetLapLength(0).Millimeters,Is.EqualTo(3333333));
            Assert.That(route.GetLapLength(2).Millimeters,Is.EqualTo(3333334));Assert.That(route.GetSection(3).Length.Millimeters,Is.EqualTo(1));
            Assert.That(map.MapRoute(9999999*SkiState.NanometersPerMillimeter).LocalNanometers,Is.Zero);
            Assert.That(map.MapRoute(10000000*SkiState.NanometersPerMillimeter).LocalNanometers,Is.EqualTo(SkiState.NanometersPerMillimeter));
            Assert.That(route.Sample(route.Length).Track.Distance.Millimeters,Is.EqualTo(1));
        }
    }
}
