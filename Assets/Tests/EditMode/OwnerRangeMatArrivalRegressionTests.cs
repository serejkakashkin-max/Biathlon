using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class OwnerRangeMatArrivalRegressionTests
    {
        [Test] public void ThirtyAthletesPassTheFirstShootingAndContinueOnOneClock()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);var field=f.runner.Field;
            int completed=0;
            for(int i=0;i<15000&&completed<30;i++)
            {
                f.runner.Step();completed=0;
                for(int a=0;a<field.Count;a++)if(field.GetAthlete(a).Lifecycle.CompletedBouts>0)completed++;
            }
            Assert.That(completed,Is.EqualTo(30));
            long paid=field.Tick.Value;
            for(int i=0;i<300;i++)f.runner.Step();
            Assert.That(field.Tick.Value,Is.EqualTo(paid+300));
            Assert.That(f.stepper.World.Tick,Is.EqualTo(field.Tick));
            Assert.That(f.runner.IsFaulted,Is.False);
            TestContext.WriteLine($"first_bout_all_30_tick={paid} continued_to={field.Tick.Value}");
        }
        [Test] public void AssistedFinalMatTurnAndDepartureHavePracticalDurations()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",1);var field=f.runner.Field;
            long enter=-1,shot=-1,leave=-1,exit=-1;
            for(int i=0;i<10000&&exit<0;i++)
            {
                var a=field.GetAthlete(0);var c=f.stepper.World.GetCursor(0);var row=f.stepper.World.GetCatalog(0).Get(c.OccurrenceIndex);
                if(row.End==ManualRacePathEnd.FiringLine&&enter<0)enter=field.Tick.Value;
                if(a.ActiveShooting!=null&&shot<0)shot=field.Tick.Value;
                if(a.CurrentRangeMotion?.Phase==RangeTraversalPhase.RangeExit&&leave<0)leave=field.Tick.Value;
                if(leave>=0)
                {
                    f.stepper.World.GetCatalog(0).TrySample(row.Index,row.LengthNanometers,out var end);
                    var pose=f.stepper.World.GetActor(0).Pose;
                    double dot=Math.Sin(pose.HeadingRadians)*end.ForwardX+Math.Cos(pose.HeadingRadians)*end.ForwardZ;
                    if(row.LoopId!=a.RangePath.MatAnchor.LoopId||c.ObservedArcNanometers>=SkiState.NanometersPerMeter&&dot>=Math.Cos(12*Math.PI/180))
                    {exit=field.Tick.Value;break;}
                }
                var tick=field.Tick.Next();var id=a.AthleteId;
                f.runner.StepWithManualPlayer(0,new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,
                    leave>=0?800:650),1,1,0),new WeaponAimCommand(tick,id,0,0),a.ActiveShooting?.Phase==ShootingPhase.Aiming),0,true);
            }
            TestContext.WriteLine($"final_mat_seconds={(shot-enter)/20d} exit_turn_seconds={(exit-leave)/20d} ticks={enter},{shot},{leave},{exit}");
            Assert.That(enter,Is.GreaterThan(0));Assert.That(shot,Is.GreaterThan(enter));Assert.That(exit,Is.GreaterThan(leave));
            Assert.That((shot-enter)/20d,Is.LessThan(8),"The last mat turn must not crawl for tens of seconds.");
            Assert.That((exit-leave)/20d,Is.LessThan(8),"The physical two-turn departure must not crawl for tens of seconds.");
        }
        [Test] public void ActualOwnerAi26ArrivalPlantsWithoutRejectingTheWholeRaceTick()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            var c=f.stepper.World.GetCatalog(26);int occurrence=c.GetFiringOccurrence(0);var row=c.Get(occurrence);
            var velocity=new SkiPlanarVelocity(.434291228050536,-.236032303891619);
            const double yaw=.136254653,seconds=.05;
            var pose=new ManualSkiHeadingState(-34.732826265-velocity.X*seconds,2.415833664-velocity.Z*seconds,1.878826605-yaw*seconds,yaw);
            Assert.That(c.TryProject(pose,occurrence,row.LengthNanometers,2,out var at),Is.True);
            var id=new AthleteId("owner_ai26_mat_arrival");
            var cursor=new ManualRacePathProgression(c).Reconstitute(id,occurrence,at.PhysicalArcNanometers,at.PhysicalArcNanometers,at.MappedRouteNanometers);
            var world=new ManualScopedWorldRoster(new[]{c},f.stepper.BodyDefinition,ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),
                new[]{cursor},new[]{pose},new[]{velocity},new[]{new SkiState(0,Math.Sqrt(velocity.X*velocity.X+velocity.Z*velocity.Z))},20,
                responseDefinition:ManualScopedFieldStepperTests.Group(),actorMasses:new[]{75d},
                matCapture:ManualMatCaptureDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_mat_capture_v1.json")),
                numericalTolerance:ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json")));
            // The player log contains the accepted final pose, not the complete
            // force intervals. Exercise the exact post-admission plant decision.
            var reached=new ManualSkiHeadingState(-34.732826265,2.415833664,1.878826605,yaw);
            var flags=System.Reflection.BindingFlags.NonPublic|System.Reflection.BindingFlags.Instance;
            bool eligible=(bool)typeof(ManualScopedWorldRoster).GetMethod("IsWithinMatPlantArea",flags)
                .Invoke(world,new object[]{c,row,reached});
            Assert.That(eligible,Is.True,"An admitted folded endpoint cannot require a different centreline plane before planting.");
            object[] args={0,c,row,reached,velocity};
            Assert.That((bool)typeof(ManualScopedWorldRoster).GetMethod("TryCaptureMat",flags).Invoke(world,args),Is.True);
            var final=new ManualRacePathActorFrame(id,(ManualSkiHeadingState)args[3],(SkiPlanarVelocity)args[4],at);
            Assert.That(final.Velocity,Is.EqualTo(default(SkiPlanarVelocity)));
            Assert.That(final.Pose.YawRateRadiansPerSecond,Is.Zero);
            Assert.That(final.Pose.X,Is.EqualTo(reached.X));Assert.That(final.Pose.Z,Is.EqualTo(reached.Z));
            Assert.That(world.MatCaptures,Is.EqualTo(1));
        }
    }
}
