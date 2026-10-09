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

        [Test] public void TwoMovingMatNeighboursAreForecastFromTheirRelativeAchievedMotion()
        {
            var query=Create();
            var a=new ManualSkiHeadingState(41.171640201,-1.576601689,1.40374513,.208494722);
            var b=new ManualSkiHeadingState(41.325241537,-2.373041476,1.249759586,.303018079);
            var av=new SkiPlanarVelocity(.9995293502787235,.010814817564830312);
            var bv=new SkiPlanarVelocity(.5523756277745162,-.0023748583240685885);
            Assert.That(query.TryClosingInterval(a,av,b,bv,1.5,out double time),Is.True);
            Assert.That(time,Is.InRange(0,1.5));
            Assert.That(query.TryClosingInterval(b,bv,a,av,1.5,out double reverse),Is.True);
            Assert.That(reverse,Is.EqualTo(time));
            Assert.That(query.TryClosingInterval(a,av,b,av,1.5,out _),Is.False);
        }
        [Test] public void MovingBodyOnASeparatedLineOrMovingAwayIsNotAClosingObstacle()
        {
            var query=Create();var a=new ManualSkiHeadingState(0,0,Math.PI*.5);
            var b=new ManualSkiHeadingState(3,2,Math.PI*.5);
            Assert.That(query.TryClosingInterval(a,new SkiPlanarVelocity(4,0),b,new SkiPlanarVelocity(1,0),2,out _),Is.False);
            b=new ManualSkiHeadingState(3,0,Math.PI*.5);
            Assert.That(query.TryClosingInterval(a,new SkiPlanarVelocity(1,0),b,new SkiPlanarVelocity(4,0),2,out _),Is.False);
            Assert.That(query.TryClosingInterval(a,new SkiPlanarVelocity(4,0),b,new SkiPlanarVelocity(1,0),.1,out _),Is.False);
            Assert.That(query.TryClosingInterval(a,new SkiPlanarVelocity(4,0),b,new SkiPlanarVelocity(1,0),2,out double time),Is.True);
            Assert.That(time,Is.EqualTo(.5).Within(1e-15));
        }
        [Test] public void WarmMovingBodyForecastDoesNotAllocate()
        {
            var query=Create();var a=new ManualSkiHeadingState(0,0,Math.PI*.5);
            var b=new ManualSkiHeadingState(3,0,Math.PI*.5);var av=new SkiPlanarVelocity(4,0);var bv=new SkiPlanarVelocity(1,0);
            for(int i=0;i<100;i++)query.TryClosingInterval(a,av,b,bv,2,out _);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=query.TryClosingInterval(a,av,b,bv,2,out _);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);
        }

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
