using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class MassStartOrchestrationTests
    {
#if !UNITY_EDITOR
        [Test,Timeout(1200000)] public void ThirtyAiCompleteAllBoutsAndPhysicalPenalties()=>Run();
#else
        [UnityEngine.TestTools.UnityTest,Timeout(1200000)]
        public System.Collections.IEnumerator ThirtyAiCompleteAllBoutsAndPhysicalPenalties()
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var work=System.Threading.Tasks.Task.Run(()=>Run(cancellation.Token),cancellation.Token);
                try{while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}finally{cancellation.Cancel();}
            }
        }
#endif
        private static void Run(System.Threading.CancellationToken cancellation=default)
        {
            var log=new RaceEventLog(300000);var a=MassStartFieldTests.Create(30,log,new AiDecisionCounts());
            var shots=new int[30];var stalls=new int[30];var previous=new long[30];var active=new bool[30];
            var trace=new AiRaceScenarioTests.Audit();int peak=0,maxStall=0,penalties=0;bool finalBypass=false;
            while(!a.Field.IsComplete && a.Field.Tick.Value<120000)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int i=0;i<30;i++){var actor=a.Field.GetAthlete(i);previous[i]=actor.Motion.DistanceNanometers;active[i]=a.Field.IsOnCourse(i)&&actor.ActiveShooting==null;}
                a.Step();MassStartFieldTests.AssertClear(a.Field);int shooting=0;
                for(int i=0;i<30;i++)
                {
                    var actor=a.Field.GetAthlete(i);var point=a.Field.GetPhysicalPoint(i);
                    stalls[i]=active[i]&&actor.Motion.DistanceNanometers==previous[i]?stalls[i]+1:0;maxStall=Math.Max(maxStall,stalls[i]);
                    Assert.That(stalls[i],Is.LessThanOrEqualTo(2400),"Skiing stall exceeds120seconds.");
                    trace.Add(actor.Motion.DistanceNanometers);trace.Add(BitConverter.DoubleToInt64Bits(actor.Motion.SpeedMetersPerSecond));trace.Add(BitConverter.DoubleToInt64Bits(actor.PhysiologyState.HeartRateBpm));
                    if(actor.LastFrame.ShootingActive && actor.LastFrame.Shooting.Shot.Lifecycle.ShotAccepted)shots[i]++;
                    if(actor.ActiveShooting!=null){shooting++;Assert.That(point.LoopId,Is.EqualTo("range_lane_"+(i+1)));}
                    if(actor.Lifecycle.CompletedLaps==4 && actor.RouteNanometers>4800000000000L && actor.RouteNanometers<4900000000000L)
                    {Assert.That(point.LoopId,Is.EqualTo("main_long"));finalBypass=true;}
                }
                peak=Math.Max(peak,shooting);
            }
            Assert.That(a.Field.IsComplete,Is.True);Assert.That(finalBypass,Is.True);Assert.That(peak,Is.GreaterThan(1));
            for(int i=0;i<30;i++)
            {
                var actor=a.Field.GetAthlete(i);Assert.That(shots[i],Is.EqualTo(20));Assert.That(actor.Lifecycle.CompletedLaps,Is.EqualTo(5));Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(4));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((5000L+150L*actor.TotalPenaltyLoops)*SkiState.NanometersPerMeter+actor.InitialStartApproachNanometers));penalties+=actor.TotalPenaltyLoops;
            }
            var result=a.Field.MassStartResult;Assert.That(result.Count,Is.EqualTo(30));
            Assert.That(MassStartResultSnapshot.FromJson(result.ToJson(),a.Field.StartGrid,20,new SimulationTick(1)).ToJson(),Is.EqualTo(result.ToJson()));
            using(var sha=System.Security.Cryptography.SHA256.Create())
                TestContext.Out.WriteLine("MassStart30 ticks="+a.Field.Tick.Value+"; max_stall="+maxStall+"; peak_shooting="+peak+"; penalties="+penalties+"; trace="+trace.Hash.ToString("x16")+"; result_sha256="+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(result.ToJson()))).Replace("-","").ToLowerInvariant());
        }
    }
}
