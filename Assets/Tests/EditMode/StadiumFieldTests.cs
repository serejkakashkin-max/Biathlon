using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class StadiumFieldTests
    {
#if !UNITY_EDITOR
        [Test, Timeout(1200000)] public void ThirtyAthletesUseRangeLanesWithoutQueueCollapse() => DenseRace();
#else
        [UnityEngine.TestTools.UnityTest, Timeout(1200000)]
        public System.Collections.IEnumerator ThirtyAthletesUseRangeLanesWithoutQueueCollapse()
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var token=cancellation.Token;
                var work=System.Threading.Tasks.Task.Run(()=>DenseRace(token),token);
                try {while(!work.IsCompleted) yield return null;work.GetAwaiter().GetResult();}
                finally {cancellation.Cancel();}
            }
        }
#endif
        private static void DenseRace(System.Threading.CancellationToken cancellation=default)
        {
            var events=new RaceEventLog(100000);var repeatedEvents=new RaceEventLog(100000);
            var audit=new AiRaceScenarioTests.Audit();var repeatedAudit=new AiRaceScenarioTests.Audit();
            var a=Create(30,events,audit);var b=Create(30,repeatedEvents,repeatedAudit);
            var previous=new long[30];var active=new bool[30];var stalled=new long[30];long totalStopped=0,maxStopped=0;
            var body=OccupancyTests.Definition();int peakShooting=0;
            while(!a.Field.IsComplete && a.Field.Tick.Value<120000)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int i=0;i<30;i++) {previous[i]=a.Field.GetAthlete(i).Motion.DistanceNanometers;active[i]=a.Field.IsOnCourse(i)&&a.Field.GetAthlete(i).ActiveShooting==null;}
                a.Step();b.Step();int shooting=0;
                for(int i=0;i<30;i++)
                {
                    var actor=a.Field.GetAthlete(i);var other=b.Field.GetAthlete(i);var point=a.Field.GetPhysicalPoint(i);
                    if(active[i] && actor.Motion.DistanceNanometers==previous[i]) {totalStopped++;stalled[i]++;} else stalled[i]=0;
                    maxStopped=Math.Max(maxStopped,stalled[i]);
                    if(stalled[i]>2400) Assert.Fail("Stadium skiing stalled beyond the unchanged120-second bound.");
                    audit.Add(actor.Motion.DistanceNanometers);repeatedAudit.Add(other.Motion.DistanceNanometers);
                    audit.Add(BitConverter.DoubleToInt64Bits(actor.Motion.SpeedMetersPerSecond));repeatedAudit.Add(BitConverter.DoubleToInt64Bits(other.Motion.SpeedMetersPerSecond));
                    audit.Add(BitConverter.DoubleToInt64Bits(actor.PhysiologyState.HeartRateBpm));repeatedAudit.Add(BitConverter.DoubleToInt64Bits(other.PhysiologyState.HeartRateBpm));
                    if(actor.ActiveShooting!=null) shooting++;
                    if(!a.Field.IsOnCourse(i)) continue;
                    for(int j=0;j<i;j++) if(a.Field.IsOnCourse(j))
                    {
                        var neighbor=a.Field.GetPhysicalPoint(j);if(neighbor.LoopId!=point.LoopId)continue;
                        long gap=Math.Abs(point.LocalNanometers-neighbor.LocalNanometers);if(point.Closed)gap=Math.Min(gap,point.LoopLengthNanometers-gap);
                        if(gap/1e9<body.LongitudinalSeparation && Math.Abs(a.Field.GetLateral(i)-a.Field.GetLateral(j))<body.LateralSeparation)
                            Assert.Fail("Stadium physical bodies overlap.");
                    }
                }
                peakShooting=Math.Max(peakShooting,shooting);
            }
            Assert.That(a.Field.IsComplete && b.Field.IsComplete,Is.True);
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));Assert.That(a.Field.Result.ToJson(),Is.EqualTo(b.Field.Result.ToJson()));
            Assert.That(audit.Hash,Is.EqualTo(repeatedAudit.Hash));Assert.That(events.Count,Is.EqualTo(repeatedEvents.Count));
            for(int e=0;e<events.Count;e++) Assert.That(events[e],Is.EqualTo(repeatedEvents[e]));
            for(int i=0;i<30;i++)
            {
                var actor=a.Field.GetAthlete(i);int starts=0,shots=0;
                for(int e=0;e<events.Count;e++) if(events[e].AthleteId.Equals(actor.AthleteId))
                {if(events[e].EventType==RaceEventType.FinalSprintStarted)starts++;if(events[e].EventType==RaceEventType.ShotAccepted)shots++;}
                Assert.That(starts,Is.EqualTo(1));Assert.That(shots,Is.EqualTo(10));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((3000+150L*actor.TotalPenaltyLoops)*SkiState.NanometersPerMeter));
                Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(2));
            }
            Assert.That(a.Field.PrecisionFallbackTicks,Is.Zero);Assert.That(b.Field.PrecisionFallbackTicks,Is.Zero);
            // Require at least90% reduction from WP079's834788 stopped on-course ticks.
            Assert.That(totalStopped,Is.LessThanOrEqualTo(83478));Assert.That(peakShooting,Is.GreaterThan(1));
            string hash;using(var sha=System.Security.Cryptography.SHA256.Create()) hash=BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(a.Field.Result.ToJson()))).Replace("-","").ToLowerInvariant();
            TestContext.Out.WriteLine("stadium ticks="+a.Field.Tick.Value+", stopped="+totalStopped+", max_stall="+maxStopped+", peak_shooting="+peakShooting+", trace="+audit.Hash.ToString("x16")+", result="+hash);
        }
        public static PackAiRaceRunner Create(int count, RaceEventLog events, IAiDecisionSink sink, string connectionJson = null)
        {
            string Read(string name) => PhysiologyContractTests.DefinitionJson(name);
            string Fixture(string name) => PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/" + name);
            var track = PhysicalRangeLaneTests.Track(); var route = PhysicalRangeLaneTests.Route(track);
            var profile = CompetitionProfile.FromJson(PhysiologyContractTests.ReplaceNumber(CompetitionProfileTests.TestProfile().ToJson(), "start_interval_s", "2"));
            var ski = TimeTrialTests.Athlete(); var shot = ShootingSessionTests.Athlete();
            var entrants = new SprintEntrant[count]; var profiles = new AiPersonalityDefinition[count];
            for (int i = 0; i < count; i++)
            {
                string id = "athlete_" + i;
                entrants[i] = new SprintEntrant(TimeTrialAthlete.FromJson(ski.ToJson().Replace(ski.Id,id)), ShootingAthleteDefinition.FromJson(shot.ToJson().Replace(shot.Id,id)),"ski_neutral");
                profiles[i] = AiPersonalityTests.Definition(i%2==0?"attacker":"patient");
            }
            var junctions = PhysicalJunctionDefinition.FromJson(Fixture("synthetic_stadium_junctions_v1.json"),track);
            var connections = PhysicalConnectionDefinition.FromJson(connectionJson ?? Fixture("synthetic_stadium_connections_v1.json"),junctions);
            var lanes = PhysicalRangeLaneDefinition.FromJson(PhysicalRangeLaneTests.Json(),track);
            var field = new PackSprintFieldSession(new SimulationConfig(1,"sprint_field_v1",20,123),profile,new SkiModelDefinitions(Read),new ShootingModelDefinitions(Read),track,route,
                SnowConditionTests.Conditions(),SprintResultsTests.Timing(),entrants,new SimulationTick(1),"sprint_result_v1",OccupancyTests.Definition(),DraftingTests.Definition(),OvertakeTests.Definition(),junctions,connections,events,lanes);
            return new PackAiRaceRunner(field,Read,profiles,AiDifficultyTests.Definition("expert"),123,events,sink);
        }
        [Test] public void MissingPhysicalConnectionsAndInsufficientLanesFailBeforeAnyTick()
        {
            var events=new RaceEventLog(100);
            string connections=PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/synthetic_stadium_connections_v1.json");
            Assert.Throws<ArgumentException>(()=>Create(2,events,new AiDecisionCounts(),connections.Replace("\"range_lane_1_end\"","\"range_lane_2_end\"")));
            Assert.Throws<ArgumentException>(()=>Create(31,events,new AiDecisionCounts()));
            Assert.That(events.Count,Is.Zero);
        }
        [Test] public void SoloAlternativePreservesSkiPhysicsAndExactFinish()
        {
            var events=new RaceEventLog(30000); var stadium=Create(1,events,new AiDecisionCounts());
            var baseline=AiTelemetryTests.Create(1,new RaceEventLog(30000),new AiDecisionCounts());
            while(!stadium.Field.IsComplete && stadium.Field.Tick.Value<20000)
            {
                stadium.Step();baseline.Step();
                Assert.That(stadium.Field.GetAthlete(0).Motion,Is.EqualTo(baseline.Field.GetAthlete(0).Motion));
                Assert.That(stadium.Field.GetAthlete(0).PhysiologyState,Is.EqualTo(baseline.Field.GetAthlete(0).PhysiologyState));
            }
            Assert.That(stadium.Field.IsComplete && baseline.Field.IsComplete,Is.True);
            Assert.That(stadium.Field.Result.ToJson(),Is.EqualTo(baseline.Field.Result.ToJson()));
            Assert.That(stadium.ContextJson,Does.Contain("physical_range_lanes_v1"));
        }
        [Test] public void ParallelShootingUsesDistinctPhysicalBodiesAndReplaysTheWholeField()
        {
            var events=new RaceEventLog(30000);var repeatedEvents=new RaceEventLog(30000);
            var a=Create(2,events,new AiDecisionCounts());var b=Create(2,repeatedEvents,new AiDecisionCounts());
            int concurrent=0;bool finalBypass=false;
            while(!a.Field.IsComplete && a.Field.Tick.Value<20000)
            {
                a.Step();b.Step();int shooting=0;
                for(int i=0;i<2;i++)
                {
                    var actor=a.Field.GetAthlete(i);var point=a.Field.GetPhysicalPoint(i);
                    Assert.That(actor.LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                    if(actor.ActiveShooting!=null) {shooting++;Assert.That(point.LoopId,Is.EqualTo("range_lane_"+(i+1)));}
                    if(actor.Lifecycle.CompletedLaps==2 && actor.RouteNanometers>2800L*SkiState.NanometersPerMeter && actor.RouteNanometers<2900L*SkiState.NanometersPerMeter)
                    {Assert.That(point.LoopId,Is.EqualTo("main_long"));finalBypass=true;}
                }
                concurrent=Math.Max(concurrent,shooting);
            }
            Assert.That(a.Field.IsComplete && b.Field.IsComplete,Is.True);
            Assert.That(concurrent,Is.EqualTo(2));Assert.That(finalBypass,Is.True);
            Assert.That(a.Field.PrecisionFallbackTicks,Is.Zero);
            Assert.That(a.Field.Result.ToJson(),Is.EqualTo(b.Field.Result.ToJson()));
            Assert.That(events.Count,Is.EqualTo(repeatedEvents.Count));
            for(int i=0;i<events.Count;i++) Assert.That(events[i],Is.EqualTo(repeatedEvents[i]));
        }
    }
}
