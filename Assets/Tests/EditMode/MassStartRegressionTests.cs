using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class MassStartRegressionTests
    {
#if !UNITY_EDITOR
        [Test,Timeout(1800000)] public void RepeatedThirtyAiFlatRollingAndNarrowMatrix()=>Matrix();
#else
        [UnityEngine.TestTools.UnityTest,Timeout(1800000)]
        public System.Collections.IEnumerator RepeatedThirtyAiFlatRollingAndNarrowMatrix()
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var work=System.Threading.Tasks.Task.Run(()=>Matrix(cancellation.Token),cancellation.Token);
                try{while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}finally{cancellation.Cancel();}
            }
        }
#endif
        private static void Matrix(System.Threading.CancellationToken cancellation=default)
        {
            Run("synthetic_mass_stadium",123,"expert",cancellation);
            Run("synthetic_mass_stadium_rolling",456,"developing",cancellation);
            Run("synthetic_mass_stadium_narrow",789,"expert",cancellation);
        }
        private static void Run(string prefix,ulong seed,string difficulty,System.Threading.CancellationToken cancellation)
        {
            var log=new RaceEventLog(300000);var repeated=new RaceEventLog(300000);var audit=new AiRaceScenarioTests.Audit();
            var a=MassStartFieldTests.Create(30,log,new AiDecisionCounts(),seed,difficulty,prefix,true);
            var b=MassStartFieldTests.Create(30,repeated,new AiDecisionCounts(),seed,difficulty,prefix,true);
            var previous=new long[30];var stalled=new int[30];var wasSkiing=new bool[30];var lines=new double[30];
            var direction=new int[30];var rapid=new int[30];var lastReversal=new long[30];
            int maxStall=0,launchStall=0,maxRapid=0,peakShooting=0;long launchStopped=0;
            var body=OccupancyTests.Definition();
            while(!a.Field.IsComplete && a.Field.Tick.Value<120000)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int i=0;i<30;i++){var actor=a.Field.GetAthlete(i);previous[i]=actor.Motion.DistanceNanometers;wasSkiing[i]=a.Field.IsOnCourse(i)&&actor.ActiveShooting==null;lines[i]=a.Field.GetLateral(i);}
                a.Step();b.Step();MassStartFieldTests.AssertClear(a.Field);int shooting=0;
                for(int i=0;i<30;i++)
                {
                    var actor=a.Field.GetAthlete(i);Assert.That(actor.LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                    Assert.That(a.Field.GetLateral(i),Is.EqualTo(b.Field.GetLateral(i)));Assert.That(actor.Motion.DistanceNanometers,Is.GreaterThanOrEqualTo(previous[i]));
                    stalled[i]=wasSkiing[i]&&actor.Motion.DistanceNanometers==previous[i]?stalled[i]+1:0;maxStall=Math.Max(maxStall,stalled[i]);
                    Assert.That(stalled[i],Is.LessThanOrEqualTo(2400),"Persistent skiing deadlock.");
                    if(a.Field.Tick.Value<=1200){launchStall=Math.Max(launchStall,stalled[i]);if(stalled[i]>0)launchStopped++;}
                    double delta=a.Field.GetLateral(i)-lines[i];int sign=Math.Abs(delta)>1e-8?Math.Sign(delta):0;
                    if(sign!=0)
                    {
                        if(direction[i]!=0&&direction[i]!=sign){rapid[i]=a.Field.Tick.Value-lastReversal[i]<=20?rapid[i]+1:1;lastReversal[i]=a.Field.Tick.Value;maxRapid=Math.Max(maxRapid,rapid[i]);Assert.That(rapid[i],Is.LessThan(8),"Sustained lateral oscillation.");}
                        direction[i]=sign;
                    }
                    if(actor.ActiveShooting!=null){shooting++;Assert.That(a.Field.GetPhysicalPoint(i).LoopId,Is.EqualTo("range_lane_"+actor.AssignedShootingLane.Ordinal));}
                    var point=a.Field.GetPhysicalPoint(i);var loop=a.Field.Track.GetLoop(point.LoopId);
                    for(int s=0;s<loop.SegmentCount;s++)
                    {
                        var segment=loop.GetSegment(s);long end=(segment.StartDistance.Millimeters+segment.Length.Millimeters)*SkiState.NanometersPerMillimeter;
                        if(point.LocalNanometers>=segment.StartDistance.Millimeters*SkiState.NanometersPerMillimeter && point.LocalNanometers<end)
                        {Assert.That(Math.Abs(a.Field.GetLateral(i))+body.EdgeMargin,Is.LessThanOrEqualTo(segment.WidthMeters/2+1e-9));break;}
                    }
                    audit.Add(actor.Motion.DistanceNanometers);audit.Add(BitConverter.DoubleToInt64Bits(a.Field.GetLateral(i)));audit.Add(BitConverter.DoubleToInt64Bits(actor.PhysiologyState.HeartRateBpm));
                }
                peakShooting=Math.Max(peakShooting,shooting);
            }
            Assert.That(a.Field.IsComplete&&b.Field.IsComplete,Is.True);Assert.That(a.Field.PrecisionFallbackTicks,Is.Zero);
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));Assert.That(log.Count,Is.EqualTo(repeated.Count));
            int starts=0,shots=0,penalties=0,assignments=0,passes=0;var visits=new int[30];var next=new int[4];
            for(int i=0;i<log.Count;i++)
            {
                var e=log[i];Assert.That(e,Is.EqualTo(repeated[i]));
                if(e.EventType==RaceEventType.ScheduledStartDue){starts++;Assert.That(e.Tick.Value,Is.EqualTo(1));}
                if(e.EventType==RaceEventType.ShotAccepted)shots++;
                if(e.EventType==RaceEventType.PenaltyLoopCompleted)penalties++;
                if(e.EventType==RaceEventType.OvertakeCompleted)passes++;
                if(e.EventType==RaceEventType.MassStartRangeLaneAssigned)
                {int athlete=int.Parse(e.AthleteId.Value.Substring(5));int bout=visits[athlete]++;Assert.That(e.Payload,Is.EqualTo(bout==0?athlete+1:++next[bout]));assignments++;}
            }
            Assert.That(starts,Is.EqualTo(30));Assert.That(shots,Is.EqualTo(600));Assert.That(assignments,Is.EqualTo(120));
            int expectedPenalties=0;
            for(int i=0;i<30;i++)
            {
                var actor=a.Field.GetAthlete(i);expectedPenalties+=actor.TotalPenaltyLoops;
                Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(4));Assert.That(actor.Lifecycle.CompletedLaps,Is.EqualTo(5));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((5000L+150L*actor.TotalPenaltyLoops)*SkiState.NanometersPerMeter+actor.InitialStartApproachNanometers));
            }
            Assert.That(penalties,Is.EqualTo(expectedPenalties));var result=a.Field.MassStartResult;
            Assert.That(result.ToJson(),Is.EqualTo(b.Field.MassStartResult.ToJson()));Assert.That(result.Count,Is.EqualTo(30));
            using(var sha=System.Security.Cryptography.SHA256.Create())TestContext.Out.WriteLine("MassMatrix prefix="+prefix+"; seed="+seed+"; difficulty="+difficulty+"; ticks="+a.Field.Tick.Value+"; max_stall="+maxStall+"; launch_stall="+launchStall+"; launch_stopped="+launchStopped+"; rapid_reversals="+maxRapid+"; peak_shooting="+peakShooting+"; penalties="+penalties+"; passes="+passes+"; trace="+audit.Hash.ToString("x16")+"; result_sha256="+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(result.ToJson()))).Replace("-","").ToLowerInvariant());
        }
    }
}
