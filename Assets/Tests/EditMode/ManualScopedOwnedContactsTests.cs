using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedOwnedContactsTests
    {
        private static readonly SkiActorCourseEdge[] NoEdges=Array.Empty<SkiActorCourseEdge>();
        private static readonly double[] Grades={0,0};
        private static ManualScopedWorldRosterTests.Fixture AtContact(bool configured=true,double overlap=0)
        {
            double length=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json")).LongitudinalSeparation;
            return new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:length-overlap,contactResponse:configured);
        }
        private static ManualScopedWorldWindow[] Windows(ManualScopedWorldRosterTests.Fixture f,ManualScopedWorldTick tick)
        {
            var result=new ManualScopedWorldWindow[2];
            for(int i=0;i<2;i++)
            {var c=f.World.GetWorkingCursor(tick,i);result[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);}
            return result;
        }
        private static SkiWorldMotionCandidate[] Driven(ManualScopedWorldRosterTests.Fixture f,ManualScopedWorldTick tick,double first=350,double second=350)
        {
            var result=new SkiWorldMotionCandidate[2];var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Source.Venue.Occupancy,20);
            for(int i=0;i<2;i++)
            {
                var a=f.World.GetWorkingActor(tick,i);var body=new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75);
                var motion=new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond);
                var frame=drive.Preview(body,motion,a.Observation,f.Catalogs[i].SampleSurface(a.Observation),i==0?first:second,4,0,f.World.GetRemainingSeconds(tick));
                result[i]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(frame));
            }
            return result;
        }
        private static ManualScopedWorldRoster Chain(string[] ids,int[] order,int passes=64,double lastOverlap=0)
        {
            var f=new ManualRacePathCatalogTests.Fixture();var catalogs=new ManualRacePathCatalog[3];var cursors=new ManualRacePathCursor[3];
            var poses=new ManualSkiHeadingState[3];var velocities=new SkiPlanarVelocity[3];var motions=new SkiState[3];
            double length=f.Venue.Occupancy.LongitudinalSeparation;
            for(int i=0;i<3;i++)
            {
                int p=order[i];long arc=(long)Math.Round((10+p*length-(p==2?lastOverlap:0))*SkiState.NanometersPerMeter);
                catalogs[i]=f.Catalog;poses[i]=f.At(1,arc);double speed=p==0?6:0,h=poses[i].HeadingRadians;
                velocities[i]=new SkiPlanarVelocity(speed*Math.Sin(h),speed*Math.Cos(h));motions[i]=new SkiState(0,speed);
                cursors[i]=new ManualRacePathProgression(f.Catalog).Reconstitute(new AthleteId(ids[p]),1,arc,arc,f.Catalog.Get(1).RouteStartNanometers+arc);
            }
            return new ManualScopedWorldRoster(catalogs,f.Venue.Occupancy,
                SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"owned_chain_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}"),
                SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"owned_chain_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}"),
                cursors,poses,velocities,motions,20,responseDefinition:SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"owned_chain_response\",\"maximum_passes\":"+passes+",\"closing_speed_tolerance_mps\":0.00000001}"),actorMasses:new[]{75.0,75,75});
        }
        [Test] public void ReachedResponseAndPaidSuffixNeedNoCallerRegisteredContactList()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var tick=f.World.BeginTick();
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,f.Candidates(),f.Windows(),NoEdges,0,Grades,out var arrival),Is.True);
            Assert.That(arrival.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
            Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.True);Assert.That(f.World.LastOwnedContacts.Count,Is.EqualTo(1));
            var pair=f.World.GetLastOwnedContactPair(0);Assert.That(pair.First,Is.EqualTo(f.World.GetActor(0).Athlete));
            double before=f.World.GetElapsedSeconds(tick);
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,Driven(f,tick),Windows(f,tick),NoEdges,0,Grades,out var suffix),Is.True);
            Assert.That(suffix.Status,Is.EqualTo(SkiContactScheduleStatus.Clear));Assert.That(suffix.Seconds,Is.EqualTo(.05-before));
            Assert.That(f.World.TryFinalizeTick(tick,out var proof),Is.True);f.World.Commit(proof);
            Assert.That(f.World.Tick.Value,Is.EqualTo(1));Assert.That(f.World.GetPhysicalMotion(1).SpeedMetersPerSecond,Is.GreaterThan(1));
        }
        [Test] public void ActualInitialProximityRespondsWithoutChangingPositionTimeOdometerOrAcceptedState()
        {
            var f=AtContact();var accepted=f.World.GetActor(0);var tick=f.World.BeginTick();
            var before=f.World.GetWorkingActor(tick,0);var motion=f.World.GetWorkingPhysicalMotion(tick,0);
            Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.True);Assert.That(f.World.LastAttempt.Response.Impulses,Is.GreaterThan(0));
            var after=f.World.GetWorkingActor(tick,0);Assert.That(after.Pose.X,Is.EqualTo(before.Pose.X));Assert.That(after.Pose.Z,Is.EqualTo(before.Pose.Z));
            Assert.That(after.Pose.HeadingNanoradians,Is.EqualTo(before.Pose.HeadingNanoradians));Assert.That(after.Velocity,Is.Not.EqualTo(before.Velocity));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);Assert.That(f.World.GetWorkingMotionPieceCount(tick),Is.Zero);
            Assert.That(f.World.GetWorkingPhysicalMotion(tick,0).DistanceNanometers,Is.EqualTo(motion.DistanceNanometers));
            Assert.That(f.World.GetActor(0),Is.EqualTo(accepted));Assert.That(f.World.Tick.Value,Is.Zero);f.World.AbortTick(tick);
            Assert.That(f.World.GetActor(0),Is.EqualTo(accepted));Assert.That(f.World.GetPhysicalMotion(0),Is.EqualTo(motion));
        }
        [Test] public void SeparatedBodiesAreNotRegisteredAndAreStillScheduledForFutureArrivals()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);var tick=f.World.BeginTick();
            Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.True);Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.World.GetLastOwnedContactPair(0));
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,f.Candidates(),f.Windows(),NoEdges,0,Grades,out var arrival),Is.True);
            Assert.That(arrival.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(f.World.HasPendingContact(tick),Is.True);
            var before=f.World.GetWorkingActor(tick,0);
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,f.Candidates(),f.Windows(),NoEdges,0,Grades,out _),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PendingContact));Assert.That(f.World.GetWorkingActor(tick,0),Is.EqualTo(before));
        }
        [Test] public void ASeparatedPreviousPairIsDroppedOnTheNextOwnedClock()
        {
            var f=AtContact();var tick=f.World.BeginTick();Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.True);
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,Driven(f,tick,0,350),Windows(f,tick),NoEdges,0,Grades,out var separated),Is.True);
            Assert.That(separated.Status,Is.EqualTo(SkiContactScheduleStatus.Clear));Assert.That(f.World.LastOwnedContacts.Count,Is.EqualTo(1));
            Assert.That(f.World.TryFinalizeTick(tick,out var proof),Is.True);f.World.Commit(proof);
            tick=f.World.BeginTick();Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.True);Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,Driven(f,tick,0,350),Windows(f,tick),NoEdges,0,Grades,out var next),Is.True);
            Assert.That(next.Status,Is.EqualTo(SkiContactScheduleStatus.Clear));Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
        }
        [Test] public void UnequalClosingForcesRemainUnprovedInsteadOfMintingAStoppedClock()
        {
            var f=AtContact();var accepted=f.World.GetActor(0);var tick=f.World.BeginTick();Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.True);
            var before=f.World.GetWorkingActor(tick,0);var motion=f.World.GetWorkingPhysicalMotion(tick,0);
            Assert.That(f.World.TryAppendToOwnedFirstContact(tick,Driven(f,tick,350,0),Windows(f,tick),NoEdges,0,Grades,out var failed),Is.False);
            Assert.That(failed.Status,Is.EqualTo(SkiContactScheduleStatus.Unresolved));Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactSchedule));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);Assert.That(f.World.GetWorkingActor(tick,0),Is.EqualTo(before));
            Assert.That(f.World.GetWorkingPhysicalMotion(tick,0),Is.EqualTo(motion));Assert.That(f.World.TryFinalizeTick(tick,out _),Is.False);
            f.World.AbortTick(tick);Assert.That(f.World.GetActor(0),Is.EqualTo(accepted));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void DeepInitialOverlapIsNotPromotedIntoAnExistingContact()
        {
            var f=AtContact(overlap:.1);var before=f.World.GetActor(0);var tick=f.World.BeginTick();
            Assert.That(f.World.TryResolveOwnedCurrentContacts(tick),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactGeometry));
            Assert.That(f.World.LastOwnedContacts.IsValid,Is.False);Assert.That(f.World.LastOwnedContacts.Count,Is.Zero);
            Assert.That(f.World.LastOwnedContacts.Geometry.Status,Is.EqualTo(SkiContactPreviewStatus.Penetrating));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.World.GetLastOwnedContactPair(0));
            Assert.That(f.World.GetWorkingActor(tick,0),Is.EqualTo(before));Assert.That(f.World.HasPendingContact(tick),Is.False);
            Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);
        }
        [Test] public void MissingResponsePolicyAndStaleTicksCannotAuthorizeInitialImpulseOrContinuation()
        {
            var f=AtContact(configured:false);var before=f.World.GetActor(0);var tick=f.World.BeginTick();
            Assert.Throws<InvalidOperationException>(()=>f.World.TryResolveOwnedCurrentContacts(tick));
            Assert.That(f.World.HasPendingContact(tick),Is.False);Assert.That(f.World.GetWorkingActor(tick,0),Is.EqualTo(before));
            f.World.AbortTick(tick);Assert.Throws<ArgumentException>(()=>f.World.TryResolveOwnedCurrentContacts(tick));
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void CompleteInitialContactSetsAndResponseOrderFollowStableIdsInsteadOfRosterEnumeration()
        {
            var first=Chain(new[]{"z","a","m"},new[]{0,1,2});var second=Chain(new[]{"z","a","m"},new[]{2,1,0});
            var a=first.BeginTick();var b=second.BeginTick();Assert.That(first.TryResolveOwnedCurrentContacts(a),Is.True);
            Assert.That(second.TryResolveOwnedCurrentContacts(b),Is.True);Assert.That(first.LastOwnedContacts.Count,Is.EqualTo(2));
            Assert.That(first.GetLastOwnedContactPair(0).First.Value,Is.EqualTo("a"));Assert.That(first.GetLastOwnedContactPair(0).Second.Value,Is.EqualTo("m"));
            Assert.That(first.GetLastOwnedContactPair(1).First.Value,Is.EqualTo("a"));Assert.That(first.GetLastOwnedContactPair(1).Second.Value,Is.EqualTo("z"));
            for(int i=0;i<3;i++)
            {
                Assert.That(first.GetWorkingActor(a,i).Athlete,Is.EqualTo(second.GetWorkingActor(b,2-i).Athlete));
                Assert.That(first.GetWorkingActor(a,i).Velocity,Is.EqualTo(second.GetWorkingActor(b,2-i).Velocity));
            }
            Assert.That(second.GetLastOwnedContactPair(0),Is.EqualTo(first.GetLastOwnedContactPair(0)));
            Assert.That(second.GetLastOwnedContactPair(1),Is.EqualTo(first.GetLastOwnedContactPair(1)));
        }
        [Test] public void ALastPairGeometryFailureCannotExposeTheEarlierValidPairAsACompleteSet()
        {
            var world=Chain(new[]{"a","b","c"},new[]{0,1,2},lastOverlap:.1);var tick=world.BeginTick();
            var before=world.GetWorkingActor(tick,0);Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.False);
            Assert.That(world.LastOwnedContacts.Count,Is.Zero);Assert.That(world.LastOwnedContacts.IsValid,Is.False);
            Assert.That(world.LastOwnedContacts.Limit.First.Value,Is.EqualTo("b"));Assert.That(world.LastOwnedContacts.Limit.Second.Value,Is.EqualTo("c"));
            Assert.That(world.GetWorkingActor(tick,0),Is.EqualTo(before));Assert.That(world.HasPendingContact(tick),Is.False);
        }
        [Test] public void UnresolvedInitialGroupRetainsEveryWorkingRowAndBlocksMotionInsteadOfPayingAWait()
        {
            var world=Chain(new[]{"z","a","m"},new[]{0,1,2},passes:1);var tick=world.BeginTick();var before=new ManualRacePathActorFrame[3];
            for(int i=0;i<3;i++)before[i]=world.GetWorkingActor(tick,i);
            Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.False);Assert.That(world.HasPendingContact(tick),Is.True);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactResponse));
            Assert.That(world.LastAttempt.Response.Status,Is.EqualTo(SkiContactGroupStatus.WorkLimit));Assert.That(world.LastAttempt.Response.Impulses,Is.GreaterThan(0));
            for(int i=0;i<3;i++)Assert.That(world.GetWorkingActor(tick,i),Is.EqualTo(before[i]));
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.TryFinalizeTick(tick,out _),Is.False);
            world.AbortTick(tick);for(int i=0;i<3;i++)Assert.That(world.GetActor(i),Is.EqualTo(before[i]));
        }
        [Test] public void WarmOwnedContactResponseScheduleAndCompleteClockQueriesAllocateNothing()
        {
            var f=AtContact();var setup=f.World.BeginTick();Assert.That(f.World.TryResolveOwnedCurrentContacts(setup),Is.True);
            var candidates=Driven(f,setup);var windows=Windows(f,setup);f.World.AbortTick(setup);
            bool Cycle()
            {
                var tick=f.World.BeginTick();bool ok=f.World.TryResolveOwnedCurrentContacts(tick);
                ok&=f.World.TryAppendToOwnedFirstContact(tick,candidates,windows,NoEdges,0,Grades,out _);
                ok&=f.World.TryFinalizeTick(tick,out _);return ok;
            }
            for(int i=0;i<128;i++)Assert.That(Cycle(),Is.True);long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=Cycle();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.That(f.World.ContextJson,Does.Contain("owned_current_pose_proximity_pairs"));
        }
    }
}
