using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualAiObstacleCorridorTests
    {
        private static ManualAiObstacleCorridor Create()=>new ManualAiObstacleCorridor(
            OccupancyDefinition.FromJson("{\"schema_version\":1,\"id\":\"captured_mat_body\",\"body_length_m\":1.4,\"body_width_m\":0.6,\"personal_clearance_m\":0.1,\"edge_clearance_m\":0.1}"));
        private static ManualSkiHeadingState Mover()=>new ManualSkiHeadingState(34.18893769,-2.286017692,1.469005342);
        private static ManualSkiHeadingState Mat()=>new ManualSkiHeadingState(37.505951752,-.819570848,5.608743611);
        private static SkiPlanarVelocity Motion()=>new SkiPlanarVelocity(4.758086687814439,.5362074706213164);

        [Test] public void AchievedMotionTowardRotatedMatIsDetectedBeforeCurrentLinesOverlap()
        {
            var query=Create();var self=Mover();var other=Mat();var velocity=Motion();
            const double lateral=-.2977659466421807;
            Assert.That(query.Overlaps(self,velocity,1,0,lateral,other,lateral,false),Is.False,
                "Captured current line is clear; the actual velocity is already approaching the mat.");
            Assert.That(query.Overlaps(self,velocity,1,0,lateral,other,lateral,true),Is.True);
            Assert.That(query.Overlaps(self,velocity,1,0,lateral,other,lateral+.9,false),Is.False,
                "A candidate away from the mat must remain selectable despite the old velocity.");
        }
        [Test] public void AchievedMotionAwayFromMatDoesNotInventAnObstruction()
        {
            var query=Create();const double lateral=-.2977659466421807;
            Assert.That(query.Overlaps(Mover(),new SkiPlanarVelocity(Motion().X,-Motion().Z),1,0,lateral,Mat(),lateral,true),Is.False);
            Assert.That(query.Overlaps(Mover(),new SkiPlanarVelocity(0,0),1,0,lateral,Mat(),lateral,true),Is.False);
        }
        [Test] public void RotatedBodyEnvelopeAndAllocationFreeQueryArePreserved()
        {
            var query=Create();var self=new ManualSkiHeadingState(0,0,Math.PI*.5);
            var other=new ManualSkiHeadingState(3,1,0);
            Assert.That(query.Overlaps(self,new SkiPlanarVelocity(4,0),1,0,0,other,0,true),Is.True,
                "The longitudinal axis of a rotated stationary skier extends into this corridor.");
            for(int i=0;i<100;i++)query.Overlaps(Mover(),Motion(),1,0,-.2977659466421807,Mat(),-.2977659466421807,true);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)query.Overlaps(Mover(),Motion(),1,0,-.2977659466421807,Mat(),-.2977659466421807,true);
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
    }
}
