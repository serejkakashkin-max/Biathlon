using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualMatBrakingTests
    {
        private static string Json=>PhysiologyContractTests.DefinitionJson("manual_mat_braking_v1.json");
        [Test] public void TerminalPolicyIsExplicitVersionedData()
        {
            var policy=ManualMatBrakingDefinition.FromJson(Json);
            Assert.That(policy.SchemaVersion,Is.EqualTo(1));Assert.That(policy.Id,Is.EqualTo("manual_mat_braking_v1"));
            Assert.That(ManualMatBrakingDefinition.FromJson(policy.ToJson()).TerminalDistanceMeters,Is.EqualTo(policy.TerminalDistanceMeters));
            var f=new ManualScopedFieldStepperTests.Fixture();
            Assert.That(f.Stepper.ContextJson,Does.Contain("terminal_mat_braking_not_selected"));
            Assert.That(ManualScopedFieldStepperTests.InitialCandidate(3).stepper.ContextJson,Does.Contain("manual_mat_braking_v1"));
        }
        [TestCase("0")][TestCase("-1")][TestCase("3.01")]
        public void InvalidTerminalStripIsRejected(string distance)
            =>Assert.Catch(()=>ManualMatBrakingDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Json,"terminal_braking_distance_m",distance)));
        [Test] public void MissingSchemaOrStableIdentityIsRejected()
        {
            Assert.Catch(()=>ManualMatBrakingDefinition.FromJson(Json.Replace("\"schema_version\": 1","\"schema_version\": 2")));
            Assert.Catch(()=>ManualMatBrakingDefinition.FromJson(Json.Replace("manual_mat_braking_v1","")));
        }
        [TestCase(5.43772393,.004111363)]
        [TestCase(5.575993686,.014796703)]
        public void FeasibleNeutralMinimumCannotLoseItsExactNullSpaceToEnergyRoundoff(double heading,double speed)
        {
            var f=new ManualScopedFieldStepperTests.Fixture();var body=f.Source.Source.Venue.Occupancy;
            var pose=new ManualSkiHeadingState(3,5,heading);
            var velocity=new SkiPlanarVelocity(speed*Math.Sin(pose.HeadingRadians),speed*Math.Cos(pose.HeadingRadians));
            var actor=new ManualSkiContactBody(new AthleteId("roundoff_witness"),pose,velocity,75);
            var snow=new ManualSkiSteeredSnowSystem(body,ManualScopedFieldStepperTests.Steering(),20)
                .Preview(actor,new SkiContactMotion(velocity,0),0,4,.05).Snow;
            Assert.That(snow.HasExactNeutralConstraint,Is.True);
            Assert.That(snow.Motion.YawRate,Is.Zero);
            Assert.That(snow.DissipatedEnergyJoules,Is.GreaterThanOrEqualTo(0));
            var ice=new ManualSkiSteeredSnowSystem(body,ManualScopedFieldStepperTests.Steering(),20)
                .Preview(actor,new SkiContactMotion(velocity,1),0,0,.05).Snow;
            Assert.That(ice.HasExactNeutralConstraint,Is.False);
            Assert.That(ice.Motion.YawRate,Is.EqualTo(1),"A receipt must never erase unpaid spin.");
        }
        [TestCase(0,43.503951769,-.842090308,5.575993686,-.009613438781061018,.011248297473418782)]
        [TestCase(2,37.530411878,-.571351258,5.43772393,-.003076438922499671,.0027274206578918707)]
        public void ActualMatWitnessPaysTheWholePhysicalStopBeforeFiringAdmission(int index,double x,double z,double heading,double vx,double vz)
        {
            // This captured sub-millimetre stop tests the exact boundary law.
            // Production's bounded mat-admission tolerance already owns this
            // endpoint, so it cannot exercise the original near-double root.
            var f=ManualScopedFieldStepperTests.InitialCandidate(3,exactMatBoundary:true);var catalog=f.stepper.World.GetCatalog(index);
            int occurrence=catalog.GetFiringOccurrence(0);var row=catalog.Get(occurrence);
            var pose=new ManualSkiHeadingState(x,z,heading);var velocity=new SkiPlanarVelocity(vx,vz);
            Assert.That(catalog.TryProject(pose,occurrence,row.LengthNanometers,1,out var at),Is.True);
            Assert.That(catalog.TrySample(occurrence,row.LengthNanometers,out var goal),Is.True);
            double distance=(goal.X-x)*goal.ForwardX+(goal.Z-z)*goal.ForwardZ;
            double speed=vx*goal.ForwardX+vz*goal.ForwardZ;
            double after=2*distance/speed;
            Assert.That(after,Is.InRange(.001,.05));
            var drive=new ManualSkiControlledDriveSystem(f.stepper.PhysicsDefinition,f.stepper.BodyDefinition,
                ManualScopedFieldStepperTests.Steering(),f.stepper.CorneringDefinition,20);
            var body=new ManualSkiContactBody(f.stepper.World.GetActor(index).Athlete,pose,velocity,75);
            var result=drive.PreviewStopping(body,new SkiContactMotion(velocity,0),at,catalog.SampleSurface(at),
                new ManualSkiMotionInput(0,0,100,100),after,after);
            Assert.That(result.WithinBrakingBudget,Is.True);Assert.That(result.BrakeFraction,Is.InRange(0,1));
            var trajectory=new ManualSkiWorldTrajectory(result.Frame.Drive);
            Assert.That(trajectory.VelocityAt(1).X,Is.Zero);Assert.That(trajectory.VelocityAt(1).Z,Is.Zero);
            Assert.That(trajectory.Sample(1).YawRateRadiansPerSecond,Is.Zero);
            Assert.That(trajectory.VelocityAt(.5).X*trajectory.VelocityAt(.5).X+
                trajectory.VelocityAt(.5).Z*trajectory.VelocityAt(.5).Z,Is.GreaterThan(0));
            var window=new ManualScopedWorldWindow(body.Athlete,occurrence,at.PhysicalArcNanometers,1,row.LengthNanometers);
            var boundary=new ManualRacePathBoundaryScheduler(catalog,ManualScopedFieldStepperTests.Work()).Preview(trajectory,window);
            Assert.That(boundary.Status,Is.EqualTo(ManualRacePathBoundaryStatus.Boundary));
            Assert.That(boundary.Seconds,Is.EqualTo(after),"Near-double roots cannot truncate the actual paid stop.");
            Assert.That(boundary.Admission.Status,Is.EqualTo(ManualRacePathGateStatus.Clear));
        }
    }
}
