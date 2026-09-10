using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PursuitFieldTests
    {
        public static PackAiRaceRunner Create(PursuitQualificationSnapshot q, RaceEventLog events, IAiDecisionSink sink,
            bool reverseRoster = false, CompetitionProfile overrideProfile = null, ulong seed = 123, string difficulty = "expert")
        {
            string Read(string name) => PhysiologyContractTests.DefinitionJson(name);
            string Fixture(string name) => PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+name);
            var profile=overrideProfile??CompetitionProfile.FromJson(q.PursuitProfileJson);
            var track=PhysicalRangeLaneTests.Track(); var requirements=profile.ToRouteRequirements();
            var route=new RaceRoute(track,RaceRoutePlan.TimeTrial("pursuit_stadium_route",track.Id,"main_long",requirements),requirements);
            var ski=TimeTrialTests.Athlete(); var shot=ShootingSessionTests.Athlete();
            var entrants=new SprintEntrant[q.Count]; var personalities=new AiPersonalityDefinition[q.Count];
            for(int i=0;i<q.Count;i++)
            {
                string id=q.Get(reverseRoster?q.Count-i-1:i).AthleteId;
                entrants[i]=new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id)),
                    ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,id)),"ski_neutral");
                personalities[i]=AiPersonalityTests.Definition(i%2==0?"attacker":"patient");
            }
            var junctions=PhysicalJunctionDefinition.FromJson(Fixture("synthetic_stadium_junctions_v1.json"),track);
            var connections=PhysicalConnectionDefinition.FromJson(Fixture("synthetic_stadium_connections_v1.json"),junctions);
            var lanes=PhysicalRangeLaneDefinition.FromJson(PhysicalRangeLaneTests.Json(),track);
            var field=new PackSprintFieldSession(new SimulationConfig(1,"pursuit_field_v1",20,seed),profile,new SkiModelDefinitions(Read),
                new ShootingModelDefinitions(Read),track,route,SnowConditionTests.Conditions(),SprintResultsTests.Timing(),entrants,
                new SimulationTick(1),"pursuit_result_v1",OccupancyTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),
                junctions,connections,events,lanes,q);
            return new PackAiRaceRunner(field,Read,personalities,AiDifficultyTests.Definition(difficulty),seed,events,sink);
        }
        [Test] public void CompleteSprintFeedsRepeatedSharedPursuitWithExactStartsAndPpss()
        {
            var sprint=StadiumFieldTests.Create(2,new RaceEventLog(30000),new AiDecisionCounts());
            while(!sprint.Field.IsComplete && sprint.Field.Tick.Value<20000) sprint.Step();
            Assert.That(sprint.Field.IsComplete,Is.True);
            var q=PursuitQualificationSnapshot.Create("chain",sprint.Field.Result,PursuitQualificationTests.Profile());
            var la=new RaceEventLog(60000);var lb=new RaceEventLog(60000);
            var a=Create(q,la,new AiDecisionCounts());
            var restored=PursuitQualificationSnapshot.FromJson(q.ToJson(),q.SourceResultId,PursuitQualificationTests.Profile());
            var b=Create(restored,lb,new AiDecisionCounts());
            var shots=new int[2]; bool finalBypass=false, sawChase=false, sawLastRange=false;
            while(!a.Field.IsComplete && a.Field.Tick.Value<40000)
            {
                a.Step();b.Step();
                for(int i=0;i<2;i++)
                {
                    var plan=a.GetLastPursuitPlan(i); Assert.That(plan,Is.EqualTo(b.GetLastPursuitPlan(i)));
                    sawChase |= (plan.Reason & PursuitTacticalReason.Chase)!=0;
                    sawLastRange |= (plan.Reason & PursuitTacticalReason.LastRange)!=0;
                    Assert.That(plan.ShootingUrgency,Is.InRange(0,1));
                }
                for(int i=0;i<2;i++)
                {
                    var actor=a.Field.GetAthlete(i);
                    Assert.That(actor.LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                    if(actor.LastFrame.ShootingActive && actor.LastFrame.Shooting.Shot.Lifecycle.ShotAccepted) shots[i]++;
                    if(actor.ActiveShooting!=null) Assert.That(a.Field.GetPhysicalPoint(i).LoopId,Is.EqualTo("range_lane_"+(i+1)));
                    if(actor.Lifecycle.CompletedLaps==4 && actor.RouteNanometers>4800L*SkiState.NanometersPerMeter && actor.RouteNanometers<4900L*SkiState.NanometersPerMeter)
                    {Assert.That(a.Field.GetPhysicalPoint(i).LoopId,Is.EqualTo("main_long"));finalBypass=true;}
                }
            }
            Assert.That(a.Field.IsComplete && b.Field.IsComplete,Is.True); Assert.That(finalBypass,Is.True);
            Assert.That(sawChase && sawLastRange,Is.True); Assert.That(a.ContextJson,Does.Contain("pursuit_tactics_provisional_v1"));
            Assert.That(shots,Is.EqualTo(new[]{20,20}));Assert.That(a.Field.PrecisionFallbackTicks,Is.Zero);
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));
            var result=a.Field.PursuitResult;Assert.That(a.Field.Result,Is.Null);
            Assert.That(result.ToJson(),Is.EqualTo(b.Field.PursuitResult.ToJson()));
            Assert.That(PursuitResultSnapshot.FromJson(result.ToJson(),q,20,new SimulationTick(1)).ToJson(),Is.EqualTo(result.ToJson()));
            for(int i=0;i<2;i++)
            {
                var actor=a.Field.GetAthlete(i);
                Assert.That(actor.Lifecycle.CompletedLaps,Is.EqualTo(5));Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(4));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((5000L+150L*actor.TotalPenaltyLoops)*SkiState.NanometersPerMeter));
                Assert.That((actor.ScheduledStart.Value-1)*q.GapUnitsPerSecond,Is.EqualTo(q.Get(i).StartGapUnits*20));
            }
            Assert.That(result.Get(0).GlobalFinish(20).CompareTo(result.Get(1).GlobalFinish(20)),Is.LessThanOrEqualTo(0));
            Assert.That(la.Count,Is.EqualTo(lb.Count));for(int i=0;i<la.Count;i++)Assert.That(la[i],Is.EqualTo(lb[i]));
            var perception=new SprintPerceptionSource(a.Field);
            for(int i=0;i<2;i++)
            {
                var observed=perception.Capture(i,TrackDistance.FromMeters(100),Array.Empty<TrackDistance>(),Array.Empty<AiRouteLookahead>(),new AiRouteNeighbor[1]);
                Assert.That(observed.Format,Is.EqualTo(CompetitionFormat.Pursuit));
                Assert.That(observed.TimingClock,Is.EqualTo(CheckpointClock.Global));
                Assert.That(observed.ElapsedGapToCheckpointLeaderSeconds,Is.Null);
                Assert.That(observed.CheckpointGapToLeaderSeconds,Is.EqualTo(a.Field.Timing.GapSeconds(a.Field.GetAthlete(i).AthleteId,observed.LatestCheckpointOrdinal)));
            }
            using(var sha=System.Security.Cryptography.SHA256.Create())
                TestContext.Out.WriteLine("Pursuit field ticks="+a.Field.Tick.Value+"; result_sha256="+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(result.ToJson()))).Replace("-","").ToLowerInvariant());
        }
        [Test] public void WrongRosterAndProfileFailBeforeRaceEvents()
        {
            var q=PursuitQualificationSnapshot.Create("q",PursuitQualificationTests.Source(),PursuitQualificationTests.Profile());
            var log=new RaceEventLog(100);
            Assert.Catch(()=>Create(q,log,new AiDecisionCounts(),true));
            var wrong=CompetitionProfile.FromJson(q.PursuitProfileJson.Replace("pursuit_test_v1","other_pursuit"));
            Assert.Catch(()=>Create(q,log,new AiDecisionCounts(),overrideProfile:wrong));
            Assert.That(log.Count,Is.Zero);
        }
    }
}
