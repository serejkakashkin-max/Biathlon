using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class OwnerRangeWorldRejectionTests
    {
        [Test]
        public void FoldedMatEdgeClearanceChangesContinuouslyDuringTheNormalProbe()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);var c=f.stepper.World.GetCatalog(29);
            var pose=new ManualSkiHeadingState(-38.294496537,-2.85639182,1.114309025,-1.136177026);
            Assert.That(c.TryProject(pose,3,1169000000,1,out var at),Is.True);
            var sweep=new ManualRouteRibbonSweep(c.ProjectionGeometry,f.stepper.BodyDefinition,ManualScopedFieldStepperTests.Work(),
                ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            var still=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.stepper.BodyDefinition,20).PreviewTrajectory(new ManualSkiContactBody(f.runner.Field.GetAthlete(29).AthleteId,pose,default,75),new SkiContactMotion(default,0),0,0,.05));
            sweep.Preview(still,c.GetGeometryObservation(at));
            var method=typeof(ManualRouteRibbonSweep).GetMethod("CurrentRibbonClearance",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            double Clear(ManualSkiHeadingState p)=>(double)method.Invoke(sweep,new object[]{p});
            foreach(var p in new[]{pose,new ManualSkiHeadingState(-38.2954243,-2.842236522,1.098574566)})
            {
                double value=Clear(p),yaw=(Clear(new ManualSkiHeadingState(p.X,p.Z,p.HeadingRadians+.001))-Clear(new ManualSkiHeadingState(p.X,p.Z,p.HeadingRadians-.001)))/.002;
                TestContext.WriteLine($"pose={p.HeadingRadians:R} clearance={value:R} angular_gradient={yaw:R}");
            }
            double maximum=0;
            for(int axis=0;axis<2;axis++)for(int k=-400;k<400;k++)
            {
                double x=axis==0?k*.001:0,z=axis==1?k*.001:0;
                var a=new ManualSkiHeadingState(pose.X+x,pose.Z+z,pose.HeadingRadians);
                var b=new ManualSkiHeadingState(a.X+(axis==0?.001:0),a.Z+(axis==1?.001:0),a.HeadingRadians);
                maximum=Math.Max(maximum,Math.Abs(Clear(a)-Clear(b)));
            }
            TestContext.WriteLine("maximum_gap_per_mm="+maximum);
            Assert.That(maximum,Is.LessThanOrEqualTo(.001001),"A finite-difference contact normal must use a continuous clearance field.");
        }
        [TestCase(1000,0,Category="OwnerFastArrival")]
        [TestCase(650,500,Category="OwnerDelayedArrival")]
        public void PlayerMassArrivalKeepsFinalSetupAndOtherActorsOnTheSharedClock(int effort,int waitingTicks)
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            var id=f.runner.Field.GetAthlete(0).AthleteId;bool aiming=false;long firstAim=0;
            var watch=new System.Diagnostics.Stopwatch();double totalMs=0,worstMs=0;int steps=0,edgeResponses=0;
            for(int t=0;t<6500;t++)
            {
                var field=f.runner.Field;var tick=field.Tick.Next();var player=field.GetAthlete(0);
#if !UNITY_EDITOR
                if(t%100==0)System.IO.File.WriteAllText(System.IO.Path.Combine(System.IO.Path.GetTempPath(),"biathlon-owner-arrival-progress.txt"),"tick="+t+" player_phase="+player.ActiveShooting?.Phase+" player_route="+player.RouteNanometers+" responses="+f.stepper.World.CourseEdgeResponses);
#endif
                var aim=new WeaponAimCommand(tick,id,0,0);
                bool fire=player.ActiveShooting?.Phase==ShootingPhase.Aiming;
                var input=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,t<waitingTicks?0:effort),1,1,0),aim,fire);
                try{watch.Restart();f.runner.StepWithManualPlayer(0,input,0,true);watch.Stop();double ms=watch.Elapsed.TotalMilliseconds;
                    totalMs+=ms;worstMs=Math.Max(worstMs,ms);steps++;edgeResponses+=f.stepper.World.CourseEdgeResponses;}
                catch(Exception e)
                {
                    var failure=f.stepper.LastStep.WorldAttempt;
                    var edge=f.stepper.World;var threat=edge.LastCourseThreatenedPose;
                    TestContext.WriteLine($"normal={edge.LastCourseNormalX:R},{edge.LastCourseNormalZ:R} lever={edge.LastCourseAngularLever:R} threat={threat.X:R},{threat.Z:R},{threat.HeadingRadians:R}");
                    for(int i=0;i<f.stepper.World.Count;i++)
                    {
                        var a=f.stepper.World.GetActor(i);if(!a.Athlete.Equals(failure.Athlete))continue;
                        var c=f.stepper.World.GetCursor(i);var d=f.stepper.World.GetRibbonDiagnostics(i);
                        TestContext.WriteLine($"failed_actor={i} occurrence={c.OccurrenceIndex} arc={c.ObservedArcNanometers} pose={a.Pose.X:R},{a.Pose.Z:R},{a.Pose.HeadingRadians:R},{a.Pose.YawRateRadiansPerSecond:R} velocity={a.Velocity.X:R},{a.Velocity.Z:R} loop={a.Observation.LoopId} start_clearance={d.LastStartClearance:R} minimum_clearance={d.LastMinimumSampledClearance:R} samples={d.LastClearanceSamples} disks={d.LastDiskQueries}");
                    }
                    Assert.Fail("tick="+tick.Value+" "+ManualScopedFieldStepperTests.Explain(f.stepper)+" "+e);
                }
                if(player.ActiveShooting?.Phase==ShootingPhase.Aiming&&!aiming){aiming=true;firstAim=field.Tick.Value;}
                if(aiming&&field.Tick.Value>firstAim+300&&player.Lifecycle.CompletedBouts>0)break;
            }
            TestContext.WriteLine("first_aim="+firstAim);
            TestContext.WriteLine($"steps={steps} mean_step_ms={totalMs/steps:R} worst_step_ms={worstMs:R} total_edge_responses={edgeResponses}");
            Assert.That(aiming,Is.True,"Reach the actual final setup/aim transition.");
            Assert.That(f.runner.IsFaulted,Is.False);Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
        }
        [Test] public void ProductionApronCommandsReplayThroughTheFirstBoutWithoutAi()
        {
            var source=ManualProductionFieldTests.Initial("sprint",1);var recorded=ManualProductionFieldTests.Initial("sprint",1);
            var tape=new Biathlon.Replay.ManualScopedRaceCommandTape("owner_range_apron_replay_v1",recorded.stepper,10000);
            var input=new SprintAthleteInput[1];var yaw=new int[1];var brake=new double[1];
            int afterBout=0;
            for(int t=0;t<8000&&afterBout<60;t++)
            {
                source.runner.Step();input[0]=source.runner.GetLastInput(0);yaw[0]=source.runner.GetLastManualSteering(0);brake[0]=source.runner.GetLastManualBrake(0);
                tape.StepAndRecord(recorded.stepper,input,yaw,brake);
                if(source.runner.Field.GetAthlete(0).Lifecycle.CompletedBouts>0)afterBout++;
            }
            Assert.That(afterBout,Is.EqualTo(60));
            var replay=ManualProductionFieldTests.Initial("sprint",1);
            var cursor=Biathlon.Replay.ManualScopedRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(replay.stepper);
            while(cursor.HasNext)cursor.Step();
            foreach(var candidate in new[]{recorded,replay})
            {
                Assert.That(candidate.stepper.World.Tick,Is.EqualTo(source.stepper.World.Tick));
                Assert.That(candidate.stepper.World.GetActor(0).Pose,Is.EqualTo(source.stepper.World.GetActor(0).Pose));
                Assert.That(candidate.stepper.World.GetActor(0).Velocity,Is.EqualTo(source.stepper.World.GetActor(0).Velocity));
                Assert.That(candidate.runner.Field.GetAthlete(0).PhysiologyState,Is.EqualTo(source.runner.Field.GetAthlete(0).PhysiologyState));
            }
            // The source runner also logs its AI planning decisions. Playback
            // owns only admitted commands and their resulting race events.
            Assert.That(replay.events.Count,Is.EqualTo(recorded.events.Count));
            for(int i=0;i<recorded.events.Count;i++)Assert.That(replay.events[i],Is.EqualTo(recorded.events[i]));
            Assert.That(replay.runner.TacticalDecisions,Is.Zero);
            TestContext.WriteLine("production_apron_signed_replay_ticks="+tape.TickCount);
        }
    }
}
