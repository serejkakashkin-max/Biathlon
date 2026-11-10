using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualScopedFreeDriveTests
    {
        [TestCase(ManualCourseReactionPolicy.IndependentCourseV27)]
        [TestCase(ManualCourseReactionPolicy.PassiveCurvedV26)]
        public void ForeignCourseSupportUsesTheRecordedStopCalendar(ManualCourseReactionPolicy policy)
        {
            var production=ManualProductionFieldTests.Initial("mass_start",1).stepper;
            var body=production.BodyDefinition;var physics=production.PhysicsDefinition;
            var tolerance=ManualNumericalToleranceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_gameplay_tolerance_v1.json"));
            var edgeCatalog=production.World.GetCatalog(0);
            var catalogs=new[]{edgeCatalog,edgeCatalog};
            var poses=new ManualSkiHeadingState[2];var cursors=new ManualRacePathCursor[2];var velocities=new SkiPlanarVelocity[2];var motion=new SkiState[2];
            var stopped=production.World.GetActor(0);
            double h=stopped.Pose.HeadingRadians;
            cursors[0]=production.World.GetCursor(0);
            poses[0]=new ManualSkiHeadingState(stopped.Pose.X,stopped.Pose.Z,h,.002);
            velocities[0]=new SkiPlanarVelocity(1e-10*Math.Sin(h)+.001*Math.Cos(h),1e-10*Math.Cos(h)-.001*Math.Sin(h));
            motion[0]=new SkiState(0,.001);

            // The admitted rounded-bend witness from OwnerCourseClockRejectionTests.
            int occurrence=-1;const long local=85_706_000_000;
            for(int i=0;i<edgeCatalog.BaseFlowCount;i++)
            {
                var row=edgeCatalog.Get(i);
                if(row.LoopId=="main"&&row.LocalStartNanometers<=local&&row.LocalEndNanometers>=local){occurrence=i;break;}
            }
            Assert.That(occurrence,Is.GreaterThanOrEqualTo(0));
            long arc=local-edgeCatalog.Get(occurrence).LocalStartNanometers;
            Assert.That(edgeCatalog.TrySample(occurrence,arc,out var center),Is.True);
            poses[1]=new ManualSkiHeadingState(215.131683788,-23.674832777,2.685201267,.579);
            velocities[1]=new SkiPlanarVelocity(9.614*center.ForwardX+center.ForwardZ,9.614*center.ForwardZ-center.ForwardX);
            Assert.That(edgeCatalog.TryProject(poses[1],occurrence,arc,3,out var observed),Is.True);
            cursors[1]=new ManualRacePathProgression(edgeCatalog).Reconstitute(new AthleteId("edge_skier"),occurrence,
                observed.PhysicalArcNanometers,observed.PhysicalArcNanometers,observed.MappedRouteNanometers);
            double speed=Math.Sqrt(velocities[1].X*velocities[1].X+velocities[1].Z*velocities[1].Z);
            double grade=observed.GeometricGrade*(velocities[1].X*observed.ForwardX+velocities[1].Z*observed.ForwardZ)/speed;
            motion[1]=new SkiState(0,speed*Math.Sqrt(1+grade*grade));
            var world=new ManualScopedWorldRoster(catalogs,body,
                SkiContactDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_gameplay_v1.json")),
                SkiContactSweepDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_sweep_v1.json")),
                cursors,poses,velocities,motion,20,responseDefinition:SkiContactGroupDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_ski_contact_group_gameplay_v1.json")),
                actorMasses:new[]{physics.MassKilograms,physics.MassKilograms},
                numericalTolerance:tolerance,
                courseContact:ManualCourseContactDefinition.FromJson(PhysiologyContractTests.DefinitionJson("manual_course_contact_gameplay_v2.json")),
                courseReactionPolicy:policy);
            var system=new ManualSkiControlledDriveSystem(physics,body,ManualScopedFieldStepperTests.Steering(),production.CorneringDefinition,20);
            var respond=typeof(ManualScopedWorldRoster).GetMethod("TryRespondToCourseEdge",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic);
            var drives=new SkiDrivenContactTrajectory[2];var candidates=new SkiWorldMotionCandidate[2];var windows=new ManualScopedWorldWindow[2];var grades=new double[2];
            var tick=world.BeginTick();bool exercised=false;int pieces=0;
            while(world.GetRemainingSeconds(tick)>0)
            {
                double remaining=world.GetRemainingSeconds(tick);
                Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.True);
                for(int i=0;i<2;i++)
                {
                    var a=world.GetWorkingActor(tick,i);var c=world.GetWorkingCursor(tick,i);var row=catalogs[i].Get(c.OccurrenceIndex);
                    drives[i]=system.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,physics.MassKilograms),
                        new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),a.Observation,catalogs[i].SampleSurface(a.Observation),
                        new ManualSkiMotionInput(i==0?300:1000,i==0?0:350,70,70),remaining).Drive;
                    windows[i]=new ManualScopedWorldWindow(a.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,
                        physics.MaximumSpeedMetersPerSecond*remaining*a.Observation.StationMetersPerPlanarMeter,row.LengthNanometers);
                    grades[i]=a.Observation.GradeAlong(a.Pose);
                }
                Assert.That(world.TryPreviewContactDrivenMotion(tick,drives,candidates),Is.True);
                if(world.CourseEdgeResponses>0&&world.LastOwnedContacts.Count==0)
                {
                    exercised=true;
                    Assert.That(drives[0].Snow.HasExactNeutralConstraint,Is.False);
                    Assert.That(drives[0].ConstantAccelerationSeconds,Is.LessThan(.0001));
                    if(policy==ManualCourseReactionPolicy.PassiveCurvedV26)
                    {
                        Assert.That(candidates[0].Trajectory.DurationSeconds,Is.EqualTo(.0001));
                        world.AbortTick(tick);
                        return;
                    }
                    Assert.That(candidates[0].Trajectory.DurationSeconds,Is.GreaterThan(.01),"An unrelated almost-stopped skier must not force the supported skier through short slices.");
                    double seconds=candidates[0].Trajectory.DurationSeconds;
                    Assert.That(candidates[0].Trajectory.VelocityAt(1),Is.EqualTo(drives[0].MotionAt(seconds/remaining).Velocity));
                }
                if(!world.TryAppendToOwnedFirstEvent(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,grades,out _))
                {Assert.That((bool)respond.Invoke(world,new object[]{tick}),Is.True,world.LastAttempt.Status.ToString());continue;}
                Assert.That(++pieces,Is.LessThan(16));
            }
            Assert.That(exercised,Is.True,"The fixture must reach an actual course-edge response.");
            Assert.That(policy,Is.EqualTo(ManualCourseReactionPolicy.IndependentCourseV27));
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);world.Commit(proof);Assert.That(world.Tick.Value,Is.EqualTo(1));
        }

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
