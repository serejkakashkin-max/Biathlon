using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualScopedContactArrivalTests
    {
        private static readonly SkiReachedContactPair[] EmptyPairs=Array.Empty<SkiReachedContactPair>();
        private static readonly SkiActorCourseEdge[] EmptyEdges=Array.Empty<SkiActorCourseEdge>();
        private static readonly double[] Grades={0,0};
        private static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"arrival_contact_test_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        private static SkiContactSweepDefinition Work()=>SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"arrival_work_test_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
        private static ManualScopedWorldRosterTests.Fixture Fixture(double[] masses=null)=>new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true,actorMasses:masses);
        private static SkiReachedContactPair[] Pair(ManualScopedWorldRoster world)=>new[]{new SkiReachedContactPair(world.GetActor(0).Athlete,world.GetActor(1).Athlete)};
        private static double ArrivalTime(ManualScopedWorldRosterTests.Fixture f)
        {
            var a=f.World.GetActor(0);var b=f.World.GetActor(1);
            var hit=new ManualSkiContactSystem(f.Source.Venue.Occupancy,Contact()).PreviewLinearContact(
                new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new ManualSkiContactBody(b.Athlete,b.Pose,b.Velocity,75),.05);
            Assert.That(hit.HasContact,Is.True);Assert.That(hit.ContactSeconds,Is.InRange(.001,.049));return hit.ContactSeconds;
        }
        private static ManualScopedWorldWindow[] Windows(ManualScopedWorldRoster world,ManualRacePathCatalog[] catalogs,ManualScopedWorldTick tick)
        {
            var result=new ManualScopedWorldWindow[world.Count];
            for(int i=0;i<result.Length;i++)
            {var c=world.GetWorkingCursor(tick,i);result[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);}
            return result;
        }
        private static SkiWorldMotionCandidate[] Candidates(ManualScopedWorldRoster world,OccupancyDefinition body,ManualScopedWorldTick tick,double seconds)
        {
            var result=new SkiWorldMotionCandidate[world.Count];var snow=new ManualSkiSnowContactSystem(body,1);
            for(int i=0;i<result.Length;i++)
            {
                var actor=world.GetWorkingActor(tick,i);var contactBody=new ManualSkiContactBody(actor.Athlete,actor.Pose,actor.Velocity,75);
                result[i]=new SkiWorldMotionCandidate(actor.Athlete,new ManualSkiWorldTrajectory(snow.PreviewTrajectory(
                    contactBody,new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),0,0,seconds)));
            }
            return result;
        }
        private static bool Arrive(ManualScopedWorldRosterTests.Fixture f,ManualScopedWorldTick tick,double seconds,SkiReachedContactPair[] pairs)
            =>f.World.TryAppendArrivalInterval(tick,Candidates(f.World,f.Source.Venue.Occupancy,tick,seconds),Windows(f.World,f.Catalogs,tick),
                EmptyPairs,0,EmptyEdges,0,Grades,pairs,pairs.Length);

        [TestCase(75,75)] [TestCase(100,50)]
        public void ReachedCollisionRespondsAtTheSamePoseThenPaysTheSuffixInOneWorldTick(double firstMass,double secondMass)
        {
            var masses=new[]{firstMass,secondMass};var f=Fixture(masses);string context=f.World.ContextJson;
            masses[0]=1;masses[1]=1000; // Caller mutation cannot alter bound athlete physics.
            var original0=f.World.GetActor(0);var original1=f.World.GetActor(1);var pairs=Pair(f.World);
            double seconds=ArrivalTime(f);var tick=f.World.BeginTick();
            Assert.That(Arrive(f,tick,seconds,pairs),Is.True,f.World.LastAttempt.Status.ToString());
            Assert.That(f.World.HasPendingContact(tick),Is.True);
            Assert.That(f.World.TryFinalizeTick(tick,out _),Is.False);Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PendingContact));
            var reached0=f.World.GetWorkingActor(tick,0);var reached1=f.World.GetWorkingActor(tick,1);
            var motion0=f.World.GetWorkingPhysicalMotion(tick,0);var motion1=f.World.GetWorkingPhysicalMotion(tick,1);
            Assert.That(f.World.TryAppendInterval(tick,Candidates(f.World,f.Source.Venue.Occupancy,tick,f.World.GetRemainingSeconds(tick)),
                Windows(f.World,f.Catalogs,tick),pairs,1,EmptyEdges,0,Grades),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.PendingContact));
            Assert.That(f.World.TryResolveArrivals(tick),Is.True,f.World.LastAttempt.Status.ToString());
            Assert.That(f.World.LastAttempt.Response.Status,Is.EqualTo(SkiContactGroupStatus.Resolved));Assert.That(f.World.LastAttempt.Response.Impulses,Is.GreaterThan(0));
            Assert.That(f.World.HasPendingContact(tick),Is.False);Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(seconds));
            double expected=2*firstMass/(firstMass+secondMass);
            for(int i=0;i<2;i++)
            {
                var actor=f.World.GetWorkingActor(tick,i);var reached=i==0?reached0:reached1;
                Assert.That(actor.Pose.X,Is.EqualTo(reached.Pose.X));Assert.That(actor.Pose.Z,Is.EqualTo(reached.Pose.Z));
                Assert.That(actor.Pose.HeadingNanoradians,Is.EqualTo(reached.Pose.HeadingNanoradians));
                double forward=actor.Velocity.X*Math.Sin(actor.Pose.HeadingRadians)+actor.Velocity.Z*Math.Cos(actor.Pose.HeadingRadians);
                Assert.That(forward,Is.EqualTo(expected).Within(1e-8));
                Assert.That(f.World.GetWorkingPhysicalMotion(tick,i).DistanceNanometers,Is.EqualTo((i==0?motion0:motion1).DistanceNanometers));
            }
            Assert.That(f.World.GetActor(0),Is.EqualTo(original0));Assert.That(f.World.GetActor(1),Is.EqualTo(original1));Assert.That(f.World.Tick.Value,Is.Zero);
            double rest=f.World.GetRemainingSeconds(tick);
            Assert.That(f.World.TryAppendInterval(tick,Candidates(f.World,f.Source.Venue.Occupancy,tick,rest),Windows(f.World,f.Catalogs,tick),pairs,1,EmptyEdges,0,Grades),Is.True);
            Assert.That(f.World.TryFinalizeTick(tick,out var full),Is.True);
            f.World.Commit(full);Assert.That(f.World.Tick.Value,Is.EqualTo(1));Assert.That(f.World.ContextJson,Is.EqualTo(context));
            Assert.That(f.World.GetActor(0).Pose,Is.Not.EqualTo(reached0.Pose));Assert.That(f.World.GetActor(1).Pose,Is.Not.EqualTo(reached1.Pose));
            Assert.That(f.World.GetPhysicalMotion(0).DistanceNanometers,Is.EqualTo((long)Math.Round((2*seconds+expected*rest)*SkiState.NanometersPerMeter)));
        }
        [TestCase(-.001)] [TestCase(.001)]
        public void SeparatedOrPenetratingEndpointCannotBeCalledAReachedContact(double offset)
        {
            var f=Fixture();var before=f.World.GetActor(0);var tick=f.World.BeginTick();
            Assert.That(Arrive(f,tick,ArrivalTime(f)+offset,Pair(f.World)),Is.False);
            Assert.That(f.World.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.BodyOrEdge));
            Assert.That(f.World.LastAttempt.BodyOrEdge.Pair.Status,Is.EqualTo(SkiContactSweepStatus.UnreachedArrival));
            Assert.That(f.World.LastAttempt.BodyOrEdge.Pair.IsContactArrival,Is.True);
            Assert.That(f.World.HasPendingContact(tick),Is.False);Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));Assert.Throws<InvalidOperationException>(()=>f.World.TryResolveArrivals(tick));
            f.World.AbortTick(tick);
        }
        [Test] public void ArrivalPolicyMassesAndPairIdentitiesMustBeComplete()
        {
            var disabled=new ManualScopedWorldRosterTests.Fixture();var disabledTick=disabled.World.BeginTick();
            Assert.Throws<InvalidOperationException>(()=>Arrive(disabled,disabledTick,.02,Pair(disabled.World)));
            Assert.Throws<ArgumentException>(()=>Fixture(new[]{75.0}));Assert.Catch(()=>Fixture(new[]{0.0,75}));
            var f=Fixture();var tick=f.World.BeginTick();var pair=Pair(f.World);double seconds=ArrivalTime(f);
            Assert.Throws<ArgumentException>(()=>Arrive(f,tick,seconds,new[]{pair[0],pair[0]}));
            Assert.Throws<ArgumentException>(()=>Arrive(f,tick,seconds,new[]{new SkiReachedContactPair(pair[0].First,new AthleteId("foreign"))}));
            Assert.Throws<ArgumentException>(()=>f.World.TryAppendArrivalInterval(tick,Candidates(f.World,f.Source.Venue.Occupancy,tick,seconds),
                Windows(f.World,f.Catalogs,tick),pair,1,EmptyEdges,0,Grades,pair,1));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.Zero);Assert.That(f.World.Tick.Value,Is.Zero);
        }
        [Test] public void AReachedEndpointCannotHideEarlierDeepPenetrationInsideTheArrivalInterval()
        {
            var f=Fixture();var tick=f.World.BeginTick();var pair=Pair(f.World);
            Assert.That(f.World.TryAppendInterval(tick,Candidates(f.World,f.Source.Venue.Occupancy,tick,.02),
                Windows(f.World,f.Catalogs,tick),EmptyPairs,0,EmptyEdges,0,Grades),Is.True);
            var a=f.World.GetWorkingActor(tick,0);var b=f.World.GetWorkingActor(tick,1);
            var gap=new ManualSkiContactSystem(f.Source.Venue.Occupancy,Contact()).Preview(
                new ManualSkiContactBody(a.Athlete,a.Pose,default,75),new ManualSkiContactBody(b.Athlete,b.Pose,default,75));
            double seconds=f.World.GetRemainingSeconds(tick),normal=a.Velocity.X*gap.NormalX+a.Velocity.Z*gap.NormalZ;
            double ratio=(-2*gap.GapMeters/seconds-normal)/normal;
            var final=new SkiContactMotion(new SkiPlanarVelocity(a.Velocity.X*ratio,a.Velocity.Z*ratio),0);
            var flags=System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic;
            var snow=(SkiSnowContactPreview)typeof(SkiSnowContactPreview).GetConstructor(flags,null,new[]{typeof(SkiContactMotion),typeof(double),typeof(double),typeof(double),typeof(double)},null).Invoke(new object[]{final,0.0,0.0,0.0,0.0});
            var contact=(SkiContactTrajectory)typeof(SkiContactTrajectory).GetConstructors(flags)[0].Invoke(new object[]{a.Pose,new SkiContactMotion(a.Velocity,0),snow,seconds});
            var candidates=Candidates(f.World,f.Source.Venue.Occupancy,tick,seconds);
            candidates[0]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(contact));
            var end=new ManualSkiContactSystem(f.Source.Venue.Occupancy,Contact()).Preview(
                new ManualSkiContactBody(a.Athlete,contact.State,default,75),new ManualSkiContactBody(b.Athlete,b.Pose,default,75));
            Assert.That(Math.Abs(end.GapMeters),Is.LessThan(Contact().ContactTolerance));
            Assert.That(f.World.TryAppendArrivalInterval(tick,candidates,Windows(f.World,f.Catalogs,tick),EmptyPairs,0,EmptyEdges,0,Grades,pair,1),Is.False);
            Assert.That(f.World.LastAttempt.BodyOrEdge.Pair.Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
            Assert.That(f.World.GetElapsedSeconds(tick),Is.EqualTo(.02));Assert.That(f.World.GetWorkingActor(tick,0),Is.EqualTo(a));
            Assert.That(f.World.HasPendingContact(tick),Is.False);Assert.That(f.World.Tick.Value,Is.Zero);f.World.AbortTick(tick);
        }
        [Test] public void AbortingAnAlreadyResolvedScratchImpulseRestoresTheOriginalAcceptedWorld()
        {
            var f=Fixture();var before=f.World.GetActor(0);var beforeMotion=f.World.GetPhysicalMotion(0);var tick=f.World.BeginTick();
            Assert.That(Arrive(f,tick,ArrivalTime(f),Pair(f.World)),Is.True);Assert.That(f.World.TryResolveArrivals(tick),Is.True);
            Assert.That(f.World.GetWorkingActor(tick,0).Velocity,Is.Not.EqualTo(before.Velocity));f.World.AbortTick(tick);
            Assert.That(f.World.GetActor(0),Is.EqualTo(before));Assert.That(f.World.GetPhysicalMotion(0),Is.EqualTo(beforeMotion));Assert.That(f.World.Tick.Value,Is.Zero);
            Assert.Throws<ArgumentException>(()=>f.World.TryResolveArrivals(tick));
        }
        [Test] public void WarmArrivalResponseAndPaidContinuationAllocateNothing()
        {
            var f=Fixture();var pair=Pair(f.World);double seconds=ArrivalTime(f);var setup=f.World.BeginTick();
            var first=Candidates(f.World,f.Source.Venue.Occupancy,setup,seconds);var firstWindows=Windows(f.World,f.Catalogs,setup);
            Assert.That(f.World.TryAppendArrivalInterval(setup,first,firstWindows,EmptyPairs,0,EmptyEdges,0,Grades,pair,1),Is.True);
            Assert.That(f.World.TryResolveArrivals(setup),Is.True);
            var tail=Candidates(f.World,f.Source.Venue.Occupancy,setup,f.World.GetRemainingSeconds(setup));var tailWindows=Windows(f.World,f.Catalogs,setup);
            f.World.AbortTick(setup);
            bool Cycle()
            {
                var tick=f.World.BeginTick();bool ok=f.World.TryAppendArrivalInterval(tick,first,firstWindows,EmptyPairs,0,EmptyEdges,0,Grades,pair,1);
                ok&=f.World.TryResolveArrivals(tick);ok&=f.World.TryAppendInterval(tick,tail,tailWindows,pair,1,EmptyEdges,0,Grades);
                ok&=f.World.TryFinalizeTick(tick,out _);return ok;
            }
            for(int i=0;i<128;i++)Assert.That(Cycle(),Is.True);
            long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=Cycle();
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);
        }
        [Test] public void UnresolvedThreeBodyResponseCannotPublishAnyProvisionalImpulse()
        {
            var source=new ManualRacePathCatalogTests.Fixture();var catalogs=new[]{source.Catalog,source.Catalog,source.Catalog};
            var body=source.Venue.Occupancy;source.Catalog.TrySample(1,10*SkiState.NanometersPerMeter,out var target);
            double h=Math.Atan2(target.ForwardX,target.ForwardZ),length=body.LongitudinalSeparation;
            var poses=new ManualSkiHeadingState[3];var velocities=new SkiPlanarVelocity[3];var cursors=new ManualRacePathCursor[3];var motions=new SkiState[3];
            var progression=new ManualRacePathProgression(source.Catalog);
            for(int i=0;i<3;i++)
            {
                var id=new AthleteId("chain_"+i);double offset=i==0?0:i*length+.05,speed=i==0?2:0;
                poses[i]=new ManualSkiHeadingState(target.X+offset*Math.Sin(h),target.Z+offset*Math.Cos(h),h);
                velocities[i]=new SkiPlanarVelocity(speed*Math.Sin(h),speed*Math.Cos(h));motions[i]=new SkiState(0,speed);
                Assert.That(source.Catalog.TryProject(poses[i],1,(long)Math.Round((10+offset)*SkiState.NanometersPerMeter),1,out var observed),Is.True);
                cursors[i]=progression.Reconstitute(id,1,observed.PhysicalArcNanometers,observed.PhysicalArcNanometers,observed.MappedRouteNanometers);
            }
            var group=SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"arrival_one_pass_v1\",\"maximum_passes\":1,\"closing_speed_tolerance_mps\":0.00000001}");
            var world=new ManualScopedWorldRoster(catalogs,body,Contact(),Work(),cursors,poses,velocities,motions,20,
                responseDefinition:group,actorMasses:new[]{75.0,75,75});
            var contact=new ManualSkiContactSystem(body,Contact());
            var hit=contact.PreviewLinearContact(new ManualSkiContactBody(cursors[0].Athlete,poses[0],velocities[0],75),
                new ManualSkiContactBody(cursors[1].Athlete,poses[1],velocities[1],75),.05);
            Assert.That(hit.HasContact,Is.True);var tick=world.BeginTick();
            var continuing=new[]{new SkiReachedContactPair(cursors[1].Athlete,cursors[2].Athlete)};
            var arriving=new[]{new SkiReachedContactPair(cursors[0].Athlete,cursors[1].Athlete)};
            Assert.That(world.TryAppendArrivalInterval(tick,Candidates(world,body,tick,hit.ContactSeconds),Windows(world,catalogs,tick),
                continuing,1,EmptyEdges,0,new[]{0.0,0.0,0.0},arriving,1),Is.True,world.LastAttempt.Status.ToString());
            var reached=new ManualRacePathActorFrame[3];var reachedMotion=new SkiState[3];
            for(int i=0;i<3;i++){reached[i]=world.GetWorkingActor(tick,i);reachedMotion[i]=world.GetWorkingPhysicalMotion(tick,i);}
            Assert.That(world.TryResolveArrivals(tick),Is.False);
            Assert.That(world.LastAttempt.Status,Is.EqualTo(ManualScopedWorldStatus.ContactResponse));
            Assert.That(world.LastAttempt.Response.Status,Is.EqualTo(SkiContactGroupStatus.WorkLimit));
            Assert.That(world.LastAttempt.Response.Impulses,Is.GreaterThan(0));
            for(int i=0;i<3;i++)
            {Assert.That(world.GetWorkingActor(tick,i),Is.EqualTo(reached[i]));Assert.That(world.GetWorkingPhysicalMotion(tick,i),Is.EqualTo(reachedMotion[i]));}
            Assert.That(world.HasPendingContact(tick),Is.True);Assert.That(world.TryFinalizeTick(tick,out _),Is.False);
            Assert.That(world.Tick.Value,Is.Zero);world.AbortTick(tick);
            for(int i=0;i<3;i++)Assert.That(world.GetActor(i).Velocity,Is.EqualTo(velocities[i]));
        }
    }
}
