using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class RangeFlowOwnerTests
    {
        [Test] public void AssistedMassStartCannotWaitBehindBodiesOnTheSecondApproach()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);int stopped=0;bool second=false;
            double worstMs=0,totalMs=0;long bytes=0;int n=0;
            var timer=new System.Diagnostics.Stopwatch();
            for(int t=0;t<18000&&!second;t++)
            {
                var a=f.runner.Field.GetAthlete(0);var s=a.ActiveShooting;var tick=f.runner.Field.Tick.Next();
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,a.AthleteId,1000),1,1,0),
                    new WeaponAimCommand(tick,a.AthleteId,1000,1000),trigger:true,cycleBolt:s?.NeedsBoltCycle==true,manualBoltCycle:true);
                long before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
                try{f.runner.StepWithManualPlayer(0,input,0,true);}catch(Exception e)
                {
                    for(int j=0;j<30;j++){var b=f.stepper.World.GetActor(j);var cur=f.stepper.World.GetCursor(j);var cat=f.stepper.World.GetCatalog(j);var row=cat.Get(cur.OccurrenceIndex);
                        if(row.End==ManualRacePathEnd.FiringLine){cat.TrySample(row.Index,row.LengthNanometers,out var goal);TestContext.WriteLine($"actor={j} arc={cur.ObservedArcNanometers}/{row.LengthNanometers} speed={f.runner.Field.GetAthlete(j).Motion.SpeedMetersPerSecond:R} yaw={b.Pose.YawRateRadiansPerSecond:R} dist={Math.Sqrt(Math.Pow(b.Pose.X-goal.X,2)+Math.Pow(b.Pose.Z-goal.Z,2)):R}");}}
                    Assert.Fail($"tick={tick.Value}, bout={a.ActiveBout}, mean_ms={totalMs/Math.Max(1,n):R}, max_ms={worstMs:R}; "+e);
                }
                timer.Stop();
                if(t>100){worstMs=Math.Max(worstMs,timer.Elapsed.TotalMilliseconds);totalMs+=timer.Elapsed.TotalMilliseconds;bytes+=GC.GetAllocatedBytesForCurrentThread()-before;n++;}
                if(a.CurrentRangeMotion?.Phase==RangeTraversalPhase.Approach)
                {
                    stopped=a.Motion.SpeedMetersPerSecond<.5?stopped+1:0;
                    Assert.That(stopped,Is.LessThan(160),$"Assisted approach blocked for 8 seconds: tick={tick.Value}, bout={a.ActiveBout}, loop={f.stepper.World.GetActor(0).Observation.LoopId}");
                }
                else stopped=0;
                second=a.ActiveBout==1&&a.ActiveShooting!=null;
            }
            TestContext.WriteLine($"ticks={f.runner.Field.Tick.Value}; mean_ms={totalMs/Math.Max(1,n):R}; max_ms={worstMs:R}; bytes_per_tick={bytes/Math.Max(1,n)}");
            Assert.That(second,Is.True,"Reach the second real firing position.");
        }
    }
}
