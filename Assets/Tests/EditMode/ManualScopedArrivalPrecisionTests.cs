using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualScopedArrivalPrecisionTests
    {
        [TestCase(false)][TestCase(true)]
        public void SubOrderingPrecisionPhysicalPieceRetainsWholeClockAndConservativeArrival(bool tinySuffix)
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var world=f.World;
            long line=world.GetCursor(1).CreditedRouteNanometers+50_000_000;
            var tick=world.BeginTick();var snow=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,1);
            double tiny=1e-12;
            for(int p=0;p<2;p++)
            {
                double dt=p==1?world.GetRemainingSeconds(tick):tinySuffix?.05-tiny:tiny;
                var actors=new SkiWorldMotionCandidate[2];var windows=new ManualScopedWorldWindow[2];
                for(int i=0;i<2;i++)
                {
                    var actor=world.GetWorkingActor(tick,i);var cursor=world.GetWorkingCursor(tick,i);
                    var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                    var travel=snow.PreviewTrajectory(body,new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),0,0,dt);
                    actors[i]=new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(travel));
                    windows[i]=new ManualScopedWorldWindow(actor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,1,
                        world.GetCatalog(i).Get(cursor.OccurrenceIndex).LengthNanometers);
                }
                Assert.That(world.TryAppendInterval(tick,actors,windows,Array.Empty<SkiReachedContactPair>(),0,
                    Array.Empty<SkiActorCourseEdge>(),0,new double[2]),Is.True,world.LastAttempt.Status.ToString());
            }
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);
            Assert.That(world.GetProposedMotionPieceCount(proof),Is.EqualTo(2));
            var method=typeof(PhysicalRangeArrival).GetMethod("TryFromScopedMotion",BindingFlags.Static|BindingFlags.NonPublic);
            object[] args={proof,1,0,line,default(PhysicalRangeArrival)};
            Assert.That((bool)method.Invoke(null,args),Is.True);
            var arrival=(PhysicalRangeArrival)args[4];
            // The tiny interval remains in the physical receipt, with no fabricated
            // pause or lost clock. Arrival ordering rounds the interpolated actual
            // crossing UP, rather than requiring every physical piece to be at
            // least one billionth of a tick long.
            long expected=tinySuffix?500_000_000L:500_000_001L;
            Assert.That(arrival.Fraction,Is.EqualTo(expected));
            Assert.That(world.Tick.Value,Is.Zero);
            Assert.That(world.GetProposedPhysicalMotion(proof,1).DistanceNanometers,Is.EqualTo(1_100_000_000));
            Assert.That(world.GetProposedMotionPiece(proof,1,1).EndSeconds,Is.EqualTo(.05));
        }
    }
}
