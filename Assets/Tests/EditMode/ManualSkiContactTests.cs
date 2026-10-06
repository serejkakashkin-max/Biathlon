using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiContactTests
    {
        private const string Json="{\"schema_version\":1,\"id\":\"contact_test_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}";
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static ManualSkiContactSystem System(double friction=.2,double restitution=0)=>new ManualSkiContactSystem(Body(),
            SkiContactDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(PhysiologyContractTests.ReplaceNumber(Json,"sliding_friction",friction.ToString(global::System.Globalization.CultureInfo.InvariantCulture)),"restitution",restitution.ToString(global::System.Globalization.CultureInfo.InvariantCulture))));
        private static ManualSkiContactBody Actor(string id,double x,double z,double vx,double vz,double yaw=0,double mass=75)
            =>new ManualSkiContactBody(new AthleteId(id),new ManualSkiHeadingState(x,z,yaw),new SkiPlanarVelocity(vx,vz),mass);

        [Test]
        public void SideSwipeTransfersClosingVelocityWithoutStoppingForwardTravel()
        {
            double width=Body().LateralSeparation;
            var a=Actor("a",0,0,.6,6);var b=Actor("b",width,0,0,6);
            var hit=System().Preview(a,b);
            Assert.That(hit.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            Assert.That(hit.FirstVelocity.X,Is.EqualTo(.3).Within(1e-12));Assert.That(hit.SecondVelocity.X,Is.EqualTo(.3).Within(1e-12));
            Assert.That(hit.FirstVelocity.Z,Is.EqualTo(6));Assert.That(hit.SecondVelocity.Z,Is.EqualTo(6));
            Assert.That(a.Pose.X,Is.Zero);Assert.That(b.Pose.X,Is.EqualTo(width).Within(1e-9));
            Assert.That(hit.TangentImpulseNewtonSeconds,Is.Zero);
        }

        [Test]
        public void RearContactSharesMomentumInsteadOfStoppingBothBodies()
        {
            var a=Actor("a",0,0,0,8,mass:60);var b=Actor("b",0,Body().LongitudinalSeparation,0,4,mass:90);
            var hit=System().Preview(a,b);double common=(60*8+90*4)/150.0;
            Assert.That(hit.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            Assert.That(hit.FirstVelocity.Z,Is.EqualTo(common).Within(1e-12));Assert.That(hit.SecondVelocity.Z,Is.EqualTo(common).Within(1e-12));
            Assert.That(common,Is.GreaterThan(4));
            Assert.That(Energy(hit.FirstVelocity,60)+Energy(hit.SecondVelocity,90),Is.LessThan(Energy(a.Velocity,60)+Energy(b.Velocity,90)));
        }

        [Test]
        public void TangentFrictionIsBoundedAndSeparatingBodiesAreNotPulledBack()
        {
            var a=Actor("a",0,0,1,8);var b=Actor("b",Body().LateralSeparation,0,0,4);
            var hit=System().Preview(a,b);
            Assert.That(Math.Abs(hit.TangentImpulseNewtonSeconds),Is.EqualTo(.2*hit.NormalImpulseNewtonSeconds).Within(1e-12));
            Assert.That(hit.FirstVelocity.Z,Is.GreaterThan(hit.SecondVelocity.Z));
            var sliding=System(0).Preview(a,b);
            Assert.That(sliding.FirstVelocity.Z,Is.EqualTo(8));Assert.That(sliding.SecondVelocity.Z,Is.EqualTo(4));
            var away=Actor("a",0,0,-1,8);var result=System().Preview(away,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactPreviewStatus.SeparatingOrResting));
            Assert.That(result.FirstVelocity,Is.EqualTo(away.Velocity));Assert.That(result.NormalImpulseNewtonSeconds,Is.Zero);
        }

        [Test]
        public void ActualBodyOrientationReplacesRouteAlignedOrCircumcircleAssumptions()
        {
            var a=Actor("a",0,0,0,6);var parallel=Actor("b",1,0,0,6);
            Assert.That(System().Preview(a,parallel).Status,Is.EqualTo(SkiContactPreviewStatus.Separated));
            var sideways=Actor("b",1,0,0,6,Math.PI/2);
            var conflict=System().Preview(a,sideways);
            Assert.That(conflict.Status,Is.EqualTo(SkiContactPreviewStatus.Penetrating));
            Assert.That(conflict.FirstVelocity,Is.EqualTo(a.Velocity),"Overlap must be handled by the continuous solver, not teleported out here.");
            Assert.That(conflict.GapMeters,Is.LessThan(-.09));
        }

        [Test]
        public void SeparatedFastBodiesDoNotGetAFalseWholeTickSafetyGrant()
        {
            var a=Actor("a",0,0,100,0);var b=Actor("b",10,0,-100,0);
            var result=System().Preview(a,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactPreviewStatus.Separated));
            Assert.That(result.FirstVelocity,Is.EqualTo(a.Velocity));
            // They will collide later. The API deliberately provides no accepted
            // displacement or time interval: an endpoint query is not swept admission.
            Assert.That(result.NormalImpulseNewtonSeconds,Is.Zero);
        }

        [Test]
        public void LinearSweepDetectsFastPassThroughAtTheFirstTouchNotTheEndPosition()
        {
            var system=System(0);var a=Actor("a",0,0,100,0);var b=Actor("b",10,0,-100,0);
            var hit=system.PreviewLinearContact(a,b,.1);
            Assert.That(hit.HasContact,Is.True);Assert.That(hit.ContactSeconds,Is.EqualTo((10-Body().LateralSeparation)/200).Within(1e-12));
            Assert.That(hit.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            Assert.That(hit.Contact.FirstVelocity.X,Is.EqualTo(0).Within(1e-12));
            Assert.That(system.Preview(Actor("a",10,0,100,0),Actor("b",0,0,-100,0)).Status,Is.EqualTo(SkiContactPreviewStatus.Separated),
                "Endpoint-only collision tests would miss this crossing.");
            var reversed=system.PreviewLinearContact(b,a,.1);
            Assert.That(reversed.ContactSeconds,Is.EqualTo(hit.ContactSeconds));
            Assert.That(reversed.Contact.FirstVelocity,Is.EqualTo(hit.Contact.SecondVelocity));
        }

        [Test]
        public void LinearSweepAllowsSeparatedParallelTravelAndUsesActualOrientation()
        {
            var system=System();var a=Actor("a",0,0,0,6);var b=Actor("b",1,0,0,8);
            Assert.That(system.PreviewLinearContact(a,b,1).HasContact,Is.False);
            var rotated=Actor("b",2,0,-1,0,Math.PI/2);
            var fixedA=Actor("a",0,0,0,0);
            var hit=system.PreviewLinearContact(fixedA,rotated,2);
            double expected=2-(Body().LongitudinalSeparation+Body().LateralSeparation)*.5;
            Assert.That(hit.HasContact,Is.True);Assert.That(hit.ContactSeconds,Is.EqualTo(expected).Within(1e-9));
            Assert.That(hit.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
        }

        [Test]
        public void LinearSweepRejectsAngularMotionAndDoesNotHideExistingOverlap()
        {
            var system=System();var a=Actor("a",0,0,0,6);var overlap=Actor("b",0,.1,0,6);
            var hit=system.PreviewLinearContact(a,overlap,.05);
            Assert.That(hit.HasContact,Is.True);Assert.That(hit.ContactSeconds,Is.Zero);
            Assert.That(hit.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Penetrating));
            var rotating=new ManualSkiContactBody(new AthleteId("b"),new ManualSkiHeadingState(2,0,0,.1),default,75);
            Assert.Catch(()=>system.PreviewLinearContact(a,rotating,.05));
            Assert.Catch(()=>system.PreviewLinearContact(a,overlap,double.NaN));
            Assert.That(default(SkiLinearContactPreview).IsValid,Is.False);
        }

        [Test]
        public void RotationAtContactIsNotMisclassifiedAsStationarySafeContact()
        {
            var a=new ManualSkiContactBody(new AthleteId("a"),new ManualSkiHeadingState(0,0,0,.2),default,75);
            var b=Actor("b",Body().LateralSeparation,0,0,0);
            var result=System().Preview(a,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactPreviewStatus.RequiresAngularResolution));
            Assert.That(result.IsValid,Is.True);Assert.That(result.NormalImpulseNewtonSeconds,Is.Zero);
            Assert.That(default(SkiContactPreview).IsValid,Is.False);
        }

        [Test]
        public void StableActorOrderMakesSwapExactlySymmetricEvenForCoincidentCentres()
        {
            var system=System();
            foreach(var b in new[]{Actor("b",Body().LateralSeparation,0,0,4),Actor("b",0,0,1,2,Math.PI/2)})
            {
                var a=Actor("a",0,0,1,8);var ab=system.Preview(a,b);var ba=system.Preview(b,a);
                Assert.That(ba.Status,Is.EqualTo(ab.Status));Assert.That(ba.GapMeters,Is.EqualTo(ab.GapMeters));
                Assert.That(ba.NormalX,Is.EqualTo(-ab.NormalX));Assert.That(ba.NormalZ,Is.EqualTo(-ab.NormalZ));
                Assert.That(ba.FirstVelocity,Is.EqualTo(ab.SecondVelocity));Assert.That(ba.SecondVelocity,Is.EqualTo(ab.FirstVelocity));
            }
        }

        [Test]
        public void AllSupportedRestitutionAndFrictionPreserveMomentumWithoutAddingEnergy()
        {
            foreach(double restitution in new[]{0,.2,1})foreach(double friction in new[]{0,.2,1})
            {
                var system=System(friction,restitution);
                for(int i=1;i<=30;i++)
                {
                    var a=Actor("a",0,0,i*.03,6+i*.1,mass:50+i);var b=Actor("b",Body().LateralSeparation,0,-i*.02,8-i*.1,mass:100-i);
                    var hit=system.Preview(a,b);Assert.That(hit.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
                    Assert.That(hit.FirstVelocity.X*a.MassKilograms+hit.SecondVelocity.X*b.MassKilograms,
                        Is.EqualTo(a.Velocity.X*a.MassKilograms+b.Velocity.X*b.MassKilograms).Within(1e-10));
                    Assert.That(hit.FirstVelocity.Z*a.MassKilograms+hit.SecondVelocity.Z*b.MassKilograms,
                        Is.EqualTo(a.Velocity.Z*a.MassKilograms+b.Velocity.Z*b.MassKilograms).Within(1e-10));
                    Assert.That(Energy(hit.FirstVelocity,a.MassKilograms)+Energy(hit.SecondVelocity,b.MassKilograms),
                        Is.LessThanOrEqualTo(Energy(a.Velocity,a.MassKilograms)+Energy(b.Velocity,b.MassKilograms)+1e-9));
                    Assert.That((hit.FirstVelocity.X-hit.SecondVelocity.X)*hit.NormalX+(hit.FirstVelocity.Z-hit.SecondVelocity.Z)*hit.NormalZ,
                        Is.GreaterThanOrEqualTo(-1e-12));
                }
            }
        }

        [Test]
        public void ConfigurationAndBodyValidationRejectInvalidContracts()
        {
            Assert.That(SkiContactDefinition.FromJson(Json).ToJson(),Is.EqualTo(SkiContactDefinition.FromJson(SkiContactDefinition.FromJson(Json).ToJson()).ToJson()));
            foreach(var pair in new[]{("schema_version","2"),("restitution","1.1"),("sliding_friction","-1"),("contact_tolerance_m","0.1")})
                Assert.Catch(()=>SkiContactDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Json,pair.Item1,pair.Item2)));
            Assert.Catch(()=>SkiContactDefinition.FromJson(Json.Replace("\"restitution\"","\"missing\"")));
            Assert.Catch(()=>new SkiPlanarVelocity(double.NaN,0));
            Assert.Catch(()=>Actor("a",0,0,0,0,mass:0));
            var a=Actor("a",0,0,0,0);Assert.Catch(()=>System().Preview(a,a));Assert.Catch(()=>System().Preview(a,default));
        }

        [Test]
        public void RotatingSkiCornerContactsEvenWhenBothCentresAreStationary()
        {
            var body=Body();double angle=Math.PI/4;
            var a=Actor("a",0,0,0,0);
            double x=body.LateralSeparation*.5+(body.LongitudinalSeparation+body.LateralSeparation)*.5/Math.Sqrt(2);
            var b=new ManualSkiContactBody(new AthleteId("b"),new ManualSkiHeadingState(x,0,angle,.3),default,75);
            var system=System();var hit=system.PreviewAngularContact(a,b);
            Assert.That(hit.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            Assert.That(hit.Contact.NormalImpulseNewtonSeconds,Is.GreaterThan(0));
            Assert.That(hit.Contact.FirstVelocity.X,Is.LessThan(0));
            Assert.That(hit.FirstYawRate,Is.Not.Zero);Assert.That(hit.SecondYawRate,Is.LessThan(.3));
            AssertAngularInvariants(a,b,hit,0);
            var swap=system.PreviewAngularContact(b,a);
            Assert.That(swap.FirstYawRate,Is.EqualTo(hit.SecondYawRate));
            Assert.That(swap.SecondYawRate,Is.EqualTo(hit.FirstYawRate));
            Assert.That(swap.Contact.FirstVelocity,Is.EqualTo(hit.Contact.SecondVelocity));
            Assert.That(swap.ContactX,Is.EqualTo(hit.ContactX));Assert.That(swap.ContactZ,Is.EqualTo(hit.ContactZ));
        }

        [Test]
        public void AngularImpactConservesLinearAndAngularMomentumWithoutIncreasingEnergy()
        {
            var body=Body();int hits=0;
            foreach(double restitution in new[]{0,.3,1})foreach(double friction in new[]{0,.2,1})
            for(int i=1;i<=30;i++)
            {
                double yaw=.02*i;
                var encoded=new ManualSkiHeadingState(0,0,yaw);
                double extent=body.LongitudinalSeparation*.5*Math.Abs(Math.Sin(encoded.HeadingRadians))
                    +body.LateralSeparation*.5*Math.Abs(Math.Cos(encoded.HeadingRadians));
                var a=new ManualSkiContactBody(new AthleteId("a"),new ManualSkiHeadingState(0,0,0,(i%5-2)*.1),new SkiPlanarVelocity(.3,6),60);
                var b=new ManualSkiContactBody(new AthleteId("b"),new ManualSkiHeadingState(body.LateralSeparation*.5+extent,0,yaw,(i%7-3)*.1),new SkiPlanarVelocity(-.5,5),90);
                var hit=System(friction,restitution).PreviewAngularContact(a,b);
                Assert.That(hit.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));hits++;
                AssertAngularInvariants(a,b,hit,restitution);
                Assert.That(Math.Abs(hit.Contact.TangentImpulseNewtonSeconds),Is.LessThanOrEqualTo(friction*hit.Contact.NormalImpulseNewtonSeconds+1e-9));
            }
            Assert.That(hits,Is.EqualTo(270));
        }

        [Test]
        public void CentralRearAngularImpulseDoesNotInventSpinAndKeepsForwardMotion()
        {
            var a=Actor("a",0,0,0,8);var b=Actor("b",0,Body().LongitudinalSeparation,0,4);
            var result=System().PreviewAngularContact(a,b);
            Assert.That(result.Contact.Status,Is.EqualTo(SkiContactPreviewStatus.Impulse));
            Assert.That(result.FirstYawRate,Is.Zero);Assert.That(result.SecondYawRate,Is.Zero);
            Assert.That(result.Contact.FirstVelocity.Z,Is.EqualTo(6));Assert.That(result.Contact.SecondVelocity.Z,Is.EqualTo(6));
            AssertAngularInvariants(a,b,result,0);
        }

        private static void AssertAngularInvariants(ManualSkiContactBody a,ManualSkiContactBody b,SkiAngularContactPreview hit,double restitution)
        {
            var body=Body();double inertiaScale=(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)/12;
            double ia=a.MassKilograms*inertiaScale,ib=b.MassKilograms*inertiaScale;
            var va=hit.Contact.FirstVelocity;var vb=hit.Contact.SecondVelocity;
            double before=Energy(a.Velocity,a.MassKilograms)+Energy(b.Velocity,b.MassKilograms)+.5*ia*a.Pose.YawRateRadiansPerSecond*a.Pose.YawRateRadiansPerSecond+.5*ib*b.Pose.YawRateRadiansPerSecond*b.Pose.YawRateRadiansPerSecond;
            double after=Energy(va,a.MassKilograms)+Energy(vb,b.MassKilograms)+.5*ia*hit.FirstYawRate*hit.FirstYawRate+.5*ib*hit.SecondYawRate*hit.SecondYawRate;
            Assert.That(after,Is.LessThanOrEqualTo(before+1e-9));
            Assert.That(va.X*a.MassKilograms+vb.X*b.MassKilograms,Is.EqualTo(a.Velocity.X*a.MassKilograms+b.Velocity.X*b.MassKilograms).Within(1e-10));
            Assert.That(va.Z*a.MassKilograms+vb.Z*b.MassKilograms,Is.EqualTo(a.Velocity.Z*a.MassKilograms+b.Velocity.Z*b.MassKilograms).Within(1e-10));
            double Moment(ManualSkiContactBody v,SkiPlanarVelocity speed,double spin,double inertia)=>v.MassKilograms*(v.Pose.Z*speed.X-v.Pose.X*speed.Z)+inertia*spin;
            Assert.That(Moment(a,va,hit.FirstYawRate,ia)+Moment(b,vb,hit.SecondYawRate,ib),
                Is.EqualTo(Moment(a,a.Velocity,a.Pose.YawRateRadiansPerSecond,ia)+Moment(b,b.Velocity,b.Pose.YawRateRadiansPerSecond,ib)).Within(1e-9));
            double Closing(SkiPlanarVelocity av,SkiPlanarVelocity bv,double aw,double bw)
            {
                double x=av.X+aw*(hit.ContactZ-a.Pose.Z)-bv.X-bw*(hit.ContactZ-b.Pose.Z);
                double z=av.Z-aw*(hit.ContactX-a.Pose.X)-bv.Z+bw*(hit.ContactX-b.Pose.X);
                return x*hit.Contact.NormalX+z*hit.Contact.NormalZ;
            }
            Assert.That(Closing(va,vb,hit.FirstYawRate,hit.SecondYawRate),
                Is.EqualTo(-restitution*Closing(a.Velocity,b.Velocity,a.Pose.YawRateRadiansPerSecond,b.Pose.YawRateRadiansPerSecond)).Within(1e-10));
        }

        [Test]
        public void InstantPairPreviewIsDeterministicAndAllocationFree()
        {
            var system=System();var a=Actor("a",0,0,1,8);var b=Actor("b",Body().LateralSeparation,0,0,6);
            var expected=system.Preview(a,b);long before=GC.GetAllocatedBytesForCurrentThread();SkiContactPreview actual=default;
            var far=Actor("c",5,0,-1,6);system.PreviewLinearContact(a,far,2);
            before=GC.GetAllocatedBytesForCurrentThread();
            system.PreviewAngularContact(a,b);before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++){actual=system.Preview(a,b);system.PreviewLinearContact(a,far,2);system.PreviewAngularContact(a,b);}
            long allocation=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocation,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
            Assert.That(system.ContextJson,Is.EqualTo(System().ContextJson));
        }
        private static double Energy(SkiPlanarVelocity v,double mass)=>.5*mass*(v.X*v.X+v.Z*v.Z);
    }
}
