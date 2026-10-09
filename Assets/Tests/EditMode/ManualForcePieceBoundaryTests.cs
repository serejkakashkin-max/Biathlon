using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualForcePieceBoundaryTests
    {
        [TestCase(2.913896895,false)][TestCase(2.915,true)]
        public void ARealForwardStopDoesNotHideTheSideSlipPortalArrival(double heading,bool afterStop)
        {
            var f=new ManualRacePathCatalogTests.Fixture();
            var pose=new ManualSkiHeadingState(43.51443678,-8.37568061,heading,-.587481722);
            int index=f.Catalog.GetFiringOccurrence(1)+1;
            const long arc=8778795684;
            Assert.That(f.Catalog.TryProject(pose,index,arc,1,out var at),Is.True);
            var model=new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson);
            var velocity=new SkiPlanarVelocity(1.9533871124224198,.44867487007212153);
            var body=new ManualSkiContactBody(new AthleteId("force_piece_boundary"),pose,velocity,model.Physics.MassKilograms);
            var frame=new ManualSkiContactDriveSystem(model.Physics,f.Venue.Occupancy,20).Preview(body,
                new SkiContactMotion(velocity,pose.YawRateRadiansPerSecond),at,f.Catalog.SampleSurface(at),0,0,0,.05,
                brakingNewtons:model.Physics.MassKilograms*model.Cornering.MaximumBraking);
            var trajectory=new ManualSkiWorldTrajectory(frame);
            Assert.That(frame.ConstantAccelerationSeconds,Is.InRange(0.000000001,.049));
            Assert.That(trajectory.TryGetConstantPlanarAcceleration(out _),Is.False,"This is a genuine force breakpoint, not a fitted constant law.");
            var window=new ManualScopedWorldWindow(body.Athlete,index,arc,1,f.Catalog.Get(index).LengthNanometers);
            var preview=new ManualRacePathBoundaryScheduler(f.Catalog,ManualScopedFieldStepperTests.Work()).Preview(trajectory,window);
            Assert.That(preview.Status,Is.EqualTo(ManualRacePathBoundaryStatus.Boundary));
            Assert.That(preview.Seconds,Is.InRange(.000000001,.01));
            Assert.That(preview.Seconds>frame.ConstantAccelerationSeconds,Is.EqualTo(afterStop));
            var prefix=trajectory.Slice(0,preview.Seconds);
            Assert.That(prefix.Sample(0),Is.EqualTo(pose));Assert.That(prefix.VelocityAt(0),Is.EqualTo(velocity));
            Assert.That(f.Catalog.TryProject(prefix.Sample(1),index,arc,1,out var end),Is.True);
            Assert.That(end.PhysicalArcNanometers,Is.EqualTo(window.MaximumAdmittedArcNanometers));
            Assert.That(prefix.VelocityAt(1).X,Is.GreaterThan(1),"A forward-speed stop must retain physical sideways motion.");
        }
    }
}
