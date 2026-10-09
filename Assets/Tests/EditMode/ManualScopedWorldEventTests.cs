using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedWorldEventTests
    {
        private const long N=SkiState.NanometersPerMeter;
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        private static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"event_contacts\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        private static SkiContactSweepDefinition Work()=>SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"event_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
        private static SkiContactGroupDefinition Group()=>SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"event_group\",\"maximum_passes\":128,\"closing_speed_tolerance_mps\":0.00000001}");
        private static ManualScopedWorldWindow[] Windows(ManualScopedWorldRoster world,ManualScopedWorldTick tick,ManualRacePathCatalog[] catalogs,long[] caps=null)
        {
            var windows=new ManualScopedWorldWindow[world.Count];
            for(int i=0;i<world.Count;i++)
            {
                var c=world.GetWorkingCursor(tick,i);
                windows[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,2,caps==null?catalogs[i].Get(c.OccurrenceIndex).LengthNanometers:caps[i]);
            }
            return windows;
        }
        private static SkiWorldMotionCandidate[] Passive(ManualScopedWorldRoster world,ManualScopedWorldTick tick,OccupancyDefinition body)
        {
            var candidates=new SkiWorldMotionCandidate[world.Count];var snow=new ManualSkiSnowContactSystem(body,20);
            for(int i=0;i<world.Count;i++)
            {
                var a=world.GetWorkingActor(tick,i);var b=new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75);
                candidates[i]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(b,
                    new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),0,0,world.GetRemainingSeconds(tick))));
            }
            return candidates;
        }
        private static double[] Grades(ManualScopedWorldRoster world,ManualScopedWorldTick tick)
        {
            var result=new double[world.Count];for(int i=0;i<result.Length;i++){var a=world.GetWorkingActor(tick,i);result[i]=a.Observation.GradeAlong(a.Pose);}return result;
        }
        [TestCase("start_seam")][TestCase("base_seam")]
        public void OrdinaryBoundaryAndContinuationPayOneWholeSessionTickWithoutStopping(string phase)
        {
            var f=new ManualScopedSessionTests.Fixture(phase);var world=f.World;var catalogs=new[]{f.Source.Catalog};
            var batch=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var pending=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0)};
            var original=world.GetActor(0);long odometer=world.GetPhysicalMotion(0).DistanceNanometers;int events=f.Events.Count;
            var tick=world.BeginTick();
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,catalogs),NoEdges,0,Grades(world,tick),out var boundary),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(boundary.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(boundary.BoundaryCount,Is.EqualTo(1));
            Assert.That(boundary.PaidSeconds,Is.InRange(.000001,.049));Assert.That(world.Tick,Is.EqualTo(f.Session.Tick));Assert.That(f.Events.Count,Is.EqualTo(events));
            var reached=world.GetLastBoundary(tick,0);Assert.That(reached.IsOccurrenceEnd,Is.True);Assert.That(reached.End,Is.EqualTo(ManualRacePathEnd.Continue));
            var at=world.GetWorkingActor(tick,0);var motion=world.GetWorkingPhysicalMotion(tick,0);var cursor=world.GetWorkingCursor(tick,0);
            Assert.That(world.TryHandoffOrdinary(tick,new[]{reached.ActorIndex}),Is.True);
            Assert.That(world.GetWorkingActor(tick,0).Pose,Is.EqualTo(at.Pose));Assert.That(world.GetWorkingActor(tick,0).Velocity,Is.EqualTo(at.Velocity));
            Assert.That(world.GetWorkingPhysicalMotion(tick,0),Is.EqualTo(motion));Assert.That(world.GetWorkingCursor(tick,0).CreditedRouteNanometers,Is.EqualTo(cursor.CreditedRouteNanometers));
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,catalogs),NoEdges,0,Grades(world,tick),out var suffix),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(suffix.Status,Is.EqualTo(ManualScopedWorldEventStatus.Clear));Assert.That(boundary.PaidSeconds+suffix.PaidSeconds,Is.EqualTo(.05));
            Assert.Throws<ArgumentOutOfRangeException>(()=>world.GetLastBoundary(tick,0));
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);Assert.That(world.GetProposedMotionPieceCount(proof),Is.EqualTo(2));batch.Commit(pending,proof);
            Assert.That(f.Session.Motion.SpeedMetersPerSecond,Is.GreaterThan(0));Assert.That(f.Session.Motion.DistanceNanometers,Is.GreaterThan(odometer));
            Assert.That(world.GetActor(0).Velocity,Is.EqualTo(original.Velocity));Assert.That(world.Tick,Is.EqualTo(f.Session.Tick));
            Assert.That(world.GetActor(0).Pose.HeadingNanoradians,Is.EqualTo(original.Pose.HeadingNanoradians));
        }
        private static ManualScopedWorldRoster BoundWorld(ManualScopedSessionTests.Fixture f)
        {
            var original=f.World;var a=original.GetActor(0);
            return new ManualScopedWorldRoster(new[]{f.Source.Catalog},f.Source.Venue.Occupancy,Contact(),Work(),
                new[]{original.GetCursor(0)},new[]{a.Pose},new[]{a.Velocity},new[]{original.GetPhysicalMotion(0)},20,original.Tick,Group(),new[]{75.0});
        }
        private static SkiWorldMotionCandidate[] Coupled(ManualScopedSessionTests.Fixture f,ManualScopedWorldRoster world,
            ManualScopedWorldTick tick,SprintStepPreview pending)
        {
            var input=f.Session.GetPreparedManualMotionInput(pending,world,tick,0,0);var a=world.GetWorkingActor(tick,0);
            var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Source.Venue.Occupancy,20).Preview(
                new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),
                a.Observation,f.Source.Catalog.SampleSurface(a.Observation),input.DeliveredPowerWatts,0,0,world.GetRemainingSeconds(tick),
                input.DragAreaMultiplier,input.SnowResistanceMultiplier,input.WindX,input.WindZ);
            var output=new SkiWorldMotionCandidate[1];Assert.That(world.TryPreviewContactDrivenMotion(tick,new[]{drive},output),Is.True);return output;
        }
        [TestCase("penalty_entry_before")][TestCase("penalty_repeat_before")][TestCase("penalty_return_before")]
        public void SharedTractionBoundaryAndAuthorizedPenaltySuffixPublishWithoutAWait(string phase)
        {
            var f=new ManualScopedSessionTests.Fixture(phase);var world=BoundWorld(f);var catalogs=new[]{f.Source.Catalog};
            var batch=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var pending=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0)};
            var source=world.GetActor(0);var state=f.Session.CurrentRangeMotion;int events=f.Events.Count;var tick=world.BeginTick();
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Coupled(f,world,tick,pending[0]),Windows(world,tick,catalogs),NoEdges,0,Grades(world,tick),out var boundary),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(boundary.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(boundary.PaidSeconds,Is.InRange(.000001,.049));
            var at=world.GetWorkingActor(tick,0);var motion=world.GetWorkingPhysicalMotion(tick,0);var c=world.GetWorkingCursor(tick,0);var reached=world.GetLastBoundary(tick,0);
            Assert.That(reached.IsOccurrenceEnd,Is.True);Assert.That(reached.End,Is.EqualTo(phase=="penalty_entry_before"?ManualRacePathEnd.PenaltyDecision:ManualRacePathEnd.PenaltyCircuit));
            Assert.That(batch.TryHandoffWorkingPhases(tick,pending,new[]{reached.ActorIndex}),Is.True);
            Assert.That(world.GetWorkingActor(tick,0).Pose,Is.EqualTo(at.Pose));Assert.That(world.GetWorkingActor(tick,0).Velocity,Is.EqualTo(at.Velocity));
            Assert.That(world.GetWorkingPhysicalMotion(tick,0),Is.EqualTo(motion));Assert.That(world.GetWorkingCursor(tick,0).CreditedRouteNanometers,Is.EqualTo(c.CreditedRouteNanometers));
            Assert.That(f.Session.CurrentRangeMotion,Is.EqualTo(state));Assert.That(f.Events.Count,Is.EqualTo(events));
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Coupled(f,world,tick,pending[0]),Windows(world,tick,catalogs),NoEdges,0,Grades(world,tick),out var suffix),Is.True,world.LastAttempt.Status+" / "+world.LastAttempt.Course.Status);
            Assert.That(suffix.PaidSeconds+boundary.PaidSeconds,Is.EqualTo(.05));Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);
            batch.Commit(pending,proof);Assert.That(world.Tick,Is.EqualTo(f.Session.Tick));Assert.That(f.Session.Motion.SpeedMetersPerSecond,Is.GreaterThan(0));
            Assert.That(world.GetActor(0).Pose.HeadingNanoradians,Is.EqualTo(source.Pose.HeadingNanoradians));
            Assert.That(world.GetPhysicalMotion(0).DistanceNanometers,Is.GreaterThan(motion.DistanceNanometers));
            if(phase=="penalty_return_before")Assert.That(f.Session.GetBoutRangeFrame(0).Phase,Is.EqualTo(RangeTraversalPhase.Complete));
            else Assert.That(f.Session.CurrentRangeMotion.Value.Phase,Is.EqualTo(RangeTraversalPhase.PenaltyLoop));
        }
        [Test] public void ContactBeforeCapIsResolvedBeforeTheSameClockReachesTheCap()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers+N*7/100,f.Catalogs[1].Get(1).LengthNanometers};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var contact),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(contact.Status,Is.EqualTo(ManualScopedWorldEventStatus.Contact));Assert.That(contact.BoundaryCount,Is.Zero);
            Assert.That(contact.PaidSeconds,Is.InRange(.024,.026));Assert.That(world.HasPendingContact(tick),Is.True);
            double time=world.GetElapsedSeconds(tick);Assert.That(world.TryFinalizeTick(tick,out _),Is.False);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out _),Is.False);
            Assert.That(world.GetElapsedSeconds(tick),Is.EqualTo(time));Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.True);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var boundary),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(boundary.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(boundary.BoundaryCount,Is.EqualTo(1));
            Assert.That(world.GetLastBoundary(tick,0).ActorIndex,Is.Zero);Assert.That(world.GetLastBoundary(tick,0).IsOccurrenceEnd,Is.False);
            Assert.That(world.GetWorkingActor(tick,0).Velocity.X*world.GetWorkingActor(tick,1).Velocity.X+
                world.GetWorkingActor(tick,0).Velocity.Z*world.GetWorkingActor(tick,1).Velocity.Z,Is.GreaterThan(0));
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs),NoEdges,0,Grades(world,tick),out var suffix),Is.True);
            Assert.That(suffix.Status,Is.EqualTo(ManualScopedWorldEventStatus.Clear));Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);world.Commit(proof);
            Assert.That(world.Tick.Value,Is.EqualTo(1));Assert.That(world.GetPhysicalMotion(1).SpeedMetersPerSecond,Is.GreaterThan(0));
            Assert.That(world.GetPhysicalMotion(1).DistanceNanometers,Is.GreaterThan(N));
        }
        [Test] public void CapBeforeContactCutsEveryAthleteOnTheSameTimeWithoutIssuingAResponse()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var candidates=Passive(world,tick,f.Source.Venue.Occupancy);var caps=new[]{world.GetCursor(0).ObservedArcNanometers+N/50,f.Catalogs[1].Get(1).LengthNanometers};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var boundary),Is.True);
            Assert.That(boundary.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(boundary.PaidSeconds,Is.EqualTo(.01).Within(1e-12));
            Assert.That(world.HasPendingContact(tick),Is.False);Assert.That(world.GetWorkingMotionPieceCount(tick),Is.EqualTo(1));
            for(int i=0;i<2;i++)Assert.That(world.GetWorkingActor(tick,i).Pose,Is.EqualTo(candidates[i].Trajectory.Sample(boundary.PaidSeconds/.05)));
            Assert.That(world.Tick.Value,Is.Zero);world.AbortTick(tick);Assert.That(world.GetCursor(0).ObservedArcNanometers,Is.EqualTo(caps[0]-N/50));
        }
        [Test] public void AnOwnedEndpointNotificationPaysNoTimeAndRequiresAnActualAuthorizedSuccessor()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:20,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers,f.Catalogs[1].Get(1).LengthNanometers};var before=world.GetWorkingActor(tick,0);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var boundary),Is.True);
            Assert.That(boundary.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(boundary.PaidSeconds,Is.Zero);
            Assert.That(world.GetWorkingActor(tick,0),Is.EqualTo(before));Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.GetWorkingMotionPieceCount(tick),Is.Zero);
            Assert.That(world.GetLastBoundary(tick,0).IsOccurrenceEnd,Is.False);Assert.That(world.TryHandoffOrdinary(tick,new[]{0}),Is.False);
            Assert.That(world.TryFinalizeTick(tick,out _),Is.False);world.AbortTick(tick);Assert.Throws<ArgumentException>(()=>world.GetLastBoundary(tick,0));
        }
        [Test] public void InvalidLastSourceCannotPublishEvenAZeroTimeFirstRowEvent()
        {
            var f=new ManualScopedWorldRosterTests.Fixture();var world=f.World;var tick=world.BeginTick();var candidates=Passive(world,tick,f.Source.Venue.Occupancy);
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers,f.Catalogs[1].Get(world.GetCursor(1).OccurrenceIndex).LengthNanometers};
            candidates[1]=new SkiWorldMotionCandidate(new AthleteId("foreign"),candidates[1].Trajectory);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);Assert.That(world.LastWorldEvent.IsValid,Is.False);Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.SourceMismatch));
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.GetWorkingMotionPieceCount(tick),Is.Zero);Assert.Throws<ArgumentOutOfRangeException>(()=>world.GetLastBoundary(tick,0));
        }
        [Test] public void DeepInitialOverlapCannotHideBehindAZeroTimeBoundaryNotification()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:.1,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers,f.Catalogs[1].Get(1).LengthNanometers};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactGeometry));Assert.That(world.GetElapsedSeconds(tick),Is.Zero);
        }
        [Test] public void ForceRootRetainsTheOriginalAccelerationAndActualSourceVelocity()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var pose=f.At(1,10*N);var body=new ManualSkiContactBody(new AthleteId("accelerating"),pose,default,75);
            Assert.That(f.Catalog.TryProject(pose,1,10*N,1,out var at),Is.True);
            var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Venue.Occupancy,20).Preview(body,default,at,f.Catalog.SampleSurface(at),350,0,0,.05);
            var output=new SkiForceCoupledTrajectory[1];var forces=new ManualSkiContactForces(f.Venue.Occupancy,Contact(),Group(),1);
            Assert.That(forces.Preview(new[]{body},new[]{drive},1,output).Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            var root=new ManualSkiWorldTrajectory(output[0]);var window=new ManualScopedWorldWindow(body.Athlete,1,10*N,1,10*N+N/500);
            var preview=new ManualRacePathBoundaryScheduler(f.Catalog,Work()).Preview(root,window);Assert.That(preview.Status,Is.EqualTo(ManualRacePathBoundaryStatus.Boundary));
            double acceleration=output[0].Acceleration.X*Math.Sin(pose.HeadingRadians)+output[0].Acceleration.Z*Math.Cos(pose.HeadingRadians);
            Assert.That(preview.Seconds,Is.EqualTo(Math.Sqrt(.004/acceleration)).Within(2e-10));
            var prefix=root.Slice(0,preview.Seconds);Assert.That(prefix.Sample(0),Is.EqualTo(pose));Assert.That(prefix.VelocityAt(0),Is.EqualTo(default(SkiPlanarVelocity)));
            Assert.That(prefix.Sample(1),Is.EqualTo(root.Sample(preview.Seconds/.05)));Assert.That(prefix.TryGetConstantPlanarAcceleration(out var actual),Is.True);
            Assert.That(actual.X,Is.EqualTo(output[0].Acceleration.X));Assert.That(actual.Z,Is.EqualTo(output[0].Acceleration.Z));
        }
        [Test] public void OriginalForceBreakpointsAreSplitBeforePolynomialBoundarySearch()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var pose=f.At(1,10*N);double h=pose.HeadingRadians;
            var velocity=new SkiPlanarVelocity(2*Math.Sin(h),2*Math.Cos(h));var body=new ManualSkiContactBody(new AthleteId("braking"),pose,velocity,75);
            Assert.That(f.Catalog.TryProject(pose,1,10*N,1,out var at),Is.True);
            var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Venue.Occupancy,20).Preview(body,new SkiContactMotion(velocity,0),at,f.Catalog.SampleSurface(at),0,0,0,.05,brakingNewtons:100000);
            var root=new ManualSkiWorldTrajectory(drive);Assert.That(drive.ConstantAccelerationSeconds,Is.InRange(.00001,.049));Assert.That(root.TryGetConstantPlanarAcceleration(out _),Is.False);
            var window=new ManualScopedWorldWindow(body.Athlete,1,10*N,1,10*N+100000);var scheduler=new ManualRacePathBoundaryScheduler(f.Catalog,Work());
            var whole=scheduler.Preview(root,window);
            Assert.That(whole.Status,Is.EqualTo(ManualRacePathBoundaryStatus.Boundary));
            var piece=root.Slice(0,drive.ConstantAccelerationSeconds);Assert.That(piece.TryGetConstantPlanarAcceleration(out _),Is.True);
            var boundary=scheduler.Preview(piece,window);Assert.That(boundary.Status,Is.EqualTo(ManualRacePathBoundaryStatus.Boundary));Assert.That(boundary.Seconds,Is.LessThan(drive.ConstantAccelerationSeconds));
            Assert.That(whole.Seconds,Is.EqualTo(boundary.Seconds));
            Assert.That(root.Sample(whole.Seconds/root.DurationSeconds),Is.EqualTo(piece.Sample(boundary.Seconds/piece.DurationSeconds)));
            Assert.That(root.Slice(0,whole.Seconds).VelocityAt(0),Is.EqualTo(velocity));
        }
        [Test] public void CurvedCommandedHeadingIsNotFittedToAFalseConstantAccelerationChord()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var pose=f.At(1,10*N);var row=f.Catalog.Get(1);
            var surface=new TrackSampler(f.Venue.Track.GetLoop(row.LoopId)).Sample(TrackDistance.FromMeters(row.LocalStartNanometers/(double)N+10));
            var travel=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(new SkiState(0,2),350,surface,.05);
            var heading=new ManualSkiSteeringSystem(ManualSkiForceTrajectoryTests.HeadingDefinition(),20).Preview(pose,800,travel,1,4);
            var root=new ManualSkiWorldTrajectory(heading);Assert.That(root.TryGetConstantPlanarAcceleration(out _),Is.False);
            var window=new ManualScopedWorldWindow(new AthleteId("curved"),1,10*N,1,10*N+N/20);
            Assert.That(new ManualRacePathBoundaryScheduler(f.Catalog,Work()).Preview(root,window).Status,Is.EqualTo(ManualRacePathBoundaryStatus.UnsupportedTrajectory));
        }
        private static ManualSkiWorldTrajectory Curved(ManualScopedWorldRosterTests.Fixture f,int actor=0)
        {
            var a=f.World.GetActor(actor);var surface=f.Catalogs[actor].SampleSurface(a.Observation).WithTravelGeometry(0,0);
            var travel=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(new SkiState(0,2),350,surface,.05);
            return new ManualSkiWorldTrajectory(new ManualSkiSteeringSystem(ManualSkiForceTrajectoryTests.HeadingDefinition(),20).Preview(a.Pose,800,travel,1,4));
        }
        [Test] public void ACurvedIntervalEndingAtItsCapCannotBeMisreportedAsEventFree()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:20,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var root=Curved(f);var c=world.GetCursor(0);
            Assert.That(f.Catalogs[0].TryProject(root.Sample(1),c.OccurrenceIndex,c.ObservedArcNanometers,2,out var end),Is.True);
            var caps=new[]{end.PhysicalArcNanometers,f.Catalogs[1].Get(1).LengthNanometers};var windows=Windows(world,tick,f.Catalogs,caps);
            Assert.That(new ManualRacePathGateSweep(f.Catalogs[0],Work()).Preview(root,1,c.ObservedArcNanometers,2,caps[0]).Status,Is.EqualTo(ManualRacePathGateStatus.Clear));
            var preview=new ManualRacePathBoundaryScheduler(f.Catalogs[0],Work()).Preview(root,windows[0]);
            Assert.That(preview.Status,Is.EqualTo(ManualRacePathBoundaryStatus.UnsupportedTrajectory));
            var candidates=Passive(world,tick,f.Source.Venue.Occupancy);candidates[0]=new SkiWorldMotionCandidate(c.Athlete,root);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,Grades(world,tick),out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.BoundarySchedule));
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.GetWorkingMotionPieceCount(tick),Is.Zero);
        }
        [Test] public void AnEarlierContactIsAdmittedBeforeALaterUnresolvedCurvedBoundary()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var c=world.GetCursor(0);var caps=new[]{c.ObservedArcNanometers+N*8/100,f.Catalogs[1].Get(1).LengthNanometers};
            var candidates=Passive(world,tick,f.Source.Venue.Occupancy);candidates[0]=new SkiWorldMotionCandidate(c.Athlete,Curved(f));
            var windows=Windows(world,tick,f.Catalogs,caps);
            Assert.That(new ManualRacePathBoundaryScheduler(f.Catalogs[0],Work()).Preview(candidates[0].Trajectory,windows[0]).Status,Is.EqualTo(ManualRacePathBoundaryStatus.UnsupportedTrajectory));
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,Grades(world,tick),out var contact),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(contact.Status,Is.EqualTo(ManualScopedWorldEventStatus.Contact));Assert.That(contact.BoundaryCount,Is.Zero);
            Assert.That(world.GetWorkingCursor(tick,0).ObservedArcNanometers,Is.LessThan(caps[0]));Assert.That(world.HasPendingContact(tick),Is.True);
            Assert.That(contact.PaidSeconds,Is.InRange(.001,.04));Assert.That(world.Tick.Value,Is.Zero);world.AbortTick(tick);
        }
        [Test] public void FiringEndpointNotificationCannotAuthorizeDepartureBeforeCompletedShooting()
        {
            var f=new ManualScopedSessionTests.Fixture("shooting");var world=f.World;var tick=world.BeginTick();var catalogs=new[]{f.Source.Catalog};
            var batch=new ManualScopedRosterSessionCommit(new[]{f.Session},world);var pending=new[]{f.Session.PrepareManualScopedStep(f.Command(),world,0)};
            int events=f.Events.Count;var phase=f.Session.CurrentRangeMotion;
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,catalogs),NoEdges,0,Grades(world,tick),out var e),Is.True);
            Assert.That(e.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(e.PaidSeconds,Is.Zero);Assert.That(world.GetLastBoundary(tick,0).End,Is.EqualTo(ManualRacePathEnd.FiringLine));
            Assert.That(batch.TryHandoffWorkingPhases(tick,pending,new[]{0}),Is.False);Assert.That(world.TryHandoffOrdinary(tick,new[]{0}),Is.False);
            Assert.That(f.Session.CurrentRangeMotion,Is.EqualTo(phase));Assert.That(f.Events.Count,Is.EqualTo(events));Assert.That(world.GetElapsedSeconds(tick),Is.Zero);world.AbortTick(tick);
        }
        [Test] public void AReverseCrossingOfTheScopeStartRemainsBlockedAndPaysNoWorldTime()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:true,startMask:2,secondSpeed:-2,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs),NoEdges,0,Grades(world,tick),out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.BoundarySchedule));
            Assert.That(world.LastAttempt.Gate.BoundaryArcNanometers,Is.Zero);Assert.That(world.GetElapsedSeconds(tick),Is.Zero);
        }
        [Test] public void WarmActualBoundarySearchAndAbortAllocateNothing()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:20,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers+N/50,f.Catalogs[1].Get(1).LengthNanometers};
            var candidates=Passive(world,tick,f.Source.Venue.Occupancy);var windows=Windows(world,tick,f.Catalogs,caps);var grades=Grades(world,tick);world.AbortTick(tick);
            bool passed=true;
            for(int i=0;i<128;i++){tick=world.BeginTick();passed&=world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out var e)&&e.Status==ManualScopedWorldEventStatus.Boundary;world.AbortTick(tick);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){tick=world.BeginTick();passed&=world.TryAppendToOwnedFirstEvent(tick,candidates,windows,NoEdges,0,grades,out var e)&&e.Status==ManualScopedWorldEventStatus.Boundary;world.AbortTick(tick);}
            long used=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(passed,Is.True);Assert.That(used,Is.Zero);
        }
        [Test] public void ContactToleranceCannotPrematurelyGrantTheNearbyPhaseBoundary()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers+N/20,f.Catalogs[1].Get(1).LengthNanometers};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var e),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(e.Status,Is.EqualTo(ManualScopedWorldEventStatus.Contact));Assert.That(e.BoundaryCount,Is.Zero);
            Assert.That(world.GetWorkingCursor(tick,0).ObservedArcNanometers,Is.LessThan(caps[0]));Assert.That(world.HasPendingContact(tick),Is.True);
            Assert.That(world.TryHandoffOrdinary(tick,new[]{0}),Is.False);Assert.That(world.TryFinalizeTick(tick,out _),Is.False);
            Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.True);
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var boundary),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(boundary.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(boundary.PaidSeconds,Is.GreaterThan(0));
            Assert.That(boundary.PaidSeconds,Is.LessThan(.0001));Assert.That(world.GetLastBoundary(tick,0).ActorIndex,Is.Zero);
            Assert.That(world.GetWorkingPhysicalMotion(tick,0).SpeedMetersPerSecond,Is.GreaterThan(0));
        }
        [Test] public void SimultaneousBoundaryOrderAndWorldStateAreIndependentOfRosterPermutation()
        {
            var source=new ManualRacePathCatalogTests.Fixture();var snapshots=new ManualRacePathActorFrame[2];double firstTime=0;
            for(int permutation=0;permutation<2;permutation++)
            {
                var catalogs=new[]{source.Catalog,source.Catalog};var cursors=new ManualRacePathCursor[2];var poses=new ManualSkiHeadingState[2];
                var velocities=new SkiPlanarVelocity[2];var motions=new[]{new SkiState(0,2),new SkiState(0,2)};
                for(int i=0;i<2;i++)
                {
                    int p=permutation==0?i:1-i;var id=new AthleteId(p==0?"z":"a");poses[i]=source.At(1,10*N,p==0?-1:1);double h=poses[i].HeadingRadians;
                    velocities[i]=new SkiPlanarVelocity(2*Math.Sin(h),2*Math.Cos(h));
                    cursors[i]=new ManualRacePathProgression(source.Catalog).Reconstitute(id,1,10*N,10*N,source.Catalog.Get(1).RouteStartNanometers+10*N);
                }
                var world=new ManualScopedWorldRoster(catalogs,source.Venue.Occupancy,Contact(),Work(),cursors,poses,velocities,motions,20,
                    responseDefinition:Group(),actorMasses:new[]{75.0,75});var tick=world.BeginTick();var caps=new[]{10*N+N/25,10*N+N/25};
                Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,source.Venue.Occupancy),Windows(world,tick,catalogs,caps),NoEdges,0,Grades(world,tick),out var e),Is.True);
                Assert.That(e.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(e.BoundaryCount,Is.EqualTo(2));
                Assert.That(world.GetLastBoundary(tick,0).Athlete.Value,Is.EqualTo("a"));Assert.That(world.GetLastBoundary(tick,1).Athlete.Value,Is.EqualTo("z"));
                if(permutation==0){firstTime=e.PaidSeconds;for(int i=0;i<2;i++)snapshots[i]=world.GetWorkingActor(tick,i);}
                else
                {
                    Assert.That(e.PaidSeconds,Is.EqualTo(firstTime));
                    for(int i=0;i<2;i++)Assert.That(world.GetWorkingActor(tick,i),Is.EqualTo(snapshots[1-i]));
                }
            }
        }
        [Test] public void ATinyEndpointRemainderIsPaidRatherThanRoundedIntoAWholeTick()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:20,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var caps=new[]{world.GetCursor(0).ObservedArcNanometers+N/10,f.Catalogs[1].Get(1).LengthNanometers};
            Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs,caps),NoEdges,0,Grades(world,tick),out var e),Is.True);
            Assert.That(e.Status,Is.EqualTo(ManualScopedWorldEventStatus.Boundary));Assert.That(e.BoundaryCount,Is.EqualTo(1));Assert.That(e.PaidSeconds,Is.EqualTo(.05).Within(1e-12));
            double remaining=world.GetRemainingSeconds(tick);
            if(remaining>0)
            {
                Assert.That(world.TryFinalizeTick(tick,out _),Is.False);Assert.That(remaining,Is.LessThan(1e-12));
                Assert.That(world.TryAppendToOwnedFirstEvent(tick,Passive(world,tick,f.Source.Venue.Occupancy),Windows(world,tick,f.Catalogs),NoEdges,0,Grades(world,tick),out var suffix),Is.True);
                Assert.That(suffix.PaidSeconds,Is.EqualTo(remaining));Assert.That(world.GetWorkingPhysicalMotion(tick,0).SpeedMetersPerSecond,Is.GreaterThan(0));
            }
            Assert.That(world.GetElapsedSeconds(tick),Is.EqualTo(.05));Assert.That(world.TryFinalizeTick(tick,out _),Is.True);
        }
    }
}
