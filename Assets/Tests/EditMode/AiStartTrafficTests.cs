using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiStartTrafficTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static PackAiRaceRunner Create(int count,RaceEventLog events,IAiDecisionSink trace,bool? hold=null,bool bypass=false,bool zeroFront=false)
        {
            string VenueRead(string id)
            {
                string json=Read(id);
                // This boundary fixture explicitly places the front row at the merge.
                // The production launch line is now87.832m before it and is covered
                // by ThirtyBodiesKeepTheirAuthoredColumnsAndAllClearTheActualCurvedApproach.
                return zeroFront&&id.EndsWith("/start_approach",StringComparison.Ordinal)?System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"launch_distance_m\"\\s*:\\s*[0-9.]+",""):json;
            }
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),VenueRead);
            string Core(string name)
            {
                string json=PhysiologyContractTests.DefinitionJson(name);
                if(name=="ai_start_traffic_provisional_v1.json")
                {
                    json=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"stationary_(bypass_after_seconds|maximum_speed_mps)\"\\s*:\\s*[0-9.]+","");
                    if(hold.HasValue)json=json.Replace("true",hold.Value?"true":"false");
                    if(bypass)json=json.Replace("{","{\"stationary_bypass_after_seconds\":1,\"stationary_maximum_speed_mps\":0.15,");
                }
                return json;
            }
            return ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read("Biathlon/Venues/woodland_basin_v1/mass_start_profile")),
                RaceRoutePlan.FromJson(Read("Biathlon/Venues/woodland_basin_v1/mass_start_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),Core,events,trace,count:count);
        }
        [Test] public void StartPolicyIsVersionedRequiredAndRoundTripsBothChoices()
        {
            string json=PhysiologyContractTests.DefinitionJson("ai_start_traffic_provisional_v1.json");
            foreach(string value in new[]{json,json.Replace("true","false")})
            {
                var definition=AiStartTrafficPolicyDefinition.FromJson(value);
                Assert.That(AiStartTrafficPolicyDefinition.FromJson(definition.ToJson()).ToJson(),Is.EqualTo(definition.ToJson()));
            }
            Assert.Catch(()=>AiStartTrafficPolicyDefinition.FromJson("{}"));
            Assert.Catch(()=>AiStartTrafficPolicyDefinition.FromJson(json.Replace("\"schema_version\": 1","\"schema_version\": 2")));
            Assert.Catch(()=>AiStartTrafficPolicyDefinition.FromJson(json.Replace("\"hold_grid_lateral_on_approach\": true","\"unknown\": true")));
        }
        [Test] public void ThirtyBodiesKeepTheirAuthoredColumnsAndAllClearTheActualCurvedApproach()
        {
            var log=new RaceEventLog(20000);var trace=new AiDecisionBuffer(512);var runner=Create(30,log,trace);
            var field=runner.Field;var held=new bool[30];int holdTicks=0;
            for(int tick=0;tick<700;tick++)
            {
                for(int i=0;i<30;i++)held[i]=field.GetPhysicalPoint(i).LoopId==field.StartApproach.LoopId;
                runner.Step();MassStartFieldTests.AssertClear(field);
                for(int i=0;i<30;i++)if(held[i])
                {
                    holdTicks++;double grid=field.StartGrid.Get(i).LateralNanometers/(double)SkiState.NanometersPerMeter;
                    Assert.That(runner.GetLastLateral(i),Is.EqualTo(grid));Assert.That(field.GetLateral(i),Is.EqualTo(grid));
                    Assert.That(runner.GetLastIntent(i).Kind,Is.Not.EqualTo(TacticalIntentKind.Overtake));
                }
            }
            Assert.That(holdTicks,Is.GreaterThan(0));
            for(int i=0;i<30;i++)
            {
                Assert.That(field.GetAthlete(i).RemainingStartApproachNanometers,Is.Zero,"Athlete "+i+" must not stall before an authored knot.");
                Assert.That(field.GetAthlete(i).RouteNanometers,Is.GreaterThan(10*SkiState.NanometersPerMeter));
            }
            Assert.That(runner.ContextJson,Does.Contain("ai_shared_start_grid_lateral_until_paid_handoff"));
        }
        [Test] public void ZeroRemainingFrontRowHoldsTheIncomingEndpointUntilPhysicalHandoff()
        {
            var runner=Create(3,new RaceEventLog(10000),new AiDecisionCounts(),zeroFront:true);var field=runner.Field;
            for(int i=0;i<3;i++)
            {
                Assert.That(field.GetAthlete(i).RemainingStartApproachNanometers,Is.Zero);
                Assert.That(field.GetPhysicalPoint(i).LoopId,Is.EqualTo(field.StartApproach.LoopId));
            }
            for(int tick=0;tick<3;tick++)
            {
                var incoming=new bool[3];for(int i=0;i<3;i++)incoming[i]=field.GetPhysicalPoint(i).LoopId==field.StartApproach.LoopId;
                runner.Step();
                for(int i=0;i<3;i++)if(incoming[i])Assert.That(runner.GetLastLateral(i),Is.EqualTo(field.StartGrid.Get(i).LateralNanometers/(double)SkiState.NanometersPerMeter));
            }
            for(int i=0;i<3;i++)Assert.That(field.GetPhysicalPoint(i).LoopId,Is.Not.EqualTo(field.StartApproach.LoopId));
        }
        [Test] public void StartPolicyCommandsReplayAndRepeatedAiKeepsTheSamePhysicalAndEventState()
        {
            var log=new RaceEventLog(10000);var repeatLog=new RaceEventLog(10000);var replayLog=new RaceEventLog(10000);
            var a=Create(6,log,new AiDecisionCounts());var b=Create(6,repeatLog,new AiDecisionCounts());
            var replay=Create(6,replayLog,new AiDecisionCounts()).Field;var tape=new PackSprintRaceCommandTape("start_policy_prefix",replay,2400);
            var inputs=new SprintAthleteInput[6];var lines=new double[6];
            for(int tick=0;tick<400;tick++)
            {
                a.Step();b.Step();for(int i=0;i<6;i++){inputs[i]=a.GetLastInput(i);lines[i]=a.GetLastLateral(i);}
                tape.StepAndRecord(replay,inputs,lines);
                for(int i=0;i<6;i++)
                {
                    Assert.That(a.Field.GetAthlete(i).LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                    Assert.That(a.Field.GetAthlete(i).LastFrame,Is.EqualTo(replay.GetAthlete(i).LastFrame));
                    Assert.That(a.Field.GetLateral(i),Is.EqualTo(replay.GetLateral(i)));
                }
            }
            Assert.That(log.Count,Is.EqualTo(repeatLog.Count));for(int i=0;i<log.Count;i++)Assert.That(log[i],Is.EqualTo(repeatLog[i]));
            var decoded=PackSprintRaceCommandTape.FromJson(tape.ToJson());var target=Create(6,new RaceEventLog(10000),new AiDecisionCounts()).Field;
            var cursor=decoded.CreateReplay(target);while(cursor.HasNext)cursor.Step();
            for(int i=0;i<6;i++)Assert.That(target.GetAthlete(i).LastFrame,Is.EqualTo(a.Field.GetAthlete(i).LastFrame));
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));Assert.That(a.ContextJson,Is.Not.EqualTo(Create(6,new RaceEventLog(1000),new AiDecisionCounts(),false).ContextJson));
        }
        [Test] public void PlayerKeepsTheirOwnSteeringRequestDuringTheSharedStartPolicy()
        {
            var runner=Create(6,new RaceEventLog(10000),new AiDecisionCounts());const int player=3;double requested=-1.8;
            for(int tick=0;tick<20;tick++)
            {
                var id=runner.Field.GetAthlete(player).AthleteId;var next=runner.Field.Tick.Next();
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,id,800),1,1,0),new WeaponAimCommand(next,id,0,0));
                runner.StepWithPlayer(player,input,requested);
                Assert.That(runner.GetLastLateral(player),Is.EqualTo(requested));Assert.That(runner.GetLastInput(player),Is.EqualTo(input));
                MassStartFieldTests.AssertClear(runner.Field);
            }
            Assert.That(runner.Field.GetLateral(player),Is.GreaterThan(-2));
        }
        [TestCase(0,6)] [TestCase(1,6)] [TestCase(2,6)] [TestCase(0,30)]
        public void RearStartRowsPassStationaryPlayerThroughPaidClearSpace(int player,int fieldSize)
        {
            var runner=Create(fieldSize,new RaceEventLog(100000),new AiDecisionCounts(),bypass:true);var field=runner.Field;
            int behind=player+3;double playerLine=field.GetLateral(player);bool attempted=false;
            for(int tick=0;tick<650;tick++)
            {
                var next=field.Tick.Next();var id=field.GetAthlete(player).AthleteId;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,id,0),1,1,0),new WeaponAimCommand(next,id,0,0));
                runner.StepWithPlayer(player,input,playerLine);
                attempted|=Math.Abs(runner.GetLastLateral(behind)-field.StartGrid.Get(behind).LateralNanometers/(double)SkiState.NanometersPerMeter)>.05;
                MassStartFieldTests.AssertClear(field);
            }
            Assert.That(attempted,Is.True,"Rear row must try a paid lateral bypass of the stopped player.");
            Assert.That(field.GetAthlete(behind).RemainingStartApproachNanometers,Is.Zero,"Clear side space must let the rear athlete leave the grid.");
            Assert.That(field.GetAthlete(behind).RouteNanometers,Is.GreaterThan(5*SkiState.NanometersPerMeter));
            Assert.That(field.GetAthlete(player).Motion.DistanceNanometers,Is.Zero);
        }
        [Test] public void StationaryBypassCommandsExactlyReplayWithoutAiAndMovingGridStillHolds()
        {
            var runner=Create(6,new RaceEventLog(40000),new AiDecisionCounts(),bypass:true);
            var replay=Create(6,new RaceEventLog(40000),new AiDecisionCounts(),bypass:true).Field;
            var tape=new PackSprintRaceCommandTape("start_obstruction_paid_prefix",replay,6*650);
            var inputs=new SprintAthleteInput[6];var lines=new double[6];
            for(int t=0;t<650;t++)
            {
                var next=runner.Field.Tick.Next();var id=runner.Field.GetAthlete(0).AthleteId;
                runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,id,0),1,1,0),new WeaponAimCommand(next,id,0,0)),-2);
                for(int i=0;i<6;i++){inputs[i]=runner.GetLastInput(i);lines[i]=runner.GetLastLateral(i);}
                tape.StepAndRecord(replay,inputs,lines);
                for(int i=0;i<6;i++){Assert.That(replay.GetAthlete(i).LastFrame,Is.EqualTo(runner.Field.GetAthlete(i).LastFrame));Assert.That(replay.GetLateral(i),Is.EqualTo(runner.Field.GetLateral(i)));}
            }
            var restored=Create(6,new RaceEventLog(40000),new AiDecisionCounts(),bypass:true).Field;
            var cursor=PackSprintRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(restored);while(cursor.HasNext)cursor.Step();
            for(int i=0;i<6;i++)Assert.That(restored.GetAthlete(i).LastFrame,Is.EqualTo(runner.Field.GetAthlete(i).LastFrame));
            var moving=Create(30,new RaceEventLog(50000),new AiDecisionCounts(),bypass:true);
            for(int t=0;t<250;t++)
            {
                var incoming=new bool[30];for(int i=0;i<30;i++)incoming[i]=moving.Field.GetPhysicalPoint(i).LoopId==moving.Field.StartApproach.LoopId;
                moving.Step();MassStartFieldTests.AssertClear(moving.Field);
                for(int i=0;i<30;i++)if(incoming[i])Assert.That(moving.GetLastLateral(i),Is.EqualTo(moving.Field.StartGrid.Get(i).LateralNanometers/(double)SkiState.NanometersPerMeter));
            }
        }
        [Test,Timeout(600000)] public void EntireThirtyAthleteFieldPassesStationaryPlayerAtTheActualCurvedStart()
        {
            var runner=Create(30,new RaceEventLog(100000),new AiDecisionCounts(),bypass:true);
            var field=runner.Field;double playerLine=field.GetLateral(0);
            for(int t=0;t<1600;t++)
            {
                var next=field.Tick.Next();var id=field.GetAthlete(0).AthleteId;
                runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,id,0),1,1,0),new WeaponAimCommand(next,id,0,0)),playerLine);
                MassStartFieldTests.AssertClear(field);
            }
            for(int i=1;i<30;i++)if(field.GetAthlete(i).RemainingStartApproachNanometers>0)
                TestContext.WriteLine("start_blocked athlete="+i+" point="+field.GetPhysicalPoint(i).LocalNanometers+" line="+field.GetLateral(i)+" requested="+runner.GetLastLateral(i)+" clear_left="+field.IsStartContinuationLateralRequestClear(i,field.GetLateral(i)-.8)+" clear_right="+field.IsStartContinuationLateralRequestClear(i,field.GetLateral(i)+.8));
            for(int i=1;i<30;i++)
            {
                Assert.That(field.GetAthlete(i).RemainingStartApproachNanometers,Is.Zero,"Rear athlete "+i+" must clear the complete curved start.");
                Assert.That(field.GetAthlete(i).RouteNanometers,Is.GreaterThan(5*SkiState.NanometersPerMeter),"Rear athlete "+i+" must actually pass the stationary player.");
            }
            Assert.That(field.GetAthlete(0).Motion.DistanceNanometers,Is.Zero);
        }
        [Test] public void LegacyAiDoesNotReadOrRecordAnUnusedStartTrafficPolicy()
        {
            var log=new RaceEventLog(10000);var field=PackSprintFieldTests.Create(log,1,2);
            string Core(string name)
            {if(name=="ai_start_traffic_provisional_v1.json")throw new InvalidOperationException("Unused start policy read.");return PhysiologyContractTests.DefinitionJson(name);}
            var runner=new PackAiRaceRunner(field,Core,new[]{AiPersonalityTests.Definition("attacker")},AiDifficultyTests.Definition("expert"),123,log,new AiDecisionCounts());
            runner.Step();Assert.That(runner.ContextJson,Does.Not.Contain("ai_start_traffic"));
        }
        [Test] public void StationaryPolicyRejectsPartialAndUnboundedLimits()
        {
            string legacy="{\"schema_version\":1,\"id\":\"test_start\",\"hold_grid_lateral_on_approach\":true}";
            foreach(string limits in new[]{"\"stationary_bypass_after_seconds\":1,","\"stationary_maximum_speed_mps\":0.15,",
                "\"stationary_bypass_after_seconds\":0,\"stationary_maximum_speed_mps\":0.15,",
                "\"stationary_bypass_after_seconds\":1,\"stationary_maximum_speed_mps\":2,"})
                Assert.Catch(()=>AiStartTrafficPolicyDefinition.FromJson(legacy.Replace("{","{"+limits)));
            Assert.That(AiStartTrafficPolicyDefinition.FromJson(legacy).ToJson(),Does.Not.Contain("stationary_"));
        }
        [Test] public void OptedInPerceptionSeesTheFrontBodyAcrossThePaidSeamWithoutAllocations()
        {
            var runner=Create(6,new RaceEventLog(10000),new AiDecisionCounts(),zeroFront:true);var field=runner.Field;
            // Pay the seam with ordinary movement. Zero effort at today's production
            // launch cannot put an athlete87.832m farther along the course.
            for(int t=0;t<100&&field.GetPhysicalPoint(0).LoopId==field.StartApproach.LoopId;t++)
            {
                var next=field.Tick.Next();var id=field.GetAthlete(0).AthleteId;
                runner.StepWithPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,id,800),1,1,0),new WeaponAimCommand(next,id,0,0)),-2);
            }
            Assert.That(field.GetPhysicalPoint(0).LoopId,Is.Not.EqualTo(field.StartApproach.LoopId));
            Assert.That(field.GetPhysicalPoint(3).LoopId,Is.EqualTo(field.StartApproach.LoopId));
            var source=new PackPerceptionSource(field);var rows=new SkiPlacement[6];var radius=TrackDistance.FromMeters(50);
            bool HasPlayer(int count){for(int i=0;i<count;i++)if(rows[i].AthleteId.Equals(field.GetAthlete(0).AthleteId))return true;return false;}
            Assert.That(HasPlayer(source.CapturePlacements(3,radius,rows)),Is.False);
            Assert.That(HasPlayer(source.CapturePlacements(3,radius,rows,true)),Is.True);
            Assert.That(field.TryGetStartContinuationObservation(0,out long front),Is.True);
            Assert.That(field.TryGetStartContinuationObservation(3,out long rear),Is.True);Assert.That(front,Is.GreaterThan(rear));
            var priorFrame=field.GetAthlete(3).LastFrame;double priorLine=field.GetLateral(3);
            Assert.Throws<ArgumentOutOfRangeException>(()=>field.IsStartContinuationLateralRequestClear(-1,0));
            Assert.Catch(()=>field.IsStartContinuationLateralRequestClear(3,double.NaN));
            for(int i=0;i<200;i++)field.IsStartContinuationLateralRequestClear(3,0);
            long queryBefore=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<200;i++)field.IsStartContinuationLateralRequestClear(3,0);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-queryBefore,Is.Zero);
            Assert.That(field.GetAthlete(3).LastFrame,Is.EqualTo(priorFrame));Assert.That(field.GetLateral(3),Is.EqualTo(priorLine));
            for(int i=0;i<200;i++)source.CapturePlacements(3,radius,rows,true);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<200;i++)source.CapturePlacements(3,radius,rows,true);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}
