using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualFiringDeparturePortalTests
    {
        [TestCase(0,.04,5.575993686)]
        [TestCase(2,.31,5.43772393)]
        [TestCase(2,-.31,5.43772393)]
        public void AnActualPaidOffCentreMatPoseIsAlsoOnItsDepartureEntry(int lane,double side,double heading)
        {
            var f=new ManualRacePathCatalogTests.Fixture(lane:lane);var catalog=f.Catalog;
            int arrival=catalog.GetFiringOccurrence(0),departure=catalog.GetBaseFlowSuccessor(arrival);
            var row=catalog.Get(arrival);
            Assert.That(catalog.TrySample(arrival,row.LengthNanometers-1,out var incoming),Is.True);
            Assert.That(catalog.TrySample(arrival,row.LengthNanometers,out var goal),Is.True);
            var pose=new ManualSkiHeadingState(goal.X+incoming.ForwardZ*side,goal.Z-incoming.ForwardX*side,heading);
            Assert.That(catalog.TryProject(pose,arrival,row.LengthNanometers,1,out var before),Is.True);
            Assert.That(before.PhysicalArcNanometers,Is.EqualTo(row.LengthNanometers));
            Assert.That(catalog.TryProject(pose,departure,0,1,out var after),Is.True);
            Assert.That(after.PhysicalArcNanometers,Is.Zero);
            var body=new ManualSkiContactBody(new AthleteId("mat_pose"),pose,default,75);
            var trajectory=new ManualSkiWorldTrajectory(new ManualSkiSnowContactSystem(f.Venue.Occupancy,20)
                .PreviewTrajectory(body,default,0,0,.05));
            var gates=new ManualRacePathGateSweep(catalog,ManualScopedFieldStepperTests.Work());
            Assert.That(gates.Preview(trajectory,arrival,row.LengthNanometers,1,row.LengthNanometers).Status,
                Is.EqualTo(ManualRacePathGateStatus.Clear));
            Assert.That(gates.Preview(trajectory,departure,0,1,catalog.Get(departure).LengthNanometers).Status,
                Is.EqualTo(ManualRacePathGateStatus.Clear),"The phase handoff cannot put an unchanged stationary body outside its entry plane.");
            Assert.That(catalog.Get(arrival).End,Is.EqualTo(ManualRacePathEnd.FiringLine));
        }
    }
}
