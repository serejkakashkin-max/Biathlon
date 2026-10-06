using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiSnowContactTests
    {
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static ManualSkiContactBody Actor(double side,double forward,double yaw=0,double heading=0,double mass=75)
        {
            var pose=new ManualSkiHeadingState(3,5,heading,yaw);double h=pose.HeadingRadians;
            return new ManualSkiContactBody(new AthleteId("snow"),pose,
                new SkiPlanarVelocity(side*Math.Cos(h)+forward*Math.Sin(h),-side*Math.Sin(h)+forward*Math.Cos(h)),mass);
        }
        private static SkiContactMotion Motion(ManualSkiContactBody body)=>new SkiContactMotion(body.Velocity,body.Pose.YawRateRadiansPerSecond);
        private static double InertiaScale(){var b=Body();return (b.LongitudinalSeparation*b.LongitudinalSeparation+b.LateralSeparation*b.LateralSeparation)/12;}
        private static double Energy(ManualSkiContactBody body,SkiContactMotion motion)=>body.MassKilograms*.5*(motion.Velocity.X*motion.Velocity.X+motion.Velocity.Z*motion.Velocity.Z+InertiaScale()*motion.YawRate*motion.YawRate);

        [Test]
        public void PureSideSlipDecaysByAvailableGripWithoutSpinOrForwardStop()
        {
            var body=Actor(.8,6);var result=new ManualSkiSnowContactSystem(Body(),20).Preview(body,Motion(body),4,0,.05);
            Assert.That(result.Motion.Velocity.X,Is.EqualTo(.6).Within(1e-12));Assert.That(result.Motion.Velocity.Z,Is.EqualTo(6));
            Assert.That(result.Motion.YawRate,Is.Zero);Assert.That(result.FrontImpulseNewtonSeconds,Is.EqualTo(result.RearImpulseNewtonSeconds));
            Assert.That(result.DissipatedEnergyJoules,Is.EqualTo(.5*75*(.8*.8-.6*.6)).Within(1e-10));
            Assert.That(body.Pose.X,Is.EqualTo(3));Assert.That(body.Pose.Z,Is.EqualTo(5));
        }

        [Test]
        public void PureSpinDampsWithoutInventingCentreTranslation()
        {
            var body=Actor(0,0,.8);var result=new ManualSkiSnowContactSystem(Body(),20).Preview(body,Motion(body),4,0,.05);
            Assert.That(result.Motion.Velocity,Is.EqualTo(body.Velocity));
            Assert.That(result.Motion.YawRate,Is.InRange(0,.799999));
            Assert.That(result.FrontImpulseNewtonSeconds,Is.EqualTo(-result.RearImpulseNewtonSeconds));
            Assert.That(result.DissipatedEnergyJoules,Is.GreaterThan(0));
        }

        [Test]
        public void GripConsumedByTurningCannotAlsoCancelContactSlip()
        {
            var body=Actor(.8,6,.2);var system=new ManualSkiSnowContactSystem(Body(),20);
            foreach(double used in new[]{4.0,5})
            {
                var result=system.Preview(body,Motion(body),4,used,.05);
                Assert.That(result.Motion,Is.EqualTo(Motion(body)));Assert.That(result.AvailableLateralAcceleration,Is.Zero);
                Assert.That(result.DissipatedEnergyJoules,Is.Zero);
            }
            var partial=system.Preview(body,Motion(body),4,3,.05);
            Assert.That(partial.AvailableLateralAcceleration,Is.EqualTo(1));
            Assert.That(Math.Abs(partial.FrontImpulseNewtonSeconds)+Math.Abs(partial.RearImpulseNewtonSeconds),Is.LessThanOrEqualTo(75*.05));
        }

        [Test]
        public void SmallResidualStopsWithoutReversingOrBeingQuantizedPerImpulse()
        {
            var body=Actor(.01,6,.001);var result=new ManualSkiSnowContactSystem(Body(),20).Preview(body,Motion(body),4,0,.05);
            Assert.That(result.Motion.Velocity.X,Is.EqualTo(0).Within(1e-15));Assert.That(result.Motion.YawRate,Is.EqualTo(0).Within(1e-15));
            Assert.That(result.Motion.Velocity.Z,Is.EqualTo(6));
            var weak=new ManualSkiSnowContactSystem(Body(),20).Preview(Actor(.7,6,.777777777),Motion(Actor(.7,6,.777777777)),.123456789,0,.013);
            Assert.That(weak.Motion.Velocity.X,Is.Not.EqualTo(Math.Round(weak.Motion.Velocity.X,9)));
        }

        [Test]
        public void FixedHeadingSlipSpinMatrixNeverAddsEnergyOrConsumesForwardMomentum()
        {
            var system=new ManualSkiSnowContactSystem(Body(),20);
            foreach(double heading in new[]{0.0,.3,1.5,3.1})foreach(double side in new[]{-2.0,-.1,0,.1,2})
            foreach(double yaw in new[]{-1.0,-.1,0,.1,1})foreach(double grip in new[]{0.0,.1,4})
            {
                var body=Actor(side,6,yaw,heading);var initial=Motion(body);var result=system.Preview(body,initial,grip,0,.05);
                Assert.That(Energy(body,result.Motion),Is.LessThanOrEqualTo(Energy(body,initial)+1e-10));
                Assert.That(Energy(body,initial)-Energy(body,result.Motion),Is.EqualTo(result.DissipatedEnergyJoules).Within(1e-10));
                double h=body.Pose.HeadingRadians;
                Assert.That(result.Motion.Velocity.X*Math.Sin(h)+result.Motion.Velocity.Z*Math.Cos(h),Is.EqualTo(6).Within(1e-12));
                Assert.That(Math.Abs(result.FrontImpulseNewtonSeconds),Is.LessThanOrEqualTo(75*grip*.05*.5+1e-12));
                Assert.That(Math.Abs(result.RearImpulseNewtonSeconds),Is.LessThanOrEqualTo(75*grip*.05*.5+1e-12));
            }
        }

        [Test]
        public void MirroringReversesSlipAndSpinButKeepsForwardTravelAndEnergyLoss()
        {
            var system=new ManualSkiSnowContactSystem(Body(),20);var a=Actor(.7,6,.8);var b=Actor(-.7,6,-.8);
            var left=system.Preview(a,Motion(a),4,0,.05);var right=system.Preview(b,Motion(b),4,0,.05);
            Assert.That(left.Motion.Velocity.X,Is.EqualTo(-right.Motion.Velocity.X));Assert.That(left.Motion.YawRate,Is.EqualTo(-right.Motion.YawRate));
            Assert.That(left.Motion.Velocity.Z,Is.EqualTo(right.Motion.Velocity.Z));Assert.That(left.DissipatedEnergyJoules,Is.EqualTo(right.DissipatedEnergyJoules));
        }

        [Test]
        public void SharedCorneringCapabilitySuppliesSlipBudgetAndContactOutputIsRetained()
        {
            var occupancy=Body();var body=Actor(0,6);var other=new ManualSkiContactBody(new AthleteId("other"),new ManualSkiHeadingState(3.7,5,0),new SkiPlanarVelocity(-1,6),75);
            var pair=new ManualSkiContactSystem(occupancy,SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"snow_pair_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}"));
            var hit=pair.PreviewAngularContact(body,other);Assert.That(hit.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            var cornering=new CorneringSystem(CorneringTests.Definition(),SkiForceTests.Definition());var surface=TuckTests.Descent();
            var expert=cornering.EvaluateWorldHeading(new SkiState(0,6),surface,0,100,100,1,0);
            var novice=cornering.EvaluateWorldHeading(new SkiState(0,6),surface,0,0,0,1,0);
            var system=new ManualSkiSnowContactSystem(occupancy,20);var contactMotion=new SkiContactMotion(hit.Contact.FirstVelocity,hit.FirstYawRate);
            var a=system.Preview(body,contactMotion,expert.LateralAccelerationLimit,0,.05);
            var b=system.Preview(body,contactMotion,novice.LateralAccelerationLimit,0,.05);
            Assert.That(Math.Abs(a.Motion.Velocity.X),Is.LessThan(Math.Abs(b.Motion.Velocity.X)));
            Assert.That(a.Motion.Velocity.X,Is.LessThan(0),"Contact side velocity must not be replaced by the body's pre-impact velocity.");
            Assert.That(a.Motion.Velocity.Z,Is.EqualTo(6));
        }

        [Test]
        public void RepeatedVelocityOperatorIsAllocationFreeAndInputValidationIsExplicit()
        {
            var system=new ManualSkiSnowContactSystem(Body(),20);var body=Actor(.7,6,.8);var motion=Motion(body);
            var expected=system.Preview(body,motion,4,1,.05);SkiSnowContactPreview actual=default;
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++)actual=system.Preview(body,motion,4,1,.05);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
            Assert.Catch(()=>system.Preview(default,motion,4,0,.05));Assert.Catch(()=>system.Preview(body,motion,-1,0,.05));
            Assert.Catch(()=>system.Preview(body,motion,4,double.NaN,.05));Assert.Catch(()=>system.Preview(body,motion,4,0,.051));
            Assert.Catch(()=>system.Preview(body,motion,4,0,0));Assert.Catch(()=>new ManualSkiSnowContactSystem(Body(),0));
            Assert.That(default(SkiSnowContactPreview).IsValid,Is.False);
        }
    }
}
