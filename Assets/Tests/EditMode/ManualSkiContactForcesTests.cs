using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiContactForcesTests
    {
        private sealed class Fixture
        {
            internal readonly ManualRacePathCatalogTests.Fixture Source=new ManualRacePathCatalogTests.Fixture();
            internal readonly ManualSkiHeadingState Origin;
            internal readonly double Fx,Fz,Rx,Rz;
            internal readonly SkiContactDefinition Contact;
            internal readonly ManualSkiContactForces System;
            internal Fixture(int capacity=3,int passes=128,double restitution=0)
            {
                Origin=Source.At(1,10*SkiState.NanometersPerMeter);Fx=Math.Sin(Origin.HeadingRadians);Fz=Math.Cos(Origin.HeadingRadians);Rx=Fz;Rz=-Fx;
                Contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"force_contacts\",\"restitution\":"+
                    restitution.ToString(global::System.Globalization.CultureInfo.InvariantCulture)+",\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
                var work=SkiContactGroupDefinition.FromJson("{\"schema_version\":1,\"id\":\"force_group\",\"maximum_passes\":"+passes+",\"closing_speed_tolerance_mps\":0.00000001}");
                System=new ManualSkiContactForces(Source.Venue.Occupancy,Contact,work,capacity);
            }
            internal ManualSkiContactBody Actor(string id,double station,double forward=6,double side=0,double lateral=0,double mass=75,double yaw=0)
                =>new ManualSkiContactBody(new AthleteId(id),new ManualSkiHeadingState(Origin.X+Fx*station+Rx*lateral,Origin.Z+Fz*station+Rz*lateral,
                    Origin.HeadingRadians,yaw),new SkiPlanarVelocity(Fx*forward+Rx*side,Fz*forward+Rz*side),mass);
            internal SkiDrivenContactTrajectory Drive(ManualSkiContactBody actor,double power=350,double grip=0,double seconds=.05,double drag=1,double snow=1)
            {
                Assert.That(Source.Catalog.TryProject(actor.Pose,1,10*SkiState.NanometersPerMeter,50,out var observation),Is.True);
                var physics=SkiPhysicsDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(SkiForceTests.Definition().ToJson(),"mass_kg",
                    actor.MassKilograms.ToString(global::System.Globalization.CultureInfo.InvariantCulture)));
                return new ManualSkiContactDriveSystem(physics,Source.Venue.Occupancy,20).Preview(actor,
                    new SkiContactMotion(actor.Velocity,actor.Pose.YawRateRadiansPerSecond),observation,Source.Catalog.SampleSurface(observation),power,grip,0,seconds,drag,snow);
            }
            internal double Forward(SkiPlanarVelocity velocity)=>velocity.X*Fx+velocity.Z*Fz;
            internal double Side(SkiPlanarVelocity velocity)=>velocity.X*Rx+velocity.Z*Rz;
            internal double Length=>Source.Venue.Occupancy.LongitudinalSeparation;
        }
        [TestCase(0)][TestCase(6)]
        public void RearTractionSharesAForceWithoutChangingTheInitialPoseOrPayingAStop(double speed)
        {
            var f=new Fixture();var bodies=new[]{f.Actor("rear",0,speed),f.Actor("front",f.Length,speed)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var result=f.System.Preview(bodies,free,2,output);Assert.That(result.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(result.SupportPoints,Is.EqualTo(2));Assert.That(result.DurationSeconds,Is.EqualTo(.05));
            double expected=(f.Forward(free[0].MotionAt(1).Velocity)+f.Forward(free[1].MotionAt(1).Velocity))*.5;
            for(int i=0;i<2;i++)
            {
                Assert.That(output[i].Sample(0),Is.EqualTo(bodies[i].Pose));Assert.That(output[i].MotionAt(0).Velocity,Is.EqualTo(bodies[i].Velocity));
                Assert.That(f.Forward(output[i].MotionAt(1).Velocity),Is.EqualTo(expected).Within(2e-8));
                Assert.That(output[i].State.YawRateRadiansPerSecond,Is.EqualTo(0).Within(2e-8));
                double travel=(output[i].State.X-bodies[i].Pose.X)*f.Fx+(output[i].State.Z-bodies[i].Pose.Z)*f.Fz;
                Assert.That(travel,Is.GreaterThan(0));
            }
            Assert.That(expected,Is.GreaterThan(speed));
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"force_sweep\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var sweep=new ManualSkiContactSweep(f.Source.Venue.Occupancy,f.Contact,work);
            var proof=sweep.PreviewExistingContact(bodies[0].Athlete,new ManualSkiWorldTrajectory(output[0]),bodies[1].Athlete,new ManualSkiWorldTrajectory(output[1]));
            Assert.That(proof.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            AssertConservation(f,bodies,free,output,result.DurationSeconds);
        }
        [Test] public void UnequalMassesReceiveTheSameContactAccelerationAndConserveLinearAndAngularMomentum()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,mass:50),f.Actor("b",f.Length,mass:100)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var r=f.System.Preview(bodies,free,2,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            double expected=(50*f.Forward(free[0].MotionAt(1).Velocity)+100*f.Forward(free[1].MotionAt(1).Velocity))/150;
            Assert.That(f.Forward(output[0].MotionAt(1).Velocity),Is.EqualTo(expected).Within(2e-8));
            Assert.That(f.Forward(output[1].MotionAt(1).Velocity),Is.EqualTo(expected).Within(2e-8));
            AssertConservation(f,bodies,free,output,r.DurationSeconds);
        }
        [Test] public void SlidingRotatingContactDoesNotSpendItsGapOnEveryFrozenForcePiece()
        {
            var f=new Fixture();
            var bodies=new[]{
                new ManualSkiContactBody(new AthleteId("a"),new ManualSkiHeadingState(138.792819999,-6.661073462,1.556764023,-.298906705),new SkiPlanarVelocity(1.8808856345668028,.1452044149532977),75),
                new ManualSkiContactBody(new AthleteId("b"),new ManualSkiHeadingState(137.271529425,-7.151588799,1.645348047,-.839529258),new SkiPlanarVelocity(2.1889557426408057,-.938862874436923),75)};
            var driver=new ManualSkiControlledDriveSystem(SkiForceTests.Definition(),f.Source.Venue.Occupancy,
                ManualScopedFieldStepperTests.Steering(),CorneringTests.Definition(),20);
            var free=new SkiDrivenContactTrajectory[2];
            for(int i=0;i<2;i++)
            {
                Assert.That(f.Source.Catalog.TryProject(bodies[i].Pose,i==0?1:0,
                    (i==0?0:39)*SkiState.NanometersPerMeter,3,out var observation),Is.True);
                free[i]=driver.Preview(bodies[i],new SkiContactMotion(bodies[i].Velocity,bodies[i].Pose.YawRateRadiansPerSecond),
                    observation,f.Source.Catalog.SampleSurface(observation),new ManualSkiMotionInput(0,i==0?0:350,100,100),.05).Drive;
            }
            var output=new SkiForceCoupledTrajectory[2];
            var result=f.System.Preview(bodies,free,2,output);
            Assert.That(result.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            var geometry=new ManualSkiContactSystem(f.Source.Venue.Occupancy,f.Contact);
            double initial=geometry.Preview(bodies[0],bodies[1]).GapMeters;
            double end=geometry.Preview(new ManualSkiContactBody(bodies[0].Athlete,output[0].State,output[0].MotionAt(1).Velocity,75),
                new ManualSkiContactBody(bodies[1].Athlete,output[1].State,output[1].MotionAt(1).Velocity,75)).GapMeters;
            Assert.That(end,Is.GreaterThanOrEqualTo(initial-1e-8),"A zero instantaneous normal acceleration must not conceal cubic closing over the force interval.");
            AssertConservation(f,bodies,free,output,result.DurationSeconds);
        }
        [Test] public void AStrongerFrontAthleteSeparatesWithoutAdhesiveReaction()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0),f.Actor("b",f.Length)};
            var free=new[]{f.Drive(bodies[0],0),f.Drive(bodies[1],350)};var output=new SkiForceCoupledTrajectory[2];
            var r=f.System.Preview(bodies,free,2,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            for(int k=0;k<r.SupportPoints;k++)Assert.That(f.System.GetReaction(k).NormalNewtons,Is.Zero);
            for(int i=0;i<2;i++)Assert.That(output[i].MotionAt(1).Velocity,Is.EqualTo(free[i].MotionAt(1).Velocity));
            Assert.That(f.Forward(output[1].MotionAt(1).Velocity),Is.GreaterThan(f.Forward(output[0].MotionAt(1).Velocity)));
        }
        [Test] public void AlreadyOpeningBodiesRemainFreeEvenWithAClosingFutureForce()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,6),f.Actor("b",f.Length,6.001)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var r=f.System.Preview(bodies,free,2,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));Assert.That(r.SupportPoints,Is.Zero);
            Assert.That(r.DurationSeconds,Is.GreaterThan(0).And.LessThan(.05));
            for(int i=0;i<2;i++)Assert.That(output[i].MotionAt(1).Velocity,Is.EqualTo(free[i].MotionAt(r.DurationSeconds/.05).Velocity));
            // Free opening motion ends at the actual gap-velocity reversal;
            // the later return to contact still requires another force solve.
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"opening_recontact\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var receipt=new ManualSkiContactSweep(f.Source.Venue.Occupancy,f.Contact,work).PreviewExistingContact(bodies[0].Athlete,
                new ManualSkiWorldTrajectory(output[0]),bodies[1].Athlete,new ManualSkiWorldTrajectory(output[1]));
            Assert.That(receipt.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            var full=new ManualSkiContactSweep(f.Source.Venue.Occupancy,f.Contact,work).PreviewExistingContact(bodies[0].Athlete,
                new ManualSkiWorldTrajectory(free[0]),bodies[1].Athlete,new ManualSkiWorldTrajectory(free[1]));
            Assert.That(full.Status,Is.Not.EqualTo(SkiContactSweepStatus.Clear));
        }
        [Test] public void SubSkinOpeningDoesNotRepeatAnUnresolvableGapVelocityPeak()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,6),f.Actor("b",f.Length,6.0000008)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var result=f.System.Preview(bodies,free,2,output);
            Assert.That(result.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(result.DurationSeconds,Is.EqualTo(.05));
            Assert.That(result.SupportPoints,Is.GreaterThan(0));
            AssertConservation(f,bodies,free,output,result.DurationSeconds);
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"subskin_opening\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var proof=new ManualSkiContactSweep(f.Source.Venue.Occupancy,f.Contact,work).PreviewExistingContact(bodies[0].Athlete,
                new ManualSkiWorldTrajectory(output[0]),bodies[1].Athlete,new ManualSkiWorldTrajectory(output[1]));
            Assert.That(proof.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
        }
        [Test] public void ThreeBodyForcePropagationIsStableAcrossEveryRosterPermutation()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("z",0),f.Actor("a",f.Length),f.Actor("m",2*f.Length)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0),f.Drive(bodies[2],0)};var expected=new SkiForceCoupledTrajectory[3];
            var reference=f.System.Preview(bodies,free,3,expected);Assert.That(reference.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(reference.SupportPoints,Is.EqualTo(4));Assert.That(reference.Passes,Is.GreaterThan(1));
            foreach(var order in new[]{new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}})
            {
                var actors=new[]{bodies[order[0]],bodies[order[1]],bodies[order[2]]};var drives=new[]{free[order[0]],free[order[1]],free[order[2]]};
                var output=new SkiForceCoupledTrajectory[3];var actual=f.System.Preview(actors,drives,3,output);Assert.That(actual,Is.EqualTo(reference));
                for(int i=0;i<3;i++)Assert.That(output[i],Is.EqualTo(expected[order[i]]));
                AssertConservation(f,actors,drives,output,actual.DurationSeconds);
            }
        }
        [Test] public void TwoSupportEndpointsPreserveAParallelFaceAgainstSpuriousForceInducedRoll()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0),f.Actor("b",f.Length)};
            var manifold=new ManualSkiContactSystem(f.Source.Venue.Occupancy,f.Contact).PreviewManifold(bodies[0],bodies[1]);
            Assert.That(manifold.HasSupport,Is.True);Assert.That(manifold.FirstX,Is.Not.EqualTo(manifold.SecondX));
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var r=f.System.Preview(bodies,free,2,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            double reaction=0;
            for(int k=0;k<2;k++){var p=f.System.GetReaction(k);Assert.That(p.NormalNewtons,Is.GreaterThan(0));reaction+=p.NormalNewtons;}
            Assert.That(reaction,Is.GreaterThan(0));Assert.That(Math.Abs(output[0].Acceleration.Yaw),Is.LessThan(1e-6));
            Assert.That(Math.Abs(output[1].Acceleration.Yaw),Is.LessThan(1e-6));
        }
        [Test] public void SlidingFrictionOpposesSlipAndStaysInsideEveryReactionCone()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,side:1),f.Actor("b",f.Length)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var r=f.System.Preview(bodies,free,2,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            double frictionPower=0;
            for(int k=0;k<r.SupportPoints;k++)
            {var p=f.System.GetReaction(k);Assert.That(p.NormalNewtons,Is.GreaterThanOrEqualTo(0));Assert.That(Math.Abs(p.TangentNewtons),Is.LessThanOrEqualTo(.2*p.NormalNewtons+1e-12));frictionPower+=p.TangentNewtons;}
            Assert.That(frictionPower,Is.Not.Zero);Assert.That(f.Side(output[0].MotionAt(1).Velocity),Is.LessThan(1));
            Assert.That(f.Side(output[1].MotionAt(1).Velocity),Is.GreaterThan(0));AssertConservation(f,bodies,free,output,r.DurationSeconds);
        }
        [Test] public void PersistentForceDoesNotUseImpactRestitution()
        {
            var zero=new Fixture(restitution:0);var bounce=new Fixture(restitution:1);
            var bodies=new[]{zero.Actor("a",0),zero.Actor("b",zero.Length)};var free=new[]{zero.Drive(bodies[0]),zero.Drive(bodies[1],0)};
            var a=new SkiForceCoupledTrajectory[2];var b=new SkiForceCoupledTrajectory[2];
            Assert.That(zero.System.Preview(bodies,free,2,a).Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(bounce.System.Preview(bodies,free,2,b).Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            for(int i=0;i<2;i++)Assert.That(a[i],Is.EqualTo(b[i]));
        }
        [Test] public void CommonForcePiecesEndBeforeAReverseStopOrSpeedCap()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,-.001,side:.4),f.Actor("b",10,SkiForceTests.Definition().MaximumSpeedMetersPerSecond-.0001)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],drag:0,snow:1e-9)};var output=new SkiForceCoupledTrajectory[2];
            Assert.That(free[0].RestartDrive.IsValid,Is.True);var r=f.System.Preview(bodies,free,2,output);
            Assert.That(free[1].ConstantAccelerationSeconds,Is.LessThan(.05),"Real cap breakpoint");
            Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(r.DurationSeconds,Is.EqualTo(Math.Min(free[0].ConstantAccelerationSeconds,free[1].ConstantAccelerationSeconds)));
            Assert.That(r.DurationSeconds,Is.InRange(0,.049));
            for(int i=0;i<2;i++)
            {Assert.That(output[i].MotionAt(1).Velocity.X,Is.EqualTo(free[i].MotionAt(r.DurationSeconds/.05).Velocity.X).Within(1e-12));Assert.That(output[i].State,Is.EqualTo(free[i].Sample(r.DurationSeconds/.05)));}
        }
        [Test] public void PhysicalSlicesReuseTheSameCoupledRootAndCountSideSlipWithoutRouteCredit()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,side:.4),f.Actor("b",10,side:.4)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1])};var output=new SkiForceCoupledTrajectory[2];
            Assert.That(f.System.Preview(bodies,free,2,output).Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            var root=new ManualSkiWorldTrajectory(output[0]);var prefix=root.Slice(0,.02);var suffix=root.Slice(.02,.05);
            Assert.That(prefix.Sample(1),Is.EqualTo(suffix.Sample(0)));Assert.That(prefix.VelocityAt(1),Is.EqualTo(suffix.VelocityAt(0)));
            Assert.That(root.PlanarTravelDistanceAt(1),Is.GreaterThan(0));Assert.That(suffix.PhysicalTravelDistanceAt(1,.1),Is.GreaterThan(0));
            Assert.That(suffix.Slice(.005,.03).Sample(0),Is.EqualTo(root.Sample(.5)));
        }
        [Test] public void AnUnresolvedPassBudgetOrVelocityContactNeverPublishesPartialOutput()
        {
            var f=new Fixture(passes:1);var bodies=new[]{f.Actor("a",0),f.Actor("b",f.Length),f.Actor("c",2*f.Length)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0),f.Drive(bodies[2],0)};var output=new SkiForceCoupledTrajectory[3];
            var r=f.System.Preview(bodies,free,3,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.WorkLimit));
            foreach(var row in output)Assert.That(row.IsValid,Is.False);
            bodies[0]=f.Actor("a",0,7);free[0]=f.Drive(bodies[0]);
            Assert.That(f.System.Preview(bodies,free,3,output).Status,Is.EqualTo(SkiContactForceStatus.VelocityResponseRequired));
            foreach(var row in output)Assert.That(row.IsValid,Is.False);
        }
        [Test] public void DeepOverlapAndForeignLateDriveFailBeforeCallerOutputChanges()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0),f.Actor("b",f.Length-.01)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1])};var output=new SkiForceCoupledTrajectory[2];
            Assert.That(f.System.Preview(bodies,free,2,output).Status,Is.EqualTo(SkiContactForceStatus.Penetrating));
            foreach(var row in output)Assert.That(row.IsValid,Is.False);
            bodies[1]=f.Actor("b",10);free[1]=f.Drive(f.Actor("b",11));
            Assert.Throws<ArgumentException>(()=>f.System.Preview(bodies,free,2,output));
            foreach(var row in output)Assert.That(row.IsValid,Is.False);
        }
        [Test] public void WarmForceManifoldSolvesAndTrajectorySamplesAllocateNothing()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0),f.Actor("b",f.Length)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            bool Cycle()=>f.System.Preview(bodies,free,2,output).Status==SkiContactForceStatus.Resolved&&output[0].Sample(.5).IsValid;
            for(int i=0;i<128;i++)Assert.That(Cycle(),Is.True);long before=GC.GetAllocatedBytesForCurrentThread();bool ok=true;
            for(int i=0;i<1000;i++)ok&=Cycle();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(ok,Is.True);Assert.That(allocated,Is.Zero);Assert.That(f.System.ContextJson,Does.Contain("two_support_endpoints"));
        }
        [Test] public void FortyBodiesInFiveDenseColumnsShareRealTractionWithoutAStationaryColumn()
        {
            var f=new Fixture(capacity:40,passes:4096);var bodies=new ManualSkiContactBody[40];var free=new SkiDrivenContactTrajectory[40];
            var output=new SkiForceCoupledTrajectory[40];
            for(int column=0;column<5;column++)for(int row=0;row<8;row++)
            {
                int i=column*8+row;bodies[i]=f.Actor("dense_"+i.ToString("D2"),row*f.Length,lateral:column-2);
                free[i]=f.Drive(bodies[i],row==0?350:0);
            }
            var r=f.System.Preview(bodies,free,40,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(r.SupportPoints,Is.EqualTo(70));Assert.That(r.DurationSeconds,Is.EqualTo(.05));
            Assert.That(r.Passes,Is.LessThanOrEqualTo(4096));
            foreach(var actor in output)Assert.That(f.Forward(actor.MotionAt(1).Velocity),Is.GreaterThan(5.9));
            AssertConservation(f,bodies,free,output,r.DurationSeconds);
            TestContext.WriteLine(FormattableString.Invariant($"dense_40;support_points={r.SupportPoints};passes={r.Passes};closing_acceleration={r.MaximumClosingAcceleration:R};residual_speed={r.MaximumReactionResidualSpeed:R}"));
        }
        [Test] public void OwnedWorldPaysUnequalForceContinuationOnItsCompleteClock()
        {
            var source=new ManualRacePathCatalogTests.Fixture();var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,
                gap:source.Venue.Occupancy.LongitudinalSeparation,contactResponse:true);var world=f.World;var tick=world.BeginTick();
            var before=world.GetActor(0);Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.True);
            var drives=new SkiDrivenContactTrajectory[2];var candidates=new SkiWorldMotionCandidate[2];var windows=new ManualScopedWorldWindow[2];
            var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),source.Venue.Occupancy,20);
            for(int i=0;i<2;i++)
            {
                var a=world.GetWorkingActor(tick,i);var c=world.GetWorkingCursor(tick,i);
                drives[i]=drive.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),
                    a.Observation,f.Catalogs[i].SampleSurface(a.Observation),i==0?350:0,4,0,.05);
                windows[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
            }
            Assert.That(world.TryPreviewContactDrivenMotion(tick,drives,candidates),Is.True,world.LastContactForces.Status.ToString());
            Assert.That(world.GetElapsedSeconds(tick),Is.Zero);Assert.That(world.GetActor(0),Is.EqualTo(before));
            Assert.That(world.TryAppendToOwnedFirstContact(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,new[]{0.0,0},out var receipt),Is.True,world.LastAttempt.Status.ToString());
            Assert.That(receipt.Status,Is.EqualTo(SkiContactScheduleStatus.Clear));Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);world.Commit(proof);
            Assert.That(world.Tick.Value,Is.EqualTo(1));Assert.That(world.GetPhysicalMotion(0).SpeedMetersPerSecond,Is.GreaterThan(1));
            Assert.That(world.GetActor(0).Pose,Is.EqualTo(candidates[0].Trajectory.Sample(1)));Assert.That(world.GetPhysicalMotion(1).DistanceNanometers,Is.GreaterThan(SkiState.NanometersPerMeter));
        }
        [Test] public void AnUnequalTractionPairKeepsMovingAcrossOneHundredCompleteOwnedTicks()
        {
            var source=new ManualRacePathCatalogTests.Fixture();var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,
                gap:source.Venue.Occupancy.LongitudinalSeparation,contactResponse:true);var world=f.World;
            var drives=new SkiDrivenContactTrajectory[2];var candidates=new SkiWorldMotionCandidate[2];var windows=new ManualScopedWorldWindow[2];
            var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),source.Venue.Occupancy,20);
            var before=new[]{world.GetPhysicalMotion(0),world.GetPhysicalMotion(1)};
            for(int t=0;t<100;t++)
            {
                var tick=world.BeginTick();Assert.That(world.TryResolveOwnedCurrentContacts(tick),Is.True,"Response tick "+t);
                for(int i=0;i<2;i++)
                {
                    var a=world.GetWorkingActor(tick,i);var c=world.GetWorkingCursor(tick,i);
                    drives[i]=drive.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),
                        a.Observation,f.Catalogs[i].SampleSurface(a.Observation),i==0?350:0,4,0,.05);
                    windows[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
                }
                Assert.That(world.TryPreviewContactDrivenMotion(tick,drives,candidates),Is.True,"Forces tick "+t+": "+world.LastContactForces.Status);
                bool advanced=world.TryAppendToOwnedFirstContact(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,new[]{0.0,0},out var receipt);
                if(!advanced)
                {
                    var a=world.GetWorkingActor(tick,0);var b=world.GetWorkingActor(tick,1);
                    var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"diagnostic\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
                    var geometry=new ManualSkiContactSystem(source.Venue.Occupancy,contact).Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new ManualSkiContactBody(b.Athlete,b.Pose,b.Velocity,75));
                    TestContext.WriteLine(FormattableString.Invariant($"tick={t};gap={geometry.GapMeters:R};wa={a.Pose.YawRateRadiansPerSecond:R};wb={b.Pose.YawRateRadiansPerSecond:R};force_seconds={world.LastContactForces.DurationSeconds:R};points={world.LastContactForces.SupportPoints};passes={world.LastContactForces.Passes};closing={world.LastContactForces.MaximumClosingAcceleration:R};schedule={receipt.Status};admission={receipt.Admission.Status};clear={receipt.Seconds:R}"));
                }
                Assert.That(advanced,Is.True,"Motion tick "+t+": "+world.LastAttempt.Status);
                Assert.That(receipt.Status,Is.EqualTo(SkiContactScheduleStatus.Clear));Assert.That(world.TryFinalizeTick(tick,out var proof),Is.True);world.Commit(proof);
                for(int i=0;i<2;i++)
                {
                    var current=world.GetPhysicalMotion(i);Assert.That(current.SpeedMetersPerSecond,Is.GreaterThan(.9));
                    Assert.That(current.DistanceNanometers,Is.GreaterThan(before[i].DistanceNanometers));before[i]=current;
                }
            }
            Assert.That(world.Tick.Value,Is.EqualTo(100));
        }
        [Test] public void KineticFrictionEndsItsPieceAtZeroSlipInsteadOfReversingAndAddingEnergy()
        {
            var f=new Fixture();var bodies=new[]{f.Actor("a",0,side:1e-5),f.Actor("b",f.Length)};
            var free=new[]{f.Drive(bodies[0]),f.Drive(bodies[1],0)};var output=new SkiForceCoupledTrajectory[2];
            var r=f.System.Preview(bodies,free,2,output);Assert.That(r.Status,Is.EqualTo(SkiContactForceStatus.Resolved));
            Assert.That(r.DurationSeconds,Is.InRange(0,.049));
            double inertia=(f.Source.Venue.Occupancy.LongitudinalSeparation*f.Source.Venue.Occupancy.LongitudinalSeparation+
                f.Source.Venue.Occupancy.LateralSeparation*f.Source.Venue.Occupancy.LateralSeparation)/12;
            double freeEnergy=0,coupledEnergy=0;
            for(int i=0;i<2;i++)
            {
                var a=free[i].MotionAt(r.DurationSeconds/.05);var b=output[i].MotionAt(1);
                freeEnergy+=75*(a.Velocity.X*a.Velocity.X+a.Velocity.Z*a.Velocity.Z+inertia*a.YawRate*a.YawRate)*.5;
                coupledEnergy+=75*(b.Velocity.X*b.Velocity.X+b.Velocity.Z*b.Velocity.Z+inertia*b.YawRate*b.YawRate)*.5;
            }
            Assert.That(coupledEnergy,Is.LessThanOrEqualTo(freeEnergy+1e-9));
        }
        [Test] public void WarmOwnedForcePreviewResponseAndCommonClockAllocateNothing()
        {
            var source=new ManualRacePathCatalogTests.Fixture();var f=new ManualScopedWorldRosterTests.Fixture(mixed:false,
                gap:source.Venue.Occupancy.LongitudinalSeparation,contactResponse:true);var world=f.World;var setup=world.BeginTick();
            Assert.That(world.TryResolveOwnedCurrentContacts(setup),Is.True);var drives=new SkiDrivenContactTrajectory[2];
            var candidates=new SkiWorldMotionCandidate[2];var windows=new ManualScopedWorldWindow[2];var grades=new[]{0.0,0};
            var drive=new ManualSkiContactDriveSystem(SkiForceTests.Definition(),source.Venue.Occupancy,20);
            for(int i=0;i<2;i++)
            {
                var a=world.GetWorkingActor(setup,i);var c=world.GetWorkingCursor(setup,i);
                drives[i]=drive.Preview(new ManualSkiContactBody(a.Athlete,a.Pose,a.Velocity,75),new SkiContactMotion(a.Velocity,a.Pose.YawRateRadiansPerSecond),
                    a.Observation,f.Catalogs[i].SampleSurface(a.Observation),i==0?350:0,4,0,.05);
                windows[i]=new ManualScopedWorldWindow(c.Athlete,c.OccurrenceIndex,c.ObservedArcNanometers,1,f.Catalogs[i].Get(c.OccurrenceIndex).LengthNanometers);
            }
            world.AbortTick(setup);
            bool Cycle()
            {
                var tick=world.BeginTick();bool ok=world.TryResolveOwnedCurrentContacts(tick)&&world.TryPreviewContactDrivenMotion(tick,drives,candidates);
                ok&=world.TryAppendToOwnedFirstContact(tick,candidates,windows,Array.Empty<SkiActorCourseEdge>(),0,grades,out _);
                ok&=world.TryFinalizeTick(tick,out _);return ok;
            }
            for(int i=0;i<128;i++)Assert.That(Cycle(),Is.True);long before=GC.GetAllocatedBytesForCurrentThread();bool all=true;
            for(int i=0;i<1000;i++)all&=Cycle();long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(all,Is.True);Assert.That(allocated,Is.Zero);Assert.That(world.Tick.Value,Is.Zero);
        }
        private static void AssertConservation(Fixture f,ManualSkiContactBody[] bodies,SkiDrivenContactTrajectory[] free,SkiForceCoupledTrajectory[] coupled,double seconds)
        {
            double x=0,z=0,angular=0,inertia=(f.Source.Venue.Occupancy.LongitudinalSeparation*f.Source.Venue.Occupancy.LongitudinalSeparation+
                f.Source.Venue.Occupancy.LateralSeparation*f.Source.Venue.Occupancy.LateralSeparation)/12;
            for(int i=0;i<bodies.Length;i++)
            {
                var a=free[i].MotionAt(seconds/free[i].DurationSeconds);var b=coupled[i].MotionAt(1);double mass=bodies[i].MassKilograms;
                double dx=mass*(b.Velocity.X-a.Velocity.X),dz=mass*(b.Velocity.Z-a.Velocity.Z);x+=dx;z+=dz;
                angular+=bodies[i].Pose.Z*dx-bodies[i].Pose.X*dz+mass*inertia*(b.YawRate-a.YawRate);
            }
            Assert.That(x,Is.EqualTo(0).Within(1e-9));Assert.That(z,Is.EqualTo(0).Within(1e-9));Assert.That(angular,Is.EqualTo(0).Within(1e-8));
        }
    }
}
