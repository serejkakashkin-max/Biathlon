using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualSeparatedNeutralStopTests
    {
        [TestCase(false)][TestCase(true)]
        public void ForeignContactCannotReplaceTheSeparatedNeutralStopClock(bool touching)
        {
            var source=new ManualRacePathCatalogTests.Fixture();var body=source.Venue.Occupancy;
            var catalogs=new[]{source.Catalog,source.Catalog,source.Catalog};
            var poses=new ManualSkiHeadingState[3];var velocities=new SkiPlanarVelocity[3];
            var cursors=new ManualRacePathCursor[3];var motions=new SkiState[3];
            for(int i=0;i<3;i++)
            {
                long arc=(long)Math.Round((i==0?10:i==1?10+(touching?body.LongitudinalSeparation:10):40)*SkiState.NanometersPerMeter);
                var origin=source.At(1,arc);
                poses[i]=i<2?origin:new ManualSkiHeadingState(origin.X,origin.Z,5.608743611);
                double h=poses[i].HeadingRadians;
                velocities[i]=i<2?new SkiPlanarVelocity(2*Math.Sin(h),2*Math.Cos(h)):
                    new SkiPlanarVelocity(-.011344254211615981,.014189019998102056);
                Assert.That(source.Catalog.TryProject(poses[i],1,arc,1,out var at),Is.True);
                cursors[i]=new ManualRacePathProgression(source.Catalog).Reconstitute(new AthleteId("neutral_stop_"+i),1,at.PhysicalArcNanometers,
                    at.PhysicalArcNanometers,at.MappedRouteNanometers);
                motions[i]=new SkiState(0,Math.Sqrt(velocities[i].X*velocities[i].X+velocities[i].Z*velocities[i].Z));
            }
            var world=new ManualScopedWorldRoster(catalogs,body,ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),
                cursors,poses,velocities,motions,20,responseDefinition:ManualScopedFieldStepperTests.Group(),actorMasses:new[]{75d,75,75});
            var tick=world.BeginTick();Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.True);
            var system=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),body,20);
            var drives=new SkiDrivenContactTrajectory[3];var candidates=new SkiWorldMotionCandidate[3];
            for(int i=0;i<3;i++)
            {
                var actor=world.GetWorkingActor(tick,i);var contact=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                drives[i]=system.Preview(contact,new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),actor.Observation,
                    source.Catalog.SampleSurface(actor.Observation),i==0?350:0,i==2?4:0,0,.05,brakingNewtons:i==2?300:0);
            }
            Assert.That(drives[2].Snow.HasExactNeutralConstraint,Is.True);
            Assert.That(drives[2].ConstantAccelerationSeconds,Is.LessThan(.05));
            Assert.That(drives[2].MotionAt(1).Velocity,Is.EqualTo(new SkiPlanarVelocity(0,0)));
            Assert.That(world.TryPreviewContactDrivenMotion(tick,drives,candidates),Is.True,world.LastContactForces.Status.ToString());
            Assert.That(world.LastOwnedContacts.Count,Is.EqualTo(touching?1:0));
            Assert.That(candidates[2].Trajectory.DurationSeconds,Is.EqualTo(.05));
            Assert.That(candidates[2].Trajectory.Sample(0),Is.EqualTo(poses[2]));
            Assert.That(candidates[2].Trajectory.Sample(1),Is.EqualTo(drives[2].Sample(1)));
            Assert.That(candidates[2].Trajectory.VelocityAt(1),Is.EqualTo(new SkiPlanarVelocity(0,0)));
            Assert.That(world.Tick.Value,Is.Zero,"A force preview cannot publish any field time.");
            Assert.That(world.GetActor(2).Velocity,Is.EqualTo(velocities[2]));world.AbortTick(tick);
        }
    }
}
