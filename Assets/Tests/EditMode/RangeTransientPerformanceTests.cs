using System;
using System.Diagnostics;
using System.Globalization;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RangeTransientPerformanceTests
    {
        [Test, Explicit("Bounded two-bout CPU diagnostic, not a portable timing gate.")]
        public void FirstTwoLapsCpuProfile()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            var timer=new Stopwatch();double window=0,worst=0,total=0;long bytes=0;int windowPieces=0;
            for(int t=0;t<10000;t++)
            {
                long before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();f.runner.Step();timer.Stop();
                double ms=timer.Elapsed.TotalMilliseconds;total+=ms;window+=ms;windowPieces+=f.stepper.LastStep.Intervals;
                bytes+=GC.GetAllocatedBytesForCurrentThread()-before;worst=Math.Max(worst,ms);
                if(ms>30)TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,
                    "slow tick={0} ms={1:F3} pieces={2}",t+1,ms,f.stepper.LastStep.Intervals));
                if(ms>60)
                for(int a=0;a<30;a++)
                {
                    var d=f.stepper.World.GetRibbonDiagnostics(a);
                    if(d.LastDiskQueries<200&&f.stepper.World.GetCourseEdgeResponses(a)==0)continue;
                    var b=f.stepper.World.GetActor(a);
                    TestContext.WriteLine($"slow_actor={a} loop={b.Observation.LoopId} pose={b.Pose.X:R},{b.Pose.Z:R},{b.Pose.HeadingRadians:R} disk_queries={d.LastDiskQueries} edge_responses={f.stepper.World.GetCourseEdgeResponses(a)}");
                }
                if(t%200==199)
                {TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,"window={0}-{1} mean_ms={2:F3} worst_ms={3:F3} pieces={4} bytes={5} player_lap={6} bouts={7}",t-198,t+1,window/200,worst,windowPieces,bytes,f.stepper.World.GetCatalog(0).Get(f.stepper.World.GetCursor(0).OccurrenceIndex).LapIndex,f.runner.Field.GetAthlete(0).Lifecycle.CompletedBouts));window=worst=0;windowPieces=0;}
                if(f.runner.Field.GetAthlete(0).Lifecycle.CompletedBouts>=2)break;
            }
            TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,"ticks={0} mean_ms={1:F3} bytes={2}",f.runner.Field.Tick.Value,total/f.runner.Field.Tick.Value,bytes));
            Assert.That(f.runner.Field.GetAthlete(0).Lifecycle.CompletedBouts,Is.GreaterThanOrEqualTo(2));
        }
        [Test, Explicit("Bounded CPU diagnostic; timings are evidence, not portable pass/fail thresholds.")]
        public void FirstBoutAndThirtySecondsOfDepartureCpuProfile()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            var timer=new Stopwatch();double total=0,worst=0;long worstTick=0,first=0;
            int setup=0;double setupMs=0;long bytes=0;
            for(int t=0;t<12000;t++)
            {
                long before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();f.runner.Step();timer.Stop();
                double ms=timer.Elapsed.TotalMilliseconds;total+=ms;bytes+=GC.GetAllocatedBytesForCurrentThread()-before;
                if(ms>worst){worst=ms;worstTick=f.runner.Field.Tick.Value;}
                int ready=0,complete=0;
                for(int a=0;a<30;a++)
                {
                    var actor=f.runner.Field.GetAthlete(a);
                    if(actor.ActiveShooting?.Phase==ShootingPhase.Setup)ready++;
                    if(actor.Lifecycle.CompletedBouts>0)complete++;
                }
                if(ready>0){setup++;setupMs+=ms;}
                if(first==0&&f.runner.Field.GetAthlete(0).Lifecycle.CompletedBouts>0)first=f.runner.Field.Tick.Value;
                if(t%200==199||ms>100)
                    TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,"tick={0} tick_ms={1:F3} cumulative_ms={2:F1} intervals={3} setting_up={4} first_bouts={5} bytes={6}",
                        f.runner.Field.Tick.Value,ms,total,f.stepper.LastStep.Intervals,ready,complete,bytes));
                if(ms>15)
                for(int a=0;a<30;a++)
                {
                    var d=f.stepper.World.GetRibbonDiagnostics(a);
                    if(d.LastDiskQueries<200)continue;
                    var b=f.stepper.World.GetActor(a);
                    TestContext.WriteLine($"actor={a} loop={b.Observation.LoopId} pose={b.Pose.X:R},{b.Pose.Z:R},{b.Pose.HeadingRadians:R} clearance_samples={d.LastClearanceSamples} disk_queries={d.LastDiskQueries} start_clearance={d.LastStartClearance:R} edge_responses={f.stepper.World.GetCourseEdgeResponses(a)}");
                }
                if(first>0&&f.runner.Field.Tick.Value>=first+600&&complete==30)break;
            }
            TestContext.WriteLine($"ticks={f.runner.Field.Tick.Value} mean_ms={total/f.runner.Field.Tick.Value:F3} worst_ms={worst:F3} worst_tick={worstTick} setup_mean_ms={setupMs/Math.Max(1,setup):F3} allocated_bytes={bytes}");
            var player=f.runner.Field.GetAthlete(0);var playerBody=f.stepper.World.GetActor(0);
            TestContext.WriteLine($"player_phase={player.Lifecycle.Phase} bouts={player.Lifecycle.CompletedBouts} shooting={player.ActiveShooting?.Phase} needs_bolt={player.ActiveShooting?.NeedsBoltCycle} input_manual={f.runner.GetLastInput(0).ManualBoltCycle} loop={playerBody.Observation.LoopId} pose={playerBody.Pose.X:R},{playerBody.Pose.Z:R} speed={player.Motion.SpeedMetersPerSecond:R}");
            Assert.That(first,Is.GreaterThan(0));
            Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
        }
    }
}
