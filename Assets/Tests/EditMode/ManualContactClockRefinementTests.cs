using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualContactClockRefinementTests
    {
        private static SkiForceCoupledTrajectory Piece(ManualSkiContactBody body,SkiContactAcceleration acceleration,double seconds)
        {
            var constructor=typeof(SkiForceCoupledTrajectory).GetConstructor(BindingFlags.Instance|BindingFlags.NonPublic,null,
                new[]{typeof(ManualSkiHeadingState),typeof(SkiContactMotion),typeof(SkiContactAcceleration),typeof(double)},null);
            return (SkiForceCoupledTrajectory)constructor.Invoke(new object[]{body.Pose,
                new SkiContactMotion(body.Velocity,body.Pose.YawRateRadiansPerSecond),acceleration,seconds});
        }
        [TestCase(0)][TestCase(1)]
        public void RealDenseStartForceResidualCanRefineTimeWithoutRelaxingConvergence(int pair)
        {
            var source=new ManualRacePathCatalogTests.Fixture();
            var bodies=pair==0?new[]{
                new ManualSkiContactBody(new AthleteId("15"),new ManualSkiHeadingState(177.69676311,-6.681848928,1.564610369,.005402183),new SkiPlanarVelocity(7.354163155344258,.03773036990592373),75),
                new ManualSkiContactBody(new AthleteId("18"),new ManualSkiHeadingState(176.197175371,-6.753356812,1.564609014,.005370901),new SkiPlanarVelocity(7.353767999754721,.04580790083059142),75)
            }:new[]{
                new ManualSkiContactBody(new AthleteId("3"),new ManualSkiHeadingState(193.170979635,-6.884693221,1.629367315,.114177678),new SkiPlanarVelocity(7.936825991821005,-.5056639073203807),75),
                new ManualSkiContactBody(new AthleteId("6"),new ManualSkiHeadingState(191.666531246,-6.807433831,1.610736784,.099628454),new SkiPlanarVelocity(7.940076782617462,-.34538453806162245),75)
            };
            var accelerations=pair==0?new[]{new SkiContactAcceleration(.08390634431325594,-.02675737934692904,.15173991113027024),
                new SkiContactAcceleration(.30903121075033013,-.05296010585012667,-.036985666697008965)}:
                new[]{new SkiContactAcceleration(.12047907305332117,-.8655761599383549,.09192040808746416),
                new SkiContactAcceleration(.15539061504574292,-.7408176023244561,-.14288831589058887)};
            const double seconds=.04854292126500124;
            var free=new[]{Piece(bodies[0],accelerations[0],seconds),Piece(bodies[1],accelerations[1],seconds)};
            var system=new ManualSkiContactForces(source.Venue.Occupancy,ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Group(),2);
            var output=new SkiForceCoupledTrajectory[2];
            var method=typeof(ManualSkiContactForces).GetMethod("PreviewRefined",BindingFlags.NonPublic|BindingFlags.Instance);
            var original=(SkiContactForcePreview)method.Invoke(system,new object[]{bodies,free,2,output,0});
            if(pair==0)
            {
                Assert.That(original.Status,Is.EqualTo(SkiContactForceStatus.WorkLimit));
                Assert.That(output[0].IsValid,Is.False);Assert.That(output[1].IsValid,Is.False);
            }
            else Assert.That(original.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            var result=(SkiContactForcePreview)method.Invoke(system,new object[]{bodies,free,2,output,32});
            Assert.That(result.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(result.MaximumReactionResidualSpeed,Is.LessThanOrEqualTo(ManualScopedFieldStepperTests.Group().ClosingSpeedTolerance));
            Assert.That(result.DurationSeconds,Is.GreaterThan(0).And.LessThanOrEqualTo(seconds));
            if(original.Status==SkiContactForceStatus.WorkLimit)
            {
                Assert.That(result.IntervalRefinements,Is.GreaterThan(0));
                Assert.That(result.DurationSeconds,Is.LessThan(seconds));
            }
            for(int i=0;i<2;i++)
            {
                Assert.That(output[i].Sample(0),Is.EqualTo(bodies[i].Pose));
                Assert.That(output[i].MotionAt(0).Velocity,Is.EqualTo(bodies[i].Velocity));
                Assert.That(output[i].DurationSeconds,Is.EqualTo(result.DurationSeconds));
                Assert.That(output[i].State,Is.Not.EqualTo(bodies[i].Pose));
            }
            double deltaX=0,deltaZ=0;
            for(int i=0;i<2;i++)
            {
                var expected=free[i].MotionAt(result.DurationSeconds/seconds);var actual=output[i].MotionAt(1);
                deltaX+=75*(actual.Velocity.X-expected.Velocity.X);deltaZ+=75*(actual.Velocity.Z-expected.Velocity.Z);
            }
            Assert.That(deltaX,Is.EqualTo(0).Within(1e-10));Assert.That(deltaZ,Is.EqualTo(0).Within(1e-10));
        }
    }
}
