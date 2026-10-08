using System;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedCourseCellTests
    {
        [Test] public void RealOuterStraightLineUsesAllFourBodyPlanesWhenTheConservativeTubeRejectsIt()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,side:2);var actor=f.World.GetActor(0);var c=f.World.GetCursor(0);
            var trajectory=f.Candidates()[0].Trajectory;
            var work=Biathlon.Domain.SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"outer_cell_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var tube=new ManualRouteTubeSweep(f.Catalogs[0].ProjectionGeometry,f.Source.Venue.Occupancy,work);
            var row=f.Catalogs[0].Get(c.OccurrenceIndex);
            // Geometry namespaces are concatenated physical occurrences; obtain
            // the window station from the scoped row's initial namespace offset.
            long geometryStart=0;for(int i=0;i<row.Index;i++)geometryStart+=f.Catalogs[0].Get(i).LengthNanometers;
            Assert.That(tube.Preview(trajectory,row.Index,geometryStart+c.ObservedArcNanometers,1,false).Status,Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var proof),Is.True,f.World.LastAttempt.Status.ToString());
            f.World.Commit(proof);Assert.That(f.World.GetActor(0).Pose,Is.Not.EqualTo(actor.Pose));
            Assert.That(f.World.GetActor(0).Observation.LateralMeters,Is.EqualTo(2).Within(1e-8));
        }
        [TestCase(2.6,0)][TestCase(2,30)]
        public void OutsideBodiesAndMotionLeavingTheSourceCellStillRejectTheEntireRoster(double side,double lateralSpeed)
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,side:side,secondSideSpeed:lateralSpeed);var before=f.World.GetActor(0);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var proof),Is.False);Assert.That(proof.IsValid,Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.Course));
            if(lateralSpeed!=0)Assert.That(f.World.LastAttempt.CourseEdge.Status,Is.EqualTo(SkiEdgeSweepStatus.PotentialBoundary));
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void ACellProofCannotBypassAnUnadmittedPhaseEndpoint()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,side:2,endMask:1);var before=f.World.GetActor(0);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out _),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));
        }
    }
}
