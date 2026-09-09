using System;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiRaceScenarioTests
    {
        // Fixed acceptance limits: 100 simulated minutes overall, 120 seconds without
        // skiing progress, and fewer than eight consecutive lateral reversals <=1s apart.
        // Shooting and start staging are separate lifecycle waits, not skiing deadlocks.
        public sealed class Audit : IAiDecisionSink
        {
            public readonly long[] Intents=new long[8];
            public readonly AiDecisionBuffer Tail=new AiDecisionBuffer(64);
            public long Decisions, DraftSelections, RecoveryRequests;
            public ulong Hash=14695981039346656037UL;
            public void Add(long value) { unchecked { Hash=(Hash^(ulong)value)*1099511628211UL; } }
            public void Record(AiDecisionTrace row)
            {
                Tail.Record(row); Decisions++; Add(row.Tick.Value);Add((int)row.Kind);Add((int)row.Intent.Kind);
                foreach(char c in row.Athlete.Value) Add(c);
                foreach(char c in row.Intent.TargetId.Value??"") Add(c);
                Add(row.Pace.Input.RequestedEffort);Add(BitConverter.DoubleToInt64Bits(row.Lateral));
                Add(BitConverter.DoubleToInt64Bits(row.Shooting.Risk));Add(row.Aim.Aim.Horizontal);Add(row.Aim.Aim.Vertical);Add(row.Aim.Trigger?1:0);
                for(int i=0;i<8;i++) { var s=row.Utilities.Get(i);Add(s.Eligible?1:0);Add(BitConverter.DoubleToInt64Bits(s.Total)); }
                if(row.Kind!=AiTraceKind.Tactical) return;
                Intents[(int)row.Intent.Kind]++;
                if(row.Draft.Selected.Strength>0) DraftSelections++;
                if(row.Recovery.Requested) RecoveryRequests++;
            }
        }
        public sealed class RunResult
        {
            public string Result, Context, Digest, Tail;
            public long Ticks, MaxStall;
            public int MaxRapidReversals;
            public long ClimbAttackTicks, FinalSprintEvents;
            public int MaxDraftTrain;
            public Audit Audit;
        }
        public static RunResult Run(string track,int count,string level,ulong seed,System.Threading.CancellationToken cancellation=default)
        {
            var events=new RaceEventLog(200000);var audit=new Audit();
            var runner=AiTelemetryTests.Create(count,events,audit,track,level,seed);var field=runner.Field;
            var oldTravel=new long[count];var oldLine=new double[count];var direction=new int[count];
            var lastReversal=new long[count];var rapid=new int[count];var stalled=new long[count];
            var body=OccupancyTests.Definition();long maxStall=0,climbAttacks=0,finalSprints=0;int maxRapid=0,maxTrain=0;
            var sampler=new TrackSampler(field.Track.GetLoop("main_long"));
            while(!field.IsComplete && field.Tick.Value<120000)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int i=0;i<count;i++) { oldTravel[i]=field.GetAthlete(i).Motion.DistanceNanometers;oldLine[i]=field.GetLateral(i); }
                runner.Step();
                int train=0;
                for(int i=0;i<count;i++)
                {
                    var actor=field.GetAthlete(i);long travel=actor.Motion.DistanceNanometers;
                    if(travel<oldTravel[i]) Assert.Fail("Non-monotone authoritative travel.");
                    bool skiing=field.IsOnCourse(i) && actor.ActiveShooting==null;
                    if(field.GetDraft(i).Strength>0) train++;
                    var physical=field.GetPhysicalPoint(i);
                    if(skiing && physical.LoopId=="main_long" && runner.GetLastIntent(i).Kind==TacticalIntentKind.Attack &&
                        sampler.Sample(new TrackDistance(physical.LocalNanometers/SkiState.NanometersPerMillimeter)).Grade>0) climbAttacks++;
                    stalled[i]=skiing && travel==oldTravel[i]?stalled[i]+1:0;
                    maxStall=Math.Max(maxStall,stalled[i]);
                    if(stalled[i]>2400)
                    {
                        for(int k=0;k<count;k++) { var p=field.GetPhysicalPoint(k);var x=field.GetAthlete(k);TestContext.Out.WriteLine("actor="+k+",point="+p.LoopId+":"+p.LocalNanometers+",line="+field.GetLateral(k)+",travel="+x.Motion.DistanceNanometers+",speed="+x.Motion.SpeedMetersPerSecond+",phase="+x.LastFrame.Competition+",finished="+x.HasFinished+",intent="+runner.GetLastIntent(k).Kind+",effort="+runner.GetLastInput(k).Ski.Effort.RequestedEffort); }
                        Assert.Fail("Skiing deadlock at tick "+field.Tick.Value+" actor "+i);
                    }
                    double delta=field.GetLateral(i)-oldLine[i];int sign=Math.Abs(delta)>1e-8?Math.Sign(delta):0;
                    if(sign!=0)
                    {
                        if(direction[i]!=0 && direction[i]!=sign)
                        {
                            rapid[i]=field.Tick.Value-lastReversal[i]<=20?rapid[i]+1:1;
                            lastReversal[i]=field.Tick.Value;maxRapid=Math.Max(maxRapid,rapid[i]);
                            if(rapid[i]>=8) Assert.Fail("Sustained lateral oscillation at tick "+field.Tick.Value+" actor "+i);
                        }
                        direction[i]=sign;
                    }
                    audit.Add(travel);audit.Add(BitConverter.DoubleToInt64Bits(field.GetLateral(i)));
                    audit.Add(BitConverter.DoubleToInt64Bits(actor.Motion.SpeedMetersPerSecond));
                    audit.Add(BitConverter.DoubleToInt64Bits(actor.PhysiologyState.HeartRateBpm));
                    if(!field.IsOnCourse(i)) continue;
                    var a=field.GetPhysicalPoint(i);
                    for(int j=0;j<i;j++) if(field.IsOnCourse(j))
                    {
                        var b=field.GetPhysicalPoint(j);if(a.LoopId!=b.LoopId) continue;
                        long gap=Math.Abs(a.LocalNanometers-b.LocalNanometers);if(a.Closed) gap=Math.Min(gap,a.LoopLengthNanometers-gap);
                        if(gap/1e9<body.LongitudinalSeparation && Math.Abs(field.GetLateral(i)-field.GetLateral(j))<body.LateralSeparation)
                            Assert.Fail("Physical overlap at tick "+field.Tick.Value+" actors "+i+","+j);
                    }
                }
                maxTrain=Math.Max(maxTrain,train);
            }
            Assert.That(field.IsComplete,Is.True,"Race completion bound");Assert.That(field.Result.Count,Is.EqualTo(count));
            Assert.That(field.PrecisionFallbackTicks,Is.Zero);
            for(int i=0;i<count;i++)
            {
                var actor=field.GetAthlete(i);Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(2));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((3000+150L*actor.TotalPenaltyLoops)*SkiState.NanometersPerMeter));
            }
            for(int i=0;i<events.Count;i++)
            {
                var e=events[i];audit.Add(e.Tick.Value);audit.Add((int)e.EventType);audit.Add(e.Payload);audit.Add(e.TickFrequency);
                foreach(char c in e.AthleteId.Value) audit.Add(c);
                if(e.EventType==RaceEventType.FinalSprintStarted) finalSprints++;
            }
            var csv=new StringWriter(CultureInfo.InvariantCulture);audit.Tail.WriteCsv(csv);
            var result=new RunResult{Result=field.Result.ToJson(),Context=runner.ContextJson,Digest=audit.Hash.ToString("x16"),Tail=csv.ToString(),Ticks=field.Tick.Value,MaxStall=maxStall,MaxRapidReversals=maxRapid,Audit=audit,ClimbAttackTicks=climbAttacks,FinalSprintEvents=finalSprints,MaxDraftTrain=maxTrain};
            using(var sha=SHA256.Create())
                TestContext.Out.WriteLine("track="+track+",count="+count+",difficulty="+level+",seed="+seed+",ticks="+result.Ticks+",max_stall_ticks="+maxStall+",rapid_reversals="+maxRapid+",decisions="+audit.Decisions+",intents="+string.Join("/",audit.Intents)+",trace_hash="+result.Digest+",result_sha256="+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(result.Result))).Replace("-","").ToLowerInvariant());
            if(count==30) SaveEvidence(result,track,level,seed);
            return result;
        }
        private static void SaveEvidence(RunResult run,string track,string level,ulong seed)
        {
            var root=new DirectoryInfo(TestContext.CurrentContext.TestDirectory);
            while(root!=null && !File.Exists(Path.Combine(root.FullName,"AGENTS.md"))) root=root.Parent;
            if(root==null) throw new DirectoryNotFoundException("BIATHLON root for evidence.");
            string directory=Path.Combine(root.FullName,"artifacts/physiology/ai-soak");Directory.CreateDirectory(directory);
            string name=Path.Combine(directory,track+"-"+level+"-"+seed);
            File.WriteAllText(name+"-context.json",run.Context);File.WriteAllText(name+"-result.json",run.Result);
            File.WriteAllText(name+"-tail.csv",run.Tail);
            File.WriteAllText(name+"-audit.json",string.Format(CultureInfo.InvariantCulture,
                "{{\"schema_version\":1,\"id\":\"ai_soak_{0}_{1}_{2}\",\"seed\":{2},\"athletes\":30,\"ticks\":{3},\"max_stall_ticks\":{4},\"max_rapid_reversals\":{5},\"trace_digest_fnv64\":\"{6}\",\"trace_total\":{7},\"tail_overwritten\":{8},\"climb_attack_ticks\":{9},\"max_draft_train\":{10},\"final_sprint_events\":{11}}}",
                track,level,seed,run.Ticks,run.MaxStall,run.MaxRapidReversals,run.Digest,run.Audit.Decisions,run.Audit.Tail.Overwritten,run.ClimbAttackTicks,run.MaxDraftTrain,run.FinalSprintEvents));
        }
        [TestCase("duel","synthetic_flat_v1",2)]
        [TestCase("pack","synthetic_rolling_v1",6)]
        [TestCase("climb_attack","synthetic_climb_v1",2)]
        [TestCase("draft_train","synthetic_flat_v1",8)]
        [TestCase("final_sprint","synthetic_flat_v1",1)]
        [TestCase("pre_range_recovery","synthetic_range_approach_v1",2)]
        public void NamedScenarioExercisesRealAi(string name,string track,int count)
        {
            var a=Run(track,count,"expert",123);
            Assert.That(a.Audit.Decisions,Is.GreaterThan(0));
            if(name=="climb_attack") Assert.That(a.ClimbAttackTicks,Is.GreaterThan(0));
            if(name=="draft_train") { Assert.That(a.MaxDraftTrain,Is.GreaterThanOrEqualTo(2));Assert.That(a.Audit.Intents[2]+a.Audit.Intents[3],Is.GreaterThan(0)); }
            if(name=="final_sprint") { Assert.That(a.Audit.Intents[(int)TacticalIntentKind.FinalSprint],Is.GreaterThan(0));Assert.That(a.FinalSprintEvents,Is.GreaterThan(0)); }
            if(name=="pre_range_recovery") { Assert.That(a.Audit.RecoveryRequests,Is.GreaterThan(0));Assert.That(a.Audit.Intents[(int)TacticalIntentKind.PreRangeRecovery],Is.GreaterThan(0)); }
        }
#if !UNITY_EDITOR
        [TestCase("synthetic_flat_v1","expert",123UL)]
        [TestCase("synthetic_rolling_v1","developing",456UL)]
        [TestCase("synthetic_flat_v1","developing",789UL)]
        // Unity's default 180s wall-clock limit is shorter than two audited Mono races.
        // Simulation-time liveness/oscillation limits above remain unchanged.
        [Timeout(1200000)]
        public void ThirtyAthletesRepeatWholeRacesWithoutDeadlockOscillationOrOverlap(string track,string level,ulong seed)
            => Repeat(track,level,seed);
#else
        // The audited simulation has no Unity API dependencies. Keep the Editor responsive
        // while Mono executes the same full workload and marshal any failure back to NUnit.
        [UnityEngine.TestTools.UnityTest, Timeout(1200000)]
        public System.Collections.IEnumerator ThirtyAthletesFlatExpert()
            => RepeatInBackground("synthetic_flat_v1","expert",123);
        [UnityEngine.TestTools.UnityTest, Timeout(1200000)]
        public System.Collections.IEnumerator ThirtyAthletesRollingDeveloping()
            => RepeatInBackground("synthetic_rolling_v1","developing",456);
        [UnityEngine.TestTools.UnityTest, Timeout(1200000)]
        public System.Collections.IEnumerator ThirtyAthletesFlatDeveloping()
            => RepeatInBackground("synthetic_flat_v1","developing",789);
        private static System.Collections.IEnumerator RepeatInBackground(string track,string level,ulong seed)
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var token=cancellation.Token;
                var work=System.Threading.Tasks.Task.Run(()=>Repeat(track,level,seed,token),token);
                try { while(!work.IsCompleted) yield return null; work.GetAwaiter().GetResult(); }
                finally { cancellation.Cancel(); }
            }
        }
#endif
        private static void Repeat(string track,string level,ulong seed,System.Threading.CancellationToken cancellation=default)
        {
            var a=Run(track,30,level,seed,cancellation);var b=Run(track,30,level,seed,cancellation);
            Assert.That(b.Context,Is.EqualTo(a.Context));Assert.That(b.Result,Is.EqualTo(a.Result));
            Assert.That(b.Digest,Is.EqualTo(a.Digest));Assert.That(b.Tail,Is.EqualTo(a.Tail));
            Assert.That(b.Ticks,Is.EqualTo(a.Ticks));Assert.That(b.Audit.Intents,Is.EqualTo(a.Audit.Intents));
        }
    }
}
