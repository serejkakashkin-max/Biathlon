using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedWorldRosterTests
    {
        private const long N=SkiState.NanometersPerMeter;
        internal sealed class Fixture
        {
            internal readonly ManualRacePathCatalogTests.Fixture Source=new ManualRacePathCatalogTests.Fixture();
            internal readonly ManualRacePathCatalog[] Catalogs;
            internal readonly ManualScopedWorldRoster World;
            internal Fixture(bool mixed=true,double gap=20,int endMask=0,double side=0,
                int startMask=0,double secondSpeed=2,double secondSideSpeed=0,double secondEndOffsetMeters=0,
                bool contactResponse=false,double[] actorMasses=null,int intervalChecks=4096)
            {
                var secondPath=new PhysicalRangePathMap(Source.Path.PhysicalMap,Source.Venue.Track,Source.Route,Source.Venue.Paths,29);
                var second=new ManualRacePathCatalog(Source.Venue.Track,Source.Route,Source.Profile,Source.Venue.Visual,secondPath,Source.Start);
                Catalogs=new[]{Source.Catalog,second};var cursors=new ManualRacePathCursor[2];
                var poses=new ManualSkiHeadingState[2];var velocities=new SkiPlanarVelocity[2];var motion=new SkiState[2];
                for(int i=0;i<2;i++)
                {
                    int index=mixed&&i==0?0:1;long arc=(long)Math.Round((10+i*gap)*N);
                    var row=Catalogs[i].Get(index);
                    if((endMask&(1<<i))!=0)arc=row.LengthNanometers;
                    if(i==1&&(endMask&2)!=0)arc-=checked((long)Math.Round(secondEndOffsetMeters*N));
                    if((startMask&(1<<i))!=0)arc=0;
                    if(!mixed&&gap<0&&i==1)arc=(long)Math.Round((10+Source.Venue.Occupancy.LongitudinalSeparation+.05)*N);
                    var point=Source.Venue.Visual.Get(row.LoopId).Sample((row.LocalStartNanometers+arc)/(double)N);
                    poses[i]=new ManualSkiHeadingState(point.X+point.ForwardZ*side,point.Z-point.ForwardX*side,Math.Atan2(point.ForwardX,point.ForwardZ));
                    double speed=mixed?(i==0?0:secondSpeed):(i==0?2:0),slip=i==1?secondSideSpeed:0,h=poses[i].HeadingRadians;
                    velocities[i]=new SkiPlanarVelocity(speed*Math.Sin(h)+slip*Math.Cos(h),speed*Math.Cos(h)-slip*Math.Sin(h));
                    motion[i]=new SkiState(i*N,Math.Sqrt(speed*speed+slip*slip));
                    cursors[i]=new ManualRacePathProgression(Catalogs[i]).Reconstitute(new AthleteId("scoped_"+i),index,arc,arc,
                        row.RouteStartNanometers+(row.CountsTowardsRoute?arc:0));
                }
                var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_world_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
                var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_world_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":"+intervalChecks+",\"time_tolerance_s\":0.0000001}");
                var group=contactResponse?SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"scoped_world_response_v1\",\"maximum_passes\":64,\"closing_speed_tolerance_mps\":0.00000001}"):null;
                World=new ManualScopedWorldRoster(Catalogs,Source.Venue.Occupancy,contact,work,cursors,poses,velocities,motion,20,
                    responseDefinition:group,actorMasses:contactResponse?(actorMasses??new[]{75.0,75}):actorMasses);
            }
            internal SkiWorldMotionCandidate[] Candidates()
            {
                var result=new SkiWorldMotionCandidate[2];var snow=new ManualSkiSnowContactSystem(Source.Venue.Occupancy,1);
                for(int i=0;i<2;i++)
                {
                    var actor=World.GetActor(i);var body=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                    var trajectory=snow.PreviewTrajectory(body,new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),0,0,.05);
                    result[i]=new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(trajectory));
                }
                return result;
            }
            internal ManualScopedWorldWindow[] Windows()
            {
                var result=new ManualScopedWorldWindow[2];
                for(int i=0;i<2;i++)
                {var c=World.GetCursor(i);result[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,
                    Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);}
                return result;
            }
            internal bool Prepare(SkiWorldMotionCandidate[] candidates,ManualScopedWorldWindow[] windows,out ManualScopedWorldPreview proposal)
                =>World.TryPrepare(candidates,windows,Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,new[]{0.0,0.0},out proposal);
        }
        [Test] public void DifferentAssignedPathsAndStationaryStartBodyShareOneOwnedPhysicalTick()
        {
            var f=new Fixture();var original=f.World.GetActor(0);var originalCredit=f.World.GetCursor(0).CreditedRouteNanometers;
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.NotPrepared));
            var candidates=f.Candidates();var windows=f.Windows();
            Assert.That(f.Prepare(candidates,windows,out var proposal),Is.True,f.World.LastAttempt.Status.ToString());
            Assert.That(f.World.Tick.Value,Is.Zero);Assert.That(f.World.GetPhysicalMotion(1).DistanceNanometers,Is.EqualTo(N));
            Assert.That(f.World.GetProposedPhysicalMotion(proposal,0).DistanceNanometers,Is.Zero);
            Assert.That(f.World.GetProposedPhysicalMotion(proposal,1).DistanceNanometers,Is.EqualTo(N+N/10));
            Assert.That(f.World.GetProposedCursor(proposal,0).CreditedRouteNanometers,Is.EqualTo(originalCredit));
            Assert.That(f.World.GetProposedCursor(proposal,1).CreditedRouteNanometers,Is.GreaterThan(f.World.GetCursor(1).CreditedRouteNanometers));
            f.World.Commit(proposal);Assert.That(f.World.Tick.Value,Is.EqualTo(1));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(original.Pose));
            Assert.Throws<ArgumentException>(()=>f.World.Commit(proposal));
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var next),Is.True);
            f.World.Commit(next);Assert.That(f.World.Tick.Value,Is.EqualTo(2));
            Assert.That(f.World.GetPhysicalMotion(1).DistanceNanometers,Is.EqualTo(N+N/5));
        }
        [Test] public void ContactPrefixCannotBecomeAStationaryWaitTickOrPartialRosterCommit()
        {
            var f=new Fixture(mixed:false,gap:-1);var before=f.World.GetActor(0);var motion=f.World.GetPhysicalMotion(0);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var rejected),Is.False);
            Assert.That(rejected.IsValid,Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.BodyOrEdge));
            Assert.That(f.World.Tick.Value,Is.Zero);Assert.That(f.World.GetActor(0),Is.EqualTo(before));
            Assert.That(f.World.GetPhysicalMotion(0),Is.EqualTo(motion));
            Assert.Throws<ArgumentException>(()=>f.World.Commit(rejected));
        }
        [Test] public void LastActorPhaseCapAndStaleSourceInvalidatePreparedProofWithoutChangingAcceptedState()
        {
            var f=new Fixture();var candidate=f.Candidates();var windows=f.Windows();
            Assert.That(f.Prepare(candidate,windows,out var first),Is.True);var accepted=f.World.GetActor(1);
            var c=f.World.GetCursor(1);windows[1]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,c.CreditedArcNanometers);
            Assert.That(f.Prepare(candidate,windows,out _),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));
            Assert.That(f.World.GetActor(1),Is.EqualTo(accepted));Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.Throws<ArgumentException>(()=>f.World.GetProposedActor(first,0));
            windows=f.Windows();Assert.That(f.Prepare(candidate,windows,out var fresh),Is.True);f.World.Commit(fresh);
            Assert.That(f.Prepare(candidate,windows,out _),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.SourceMismatch));
            Assert.That(f.World.Tick.Value,Is.EqualTo(1));
        }
        [Test] public void RestoredInitialWorldIdentityIsImmutableAndIncludesEveryScopedCoordinate()
        {
            var f=new Fixture();string initial=f.World.InitialStateContextJson;string context=f.World.ContextJson;
            Assert.That(initial,Does.Contain("scoped_0"));Assert.That(initial,Does.Contain("scoped_1"));
            Assert.That(context,Does.Contain("lane_01"));Assert.That(context,Does.Contain("lane_30"));
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var next),Is.True);f.World.Commit(next);
            Assert.That(f.World.InitialStateContextJson,Is.EqualTo(initial));Assert.That(f.World.ContextJson,Is.EqualTo(context));
            Assert.That(f.World.GetCursor(0).OccurrenceIndex,Is.Not.EqualTo(f.World.GetCursor(1).OccurrenceIndex));
        }
        [Test] public void ForeignProofIncompleteRosterAndUnacceptedHandoffFail()
        {
            var f=new Fixture();var other=new Fixture();
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var proposal),Is.True);
            Assert.Throws<ArgumentException>(()=>other.World.Commit(proposal));
            Assert.That(f.World.TryHandoffOrdinary(new[]{0}),Is.False,"Prepared tick must not be overwritten by a handoff.");
            f.World.Commit(proposal);
            Assert.That(f.World.TryHandoffOrdinary(new[]{0}),Is.False,"Actual body is not at the endpoint.");
            var candidate=f.Candidates();var windows=f.Windows();
            Assert.Throws<ArgumentException>(()=>f.World.TryPrepare(new[]{candidate[0]},windows,Array.Empty<SkiReachedContactPair>(),0,
                Array.Empty<SkiActorCourseEdge>(),0,new[]{0.0,0.0},out _));
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.NotPrepared));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.World.TryHandoffOrdinary(new[]{2}));
            Assert.Throws<ArgumentException>(()=>new ManualScopedWorldWindow(default,0,0,1,0));
        }
        [Test] public void OrdinaryHandoffsAcrossDifferentPathsValidateAllRowsAndKeepWorldMotionAndClock()
        {
            var blocked=new Fixture(endMask:1);var before=blocked.World.GetCursor(0);
            Assert.That(blocked.World.TryHandoffOrdinary(new[]{0,1}),Is.False);
            Assert.That(blocked.World.GetCursor(0),Is.EqualTo(before));
            var f=new Fixture(endMask:3);var pose0=f.World.GetActor(0).Pose;var pose1=f.World.GetActor(1).Pose;
            var motion0=f.World.GetPhysicalMotion(0);var motion1=f.World.GetPhysicalMotion(1);
            var c0=f.World.GetCursor(0);var c1=f.World.GetCursor(1);
            Assert.That(f.World.TryHandoffOrdinary(new[]{0,1}),Is.True);
            Assert.That(f.World.GetCursor(0).OccurrenceIndex,Is.EqualTo(c0.OccurrenceIndex+1));
            Assert.That(f.World.GetCursor(1).OccurrenceIndex,Is.EqualTo(c1.OccurrenceIndex+1));
            Assert.That(f.World.GetCursor(0).CreditedRouteNanometers,Is.EqualTo(c0.CreditedRouteNanometers));
            Assert.That(f.World.GetCursor(1).CreditedRouteNanometers,Is.EqualTo(c1.CreditedRouteNanometers));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(pose0));Assert.That(f.World.GetActor(1).Pose,Is.EqualTo(pose1));
            Assert.That(f.World.GetPhysicalMotion(0),Is.EqualTo(motion0));Assert.That(f.World.GetPhysicalMotion(1),Is.EqualTo(motion1));
            Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void AnUnprovedCourseStartCannotClampTheBodyOrCommitAClockTick()
        {
            var f=new Fixture(side:4);var before=f.World.GetActor(0);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out _),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.Course));
            Assert.That(f.World.LastAttempt.Course.Status,Is.EqualTo(ManualRouteTubeStatus.UnresolvedStart));
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void ClampedEndpointObservationCannotAdmitUnpaidMovementIntoTheNextOccurrence()
        {
            var f=new Fixture(endMask:2);var before=f.World.GetActor(1);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out _),Is.False,
                "A body moving beyond an unadmitted endpoint cannot be hidden by clamping its arc observation.");
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));
            Assert.That(f.World.LastAttempt.Gate.IsValid,Is.True);
            Assert.That(f.World.LastAttempt.Gate.Status,Is.EqualTo(ManualRacePathGateStatus.PotentialCrossing));
            Assert.That(f.World.GetActor(1),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [TestCase(0,0)] [TestCase(-2,0)] [TestCase(0,2)]
        public void ABodyAtTheEndpointMayStayMoveInwardOrTravelTangentially(double forward,double sideSpeed)
        {
            var f=new Fixture(endMask:2,secondSpeed:forward,secondSideSpeed:sideSpeed);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out var next),Is.True,f.World.LastAttempt.Status.ToString());
            Assert.That(f.World.GetProposedCursor(next,1).CreditedArcNanometers,Is.EqualTo(f.World.GetCursor(1).CreditedArcNanometers));
            f.World.Commit(next);Assert.That(f.World.Tick.Value,Is.EqualTo(1));
        }
        [Test] public void ClampedStartCannotAdmitBackwardsMovementIntoThePreviousOccurrence()
        {
            var f=new Fixture(startMask:2,secondSpeed:-2);var before=f.World.GetActor(1);
            Assert.That(f.Prepare(f.Candidates(),f.Windows(),out _),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));
            Assert.That(f.World.LastAttempt.Gate.BoundaryArcNanometers,Is.Zero);
            Assert.That(f.World.GetActor(1),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)]
        public void ReturningToAnAdmittedEndpointCannotHideAnExcursionAcrossAPhasePortal(bool occurrenceEnd,bool narrowWindow)
        {
            var f=new Fixture(endMask:occurrenceEnd?2:0,secondSpeed:narrowWindow?80:2);var before=f.World.GetActor(1);
            var candidates=f.Candidates();var windows=f.Windows();var cursor=f.World.GetCursor(1);
            // Build an exact constant-acceleration continuation at the representation
            // boundary, rather than changing the passive snow solver to invent a
            // reversal. Public world admission must check any supplied continuation.
            var initial=new SkiContactMotion(before.Velocity,0);
            var final=new SkiContactMotion(new SkiPlanarVelocity(-before.Velocity.X,-before.Velocity.Z),0);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var snow=(SkiSnowContactPreview)typeof(SkiSnowContactPreview).GetConstructors(flags)[0].Invoke(new object[]{final,0.0,0.0,0.0,0.0});
            var contact=(SkiContactTrajectory)typeof(SkiContactTrajectory).GetConstructors(flags)[0].Invoke(new object[]{before.Pose,initial,snow,.05});
            var trajectory=new ManualSkiWorldTrajectory(contact);
            Assert.That(trajectory.Sample(1),Is.EqualTo(before.Pose));
            Assert.That(trajectory.Sample(.5),Is.Not.EqualTo(before.Pose));
            candidates[1]=new SkiWorldMotionCandidate(before.Athlete,trajectory);
            windows[1]=new ManualScopedWorldWindow(cursor.Athlete,cursor.OccurrenceIndex,cursor.ObservedArcNanometers,
                narrowWindow?.001:1,cursor.CreditedArcNanometers+(narrowWindow?N/2:0));
            Assert.That(f.Prepare(candidates,windows,out _),Is.False,"Checking only the endpoint would accept this excursion.");
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PhaseLimit));
            Assert.That(f.World.LastAttempt.Gate.IsValid,Is.True);
            Assert.That(f.World.LastAttempt.Gate.Status,Is.EqualTo(ManualRacePathGateStatus.PotentialCrossing));
            Assert.That(f.World.GetActor(1),Is.EqualTo(before));Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void RepeatedPreparedFullRosterQueriesAllocateNothing()
        {
            var f=new Fixture();var candidates=f.Candidates();var windows=f.Windows();var grades=new[]{0.0,0.0};
            var reached=Array.Empty<SkiReachedContactPair>();var edges=Array.Empty<SkiActorCourseEdge>();
            for(int i=0;i<128;i++)f.World.TryPrepare(candidates,windows,reached,0,edges,0,grades,out _);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=f.World.TryPrepare(candidates,windows,reached,0,edges,0,grades,out _);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True,f.World.LastAttempt.Status.ToString());Assert.That(allocated,Is.Zero);
        }
    }
}
