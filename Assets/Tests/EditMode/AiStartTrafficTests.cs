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
        private static PackAiRaceRunner Create(int count,RaceEventLog events,IAiDecisionSink trace,bool? hold=null)
        {
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read("Biathlon/Venues/woodland_basin_v1/manifest")),Read);
            string Core(string name)
            {
                string json=PhysiologyContractTests.DefinitionJson(name);
                return name=="ai_start_traffic_provisional_v1.json"&&hold.HasValue?json.Replace("true",hold.Value?"true":"false"):json;
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
            var runner=Create(3,new RaceEventLog(10000),new AiDecisionCounts());var field=runner.Field;
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
        [Test] public void LegacyAiDoesNotReadOrRecordAnUnusedStartTrafficPolicy()
        {
            var log=new RaceEventLog(10000);var field=PackSprintFieldTests.Create(log,1,2);
            string Core(string name)
            {if(name=="ai_start_traffic_provisional_v1.json")throw new InvalidOperationException("Unused start policy read.");return PhysiologyContractTests.DefinitionJson(name);}
            var runner=new PackAiRaceRunner(field,Core,new[]{AiPersonalityTests.Definition("attacker")},AiDifficultyTests.Definition("expert"),123,log,new AiDecisionCounts());
            runner.Step();Assert.That(runner.ContextJson,Does.Not.Contain("ai_start_traffic"));
        }
    }
}
