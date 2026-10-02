using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiRangeTrafficTests
    {
        private static AiRangeTrafficPolicyDefinition Definition(bool enabled=true)=>AiRangeTrafficPolicyDefinition.FromJson(
            PhysiologyContractTests.DefinitionJson("ai_range_traffic_provisional_v1.json").Replace("true",enabled?"true":"false"));
        private static SkiPlacement Placement(string id,double position,double line)=>new SkiPlacement(new AthleteId(id),"main",
            new SkiState((long)Math.Round(position*SkiState.NanometersPerMeter),5),line);
        private static TrackSample Track()=>new TrackSampler(TrackDefinition.FromJson(PhysicalRangePathDefinitionTests.Fixture("track")).GetLoop("main")).Sample(default);
        private static PackAiRaceRunner Create(PhysicalRangePackTests.Fixture fixture,RaceEventLog log,bool enabled=true)
        {
            var profiles=new AiPersonalityDefinition[fixture.Entrants.Length];
            for(int i=0;i<profiles.Length;i++)profiles[i]=AiPersonalityTests.Definition(i%2==0?"attacker":"patient");
            return new PackAiRaceRunner(PhysicalRangePackTests.CreatePack(fixture,log),PhysiologyContractTests.DefinitionJson,
                profiles,AiDifficultyTests.Definition("expert"),123,log,new AiDecisionCounts(),rangeTrafficPolicy:Definition(enabled));
        }
        [Test] public void PolicyRequiresVersionIdentityEnableChoiceAndObservedPositiveLead()
        {
            string json=PhysiologyContractTests.DefinitionJson("ai_range_traffic_provisional_v1.json");
            foreach(string value in new[]{json,json.Replace("true","false")})
            {var d=AiRangeTrafficPolicyDefinition.FromJson(value);Assert.That(AiRangeTrafficPolicyDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));}
            Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson("{}"));
            Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson(json.Replace("\"schema_version\": 1","\"schema_version\": 2")));
            Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson(json.Replace("\"align_physical_range_paths\": true","\"unknown\": true")));
            Assert.Catch(()=>AiRangeTrafficPolicyDefinition.FromJson(json.Replace("\"alignment_lead_m\": 30","\"alignment_lead_m\": 0")));
            var field=PackSprintFieldTests.Create(new RaceEventLog(10000),1,2);
            Assert.Catch(()=>new PackAiRaceRunner(field,PhysiologyContractTests.DefinitionJson,new[]{AiPersonalityTests.Definition("attacker")},
                AiDifficultyTests.Definition("expert"),123,new RaceEventLog(10000),new AiDecisionCounts(),rangeTrafficPolicy:Definition()));
        }
        [Test] public void ApproachingWindowUsesExactLeadAndExitRemainsActiveAfterNextBoutAdvances()
        {
            var planner=new AiRangeTrafficPlanner(Definition(),OccupancyTests.Definition());var self=Placement("self",0,-2);var roster=new[]{self};var track=Track();
            Assert.That(planner.Evaluate(true,30*SkiState.NanometersPerMeter+1,RangeTraversalPhase.Approach,self,track,roster).Active,Is.False);
            Assert.That(planner.Evaluate(true,30*SkiState.NanometersPerMeter,RangeTraversalPhase.Approach,self,track,roster).DesiredLateralMeters,Is.Zero);
            foreach(var phase in new[]{RangeTraversalPhase.Shooting,RangeTraversalPhase.RangeExit,RangeTraversalPhase.PenaltyApproach,RangeTraversalPhase.Rejoin})
                Assert.That(planner.Evaluate(true,3000*SkiState.NanometersPerMeter,phase,self,track,roster).Active,Is.True);
            Assert.That(planner.Evaluate(true,null,RangeTraversalPhase.RangeExit,self,track,roster).Active,Is.True,"Final-bout exit has no next shooting entry.");
            Assert.That(planner.Evaluate(true,null,RangeTraversalPhase.Complete,self,track,roster).Active,Is.False);
            Assert.That(planner.Evaluate(false,0,RangeTraversalPhase.RangeExit,self,track,roster).Active,Is.False);
            Assert.That(new AiRangeTrafficPlanner(Definition(false),OccupancyTests.Definition()).Evaluate(true,0,RangeTraversalPhase.RangeExit,self,track,roster).Active,Is.False);
        }
        [Test] public void AlongsideConvergenceRetainsRealLineUntilForwardBodySeparationExists()
        {
            var planner=new AiRangeTrafficPlanner(Definition(),OccupancyTests.Definition());var self=Placement("self",0,-.35);var other=Placement("other",.3,.35);
            var blocked=planner.Evaluate(true,0,RangeTraversalPhase.Approach,self,Track(),new[]{self,other});
            Assert.That(blocked.Active,Is.True);Assert.That(blocked.AlignmentBlocked,Is.True);Assert.That(blocked.DesiredLateralMeters,Is.EqualTo(self.LateralMeters));
            other=Placement("other",OccupancyTests.Definition().LongitudinalSeparation,.35);
            var clear=planner.Evaluate(true,0,RangeTraversalPhase.Approach,self,Track(),new[]{self,other});
            Assert.That(clear.AlignmentBlocked,Is.False);Assert.That(clear.DesiredLateralMeters,Is.Zero);
        }
        [Test] public void PaidAlignmentClearsNarrowPrivateForkAndKeepsAllActualBodyWidths()
        {
            var fixture=PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3);
            var runner=Create(fixture,new RaceEventLog(30000));double before=runner.Field.GetLateral(0);bool aligned=false,shot=false;
            var blocked=Create(PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3),new RaceEventLog(30000),false);
            Assert.That(before,Is.EqualTo(-2));
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<6000)
            {
                runner.Step();PhysicalRangePackTests.AssertBodiesAndWidth(runner.Field,fixture);
                if(runner.GetLastRangeTrafficPlan(0).Active)
                {
                    aligned=true;Assert.That(runner.GetLastLateral(0),Is.Zero);
                    Assert.That(runner.GetLastIntent(0).Kind,Is.Not.EqualTo(TacticalIntentKind.Overtake));
                }
                if(runner.Field.Tick.Value<=2)
                {
                    blocked.Step();Assert.That(runner.GetLastInput(0),Is.EqualTo(blocked.GetLastInput(0)));
                    if(runner.Field.Tick.Value==2)
                    {
                        Assert.That(runner.Field.GetLateral(0),Is.GreaterThan(before).And.LessThan(0),"The first active movement cannot teleport the body.");
                        Assert.That(runner.Field.GetLateral(0)-before,Is.LessThanOrEqualTo(OvertakeTests.Definition().LateralSpeed/runner.Field.TickFrequency+PhysiologyNumbers.MinimumPositive));
                        Assert.That(runner.Field.GetAthlete(0).LastFrame.Physiology.Effort.RelativeEffort,
                            Is.GreaterThan(blocked.Field.GetAthlete(0).LastFrame.Physiology.Effort.RelativeEffort),"The same drive command pays additional lateral work.");
                    }
                }
                shot|=runner.Field.GetAthlete(0).ActiveShooting!=null;
                if(shot&&runner.Field.GetAthlete(0).CurrentRangeMotion?.Phase==RangeTraversalPhase.Complete)break;
            }
            Assert.That(aligned,Is.True);Assert.That(shot,Is.True,"Broad skiing must not stall at the narrow private fork.");
            for(int tick=2;tick<2000;tick++)blocked.Step();
            Assert.That(blocked.Field.GetLateral(0),Is.EqualTo(before));Assert.That(blocked.Field.GetAthlete(0).ActiveShooting,Is.Null);
            Assert.That(blocked.Field.GetAthlete(0).Lifecycle.CompletedBouts,Is.Zero,"Disabled policy reproduces the narrow-fork stall.");
            Assert.That(runner.ContextJson,Does.Contain("ai_physical_range_safe_alignment_v1"));
        }
        [Test] public void WarmRangePlanningAllocatesNothing()
        {
            var planner=new AiRangeTrafficPlanner(Definition(),OccupancyTests.Definition());var self=Placement("self",0,-2);var roster=new[]{self};var track=Track();
            planner.Evaluate(true,0,RangeTraversalPhase.Approach,self,track,roster);
            long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<1000;i++)planner.Evaluate(true,0,RangeTraversalPhase.Approach,self,track,roster);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero);
        }
        [Test] public void RangeCommandsRoundTripAndRepeatExactPhysicalFramesAndEvents()
        {
            var log=new RaceEventLog(30000);var repeatLog=new RaceEventLog(30000);var replayLog=new RaceEventLog(30000);
            var a=Create(PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3),log);
            var b=Create(PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3),repeatLog);
            var replay=Create(PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3),replayLog).Field;
            var tape=new PackSprintRaceCommandTape("range_alignment_prefix",replay,1000);var inputs=new SprintAthleteInput[1];var lines=new double[1];bool privateSeen=false;
            for(int tick=0;tick<1000;tick++)
            {
                a.Step();b.Step();inputs[0]=a.GetLastInput(0);lines[0]=a.GetLastLateral(0);tape.StepAndRecord(replay,inputs,lines);
                Assert.That(a.Field.GetAthlete(0).LastFrame,Is.EqualTo(b.Field.GetAthlete(0).LastFrame));
                Assert.That(a.Field.GetAthlete(0).LastFrame,Is.EqualTo(replay.GetAthlete(0).LastFrame));
                Assert.That(a.Field.GetLateral(0),Is.EqualTo(replay.GetLateral(0)));privateSeen|=a.Field.GetPhysicalPoint(0).LoopId=="mat_1";
            }
            Assert.That(privateSeen,Is.True);Assert.That(log.Count,Is.EqualTo(repeatLog.Count));for(int i=0;i<log.Count;i++)Assert.That(log[i],Is.EqualTo(repeatLog[i]));
            var target=Create(PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3),new RaceEventLog(30000)).Field;
            var cursor=PackSprintRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(target);while(cursor.HasNext)cursor.Step();
            Assert.That(target.GetAthlete(0).LastFrame,Is.EqualTo(a.Field.GetAthlete(0).LastFrame));
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));Assert.That(a.ContextJson,Is.Not.EqualTo(Create(PhysicalRangePackTests.CreateFixture(),new RaceEventLog(1000),false).ContextJson));
        }
        [Test] public void PlayerCommandsAndLegacyNullPolicyRemainUnchanged()
        {
            var fixture=PhysicalRangePackTests.CreateFixture(count:1,flatPrivate:true,gridColumns:3);var log=new RaceEventLog(30000);var runner=Create(fixture,log);
            for(int tick=0;tick<10;tick++)
            {
                var next=runner.Field.Tick.Next();var id=runner.Field.GetAthlete(0).AthleteId;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(next,id,800),1,1,0),new WeaponAimCommand(next,id,0,0));
                runner.StepWithPlayer(0,input,-1.8);Assert.That(runner.GetLastLateral(0),Is.EqualTo(-1.8));Assert.That(runner.GetLastInput(0),Is.EqualTo(input));
                Assert.That(runner.GetLastRangeTrafficPlan(0).Active,Is.False);PhysicalRangePackTests.AssertBodiesAndWidth(runner.Field,fixture);
            }
            var legacy=AiTelemetryTests.Create(1,new RaceEventLog(30000),new AiDecisionCounts());legacy.Step();
            Assert.That(legacy.ContextJson,Does.Not.Contain("ai_range_traffic"));Assert.That(legacy.GetLastRangeTrafficPlan(0).Active,Is.False);
            var legacyPath=PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(),new RaceEventLog(30000));
            string Core(string name){if(name=="ai_range_traffic_provisional_v1.json")throw new InvalidOperationException("Unused range policy read.");return PhysiologyContractTests.DefinitionJson(name);}
            var nullPolicy=new PackAiRaceRunner(legacyPath,Core,new[]{AiPersonalityTests.Definition("attacker"),AiPersonalityTests.Definition("patient")},
                AiDifficultyTests.Definition("expert"),123,new RaceEventLog(30000),new AiDecisionCounts());
            nullPolicy.Step();Assert.That(nullPolicy.ContextJson,Does.Not.Contain("ai_range_traffic"));
        }
    }
}
