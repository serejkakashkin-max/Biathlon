using System;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AuthoredStartApproachTests
    {
        private const long M=SkiState.NanometersPerMeter;
        public static PhysicalRangePackTests.Fixture Fixture(double curvature=0.08)
        {
            var f=PhysicalRangePackTests.CreateFixture();
            string source=VenuePathAuthoringTests.Loop("authored_start",false,6,10,10,10,10)
                .Replace("\"curvature_per_m\":0,","\"curvature_per_m\":"+curvature.ToString(System.Globalization.CultureInfo.InvariantCulture)+",");
            string trackJson=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("track"),"(\"loops\"\\s*:\\s*\\[)",match=>match.Value+source+",");
            f.Track=TrackDefinition.FromJson(trackJson);var requirements=f.Profile.ToRouteRequirements();
            f.Route=new RaceRoute(f.Track,RaceRoutePlan.TimeTrial("pack_path_route",f.Track.Id,"main",requirements),requirements);
            f.Map=new PhysicalTrackMap(f.Track,f.Route);
            string region="{\"id\":\"authored_start_join\",\"ports\":["+
                VenuePathAuthoringTests.Port("authored_start_end","authored_start",40,1,0)+","+
                VenuePathAuthoringTests.Port("authored_start_main","main",0,1,1)+"]}";
            string junctionJson=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("junctions"),"(\"junctions\"\\s*:\\s*\\[)",match=>match.Value+region+",");
            f.Junctions=PhysicalJunctionDefinition.FromJson(junctionJson,f.Track);
            string connection="{\"id\":\"authored_start_to_main\",\"junction_id\":\"authored_start_join\",\"from_port_id\":\"authored_start_end\",\"to_port_id\":\"authored_start_main\",\"kind\":\"coincident_seam\"}";
            string connectionJson=Regex.Replace(PhysicalRangePathDefinitionTests.Fixture("connections"),"(\"connections\"\\s*:\\s*\\[)",match=>match.Value+connection+",");
            f.Connections=PhysicalConnectionDefinition.FromJson(connectionJson,f.Junctions);
            var paths=PhysicalRangePathDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("paths"),f.Track,f.Junctions,f.Connections);
            f.Layout=new PhysicalRangeFieldLayout(f.Map,f.Track,f.Route,paths);
            f.Approach=new StartApproachMap(f.Track,f.Route,StartApproachDefinition.FromJson(
                "{\"schema_version\":1,\"id\":\"authored_start_v1\",\"track_id\":\""+f.Track.Id+"\",\"loop_id\":\"authored_start\",\"route_entry_loop_id\":\"main\"}"));
            return f;
        }
        public static PackSprintFieldSession Create(PhysicalRangePackTests.Fixture f,RaceEventLog log,bool authored=true,bool withLayout=true)
            =>new PackSprintFieldSession(new SimulationConfig(1,"pack_path_sim",20,123),f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),f.Track,f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed")),
                SprintResultsTests.Timing(),f.Entrants,new SimulationTick(1),"pack_path_result",OccupancyTests.Definition(),
                DraftingTests.Definition(),OvertakeTests.Definition(),f.Junctions,f.Connections,log,
                massGrid:f.Grid,startApproach:f.Approach,rangeLayout:withLayout?f.Layout:null,authoredStartApproach:authored);
        private static void Fill(PackSprintFieldSession field,SprintAthleteInput[] commands)
        {
            var tick=field.Tick.Next();for(int i=0;i<field.Count;i++)
            {
                var id=field.GetAthlete(i).AthleteId;
                commands[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,800),1,1,0),new WeaponAimCommand(tick,id,0,0),true);
            }
        }
        private static void EqualPoint(PhysicalTrackPoint a,PhysicalTrackPoint b)
        {Assert.That(a.LoopId,Is.EqualTo(b.LoopId));Assert.That(a.LocalNanometers,Is.EqualTo(b.LocalNanometers));Assert.That(a.Closed,Is.EqualTo(b.Closed));}

        [Test] public void ExplicitAuthoredModeMapsGridOntoItsActualOpenFortyMetreSource()
        {
            var f=Fixture();var map=new PhysicalStartApproachMap(f.Map,f.Track,f.Route,f.Approach,true,f.Junctions,f.Connections);
            Assert.That(map.UsesAuthoredBranch,Is.True);Assert.That(map.ContextJson,Does.Contain("authored_open_start_branch_v1"));
            Assert.That(map.MapRemaining(3*M).LoopId,Is.EqualTo("authored_start"));
            Assert.That(map.MapRemaining(3*M).LocalNanometers,Is.EqualTo(37*M));Assert.That(map.MapRemaining(3*M).Closed,Is.False);
            Assert.That(map.MapRemaining(40*M).LocalNanometers,Is.Zero);Assert.That(map.MapRemaining(0).LoopId,Is.EqualTo("main"));
            Assert.That(map.MapRemaining(0).LocalNanometers,Is.Zero);
            Assert.That(f.Approach.Sample(3*M).CurvaturePerMeter,Is.EqualTo(0.08));
            var field=Create(f,new RaceEventLog(1000));Assert.That(field.UsesAuthoredStartApproach,Is.True);
            EqualPoint(field.GetPhysicalPoint(1),map.MapRemaining(3*M));
            Assert.That(field.GetAthlete(1).Motion.DistanceNanometers,Is.Zero);Assert.That(field.GetAthlete(1).RouteNanometers,Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(()=>map.MapRemaining(-1));Assert.Throws<ArgumentOutOfRangeException>(()=>map.MapRemaining(40*M+1));
        }

        [Test] public void ActualApproachReceiptsKeepIncomingEndpointThenUseExplicitZeroTravelTransition()
        {
            var f=Fixture();var physical=new PhysicalStartApproachMap(f.Map,f.Track,f.Route,f.Approach,true,f.Junctions,f.Connections);
            var projection=new PhysicalMotionProjection(f.Map,0.05,approach:f.Approach,startBehindLineNanometers:3*M,startOverlay:physical,rangePath:f.Layout.GetPath(1));
            // Pure standalone athlete preparation supplies the same timed simulation receipt.
            var pack=Create(f,new RaceEventLog(3000));var athlete=pack.GetAthlete(1);
            var commands=new SprintAthleteInput[2];var lines=new double[2];bool handoff=false;
            while(!handoff&&pack.Tick.Value<3000)
            {
                Fill(pack,commands);var preview=athlete.PrepareStep(commands[1]);
                for(int s=0;s<preview.Movement.SegmentCount;s++)
                {
                    var segment=athlete.GetPreparedSegment(preview,s);if(!segment.OnStartApproach)continue;
                    var start=physical.MapRemaining(athlete.InitialStartApproachNanometers-segment.TravelBefore);
                    var receipt=projection.Project(segment,start);
                    Assert.That(receipt.End.LoopId,Is.EqualTo("authored_start"));Assert.That(receipt.TravelNanometers,Is.EqualTo(segment.TravelAfter-segment.TravelBefore));
                    if(segment.TravelAfter==athlete.InitialStartApproachNanometers)
                    {
                        handoff=true;Assert.That(receipt.End.LocalNanometers,Is.EqualTo(40*M));
                        var seam=new PhysicalConnectionMap(f.Map,f.Junctions,f.Connections).Transition("authored_start_to_main",receipt.End,segment.TravelAfter);
                        Assert.That(seam.Outgoing.LoopId,Is.EqualTo("main"));Assert.That(seam.Outgoing.LocalNanometers,Is.Zero);
                        Assert.That(seam.TravelNanometers,Is.EqualTo(3*M));
                    }
                }
                // Pack prepares and validates its own previews; this read-only preparation consumes no distance.
                pack.Step(commands,lines);
            }
            Assert.That(handoff,Is.True);Assert.That(athlete.RouteNanometers,Is.EqualTo(athlete.Motion.DistanceNanometers-athlete.InitialStartApproachNanometers));
        }

        [Test] public void StartCurvatureAffectsSharedRacePhysicsAndRealPositionWithoutRouteCredit()
        {
            var curved=Create(Fixture(0.8),new RaceEventLog(1000));var flat=Create(Fixture(0),new RaceEventLog(1000));
            var commands=new SprintAthleteInput[2];var lines=new double[2];bool different=false;
            while(curved.GetAthlete(1).RemainingStartApproachNanometers>0&&flat.GetAthlete(1).RemainingStartApproachNanometers>0&&curved.Tick.Value<3000)
            {
                Fill(curved,commands);curved.Step(commands,lines);flat.Step(commands,lines);
                var actor=curved.GetAthlete(1);different|=actor.Motion.DistanceNanometers!=flat.GetAthlete(1).Motion.DistanceNanometers;
                if(actor.RemainingStartApproachNanometers>0)
                {
                    Assert.That(curved.GetPhysicalPoint(1).LoopId,Is.EqualTo("authored_start"));
                    Assert.That(curved.GetPhysicalPoint(1).LocalNanometers,Is.EqualTo(37*M+actor.Motion.DistanceNanometers));
                    Assert.That(actor.RouteNanometers,Is.Zero);
                }
            }
            Assert.That(different,Is.True,"The physical approach must use its own authored curvature.");
        }

        [Test] public void AcceptedHandoffRetainsIncomingLateralUntilTheBodyTailClears()
        {
            var f=Fixture();var log=new RaceEventLog(5000);var field=Create(f,log);var commands=new SprintAthleteInput[2];var lines=new double[2];
            var rear=field.GetAthlete(1);bool handedOff=false;long halfBody=(long)Math.Ceiling(OccupancyTests.Definition().LongitudinalSeparation*M/2);double incoming=0;
            while(!handedOff&&field.Tick.Value<3000)
            {
                incoming=field.GetLateral(1);Fill(field,commands);field.Step(commands,lines);
                handedOff=rear.RemainingStartApproachNanometers==0;
                Assert.That(rear.RouteNanometers,Is.EqualTo(Math.Max(0,rear.Motion.DistanceNanometers-rear.InitialStartApproachNanometers)));
                MassStartFieldTests.AssertClear(field);
            }
            Assert.That(handedOff,Is.True);Assert.That(rear.RouteNanometers,Is.LessThan(halfBody));
            Assert.That(field.GetLateral(1),Is.EqualTo(incoming));
            int locked=0;
            while(rear.RouteNanometers<=halfBody&&field.Tick.Value<3000)
            {
                lines[1]=0.5;Fill(field,commands);field.Step(commands,lines);locked++;
                Assert.That(field.GetLateral(1),Is.EqualTo(incoming),"Incoming footprint must retain its seam lateral coordinate.");
            }
            Assert.That(locked,Is.GreaterThan(0));
            Fill(field,commands);field.Step(commands,lines);Assert.That(field.GetLateral(1),Is.GreaterThan(incoming));
            int seams=0;for(int i=0;i<log.Count;i++)if(log[i].EventType==RaceEventType.PhysicalSeamCrossed&&log[i].AthleteId.Equals(rear.AthleteId))seams++;
            Assert.That(seams,Is.EqualTo(1),"Authored start handoff publishes one physical seam without free distance.");
        }

        [Test] public void CompleteAuthoredStartRacePersistsAndReplaysAllTravelFramesAndEvents()
        {
            var sourceLog=new RaceEventLog(30000);var source=Create(Fixture(),sourceLog);var tape=new PackSprintRaceCommandTape("authored_start_replay",source,2);
            var commands=new SprintAthleteInput[2];var lines=new double[2];
            while(!source.IsComplete&&source.Tick.Value<80000){Fill(source,commands);tape.StepAndRecord(source,commands,lines);}
            Assert.That(source.IsComplete,Is.True);
            var restored=PackSprintRaceCommandTape.FromJson(tape.ToJson());var replayLog=new RaceEventLog(30000);var replayField=Create(Fixture(),replayLog);var replay=restored.CreateReplay(replayField);
            var direct=Create(Fixture(),new RaceEventLog(30000));
            while(replay.HasNext)
            {
                Fill(direct,commands);direct.Step(commands,lines);replay.Step();
                for(int i=0;i<2;i++){Assert.That(replayField.GetAthlete(i).LastFrame,Is.EqualTo(direct.GetAthlete(i).LastFrame));EqualPoint(replayField.GetPhysicalPoint(i),direct.GetPhysicalPoint(i));}
            }
            Assert.That(replayField.MassStartResult.ToJson(),Is.EqualTo(source.MassStartResult.ToJson()));Assert.That(replayLog.Count,Is.EqualTo(sourceLog.Count));
            for(int i=0;i<sourceLog.Count;i++)Assert.That(replayLog[i],Is.EqualTo(sourceLog[i]));
            var rear=source.GetAthlete(1);Assert.That(rear.Motion.DistanceNanometers,Is.EqualTo((1500L+150L*rear.TotalPenaltyLoops)*M+3*M));
            Assert.That(rear.Lifecycle.CompletedBouts,Is.EqualTo(4));Assert.That(rear.Lifecycle.CompletedLaps,Is.EqualTo(5));
        }

        [Test] public void MissingSeamForeignBindingAndAbsentExplicitLayoutRejectBeforeAnyFieldEvent()
        {
            var f=Fixture();string connections=f.Connections.ToJson();
            connections=Regex.Replace(connections,"\\{[^{}]*\"id\":\"authored_start_to_main\"[^{}]*\\},?","");
            f.Connections=PhysicalConnectionDefinition.FromJson(connections,f.Junctions);
            var log=new RaceEventLog(1000);Assert.Catch(()=>Create(f,log));Assert.That(log.Count,Is.Zero);
            f=Fixture();Assert.Catch(()=>Create(f,log,withLayout:false));Assert.That(log.Count,Is.Zero);
            var foreign=TrackDefinition.FromJson(f.Track.ToJson());
            Assert.Catch(()=>new PhysicalStartApproachMap(f.Map,foreign,f.Route,f.Approach,true,f.Junctions,f.Connections));
            Assert.Catch(()=>Create(f,log,authored:false));
        }

        [Test] public void DefaultOverlayContextAndH2FixturePhysicalMappingRemainUnchanged()
        {
            var f=PhysicalRangePackTests.CreateFixture();var field=PhysicalRangePackTests.CreatePack(f,new RaceEventLog(1000));
            var legacy=new PhysicalStartApproachMap(f.Map,f.Track,f.Route,f.Approach);
            Assert.That(legacy.UsesAuthoredBranch,Is.False);Assert.That(field.UsesAuthoredStartApproach,Is.False);
            Assert.That(legacy.ContextJson,Is.EqualTo(new PhysicalStartApproachMap(f.Map,f.Track,f.Route,f.Approach,false).ContextJson));
            Assert.That(legacy.ContextJson,Does.Contain("pre_start_main_loop_overlay_v1"));
            Assert.That(legacy.MapRemaining(3*M).LoopId,Is.EqualTo("main"));Assert.That(legacy.MapRemaining(3*M).LocalNanometers,Is.EqualTo(297*M));
            Assert.That(field.GetPhysicalPoint(1).LoopId,Is.EqualTo("main"));Assert.That(field.GetPhysicalPoint(1).LocalNanometers,Is.EqualTo(297*M));
            Assert.That(field.ContextJson,Does.Not.Contain("authored_open_start_branch_v1"));
        }

        [Test] public void WarmActualApproachPackStepsAllocateNoManagedMemory()
        {
            var field=Create(Fixture(),new RaceEventLog(1000));var commands=new SprintAthleteInput[2];var lines=new double[2];Fill(field,commands);field.Step(commands,lines);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<10;i++){Fill(field,commands);field.Step(commands,lines);}
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(allocated,Is.Zero);Assert.That(field.GetPhysicalPoint(1).LoopId,Is.EqualTo("authored_start"));
        }
    }
}
