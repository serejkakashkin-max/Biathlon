using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualWorldTrajectorySliceTests
    {
        private static TrackSample Flat()=>new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
        private static ManualSkiWorldTrajectory Force(double speed=2,double power=350,double brake=0)
        {
            var travel=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(new SkiState(0,speed),power,Flat(),.05,brakingNewtons:brake);
            return new ManualSkiWorldTrajectory(new ManualSkiSteeringSystem(ManualSkiForceTrajectoryTests.HeadingDefinition(),20)
                .Preview(new ManualSkiHeadingState(3,5,.3),600,travel,1,4));
        }
        private static (ManualSkiWorldTrajectory root,ManualSkiSnowContactSystem snow,ManualSkiContactBody body) Snow()
        {
            var occupancy=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
            var pose=new ManualSkiHeadingState(3,5,.3);double h=pose.HeadingRadians;
            var body=new ManualSkiContactBody(new AthleteId("slice_snow"),pose,
                new SkiPlanarVelocity(2*Math.Sin(h)+.01*Math.Cos(h),2*Math.Cos(h)-.01*Math.Sin(h)),75);
            var snow=new ManualSkiSnowContactSystem(occupancy,20);
            return (new ManualSkiWorldTrajectory(snow.PreviewTrajectory(body,new SkiContactMotion(body.Velocity,0),4,0,.05)),snow,body);
        }
        private static ManualSkiWorldTrajectory ReversingContact()
        {
            var pose=new ManualSkiHeadingState(0,0,0);
            var initial=new SkiContactMotion(new SkiPlanarVelocity(0,2),0);
            var final=new SkiContactMotion(new SkiPlanarVelocity(0,-2),0);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var snow=(SkiSnowContactPreview)typeof(SkiSnowContactPreview).GetConstructor(flags,null,new[]{typeof(SkiContactMotion),typeof(double),typeof(double),typeof(double),typeof(double)},null).Invoke(new object[]{final,0.0,0.0,0.0,0.0});
            var contact=(SkiContactTrajectory)typeof(SkiContactTrajectory).GetConstructors(flags)[0].Invoke(new object[]{pose,initial,snow,.05});
            return new ManualSkiWorldTrajectory(contact);
        }
        [Test] public void ASmallContactSliceCannotAcquireNegativePhysicalTravelFromQuadratureSubtraction()
        {
            var root=ReversingContact();double from=.05*.507935,to=.05*.507936;
            var slice=root.Slice(from,to);
            Assert.That(slice.PlanarTravelDistanceAt(1),Is.GreaterThanOrEqualTo(0));
            Assert.That(slice.PhysicalTravelDistanceAt(1,0),Is.GreaterThanOrEqualTo(0));
        }
        [TestCase(0,350,0)] [TestCase(.01,0,500)] [TestCase(24.99,2000,0)] [TestCase(5,0,0)]
        public void ForceSlicesKeepOriginalSpeedYawAndTravelAcrossAccelerationStopAndCap(double speed,double power,double brake)
        {
            var root=Force(speed,power,brake);var slice=root.Slice(.007,.041);
            Assert.That(slice.DurationSeconds,Is.EqualTo(.041-.007));
            for(int i=0;i<=10;i++)
            {
                double f=i/10.0,mapped=(.007+(.041-.007)*f)/root.DurationSeconds;
                Assert.That(slice.Sample(f),Is.EqualTo(root.Sample(mapped)));
                Assert.That(slice.VelocityAt(f),Is.EqualTo(root.VelocityAt(mapped)));
                Assert.That(slice.PhysicalSpeedAt(f,0),Is.EqualTo(root.PhysicalSpeedAt(mapped,0)));
                Assert.That(slice.PhysicalTravelDistanceAt(f,0),Is.EqualTo(root.PhysicalTravelDistanceAt(mapped,0)-root.PhysicalTravelDistanceAt(.007/.05,0)));
            }
            var prefix=root.Slice(0,.02);var suffix=root.Slice(.02,root.DurationSeconds);
            Assert.That(prefix.Sample(1),Is.EqualTo(suffix.Sample(0)));Assert.That(prefix.VelocityAt(1),Is.EqualTo(suffix.VelocityAt(0)));
            Assert.That(prefix.PhysicalTravelDistanceAt(1,0)+suffix.PhysicalTravelDistanceAt(1,0),Is.EqualTo(root.PhysicalTravelDistanceAt(1,0)).Within(1e-15));
            Assert.That(suffix.Sample(1),Is.EqualTo(root.Sample(1)));
        }
        [Test] public void SnowSlicesRetainTheOriginalImpulseInsteadOfRecomputingAShorterOne()
        {
            var f=Snow();var prefix=f.root.Slice(0,.02);var suffix=f.root.Slice(.02,.05);
            var recomputed=f.snow.PreviewTrajectory(f.body,new SkiContactMotion(f.body.Velocity,0),4,0,.02);
            Assert.That(prefix.Sample(1),Is.EqualTo(f.root.Sample(.4)));
            Assert.That(prefix.VelocityAt(1),Is.EqualTo(f.root.VelocityAt(.4)));
            Assert.That(prefix.VelocityAt(1),Is.Not.EqualTo(recomputed.MotionAt(1).Velocity));
            Assert.That(prefix.Sample(1),Is.Not.EqualTo(recomputed.State));
            Assert.That(prefix.Sample(1),Is.EqualTo(suffix.Sample(0)));Assert.That(prefix.VelocityAt(1),Is.EqualTo(suffix.VelocityAt(0)));
            Assert.That(suffix.Sample(1),Is.EqualTo(f.root.Sample(1)));Assert.That(suffix.VelocityAt(1),Is.EqualTo(f.root.VelocityAt(1)));
            const int panels=10000;double expected=0,step=suffix.DurationSeconds/panels;
            for(int i=0;i<panels;i++)
            {var v=f.root.VelocityAt((.02+(i+.5)*step)/.05);expected+=Math.Sqrt(v.X*v.X+v.Z*v.Z)*step;}
            Assert.That(suffix.PlanarTravelDistanceAt(1),Is.EqualTo(expected).Within(2e-12));
            Assert.That(suffix.PhysicalTravelDistanceAt(1,.2),Is.EqualTo(expected*Math.Sqrt(1+.2*.2)).Within(2e-12));
        }
        [TestCase(.005,.02)] [TestCase(.024,.026)] [TestCase(.02539675,.0253968)] [TestCase(.03,.045)]
        public void ContactSliceNormMatchesIndependentOneDimensionalTravelIncludingTheVelocityZero(double from,double to)
        {
            var root=ReversingContact();var slice=root.Slice(from,to);double a=2-80*from,b=2-80*to;
            double expected=a*b>=0?.5*(Math.Abs(a)+Math.Abs(b))*(to-from):
                .5*Math.Abs(a)*(.025-from)+.5*Math.Abs(b)*(to-.025);
            Assert.That(slice.PlanarTravelDistanceAt(1),Is.EqualTo(expected).Within(1e-14));
            for(int i=0;i<=100;i++)Assert.That(slice.PlanarTravelDistanceAt(i/100.0),Is.GreaterThanOrEqualTo(0));
        }
        [TestCase(false)] [TestCase(true)]
        public void NestedSlicesKeepOneRootAndTheOriginalConservativeBounds(bool snow)
        {
            var root=snow?Snow().root:Force();var nested=root.Slice(.005,.045).Slice(.003,.019);
            var direct=root.Slice(.005+.003,.005+.019);
            for(int i=0;i<=10;i++)
            {
                double f=i/10.0;
                Assert.That(nested.Sample(f),Is.EqualTo(direct.Sample(f)));
                Assert.That(nested.VelocityAt(f),Is.EqualTo(direct.VelocityAt(f)));
                Assert.That(nested.PhysicalTravelDistanceAt(f,.2),Is.EqualTo(direct.PhysicalTravelDistanceAt(f,.2)));
            }
            Assert.That(nested.PeakYawRate,Is.EqualTo(root.PeakYawRate));
            Assert.That(nested.PlanarAccelerationBound,Is.EqualTo(root.PlanarAccelerationBound));
            Assert.That(nested.PositionErrorBoundMeters,Is.EqualTo(root.PositionErrorBoundMeters));
            Assert.That(nested.HeadingErrorBoundRadians,Is.EqualTo(root.HeadingErrorBoundRadians));
        }
        [TestCase(false)] [TestCase(true)]
        public void WholeSliceRetainsExistingQueriesAndZeroSliceTravelStartsAtItsOwnOrigin(bool snow)
        {
            var root=snow?Snow().root:Force();var same=root.Slice(0,root.DurationSeconds);var late=root.Slice(.02,.04);
            for(int i=0;i<=10;i++)
            {
                double f=i/10.0;
                Assert.That(same.Sample(f),Is.EqualTo(root.Sample(f)));Assert.That(same.VelocityAt(f),Is.EqualTo(root.VelocityAt(f)));
                Assert.That(same.PlanarTravelDistanceAt(f),Is.EqualTo(root.PlanarTravelDistanceAt(f)));
                Assert.That(same.PhysicalTravelDistanceAt(f,.2),Is.EqualTo(root.PhysicalTravelDistanceAt(f,.2)));
            }
            Assert.That(late.PlanarTravelDistanceAt(0),Is.Zero);Assert.That(late.PhysicalTravelDistanceAt(0,.2),Is.Zero);
            Assert.That(late.Sample(0),Is.EqualTo(root.Sample(.4)));
        }
        [Test] public void InvalidOrUnrepresentableClockIntervalsCannotCreateASlice()
        {
            var root=Force();
            Assert.Throws<InvalidOperationException>(()=>default(ManualSkiWorldTrajectory).Slice(0,.01));
            foreach(var bounds in new[]{(-.001,.01),(0.0,.051),(.02,.01),(.01,.01),(double.NaN,.02),(0.0,double.NaN),(double.PositiveInfinity,.03)})
                Assert.Catch(()=>root.Slice(bounds.Item1,bounds.Item2));
            var tail=root.Slice(.02,.04);
            Assert.Catch(()=>tail.Slice(.005,.005+double.Epsilon));
            Assert.Catch(()=>tail.Sample(1.01));Assert.Catch(()=>tail.VelocityAt(double.NaN));
            Assert.Catch(()=>tail.PhysicalTravelDistanceAt(-.1,0));
        }
        [TestCase(false)] [TestCase(true)]
        public void WarmSliceConstructionAndQueriesAllocateNothing(bool snow)
        {
            var root=snow?Snow().root:Force();
            double Query()
            {
                var part=root.Slice(.005,.045).Slice(.003,.019);var pose=part.Sample(.7);var v=part.VelocityAt(.7);
                return pose.X+pose.Z+v.X+v.Z+part.PhysicalSpeedAt(.7,.2)+part.PhysicalTravelDistanceAt(.7,.2)+part.PlanarTravelDistanceAt(1);
            }
            for(int i=0;i<128;i++)Query();
            long before=GC.GetAllocatedBytesForCurrentThread();double sum=0;
            for(int i=0;i<1000;i++)sum+=Query();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(double.IsNaN(sum),Is.False);Assert.That(allocated,Is.Zero);
        }
    }
}
