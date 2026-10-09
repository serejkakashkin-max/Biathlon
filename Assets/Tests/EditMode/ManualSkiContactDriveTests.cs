using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiContactDriveTests
    {
        private sealed class Fixture
        {
            internal readonly ManualRacePathCatalogTests.Fixture Source=new ManualRacePathCatalogTests.Fixture();
            internal readonly ManualSkiContactDriveSystem System;
            internal readonly ManualRacePathObservation Observation;
            internal readonly TrackSample Surface;
            internal readonly ManualSkiHeadingState Pose;
            internal Fixture(bool slope=false,bool reverse=false)
            {
                int occurrence=1;long arc=10*SkiState.NanometersPerMeter;
                if(slope)
                {
                    bool found=false;
                    for(int i=0;i<Source.Catalog.BaseFlowCount&&!found;i++)
                    {
                        var row=Source.Catalog.Get(i);if(row.Kind!=ManualRacePathKind.BaseRoute)continue;
                        for(long at=SkiState.NanometersPerMeter;at<row.LengthNanometers-SkiState.NanometersPerMeter;at+=SkiState.NanometersPerMeter)
                        {
                            var p=Source.At(i,at);Source.Catalog.TryProject(p,i,at,0,out var observed);
                            if(observed.GradeAlong(p)>.02){occurrence=i;arc=at;found=true;break;}
                        }
                    }
                    Assert.That(found,Is.True,"Real ascending Woodland witness");
                }
                var pose=Source.At(occurrence,arc);Pose=new ManualSkiHeadingState(pose.X,pose.Z,pose.HeadingRadians+(reverse?Math.PI:0));
                Assert.That(Source.Catalog.TryProject(Pose,occurrence,arc,0,out var o),Is.True);
                Observation=o;Surface=Source.Catalog.SampleSurface(o);
                System=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),Source.Venue.Occupancy,20);
            }
            internal ManualSkiContactBody Actor(double forward,double side=0,double yaw=0)
            {
                double h=Pose.HeadingRadians;
                return new ManualSkiContactBody(new AthleteId("driven"),new ManualSkiHeadingState(Pose.X,Pose.Z,h,yaw),
                    new SkiPlanarVelocity(forward*Math.Sin(h)+side*Math.Cos(h),forward*Math.Cos(h)-side*Math.Sin(h)),75);
            }
            internal SkiDrivenContactTrajectory Drive(ManualSkiContactBody actor,double power=350,double grip=0,double used=0,
                double drag=1,double snow=1,double wind=0)
                =>System.Preview(actor,new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),Observation,Surface,power,grip,used,.05,
                    drag,snow,windX:wind*Math.Sin(Pose.HeadingRadians),windZ:wind*Math.Cos(Pose.HeadingRadians));
            internal double Forward(SkiPlanarVelocity v)=>v.X*Math.Sin(Pose.HeadingRadians)+v.Z*Math.Cos(Pose.HeadingRadians);
            internal double Side(SkiPlanarVelocity v)=>v.X*Math.Cos(Pose.HeadingRadians)-v.Z*Math.Sin(Pose.HeadingRadians);
            internal double Distance(ManualSkiHeadingState p)=>(p.X-Pose.X)*Math.Sin(Pose.HeadingRadians)+(p.Z-Pose.Z)*Math.Cos(Pose.HeadingRadians);
        }
        [TestCase(0)][TestCase(6)][TestCase(25)]
        public void AlignedDrivingRetainsSharedPowerFrictionDragStopAndCap(double speed)
        {
            var f=new Fixture();var a=f.Actor(speed);var result=f.Drive(a);
            var ordinary=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(new SkiState(0,speed),350,
                f.Surface.WithTravelGeometry(f.Observation.GradeAlong(f.Pose),0),.05);
            Assert.That(f.Forward(result.MotionAt(1).Velocity),Is.EqualTo(ordinary.SpeedAt(.05)).Within(2e-9));
            Assert.That(f.Distance(result.State),Is.EqualTo(ordinary.DistanceAt(.05)).Within(2e-9));
            Assert.That(result.InitialMotion.Velocity,Is.EqualTo(a.Velocity));
            Assert.That(result.State.HeadingNanoradians,Is.EqualTo(f.Pose.HeadingNanoradians));
            Assert.That(result.FirstDrive.Output.PropulsiveNewtons,Is.EqualTo(ordinary.Output.PropulsiveNewtons).Within(1e-8));
        }
        [Test] public void LongitudinalPowerCannotConsumeTheIndependentSideSlipOrSnapPhysicalSpin()
        {
            var f=new Fixture();var a=f.Actor(6,.8,3.123456789);var result=f.Drive(a);
            Assert.That(result.MotionAt(0).Velocity,Is.EqualTo(a.Velocity));
            Assert.That(f.Side(result.MotionAt(1).Velocity),Is.EqualTo(.8).Within(1e-12));
            Assert.That(f.Forward(result.MotionAt(1).Velocity),Is.GreaterThan(6));
            Assert.That(result.MotionAt(1).YawRate,Is.EqualTo(a.Pose.YawRateRadiansPerSecond));
            Assert.That(result.State.HeadingRadians-f.Pose.HeadingRadians,Is.EqualTo(a.Pose.YawRateRadiansPerSecond*.05).Within(1e-9));
            Assert.That(new ManualSkiWorldTrajectory(result).PlanarTravelDistanceAt(1),Is.GreaterThan(f.Distance(result.State)));
        }
        [Test] public void PassiveSnowSharesItsOriginalTurningReservationWhilePowerStillAdvances()
        {
            var f=new Fixture();var a=f.Actor(6,.8,.2);var result=f.Drive(a,grip:4,used:3);
            var original=new ManualSkiSnowContactSystem(f.Source.Venue.Occupancy,20).Preview(a,
                new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),4,3,.05);
            Assert.That(result.Snow,Is.EqualTo(original));Assert.That(result.Snow.AvailableLateralAcceleration,Is.EqualTo(1));
            Assert.That(f.Side(result.MotionAt(1).Velocity),Is.EqualTo(f.Side(original.Motion.Velocity)).Within(1e-12));
            Assert.That(f.Forward(result.MotionAt(1).Velocity),Is.GreaterThan(f.Forward(original.Motion.Velocity)));
            var consumed=f.Drive(a,grip:4,used:4);Assert.That(consumed.Snow.DissipatedEnergyJoules,Is.Zero);
        }
        [Test] public void BackwardContactVelocityBrakesAndPaysItsForwardRestartInsideTheSameTick()
        {
            var f=new Fixture();var a=f.Actor(-.001,.4);var result=f.Drive(a);
            Assert.That(result.RestartDrive.IsValid,Is.True);Assert.That(result.FirstDriveSeconds,Is.InRange(0,.049));
            double at=result.FirstDriveSeconds/.05;
            Assert.That(f.Forward(result.MotionAt(at).Velocity),Is.EqualTo(0).Within(1e-12));
            Assert.That(f.Distance(result.Sample(at)),Is.LessThan(0));
            Assert.That(f.Forward(result.MotionAt(1).Velocity),Is.GreaterThan(0));Assert.That(f.Distance(result.State),Is.GreaterThan(0));
            Assert.That(f.Side(result.MotionAt(1).Velocity),Is.EqualTo(.4).Within(1e-12));
            Assert.That(result.Sample(0),Is.EqualTo(a.Pose));Assert.That(result.MotionAt(0).Velocity,Is.EqualTo(a.Velocity));
        }
        [Test] public void PowerAgainstSustainedBackwardTravelNeverBecomesABackwardSpeedBonus()
        {
            var f=new Fixture();var a=f.Actor(-6,.3);var coast=f.Drive(a,power:0);var power=f.Drive(a,power:350);
            Assert.That(power.RestartDrive.IsValid,Is.False);Assert.That(f.Forward(power.MotionAt(1).Velocity),Is.LessThan(0));
            Assert.That(f.Forward(power.MotionAt(1).Velocity),Is.GreaterThan(f.Forward(coast.MotionAt(1).Velocity)));
            Assert.That(f.Distance(power.State),Is.LessThan(0));
        }
        [TestCase(-.001,350)][TestCase(.001,0)][TestCase(6,350)][TestCase(0,0)]
        public void OriginalExternalForcePiecesCoverTheWholePaidDriveWithoutRestartingSnow(double forward,double power)
        {
            var f=new Fixture();var actor=f.Actor(forward,.4,.3);var frame=f.Drive(actor,power:power,grip:4);
            var method=typeof(SkiDrivenContactTrajectory).GetMethod("TryGetAccelerationPiece",
                global::System.Reflection.BindingFlags.NonPublic|global::System.Reflection.BindingFlags.Instance);
            Assert.That(method,Is.Not.Null);
            double end=0,x=actor.Velocity.X,z=actor.Velocity.Z,yaw=actor.Pose.YawRateRadiansPerSecond;int count=0;
            for(;count<4;count++)
            {
                object[] args={count,0d,0d,default(SkiContactAcceleration)};
                if(!(bool)method.Invoke(frame,args))break;
                double from=(double)args[1],to=(double)args[2];var a=(SkiContactAcceleration)args[3];
                Assert.That(from,Is.EqualTo(end));Assert.That(to,Is.GreaterThan(from));
                x+=a.X*(to-from);z+=a.Z*(to-from);yaw+=a.Yaw*(to-from);end=to;
                var expected=frame.MotionAt(to/frame.DurationSeconds);
                Assert.That(x,Is.EqualTo(expected.Velocity.X).Within(1e-12));
                Assert.That(z,Is.EqualTo(expected.Velocity.Z).Within(1e-12));
                Assert.That(yaw,Is.EqualTo(expected.YawRate).Within(1e-12));
            }
            Assert.That(end,Is.EqualTo(frame.DurationSeconds));Assert.That(count,Is.InRange(1,3));
            if(forward<0||forward>0&&power==0)Assert.That(count,Is.EqualTo(2));
        }
        [Test] public void DirectionalGradeTuckSnowAndExposedWindKeepTheirSharedForceMeaning()
        {
            var up=new Fixture(slope:true);var down=new Fixture(slope:true,reverse:true);
            var climb=up.Drive(up.Actor(10,.3),power:0);var coast=down.Drive(down.Actor(10,.3),power:0);
            var tuck=down.Drive(down.Actor(10,.3),power:0,drag:.65);var slowSnow=down.Drive(down.Actor(10,.3),power:0,snow:2);
            var head=down.Drive(down.Actor(10,.3),power:0,wind:-5);var tail=down.Drive(down.Actor(10,.3),power:0,wind:5);
            Assert.That(climb.FirstDrive.Output.GravityNewtons,Is.GreaterThan(0));Assert.That(coast.FirstDrive.Output.GravityNewtons,Is.LessThan(0));
            Assert.That(down.Forward(coast.MotionAt(1).Velocity),Is.GreaterThan(up.Forward(climb.MotionAt(1).Velocity)));
            Assert.That(tuck.FirstDrive.Output.AirDragNewtons,Is.LessThan(coast.FirstDrive.Output.AirDragNewtons));
            Assert.That(slowSnow.FirstDrive.Output.SnowNewtons,Is.GreaterThan(coast.FirstDrive.Output.SnowNewtons));
            Assert.That(head.FirstDrive.Output.AirDragNewtons,Is.GreaterThan(tail.FirstDrive.Output.AirDragNewtons));
        }
        [Test] public void FrozenWorldSlicesKeepDriveRestartSnowMotionAndPositiveTravel()
        {
            var f=new Fixture();var frame=f.Drive(f.Actor(-.001,.4,.3),grip:4);var root=new ManualSkiWorldTrajectory(frame);
            var first=root.Slice(0,.02);var second=root.Slice(.02,.05);var nested=second.Slice(.005,.03);
            Assert.That(first.Sample(1),Is.EqualTo(second.Sample(0)));Assert.That(first.VelocityAt(1),Is.EqualTo(second.VelocityAt(0)));
            Assert.That(nested.Sample(0),Is.EqualTo(root.Sample(.5)));Assert.That(nested.Sample(1),Is.EqualTo(root.Sample(1)));
            Assert.That(root.PlanarTravelDistanceAt(1),Is.GreaterThan(0));Assert.That(nested.PhysicalTravelDistanceAt(1,0),Is.GreaterThan(0));
            Assert.That(root.PlanarAccelerationBound,Is.GreaterThan(0));Assert.That(root.PositionErrorBoundMeters,Is.GreaterThan(0));
        }
        [Test] public void SharedContinuousBodyAndEdgeChecksAlsoApplyToDrivenSlip()
        {
            var f=new Fixture();var a=f.Actor(6,20);var frame=new ManualSkiWorldTrajectory(f.Drive(a));
            var definition=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"drive_contact\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"drive_work\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            double h=f.Pose.HeadingRadians;var edge=new SkiCourseEdgePlane("drive_right",f.Pose.X+Math.Cos(h),f.Pose.Z-Math.Sin(h),-Math.Cos(h),Math.Sin(h));
            var result=new ManualSkiEdgeSweep(f.Source.Venue.Occupancy,definition,work).Preview(frame,edge);
            Assert.That(result.Status,Is.EqualTo(SkiEdgeSweepStatus.PotentialBoundary));Assert.That(result.ClearUntilSeconds,Is.LessThan(.05));
        }
        [Test] public void ActualSessionForceInputCanPublishOneCompleteScopedWorldTick()
        {
            var f=new ManualScopedSessionTests.Fixture("base");var a=f.World.GetActor(0);var before=f.Session.Motion;
            var pending=f.Session.PrepareManualScopedStep(f.Command(),f.World,0);
            var input=f.Session.GetPreparedManualMotionInput(pending,f.World,0,0);var observation=a.Observation;
            var system=new ManualSkiContactDriveSystem(f.Model.Physics,f.Source.Venue.Occupancy,20);
            var frame=system.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),
                observation,f.Source.Catalog.SampleSurface(observation),input.DeliveredPowerWatts,4,0,.05,input.DragAreaMultiplier,input.SnowResistanceMultiplier,input.WindX,input.WindZ);
            var c=f.World.GetCursor(0);var window=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Source.Catalog.Get(c.OccurrenceIndex).LengthNanometers);
            Assert.That(f.World.TryPrepare(new[]{new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(frame))},new[]{window},
                Array.Empty<SkiReachedContactPair>(),0,Array.Empty<SkiActorCourseEdge>(),0,new[]{observation.GradeAlong(a.Pose)},out var proof),Is.True,f.World.LastAttempt.Status.ToString());
            new ManualScopedRosterSessionCommit(new[]{f.Session},f.World).Commit(new[]{pending},proof);
            Assert.That(f.Session.Tick.Value,Is.EqualTo(f.World.Tick.Value));Assert.That(f.Session.Motion.DistanceNanometers,Is.GreaterThan(before.DistanceNanometers));
            Assert.That(f.World.GetActor(0).Pose,Is.EqualTo(frame.State));Assert.That(f.World.GetActor(0).Velocity,Is.EqualTo(frame.MotionAt(1).Velocity));
        }
        [Test] public void AnActualReachedContactPaysAForceDrivenSuffixOnTheSameOwnedWorldClock()
        {
            var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,gap:-1,contactResponse:true);
            var world=f.World;var tick=world.BeginTick();var noPairs=Array.Empty<SkiReachedContactPair>();var noEdges=Array.Empty<SkiActorCourseEdge>();
            var grades=new[]{0.0,0.0};var prefix=f.Candidates();var windows=f.Windows();
            Assert.That(world.TryAppendToFirstContact(tick,prefix,windows,noPairs,0,noEdges,0,grades,out var arrival),Is.True);
            Assert.That(arrival.Status,Is.EqualTo(SkiContactScheduleStatus.Arrival));Assert.That(world.TryResolveArrivals(tick),Is.True);
            double remaining=world.GetRemainingSeconds(tick);var system=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),f.Source.Venue.Occupancy,20);
            var response=new ManualRacePathActorFrame[2];
            for(int i=0;i<2;i++)
            {
                var a=response[i]=world.GetWorkingActor(tick,i);var c=world.GetWorkingCursor(tick,i);
                var frame=system.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),
                    new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),a.Observation,f.Catalogs[i].SampleSurface(a.Observation),350,4,0,remaining);
                prefix[i]=new SkiWorldMotionCandidate(a.Athlete,new ManualSkiWorldTrajectory(frame));
                windows[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
                Assert.That(frame.MotionAt(0).Velocity,Is.EqualTo(a.Velocity));
                Assert.That(frame.FirstDrive.Output.PropulsiveNewtons,Is.GreaterThan(0));
            }
            var pair=new[]{new SkiReachedContactPair(response[0].Athlete,response[1].Athlete)};
            Assert.That(world.TryAppendInterval(tick,prefix,windows,pair,1,noEdges,0,grades),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);world.Commit(proof);
            for(int i=0;i<2;i++)
            {
                var accepted=world.GetActor(i);double oldSpeed=Math.Sqrt(response[i].Velocity.X*response[i].Velocity.X+response[i].Velocity.Z*response[i].Velocity.Z);
                double speed=Math.Sqrt(accepted.Velocity.X*accepted.Velocity.X+accepted.Velocity.Z*accepted.Velocity.Z);
                Assert.That(speed,Is.GreaterThan(oldSpeed));Assert.That(accepted.Pose,Is.EqualTo(prefix[i].Trajectory.Sample(1)));
            }
            Assert.That(world.Tick.Value,Is.EqualTo(1));
        }
        [Test] public void InvalidSurfaceMassAndForceDataNeverAlterTheAcceptedBody()
        {
            var f=new Fixture();var a=f.Actor(6,.8);var motion=new SkiContactMotion(a.Velocity,0);var before=a;
            Assert.Catch(()=>f.System.Preview(default,motion,f.Observation,f.Surface,350,4,0,.05));
            Assert.Catch(()=>f.System.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,50),motion,f.Observation,f.Surface,350,4,0,.05));
            Assert.Catch(()=>f.System.Preview(a,motion,default(ManualRouteProjection),f.Surface,350,4,0,.05));
            Assert.Catch(()=>f.System.Preview(a,motion,f.Observation,default,350,4,0,.05));
            Assert.Catch(()=>f.System.Preview(a,motion,f.Observation,f.Surface,double.NaN,4,0,.05));
            Assert.Catch(()=>f.System.Preview(a,motion,f.Observation,f.Surface,350,4,0,.051));
            Assert.That(a,Is.EqualTo(before));Assert.That(default(SkiDrivenContactTrajectory).IsValid,Is.False);
        }
        [Test] public void WarmDrivenSlipAndFrozenWorldQueriesAreRepeatableWithoutAllocations()
        {
            var f=new Fixture();var a=f.Actor(-.001,.4,.3);var expected=f.Drive(a,grip:4);
            double Query()
            {var frame=f.Drive(a,grip:4);var slice=new ManualSkiWorldTrajectory(frame).Slice(.01,.05);return slice.Sample(1).X+slice.PlanarTravelDistanceAt(1)+slice.PhysicalSpeedAt(1,0);}
            for(int i=0;i<128;i++)Query();long before=GC.GetAllocatedBytesForCurrentThread();double value=0;
            for(int i=0;i<10000;i++)value=Query();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(value,Is.EqualTo(Query()));Assert.That(f.Drive(a,grip:4).State,Is.EqualTo(expected.State));
            Assert.That(f.System.ContextJson,Does.Contain("manual_contact_longitudinal_drive_v2_exact_endpoint"));
        }
    }
}
