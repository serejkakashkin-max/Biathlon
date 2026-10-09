using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualScopedFreeDriveTests
    {
        private sealed class Fixture
        {
            internal readonly ManualScopedWorldRosterTests.Fixture Source;
            internal readonly ManualScopedWorldRoster World;
            internal readonly SkiDrivenContactTrajectory[] Drives=new SkiDrivenContactTrajectory[2];
            internal readonly SkiWorldMotionCandidate[] Candidates=new SkiWorldMotionCandidate[2];
            internal readonly ManualScopedWorldWindow[] Windows=new ManualScopedWorldWindow[2];
            internal readonly double[] Grades={0,0};
            internal Fixture(bool arrival=false)
            {
                Source=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:arrival?-1:20,contactResponse:true);
                if(arrival)World=Source.World;
                else
                {
                    var cursors=new ManualRacePathCursor[2];var poses=new ManualSkiHeadingState[2];
                    var velocities=new SkiPlanarVelocity[2];var motions=new SkiState[2];
                    for(int i=0;i<2;i++)
                    {
                        var actor=Source.World.GetActor(i);cursors[i]=Source.World.GetCursor(i);
                        poses[i]=new ManualSkiHeadingState(actor.Pose.X,actor.Pose.Z,actor.Pose.HeadingRadians,i==0?.002:0);
                        double h=poses[i].HeadingRadians,forward=i==0?1e-10:.6,side=i==0?.001:0;
                        velocities[i]=new SkiPlanarVelocity(forward*Math.Sin(h)+side*Math.Cos(h),forward*Math.Cos(h)-side*Math.Sin(h));
                        motions[i]=new SkiState(0,Math.Sqrt(forward*forward+side*side));
                    }
                    World=new ManualScopedWorldRoster(Source.Catalogs,Source.Source.Venue.Occupancy,
                        ManualScopedFieldStepperTests.Contact(),ManualScopedFieldStepperTests.Work(),cursors,poses,velocities,motions,20,
                        responseDefinition:ManualScopedFieldStepperTests.Group(),actorMasses:new[]{75d,75});
                }
                var system=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),Source.Source.Venue.Occupancy,20);
                for(int i=0;i<2;i++)
                {
                    var a=World.GetActor(i);var c=World.GetCursor(i);
                    Drives[i]=system.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),
                        new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),a.Observation,
                        Source.Catalogs[i].SampleSurface(a.Observation),arrival&&i==0?350:0,4,0,.05);
                    Windows[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,
                        Source.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
                }
            }
        }
        [Test] public void TinySeparatedStopDoesNotRestartTheSnowRampOrOtherActors()
        {
            var f=new Fixture();var tick=f.World.BeginTick();
            Assert.That(f.Drives[0].ConstantAccelerationSeconds,Is.GreaterThan(0).And.LessThan(1e-6));
            Assert.That(f.World.TryPreviewContactDrivenMotion(tick,f.Drives,f.Candidates),Is.True);
            Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
            for(int i=0;i<2;i++)foreach(double fraction in new[]{0d,.01,.5,1})
            {
                Assert.That(f.Candidates[i].Trajectory.DurationSeconds,Is.EqualTo(.05));
                Assert.That(f.Candidates[i].Trajectory.Sample(fraction),Is.EqualTo(f.Drives[i].Sample(fraction)));
                Assert.That(f.Candidates[i].Trajectory.VelocityAt(fraction),Is.EqualTo(f.Drives[i].MotionAt(fraction).Velocity));
            }
            Assert.That(f.World.TryAppendToOwnedFirstEvent(tick,f.Candidates,f.Windows,Array.Empty<SkiActorCourseEdge>(),0,f.Grades,out var paid),Is.True);
            Assert.That(paid.PaidSeconds,Is.EqualTo(.05));Assert.That(f.World.GetWorkingMotionPieceCount(tick),Is.EqualTo(1));
            Assert.That(f.World.TryFinalizeTick(tick,out var proof),Is.True);f.World.Commit(proof);
            Assert.That(f.World.GetActor(0).Velocity,Is.EqualTo(f.Drives[0].MotionAt(1).Velocity));
            Assert.That(f.World.GetPhysicalMotion(1).DistanceMeters,Is.GreaterThan(.029));
        }
        [Test] public void AFreePreviewStillSchedulesTheActualFutureBodyArrival()
        {
            var f=new Fixture(arrival:true);var tick=f.World.BeginTick();
            Assert.That(f.World.TryPreviewContactDrivenMotion(tick,f.Drives,f.Candidates),Is.True);
            Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
            // A genuinely constant-force field keeps its established polynomial
            // representation; the stop/cap exception is unnecessary here.
            Assert.That(f.Candidates[0].Trajectory.TryGetConstantAngularAcceleration(out _),Is.True);
            Assert.That(f.World.TryAppendToOwnedFirstEvent(tick,f.Candidates,f.Windows,Array.Empty<SkiActorCourseEdge>(),0,f.Grades,out var arrival),Is.True);
            Assert.That(arrival.Status,Is.EqualTo(ManualScopedWorldEventStatus.Contact));
            Assert.That(arrival.PaidSeconds,Is.GreaterThan(0).And.LessThan(.05));
            Assert.That(f.World.HasPendingContact(tick),Is.True);Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.That(f.World.TryFinalizeTick(tick,out _),Is.False);f.World.AbortTick(tick);
        }
        [Test] public void InvalidLastFreeSourceCannotExposeAPartialCandidateRoster()
        {
            var f=new Fixture();var tick=f.World.BeginTick();f.Drives[1]=default;
            Assert.Throws<ArgumentException>(()=>f.World.TryPreviewContactDrivenMotion(tick,f.Drives,f.Candidates));
            for(int i=0;i<2;i++)Assert.That(f.Candidates[i].Trajectory.IsValid,Is.False);
            Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);f.World.AbortTick(tick);
        }
        [Test] public void RepeatedFreePreviewAndFullPaidClockAllocateNothing()
        {
            var f=new Fixture();
            bool Cycle()
            {
                var tick=f.World.BeginTick();bool ok=f.World.TryPreviewContactDrivenMotion(tick,f.Drives,f.Candidates);
                ok&=f.World.TryAppendToOwnedFirstEvent(tick,f.Candidates,f.Windows,Array.Empty<SkiActorCourseEdge>(),0,f.Grades,out _);
                ok&=f.World.TryFinalizeTick(tick,out _);return ok;
            }
            for(int i=0;i<128;i++)Assert.That(Cycle(),Is.True);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=Cycle();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);Assert.That(f.World.Tick.Value,Is.Zero);
        }
    }
}
