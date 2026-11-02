using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedShootingHoldTests
    {
        private const long N=SkiState.NanometersPerMeter;
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        private sealed class Fixture
        {
            internal readonly ManualScopedSessionTests.Fixture Source=new ManualScopedSessionTests.Fixture("shooting");
            internal readonly ManualScopedWorldRoster World;
            internal readonly ManualRacePathCatalog[] Catalogs;
            internal Fixture(bool mixed=false,bool reverseOrder=false,double velocity=0,double yaw=0)
            {
                var original=Source.World;var mat=original.GetActor(0);int count=mixed?2:1;
                var poses=new ManualSkiHeadingState[count];var velocities=new SkiPlanarVelocity[count];var cursors=new ManualRacePathCursor[count];
                var motion=new SkiState[count];Catalogs=new ManualRacePathCatalog[count];
                int held=mixed&&reverseOrder?1:0;poses[held]=new ManualSkiHeadingState(mat.Pose.X,mat.Pose.Z,mat.Pose.HeadingRadians,yaw);
                velocities[held]=new SkiPlanarVelocity(velocity*Math.Sin(mat.Pose.HeadingRadians),velocity*Math.Cos(mat.Pose.HeadingRadians));
                cursors[held]=original.GetCursor(0);motion[held]=new SkiState(original.GetPhysicalMotion(0).DistanceNanometers,Math.Abs(velocity));
                Catalogs[held]=Source.Source.Catalog;
                if(mixed)
                {
                    int moving=1-held;var catalog=Catalogs[moving]=Source.Source.Catalog;poses[moving]=Source.Source.At(1,10*N);
                    velocities[moving]=new SkiPlanarVelocity(6*Math.Sin(poses[moving].HeadingRadians),6*Math.Cos(poses[moving].HeadingRadians));
                    cursors[moving]=new ManualRacePathProgression(catalog).Reconstitute(new AthleteId("moving"),1,10*N,10*N,catalog.Get(1).RouteStartNanometers+10*N);
                    motion[moving]=new SkiState(0,6);
                }
                var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"hold_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
                var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"hold_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
                World=new ManualScopedWorldRoster(Catalogs,Source.Source.Venue.Occupancy,contact,work,cursors,poses,velocities,motion,20,original.Tick);
            }
            internal SkiWorldMotionCandidate[] Candidates(ManualScopedWorldTick tick)
            {
                var result=new SkiWorldMotionCandidate[World.Count];var snow=new ManualSkiSnowContactSystem(Source.Source.Venue.Occupancy,20);
                for(int i=0;i<result.Length;i++)
                {
                    var a=World.GetWorkingActor(tick,i);var body=new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75);
                    result[i]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(body,
                        new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),0,0,World.GetRemainingSeconds(tick))));
                }
                return result;
            }
            internal ManualScopedWorldWindow[] Windows(ManualScopedWorldTick tick)
            {
                var result=new ManualScopedWorldWindow[World.Count];
                for(int i=0;i<result.Length;i++){var c=World.GetWorkingCursor(tick,i);result[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,
                    c.ObservedArcNanometers,2,Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);}
                return result;
            }
            internal double[] Grades(ManualScopedWorldTick tick)
            {var result=new double[World.Count];for(int i=0;i<result.Length;i++){var a=World.GetWorkingActor(tick,i);result[i]=a.Observation.GradeAlong(a.Pose);}return result;}
        }
        [Test] public void ExplicitFiringHoldPaysACompleteShootingSessionTickWithoutDeparture()
        {
            var f=new Fixture();var world=f.World;var a=world.GetActor(0);var motion=world.GetPhysicalMotion(0);var c=world.GetCursor(0);
            var session=f.Source.Session;var batch=new ManualScopedRosterSessionCommit(new[]{session},world);
            var pending=session.PrepareManualScopedStep(f.Source.Command(),world,0);var tick=world.BeginTick();
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,f.Candidates(tick),f.Windows(tick),NoEdges,0,f.Grades(tick),out var e,new[]{0}),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(e.Status,Is.EqualTo(ManualScopedWorldEventStatus.Clear));Assert.That(e.BoundaryCount,Is.Zero);Assert.That(e.PaidSeconds,Is.EqualTo(.05));
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);batch.Commit(new[]{pending},proof);
            Assert.That(world.GetActor(0).Pose,Is.EqualTo(a.Pose));Assert.That(world.GetActor(0).Velocity,Is.EqualTo(a.Velocity));
            Assert.That(world.GetPhysicalMotion(0),Is.EqualTo(motion));Assert.That(world.GetCursor(0).CreditedRouteNanometers,Is.EqualTo(c.CreditedRouteNanometers));
            Assert.That(world.GetCursor(0).OccurrenceIndex,Is.EqualTo(c.OccurrenceIndex));Assert.That(session.Tick,Is.EqualTo(world.Tick));
            Assert.That(session.ActiveShooting.Phase,Is.Not.EqualTo(ShootingPhase.Outside));
            Assert.That(batch.TryHandoffAcceptedPhases(new[]{0}),Is.False,"A paid shooting hold cannot replace completed shooting authorization.");
        }
        [TestCase(false)][TestCase(true)] public void AnAimingAthleteCannotBlockTheOtherAthletesCommonClock(bool reverseOrder)
        {
            var f=new Fixture(mixed:true,reverseOrder:reverseOrder);var world=f.World;int held=reverseOrder?1:0,moving=1-held;
            var shooter=world.GetActor(held);var skier=world.GetActor(moving);long oldDistance=world.GetPhysicalMotion(moving).DistanceNanometers;
            var tick=world.BeginTick();Assert.That(world.TryAppendToOwnedFirstEvent(tick,f.Candidates(tick),f.Windows(tick),NoEdges,0,f.Grades(tick),out var e,new[]{held}),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(e.Status,Is.EqualTo(ManualScopedWorldEventStatus.Clear));Assert.That(e.PaidSeconds,Is.EqualTo(.05));
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);world.Commit(proof);
            Assert.That(world.GetActor(held).Pose,Is.EqualTo(shooter.Pose));Assert.That(world.GetActor(moving).Pose,Is.Not.EqualTo(skier.Pose));
            Assert.That(world.GetActor(moving).Velocity,Is.EqualTo(skier.Velocity));
            Assert.That(world.GetPhysicalMotion(moving).DistanceNanometers,Is.GreaterThan(oldDistance));
        }
        [TestCase(.001,0)][TestCase(-.001,0)][TestCase(0,.001)] public void APhysicalVelocityOrSpinCannotBeRoundedIntoAShootingHold(double velocity,double yaw)
        {
            var f=new Fixture(velocity:velocity,yaw:yaw);var world=f.World;var original=world.GetActor(0);var tick=world.BeginTick();
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,f.Candidates(tick),f.Windows(tick),NoEdges,0,f.Grades(tick),out var rejected,new[]{0}),Is.False);
            Assert.That(rejected.IsValid,Is.False);Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ShootingHold));
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.GetActor(0).Pose,Is.EqualTo(original.Pose));
            Assert.That(world.TryFinalizeTick(tick,out _),Is.False);world.AbortTick(tick);
        }
        [Test] public void AForceDrivenDepartureFromRestCannotHideBehindAHoldMarker()
        {
            var f=new Fixture();var world=f.World;var tick=world.BeginTick();var a=world.GetWorkingActor(tick,0);
            var frame=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Source.Source.Venue.Occupancy,20).Preview(
                new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,0),a.Observation,
                f.Source.Source.Catalog.SampleSurface(a.Observation),350,0,0,.05);
            Assert.That(new ManualSkiWorldTrajectory(frame).PlanarAccelerationBound,Is.GreaterThan(0));
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,new[]{new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(frame))},
                f.Windows(tick),NoEdges,0,f.Grades(tick),out _,new[]{0}),Is.False);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ShootingHold));Assert.That(world.GetElapsedSeconds(tick),Is.Zero);
        }
        [Test] public void OrdinaryBoundaryAndPartialFiringCapsCannotBecomeAnUnpaidQueue()
        {
            var baseSource=new ManualScopedSessionTests.Fixture("base_seam");var world=baseSource.World;var tick=world.BeginTick();var a=world.GetActor(0);var c=world.GetCursor(0);
            var snow=new ManualSkiSnowContactSystem(baseSource.Source.Venue.Occupancy,20);var candidate=new[]{new SkiWorldMotionCandidate(a.Athlete,
                new ManualSkiWorldTrajectory(snow.PreviewTrajectory(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,0),0,0,.05)))};
            var window=new[]{new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,2,baseSource.Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers)};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidate,window,NoEdges,0,new[]{a.Observation.GradeAlong(a.Pose)},out _,new[]{0}),Is.False);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ShootingHold));Assert.That(world.GetElapsedSeconds(tick),Is.Zero);world.AbortTick(tick);
            var f=new Fixture();world=f.World;tick=world.BeginTick();var windows=f.Windows(tick);c=world.GetCursor(0);
            // A smaller cap cannot be supplied at all once its high water has
            // reached the mat. General phase validation rejects it first.
            windows[0]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,2,c.ObservedArcNanometers-1);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,f.Candidates(tick),windows,NoEdges,0,f.Grades(tick),out _,new[]{0}),Is.False);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));Assert.That(world.GetElapsedSeconds(tick),Is.Zero);
        }
        [Test] public void HoldStillRequiresTheWholeBodyCourseAndExplicitEdgeProof()
        {
            var f=new Fixture();var world=f.World;var a=world.GetActor(0);var tick=world.BeginTick();
            var plane=new SkiCourseEdgePlane("hold_forbidden_edge",a.Pose.X+1,a.Pose.Z,1,0);
            var edges=new[]{new SkiActorCourseEdge(a.Athlete,plane)};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,f.Candidates(tick),f.Windows(tick),edges,1,f.Grades(tick),out _,new[]{0}),Is.False);
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.LastAttempt.Status,
                Is.EqualTo(ManualScopedWorldStatus.ContactSchedule).Or.EqualTo(ManualScopedWorldStatus.BodyOrEdge));
        }
        [Test] public void DuplicateForeignHoldActorsFailAndTheMarkerDoesNotPersistIntoTheNextCall()
        {
            var f=new Fixture();var world=f.World;var tick=world.BeginTick();var candidates=f.Candidates(tick);var windows=f.Windows(tick);var grades=f.Grades(tick);
            Assert.Catch(()=>world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out _,new[]{0,0}));
            Assert.Catch(()=>world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out _,new[]{1}));
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out var original),Is.True);
            Assert.That(original.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(original.PaidSeconds,Is.Zero);world.AbortTick(tick);
            tick=world.BeginTick();Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out var hold,new[]{0}),Is.True);
            Assert.That(hold.PaidSeconds,Is.EqualTo(.05));world.AbortTick(tick);
        }
        [Test] public void WarmStationaryFiringClockAndAbortAllocateNothing()
        {
            var f=new Fixture();var world=f.World;var tick=world.BeginTick();var candidates=f.Candidates(tick);var windows=f.Windows(tick);var grades=f.Grades(tick);
            var held=new[]{0};world.AbortTick(tick);bool passed=true;
            for(int i=0;i<128;i++){tick=world.BeginTick();passed&=world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out var e,held)&&e.PaidSeconds==.05;world.AbortTick(tick);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){tick=world.BeginTick();passed&=world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out var e,held)&&e.PaidSeconds==.05;world.AbortTick(tick);}
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(passed,Is.True);Assert.That(bytes,Is.Zero);
            Assert.That(world.ContextJson,Does.Contain("explicit_stationary_owned_firing_hold_v2_admitted_folded_endpoint_plant"));
        }
    }
}
