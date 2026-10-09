using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiControlledDriveTests
    {
        private static OccupancyDefinition Occupancy()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static ManualSkiSteeringDefinition Steering()=>ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_steering_v2.json"));
        private static ManualSkiContactBody Actor(double forward=6,double side=0,double yaw=0,double heading=0,double mass=75)
        {
            var pose=new ManualSkiHeadingState(3,5,heading,yaw);double h=pose.HeadingRadians;
            return new ManualSkiContactBody(new AthleteId("controlled"),pose,
                new SkiPlanarVelocity(forward*Math.Sin(h)+side*Math.Cos(h),forward*Math.Cos(h)-side*Math.Sin(h)),mass);
        }
        private static SkiContactMotion Motion(ManualSkiContactBody b)=>new SkiContactMotion(b.Velocity,b.Pose.YawRateRadiansPerSecond);
        private static double Inertia(){var b=Occupancy();return (b.LongitudinalSeparation*b.LongitudinalSeparation+b.LateralSeparation*b.LateralSeparation)/12;}
        private static double Energy(SkiContactMotion m)=>.5*(m.Velocity.X*m.Velocity.X+m.Velocity.Z*m.Velocity.Z+Inertia()*m.YawRate*m.YawRate);
        private static ManualSkiSteeredSnowSystem Snow()=>new ManualSkiSteeredSnowSystem(Occupancy(),Steering(),20);

        [Test] public void RequestMatchesExistingHeadingLimitsWithoutRejectingPhysicalSpin()
        {
            var system=new ManualSkiSteeringSystem(Steering(),20);
            foreach(int input in new[]{-1000,-431,0,431,1000})foreach(double speed in new[]{0.0,6,20})
            foreach(double grip in new[]{0.0,.1,4})foreach(double posture in new[]{0.0,.65,1})
            {
                var request=system.GetRequest(input,speed,grip,posture);
                var existing=system.Preview(new ManualSkiHeadingState(0,0,0),input,speed*.05,speed,grip,posture);
                Assert.That(request.RequestedYawRate,Is.EqualTo(existing.RequestedYawRate));
                Assert.That(request.TargetYawRate,Is.EqualTo(existing.TargetYawRate));
                Assert.That(request.YawRateLimit,Is.EqualTo(existing.YawRateLimit));
                Assert.That(request.AngularAccelerationLimit,Is.GreaterThanOrEqualTo(0));
            }
            var raw=Actor(yaw:8);var result=Snow().Preview(raw,Motion(raw),1000,4,.05);
            Assert.That(result.Snow.Motion.YawRate,Is.GreaterThan(Steering().MaximumYawRate));
        }
        [Test] public void SignedMovingSteeringUsesSnowForceAndRetainsContinuousSlip()
        {
            var body=Actor();var right=Snow().Preview(body,Motion(body),1000,4,.05);
            var left=Snow().Preview(body,Motion(body),-1000,4,.05);
            Assert.That(right.Snow.Motion.Velocity.X,Is.GreaterThan(0));Assert.That(right.Snow.Motion.YawRate,Is.GreaterThan(0));
            Assert.That(left.Snow.Motion.Velocity.X,Is.EqualTo(-right.Snow.Motion.Velocity.X));
            Assert.That(left.Snow.Motion.YawRate,Is.EqualTo(-right.Snow.Motion.YawRate));
            Assert.That(left.Snow.Motion.Velocity.Z,Is.EqualTo(right.Snow.Motion.Velocity.Z));
            Assert.That(right.Snow.Motion.Velocity.Z,Is.LessThan(6),"Snow turning dissipates travel rather than adding speed.");
            Assert.That(right.Snow.DissipatedEnergyJoules,Is.GreaterThan(0));
            double side=right.Snow.Motion.Velocity.X;
            Assert.That(side,Is.GreaterThan(0),"World velocity remains independent of the current zero heading.");
            Assert.That(body.Pose.HeadingRadians,Is.Zero);Assert.That(body.Velocity.Z,Is.EqualTo(6));
        }
        [Test] public void RestAndZeroGripCannotAcquireAnUnpaidPivotOrLateralVelocity()
        {
            var system=Snow();var rest=Actor(0);
            foreach(int input in new[]{-1000,0,1000})Assert.That(system.Preview(rest,Motion(rest),input,4,.05).Snow.Motion,Is.EqualTo(Motion(rest)));
            var moving=Actor(6,.8,3);var ice=system.Preview(moving,Motion(moving),1000,0,.05);
            Assert.That(ice.Snow.Motion,Is.EqualTo(Motion(moving)));Assert.That(ice.Snow.DissipatedEnergyJoules,Is.Zero);
        }
        [Test] public void NeutralYawDampsLocallyWithoutSeekingAnyCourseTangentOrCentre()
        {
            var b=Actor(6,0,0,.37);var result=Snow().Preview(b,Motion(b),0,4,.05);
            Assert.That(result.Snow.Motion.Velocity.X,Is.EqualTo(b.Velocity.X).Within(2e-15));
            Assert.That(result.Snow.Motion.Velocity.Z,Is.EqualTo(b.Velocity.Z).Within(2e-15));
            Assert.That(result.Snow.Motion.YawRate,Is.EqualTo(0).Within(2e-15));
            var spin=Actor(6,.8,3);var damp=Snow().Preview(spin,Motion(spin),0,4,.05);
            Assert.That(damp.Snow.Motion.YawRate,Is.InRange(3-Steering().NeutralYawDeceleration*.05,3));
            Assert.That(damp.Snow.Motion.Velocity.X,Is.GreaterThan(0),"A single tick cannot erase the contact's side-slip.");
        }
        [Test] public void FrontAndRearShareOneGripBoxAndNeverAddTotalKineticEnergy()
        {
            var system=Snow();int count=0;
            foreach(double heading in new[]{0.0,.7,2.9})foreach(double forward in new[]{-6.0,0,6,25})
            foreach(double side in new[]{-2.0,0,2})foreach(double yaw in new[]{-8.0,-.2,0,.2,8})
            foreach(int input in new[]{-1000,0,1000})foreach(double grip in new[]{0.0,.1,4})
            {
                var b=Actor(forward,side,yaw,heading);var initial=Motion(b);var p=system.Preview(b,initial,input,grip,.05);
                double before=Energy(initial),after=Energy(p.Snow.Motion);
                Assert.That(after,Is.LessThanOrEqualTo(before+1e-10));
                Assert.That((before-after)*75,Is.EqualTo(p.Snow.DissipatedEnergyJoules).Within(2e-8));
                Assert.That(Math.Abs(p.Snow.FrontImpulseNewtonSeconds),Is.LessThanOrEqualTo(75*grip*.05*.5+1e-12));
                Assert.That(Math.Abs(p.Snow.RearImpulseNewtonSeconds),Is.LessThanOrEqualTo(75*grip*.05*.5+1e-12));
                Assert.That(Math.Abs(p.Snow.Motion.YawRate-initial.YawRate),Is.LessThanOrEqualTo(p.Request.AngularAccelerationLimit*.05+2e-15));
                count++;
            }
            Assert.That(count,Is.EqualTo(1620));
        }
        [Test] public void MassScalingAndMirroredReverseTravelKeepTheSamePhysicalAcceleration()
        {
            var system=Snow();var a=Actor(-6,.7,.8,mass:50);var b=Actor(-6,.7,.8,mass:100);var mirror=Actor(-6,-.7,-.8);
            var first=system.Preview(a,Motion(a),431,4,.05);var second=system.Preview(b,Motion(b),431,4,.05);
            var reverse=system.Preview(mirror,Motion(mirror),-431,4,.05);
            Assert.That(first.Snow.Motion,Is.EqualTo(second.Snow.Motion));
            Assert.That(second.Snow.FrontImpulseNewtonSeconds,Is.EqualTo(2*first.Snow.FrontImpulseNewtonSeconds));
            Assert.That(first.FrontDeflectionRadians,Is.LessThan(0));
            Assert.That(reverse.Snow.Motion.Velocity.X,Is.EqualTo(-first.Snow.Motion.Velocity.X));
            Assert.That(reverse.Snow.Motion.Velocity.Z,Is.EqualTo(first.Snow.Motion.Velocity.Z));
            Assert.That(reverse.Snow.Motion.YawRate,Is.EqualTo(-first.Snow.Motion.YawRate));
        }
        [Test] public void ExistingSpeedGripPostureAndAngularResponseTuningIsShared()
        {
            var system=Snow();var a=Actor();var normal=system.Preview(a,Motion(a),1000,4,.05);
            var tuck=system.Preview(a,Motion(a),1000,4,.05,.65);var weak=system.Preview(a,Motion(a),1000,.1,.05);
            Assert.That(tuck.Request.TargetYawRate,Is.LessThan(normal.Request.TargetYawRate));
            Assert.That(tuck.Request.AngularAccelerationLimit,Is.LessThan(normal.Request.AngularAccelerationLimit));
            Assert.That(weak.Request.TargetYawRate,Is.LessThan(normal.Request.TargetYawRate));
            var fast=Actor(25);var high=system.Preview(fast,Motion(fast),1000,4,.05);
            Assert.That(high.Request.YawRateLimit,Is.LessThan(normal.Request.YawRateLimit));
            Assert.That(high.Request.AngularAccelerationLimit,Is.LessThan(normal.Request.AngularAccelerationLimit));
        }
        private sealed class Fixture
        {
            internal readonly ManualRacePathCatalogTests.Fixture Source=new ManualRacePathCatalogTests.Fixture();
            internal readonly ManualSkiControlledDriveSystem System;
            internal readonly ManualRacePathObservation Observation;
            internal readonly TrackSample Surface;
            internal readonly ManualSkiHeadingState Pose;
            internal Fixture()
            {
                Pose=Source.At(1,10*SkiState.NanometersPerMeter);
                Assert.That(Source.Catalog.TryProject(Pose,1,10*SkiState.NanometersPerMeter,0,out var observation),Is.True);
                Observation=observation;Surface=Source.Catalog.SampleSurface(observation);
                System=new ManualSkiControlledDriveSystem(SkiForceTests.Definition(),Source.Venue.Occupancy,Steering(),CorneringTests.Definition(),20);
            }
            internal ManualSkiContactBody Body(double side=0,double yaw=0,double forward=6)
            {
                double h=Pose.HeadingRadians;return new ManualSkiContactBody(new AthleteId("controlled"),new ManualSkiHeadingState(Pose.X,Pose.Z,h,yaw),
                    new SkiPlanarVelocity(forward*Math.Sin(h)+side*Math.Cos(h),forward*Math.Cos(h)-side*Math.Sin(h)),75);
            }
            internal ManualSkiControlledDriveFrame Preview(ManualSkiContactBody body,int steering=1000,double power=350,double seconds=.05)
                =>System.Preview(body,Motion(body),Observation,Surface,new ManualSkiMotionInput(steering,power,100,100),seconds);
        }
        [TestCase(.37)][TestCase(1.57)][TestCase(4.7)]
        public void FullyPaidNeutralStopReachesPhysicalRestAtArbitraryHeading(double heading)
        {
            var f=new Fixture();var b=Actor(.1,.0001,.0001,heading);
            var result=f.System.Preview(b,Motion(b),f.Observation,f.Surface,
                new ManualSkiMotionInput(0,0,100,100,brakeFraction:1),.05);
            Assert.That(result.Drive.FirstDrive.SpeedAt(.05),Is.Zero);
            Assert.That(result.SteeredSnow.AngularResponseScale,Is.EqualTo(1));
            Assert.That(result.Drive.MotionAt(0),Is.EqualTo(Motion(b)),"No initial velocity can be reset.");
            var end=result.Drive.MotionAt(1);
            Assert.That(end.Velocity.X,Is.Zero);Assert.That(end.Velocity.Z,Is.Zero);Assert.That(end.YawRate,Is.Zero);
            Assert.That(result.Drive.State.X!=b.Pose.X||result.Drive.State.Z!=b.Pose.Z,Is.True,"The stop must pay real travel.");
        }
        [Test] public void ALongitudinalStopCannotEraseUnresolvedSideSlipOrSpin()
        {
            var f=new Fixture();var b=Actor(.1,.01,.01,.37);
            var result=f.System.Preview(b,Motion(b),f.Observation,f.Surface,
                new ManualSkiMotionInput(0,0,100,100,gripMultiplier:.000001,brakeFraction:1),.05);
            Assert.That(result.Drive.FirstDrive.SpeedAt(.05),Is.Zero);
            var end=result.Drive.MotionAt(1);
            Assert.That(end.Velocity.X*end.Velocity.X+end.Velocity.Z*end.Velocity.Z,Is.GreaterThan(0));
            Assert.That(end.YawRate,Is.GreaterThan(0));
        }
        [Test] public void InverseStoppingPaysTheExistingForceLawAndPhysicalTravel()
        {
            var f=new Fixture();var b=f.Body(forward:.1);var input=new ManualSkiMotionInput(0,0,100,100);
            var result=f.System.PreviewStopping(b,Motion(b),f.Observation,f.Surface,input,.04,.04);
            Assert.That(result.WithinBrakingBudget,Is.True);Assert.That(result.CoastStopsEarlier,Is.False);
            Assert.That(result.BrakeFraction,Is.GreaterThan(0).And.LessThan(1));
            var actual=result.Frame.Drive;var repeated=f.System.Preview(b,Motion(b),f.Observation,f.Surface,
                input.WithBrakeFraction(result.BrakeFraction),.04).Drive;
            Assert.That(actual.State,Is.EqualTo(repeated.State));Assert.That(actual.MotionAt(1),Is.EqualTo(repeated.MotionAt(1)));
            Assert.That(actual.FirstDrive.DistanceAt(.04),Is.EqualTo(actual.FirstDrive.InitialState.SpeedMetersPerSecond*.02).Within(1e-12));
            Assert.That(actual.FirstDrive.SpeedAt(.04),Is.EqualTo(0).Within(1e-15));
            Assert.That(actual.MotionAt(0),Is.EqualTo(Motion(b)));
        }
        [Test] public void InfeasibleStoppingCannotGrantAStopOrExceedTheSharedCap()
        {
            var f=new Fixture();var b=f.Body(forward:6);var input=new ManualSkiMotionInput(0,0,100,100);
            var result=f.System.PreviewStopping(b,Motion(b),f.Observation,f.Surface,input,.01,.01);
            Assert.That(result.WithinBrakingBudget,Is.False);Assert.That(result.BrakeFraction,Is.EqualTo(1));
            Assert.That(result.Frame.Drive.FirstDrive.SpeedAt(.01),Is.GreaterThan(5));
            Assert.That(result.Frame.Drive.MotionAt(0),Is.EqualTo(Motion(b)));
        }
        [Test] public void InverseStoppingCannotUndoAutomaticCornerBrakingToDelayAStop()
        {
            var f=new Fixture();var b=f.Body(forward:6,yaw:2);var input=new ManualSkiMotionInput(0,0,100,100);
            var baseline=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
            Assert.That(baseline.Cornering.BrakingNewtons,Is.GreaterThan(0));
            var result=f.System.PreviewStopping(b,Motion(b),f.Observation,f.Surface,input,.05,6);
            Assert.That(result.WithinBrakingBudget,Is.False);Assert.That(result.CoastStopsEarlier,Is.True);
            Assert.That(result.Frame.Drive.FirstDrive.Output.BrakingNewtons,Is.EqualTo(baseline.Drive.FirstDrive.Output.BrakingNewtons));
            Assert.That(result.Frame.Drive.MotionAt(0),Is.EqualTo(Motion(b)));
        }
        [Test] public void WarmInverseStoppingQueriesAreDeterministicWithoutAllocations()
        {
            var f=new Fixture();var b=f.Body(forward:.1);var input=new ManualSkiMotionInput(0,0,100,100);
            var motion=Motion(b);var reference=f.System.PreviewStopping(b,motion,f.Observation,f.Surface,input,.04,.04);
            for(int i=0;i<128;i++)f.System.PreviewStopping(b,motion,f.Observation,f.Surface,input,.04,.04);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)
            {
                var result=f.System.PreviewStopping(b,motion,f.Observation,f.Surface,input,.04,.04);
                all&=result.WithinBrakingBudget&&result.BrakeFraction==reference.BrakeFraction&&
                    result.Frame.Drive.State.X==reference.Frame.Drive.State.X&&result.Frame.Drive.State.Z==reference.Frame.Drive.State.Z;
            }
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);
        }
        [Test] public void StoppingIntentCannotCompeteWithPoweredOrSignedSteeringInput()
        {
            var f=new Fixture();var b=f.Body();
            foreach(var input in new[]{new ManualSkiMotionInput(1,0,100,100),new ManualSkiMotionInput(0,1,100,100),
                new ManualSkiMotionInput(0,0,100,100,brakeFraction:.1)})
                Assert.Throws<ArgumentException>(()=>f.System.PreviewStopping(b,Motion(b),f.Observation,f.Surface,input,.04,.04));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.System.PreviewStopping(b,Motion(b),f.Observation,f.Surface,
                new ManualSkiMotionInput(0,0,100,100),.04,.03));
        }
        [Test] public void SharedDriveRetainsItsRawSourceForceMeaningAndFrozenPrefix()
        {
            var f=new Fixture();var b=f.Body(.8,3);var result=f.Preview(b);var root=new ManualSkiWorldTrajectory(result.Drive);
            Assert.That(root.Sample(0),Is.EqualTo(b.Pose));Assert.That(root.VelocityAt(0),Is.EqualTo(b.Velocity));
            Assert.That(result.Drive.InitialMotion.YawRate,Is.EqualTo(b.Pose.YawRateRadiansPerSecond));
            Assert.That(result.Drive.Snow,Is.EqualTo(result.SteeredSnow.Snow));
            Assert.That(result.Drive.FirstDrive.Output.PropulsiveNewtons,Is.GreaterThan(0));
            var prefix=root.Slice(0,.02);var suffix=root.Slice(.02,.05);
            Assert.That(prefix.Sample(1),Is.EqualTo(suffix.Sample(0)));Assert.That(prefix.VelocityAt(1),Is.EqualTo(suffix.VelocityAt(0)));
            Assert.That(suffix.Sample(1),Is.EqualTo(root.Sample(1)));Assert.That(root.PlanarTravelDistanceAt(1),Is.GreaterThan(0));
        }
        [Test] public void NeutralAlignedDriveKeepsLegacySharedForcesBitForBit()
        {
            var f=new Fixture();var b=f.Body();var controlled=f.Preview(b,0);
            var original=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Source.Venue.Occupancy,20).Preview(b,Motion(b),
                f.Observation,f.Surface,350,controlled.Cornering.LateralAccelerationLimit,0,.05);
            Assert.That(controlled.Drive.State,Is.EqualTo(original.State));
            Assert.That(controlled.Drive.MotionAt(1),Is.EqualTo(original.MotionAt(1)));
            Assert.That(controlled.Drive.FirstDrive.Output,Is.EqualTo(original.FirstDrive.Output));
        }
        [Test] public void RequestedHighSpeedTurnPaysBrakingBeforeGripLimitedYawCanBuild()
        {
            var f=new Fixture();var b=f.Body(forward:14);
            var neutral=f.Preview(b,0,power:0);var right=f.Preview(b,1000,power:0);var left=f.Preview(b,-1000,power:0);
            Assert.That(neutral.Cornering.BrakingNewtons,Is.Zero);
            Assert.That(right.Cornering.BrakingNewtons,Is.GreaterThan(0));
            Assert.That(left.Cornering.BrakingNewtons,Is.EqualTo(right.Cornering.BrakingNewtons));
            Assert.That(right.Drive.FirstDrive.Output.State.SpeedMetersPerSecond,
                Is.LessThan(neutral.Drive.FirstDrive.Output.State.SpeedMetersPerSecond));
            Assert.That(right.Drive.Sample(0),Is.EqualTo(b.Pose));
            Assert.That(right.Drive.MotionAt(0),Is.EqualTo(Motion(b)));
        }
        [Test] public void ExplicitBrakeRequestUsesSharedForceAndDoesNotResetSourceMotion()
        {
            var f=new Fixture();var b=f.Body();var input=new ManualSkiMotionInput(0,0,100,100);
            var coast=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
            var brake=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input.WithBrakeFraction(1),.05);
            Assert.That(brake.Drive.Sample(0),Is.EqualTo(b.Pose));
            Assert.That(brake.Drive.MotionAt(0),Is.EqualTo(Motion(b)));
            Assert.That(brake.Drive.FirstDrive.Output.State.SpeedMetersPerSecond,
                Is.LessThan(coast.Drive.FirstDrive.Output.State.SpeedMetersPerSecond));
            Assert.That(brake.Drive.FirstDrive.Acceleration,Is.EqualTo(coast.Drive.FirstDrive.Acceleration-CorneringTests.Definition().MaximumBraking).Within(1e-12));
        }
        [Test] public void BrakeRequestIsBoundedAndPreservesAllOtherPreparedControls()
        {
            var input=new ManualSkiMotionInput(431,123,70,80,.9,.2,.8,.7,1.1,.65,2,-3);
            var copied=input.WithBrakeFraction(.4);
            Assert.That(copied.BrakeFraction,Is.EqualTo(.4));
            Assert.That(copied.SteeringInput,Is.EqualTo(input.SteeringInput));
            Assert.That(copied.DeliveredPowerWatts,Is.EqualTo(input.DeliveredPowerWatts));
            Assert.That(copied.CorneringSkill,Is.EqualTo(input.CorneringSkill));Assert.That(copied.DownhillSkill,Is.EqualTo(input.DownhillSkill));
            Assert.That(copied.LineQuality,Is.EqualTo(input.LineQuality));Assert.That(copied.MuscularFatigue,Is.EqualTo(input.MuscularFatigue));
            Assert.That(copied.GripMultiplier,Is.EqualTo(input.GripMultiplier));Assert.That(copied.DragAreaMultiplier,Is.EqualTo(input.DragAreaMultiplier));
            Assert.That(copied.SnowResistanceMultiplier,Is.EqualTo(input.SnowResistanceMultiplier));Assert.That(copied.SteeringResponse,Is.EqualTo(input.SteeringResponse));
            Assert.That(copied.WindX,Is.EqualTo(input.WindX));Assert.That(copied.WindZ,Is.EqualTo(input.WindZ));
            Assert.Throws<ArgumentOutOfRangeException>(()=>input.WithBrakeFraction(-.01));
            Assert.Throws<ArgumentOutOfRangeException>(()=>input.WithBrakeFraction(1.01));
            Assert.Throws<ArgumentOutOfRangeException>(()=>input.WithBrakeFraction(double.NaN));
        }
        [Test] public void VoluntaryAndCornerBrakingShareOneCapRatherThanStacking()
        {
            var f=new Fixture();var b=f.Body(forward:14);
            var input=new ManualSkiMotionInput(1000,0,100,100,brakeFraction:1);
            var result=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
            double cap=CorneringTests.Definition().MaximumBraking*b.MassKilograms;
            Assert.That(result.Cornering.BrakingNewtons,Is.GreaterThan(0));
            Assert.That(result.Drive.FirstDrive.Output.BrakingNewtons,Is.EqualTo(cap));
            Assert.That(result.Drive.FirstDrive.Output.BrakingNewtons,Is.LessThan(result.Cornering.BrakingNewtons+cap));
        }
        [Test] public void BrakingPaysTheActualStopPrefixAndCannotReverseOrRestartAnUnpoweredSkier()
        {
            var f=new Fixture();var b=f.Body(forward:.05);
            var input=new ManualSkiMotionInput(0,0,100,100,brakeFraction:1);
            var result=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
            double stopped=result.Drive.FirstDrive.SpeedBoundarySeconds;
            Assert.That(stopped,Is.GreaterThan(0).And.LessThan(.05));
            Assert.That(result.Drive.FirstDrive.Output.State.SpeedMetersPerSecond,Is.Zero);
            Assert.That(result.Drive.ConstantAccelerationSeconds,Is.EqualTo(stopped));
            var root=new ManualSkiWorldTrajectory(result.Drive);
            Assert.That(root.PlanarTravelDistanceAt(1),Is.GreaterThan(0).And.LessThan(.05*.05));
            Assert.That(root.Sample(1),Is.EqualTo(root.Sample(stopped/.05)));
            Assert.That(root.VelocityAt(1).X,Is.EqualTo(0).Within(1e-15));
            Assert.That(root.VelocityAt(1).Z,Is.EqualTo(0).Within(1e-15));
        }
        [Test] public void BrakedPreviewIsRepeatableAllocationFreeAndIndependentOfActorIdentity()
        {
            var f=new Fixture();var b=f.Body(.2,.1);
            var same=new ManualSkiContactBody(new AthleteId("player"),b.Pose,b.Velocity,b.MassKilograms);
            var input=new ManualSkiMotionInput(200,0,100,100,brakeFraction:.75);
            var expected=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
            var player=f.System.Preview(same,Motion(same),f.Observation,f.Surface,input,.05);
            Assert.That(player.Drive.State,Is.EqualTo(expected.Drive.State));
            Assert.That(player.Drive.MotionAt(1),Is.EqualTo(expected.Drive.MotionAt(1)));
            for(int i=0;i<100;i++)f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
            long before=GC.GetAllocatedBytesForCurrentThread();bool stable=true;
            for(int i=0;i<1000;i++)
            {
                var actual=f.System.Preview(b,Motion(b),f.Observation,f.Surface,input,.05);
                var state=actual.Drive.State;var target=expected.Drive.State;
                var motion=actual.Drive.MotionAt(1);var reference=expected.Drive.MotionAt(1);
                // Compare scalars: ValueType.Equals would box both structs in
                // the measurement loop and measure the observer's allocations.
                stable&=state.X==target.X&&state.Z==target.Z&&state.HeadingNanoradians==target.HeadingNanoradians&&
                    state.YawRateRadiansPerSecond==target.YawRateRadiansPerSecond&&motion.Velocity.X==reference.Velocity.X&&
                    motion.Velocity.Z==reference.Velocity.Z&&motion.YawRate==reference.YawRate;
            }
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(stable,Is.True);Assert.That(allocated,Is.Zero);
        }
        [Test] public void ControlledSourcePassesTheActualContactForceCouplerWithoutAHeadingSnap()
        {
            var f=new Fixture();var b=f.Body(.8,3);var input=f.Preview(b);
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_force\",\"maximum_passes\":128,\"closing_speed_tolerance_mps\":0.00000001}");
            var output=new SkiForceCoupledTrajectory[1];var system=new ManualSkiContactForces(f.Source.Venue.Occupancy,contact,work,1);
            var result=system.Preview(new[]{b},new[]{input.Drive},1,output);
            Assert.That(result.Status,Is.EqualTo(SkiContactForceStatus.Resolved));Assert.That(result.DurationSeconds,Is.EqualTo(.05));
            Assert.That(output[0].Sample(0),Is.EqualTo(b.Pose));Assert.That(output[0].MotionAt(0),Is.EqualTo(Motion(b)));
            Assert.That(output[0].MotionAt(1).Velocity.X,Is.EqualTo(input.Drive.MotionAt(1).Velocity.X).Within(1e-12));
            Assert.That(output[0].MotionAt(1).YawRate,Is.EqualTo(input.Drive.MotionAt(1).YawRate).Within(1e-12));
        }
        [Test] public void ReachedPairCouplesSignedSnowForcesInStableActorOrder()
        {
            var f=new Fixture();var rear=f.Body();double h=rear.Pose.HeadingRadians,length=f.Source.Venue.Occupancy.LongitudinalSeparation;
            var front=new ManualSkiContactBody(new AthleteId("front"),new ManualSkiHeadingState(rear.Pose.X+length*Math.Sin(h),
                rear.Pose.Z+length*Math.Cos(h),h),rear.Velocity,75);
            Assert.That(f.Source.Catalog.TryProject(front.Pose,1,10*SkiState.NanometersPerMeter,5,out var observed),Is.True);
            // Request a turn within grip while maintaining rear traction. A
            // maximum-rate command now pays braking and can open this pair.
            var free=new[]{f.Preview(rear,steering:200).Drive,f.System.Preview(front,Motion(front),observed,f.Source.Catalog.SampleSurface(observed),
                new ManualSkiMotionInput(0,0,100,100),.05).Drive};
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_pair_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_pair_force\",\"maximum_passes\":128,\"closing_speed_tolerance_mps\":0.00000001}");
            var output=new SkiForceCoupledTrajectory[2];var reversed=new SkiForceCoupledTrajectory[2];
            var system=new ManualSkiContactForces(f.Source.Venue.Occupancy,contact,work,2);
            var first=system.Preview(new[]{rear,front},free,2,output);
            Assert.That(first.Status,Is.EqualTo(SkiContactForceStatus.Resolved));Assert.That(first.SupportPoints,Is.EqualTo(2));
            Assert.That(first.MaximumClosingAcceleration*first.DurationSeconds,Is.LessThanOrEqualTo(work.ClosingSpeedTolerance));
            var second=system.Preview(new[]{front,rear},new[]{free[1],free[0]},2,reversed);
            Assert.That(second.Status,Is.EqualTo(SkiContactForceStatus.Resolved));Assert.That(second.DurationSeconds,Is.EqualTo(first.DurationSeconds));
            for(int i=0;i<2;i++)
            {
                Assert.That(output[i].State,Is.EqualTo(reversed[1-i].State));
                Assert.That(output[i].MotionAt(1),Is.EqualTo(reversed[1-i].MotionAt(1)));
                Assert.That(output[i].MotionAt(0).Velocity,Is.EqualTo(rear.Velocity));
            }
            Assert.That(output[0].MotionAt(1).Velocity,Is.Not.EqualTo(free[0].MotionAt(1).Velocity));
            // A force receipt is provisional. This test does not authorize a
            // rotating pair's whole-body/course interval or a live race.
        }
        [Test] public void SuccessivePhysicalTurnsAndNeutralReleaseAreDeterministicWithoutRecentering()
        {
            var f=new Fixture();var initial=f.Body();var first=Rollout(f,initial);var second=Rollout(f,initial);
            Assert.That(first.Pose,Is.EqualTo(second.Pose));Assert.That(first.Velocity,Is.EqualTo(second.Velocity));
            Assert.That(first.Pose.HeadingRadians-initial.Pose.HeadingRadians,Is.GreaterThan(.05));
            Assert.That(Math.Abs(first.Pose.YawRateRadiansPerSecond),Is.LessThan(1e-7));
        }
        private static ManualSkiContactBody Rollout(Fixture f,ManualSkiContactBody body)
        {
            long arc=f.Observation.PhysicalArcNanometers;double achieved=0;
            for(int tick=0;tick<100;tick++)
            {
                Assert.That(f.Source.Catalog.TryProject(body.Pose,1,arc,3,out var observed),Is.True);
                arc=observed.PhysicalArcNanometers;
                var result=f.System.Preview(body,Motion(body),observed,f.Source.Catalog.SampleSurface(observed),
                    new ManualSkiMotionInput(tick<10?431:0,0,100,100),.05);
                body=new ManualSkiContactBody(body.Athlete,result.Drive.State,result.Drive.MotionAt(1).Velocity,75);
                if(tick==70)achieved=body.Pose.HeadingRadians;
                if(tick>70)Assert.That(body.Pose.HeadingRadians,Is.EqualTo(achieved).Within(2e-8),"Released input cannot seek the course tangent.");
            }
            return body;
        }
        [Test] public void PreparedSessionSignedIntentPublishesOneFullyPaidOwnedWorldTick()
        {
            var f=new ManualScopedSessionTests.Fixture("base");var initial=f.World;var a=initial.GetActor(0);
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_owned_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_owned_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var group=SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"control_owned_group\",\"maximum_passes\":128,\"closing_speed_tolerance_mps\":0.00000001}");
            var world=new ManualScopedWorldRoster(new[]{f.Source.Catalog},f.Source.Venue.Occupancy,contact,work,new[]{initial.GetCursor(0)},
                new[]{a.Pose},new[]{a.Velocity},new[]{initial.GetPhysicalMotion(0)},20,initial.Tick,group,new[]{75.0});
            var batch=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var pending=f.Session.PrepareManualScopedStep(f.Command(),world,0);
            var tick=world.BeginTick();var input=f.Session.GetPreparedManualMotionInput(pending,world,tick,0,431);
            var system=new ManualSkiControlledDriveSystem(f.Model.Physics,f.Source.Venue.Occupancy,Steering(),CorneringTests.Definition(),20);
            var frame=system.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),
                a.Observation,f.Source.Catalog.SampleSurface(a.Observation),input,.05);
            var candidates=new SkiWorldMotionCandidate[1];Assert.That(world.TryPreviewContactDrivenMotion(tick,new[]{frame.Drive},candidates),Is.True);
            var cursor=world.GetWorkingCursor(tick,0);var windows=new[]{new ManualScopedWorldWindow(cursor.Athlete,cursor.OccurrenceIndex,
                cursor.ObservedArcNanometers,1,f.Source.Catalog.Get(cursor.OccurrenceIndex).LengthNanometers)};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,
                new[]{a.Observation.GradeAlong(a.Pose)},out var receipt),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(receipt.Status,Is.EqualTo(ManualScopedWorldEventStatus.Clear));Assert.That(receipt.PaidSeconds,Is.EqualTo(.05));
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);batch.Commit(new[]{pending},proof);
            Assert.That(world.GetActor(0).Pose.HeadingNanoradians,Is.Not.EqualTo(a.Pose.HeadingNanoradians));
            Assert.That(world.GetActor(0).Velocity,Is.EqualTo(candidates[0].Trajectory.VelocityAt(1)));
            Assert.That(world.Tick,Is.EqualTo(f.Session.Tick));Assert.That(f.Session.Motion.DistanceNanometers,Is.GreaterThan(initial.GetPhysicalMotion(0).DistanceNanometers));
        }
        [Test] public void WarmControlledSnowAndDrivenQueriesAllocateNothing()
        {
            var snow=Snow();var body=Actor(6,.8,3);var f=new Fixture();var b=f.Body(.8,3);
            var first=snow.Preview(body,Motion(body),431,4,.05);var driven=f.Preview(b);ManualSkiSteeredSnowPreview actual=default;
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){actual=snow.Preview(body,Motion(body),431,4,.05);driven=f.Preview(b);driven.Drive.Sample(.4);}
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(actual,Is.EqualTo(first));Assert.That(driven.IsValid,Is.True);
        }
        [Test] public void InvalidControlSourcesAndContextsAreExplicit()
        {
            var system=Snow();var b=Actor();Assert.That(default(ManualSkiSteeredSnowPreview).IsValid,Is.False);
            Assert.Catch(()=>system.Preview(default,Motion(b),0,4,.05));Assert.Catch(()=>system.Preview(b,Motion(b),1001,4,.05));
            Assert.Catch(()=>system.Preview(b,Motion(b),0,-1,.05));Assert.Catch(()=>system.Preview(b,Motion(b),0,4,0));
            Assert.Catch(()=>system.Preview(b,Motion(b),0,4,.051));Assert.Catch(()=>system.Preview(b,Motion(b),0,4,.05,1.1));
            var f=new Fixture();Assert.Catch(()=>f.System.Preview(f.Body(),Motion(f.Body()),f.Observation,f.Surface,default,.05));
            Assert.That(system.ContextJson,Does.Contain("manual_steered_snow_constraint_v2_exact_neutral_constraint"));
            Assert.That(f.System.ContextJson,Does.Contain("manual_controlled_slip_drive_v4_inverse_stopping"));
            Assert.That(f.System.ContextJson,Does.Contain("explicit_brake_fraction_uses_same_force_cap"));
        }
    }
}
