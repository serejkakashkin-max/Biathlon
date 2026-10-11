using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Replay;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class OwnerCornerProbeTests
    {
        [TestCase("mass_start",6)] [TestCase("sprint",3)]
        public void SeparateStraightStartMergesDeterministicallyWithoutMovingTheLapLine(string format,int count)
        {
            var a=ManualProductionFieldTests.Initial(format,count);
            var b=ManualProductionFieldTests.Initial(format,count);
            var tape=new ManualScopedRaceCommandTape("separate_start_"+format,b.stepper,count*1500);
            var inputs=new SprintAthleteInput[count];var yaw=new int[count];var brake=new double[count];
            var first=a.stepper.World.GetActor(0);
            Assert.That(first.Observation.LoopId,Is.EqualTo("start_approach"));
            Assert.That(first.Observation.PhysicalArcNanometers/1e9,Is.EqualTo(35).Within(.001));
            for(int t=0;t<1500;t++)
            {
                a.runner.Step();
                for(int i=0;i<count;i++){inputs[i]=a.runner.GetLastInput(i);yaw[i]=a.runner.GetLastManualSteering(i);brake[i]=a.runner.GetLastManualBrake(i);}
                tape.StepAndRecord(b.stepper,inputs,yaw,brake);
            }
            Assert.That(a.stepper.World.GetActor(0).Observation.LoopId,Is.EqualTo("main"));
            // AI also emits tactical/overtake decisions. The tape replays accepted
            // controls and physical events without rerunning those decisions.
            for(int i=0;i<count;i++)
            {
                Assert.That(a.runner.Field.GetAthlete(i).Motion,Is.EqualTo(b.runner.Field.GetAthlete(i).Motion));
                Assert.That(a.stepper.World.GetActor(i).Pose,Is.EqualTo(b.stepper.World.GetActor(i).Pose));
            }
            var replay=ManualProductionFieldTests.Initial(format,count);
            var cursor=ManualScopedRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(replay.stepper);
            while(cursor.HasNext)cursor.Step();
            Assert.That(replay.events.Count,Is.EqualTo(b.events.Count));
            for(int e=0;e<b.events.Count;e++)Assert.That(replay.events[e],Is.EqualTo(b.events[e]));
            for(int i=0;i<count;i++)Assert.That(replay.stepper.World.GetActor(i).Pose,Is.EqualTo(a.stepper.World.GetActor(i).Pose));
        }

        [Test] public void MassStartFirstLapEmitsControlAndWorkTrace()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            string path=Environment.GetEnvironmentVariable("BIATHLON_CORNER_TRACE");
            using(var csv=string.IsNullOrEmpty(path)?null:new StreamWriter(path))
            {
                csv?.WriteLine("tick,loop,arc_m,speed,brake,tuck,effort,intervals,sim_ms");
                var timer=new Stopwatch();double sum=0,max=0;int n=0;
                for(int t=0;t<4800;t++)
                {
                    timer.Restart();f.runner.Step();timer.Stop();
                    if(t>100){sum+=timer.Elapsed.TotalMilliseconds;max=Math.Max(max,timer.Elapsed.TotalMilliseconds);n++;}
                    var a=f.runner.Field.GetAthlete(0);var body=f.stepper.World.GetActor(0);var input=f.runner.GetLastInput(0);
                    csv?.WriteLine(FormattableString.Invariant($"{a.Tick.Value},{body.Observation.LoopId},{body.Observation.PhysicalArcNanometers/(double)SkiState.NanometersPerMeter:R},{a.Motion.SpeedMetersPerSecond:R},{f.runner.GetLastManualBrake(0):R},{input.Ski.TuckRequested},{input.Ski.Effort.RequestedEffort},{f.stepper.LastStep.Intervals},{timer.Elapsed.TotalMilliseconds:R}"));
                }
                TestContext.WriteLine(string.Format(CultureInfo.InvariantCulture,"mean_ms={0:R}; max_ms={1:R}; ticks={2}",sum/n,max,f.runner.Field.Tick.Value));
                Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
                Assert.That(f.runner.Field.GetAthlete(0).RouteNanometers,Is.GreaterThan(500*SkiState.NanometersPerMeter));
                Assert.That(sum/n,Is.LessThan(50),"The whole roster must simulate faster than its real-time 20Hz clock.");
            }
        }
    }
}
