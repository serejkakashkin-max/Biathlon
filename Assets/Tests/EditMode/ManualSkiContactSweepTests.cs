using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiContactSweepTests
    {
        private const string SweepJson="{\"schema_version\":1,\"id\":\"sweep_test_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}";
        private static readonly AthleteId A=new AthleteId("a"),B=new AthleteId("b");
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static SkiContactDefinition Contact()=>SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"sweep_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
        private static ManualSkiContactSweep Sweep(string json=SweepJson)=>new ManualSkiContactSweep(Body(),Contact(),SkiContactSweepDefinition.FromJson(json));
        private static TrackSample Flat()=>new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
        private static ManualSkiHeadingFrame Frame(double x,double z,double heading=0,double yaw=0,int input=0,double speed=0,double power=0,double brake=0,double duration=1,int panels=32)
        {
            var d=ManualSkiForceTrajectoryTests.HeadingDefinition();
            d=ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),"path_integration_substeps",panels.ToString()));
            var trajectory=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(new SkiState(0,speed),power,Flat(),duration,brakingNewtons:brake);
            return new ManualSkiSteeringSystem(d,1).Preview(new ManualSkiHeadingState(x,z,heading,yaw),input,trajectory,1,100);
        }
        private static double Gap(ManualSkiHeadingFrame a,ManualSkiHeadingFrame b,double fraction)=>new ManualSkiContactSystem(Body(),Contact()).Preview(
            new ManualSkiContactBody(A,a.Sample(fraction),default,75),new ManualSkiContactBody(B,b.Sample(fraction),default,75)).GapMeters;

        [Test]
        public void TurningSkisCrossBetweenClearEndpointsWithoutMovingTheirCentres()
        {
            var a=Frame(0,0,yaw:1.2,input:1000);var b=Frame(1.177,0);
            Assert.That(Gap(a,b,0),Is.GreaterThan(.1));Assert.That(Gap(a,b,1),Is.GreaterThan(.0001));
            Assert.That(Gap(a,b,.94),Is.LessThan(0));
            var result=Sweep().Preview(A,a,B,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
            Assert.That(result.ClearUntilSeconds,Is.InRange(.8,.94));
            Assert.That(result.IntervalEndSeconds-result.ClearUntilSeconds,Is.LessThanOrEqualTo(1e-7));
            // Locate first tolerance crossing independently by bisection of geometry.
            double low=0,high=.94;
            for(int i=0;i<50;i++){double mid=(low+high)*.5;if(Gap(a,b,mid)>1e-6)low=mid;else high=mid;}
            Assert.That(result.ClearUntilSeconds,Is.LessThanOrEqualTo(high));
            Assert.That(high-result.ClearUntilSeconds,Is.LessThan(.0001));
        }

        [Test]
        public void FastPassThroughIsNotClearedBySeparatedEndpoints()
        {
            var a=Frame(-3,0,Math.PI/2,speed:20);var b=Frame(0,0);
            Assert.That(Gap(a,b,0),Is.GreaterThan(0));Assert.That(Gap(a,b,1),Is.GreaterThan(0));
            var result=Sweep().Preview(A,a,B,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
            Assert.That(result.ClearUntilSeconds,Is.InRange(.09,.12));
        }

        [Test]
        public void AcceleratingRearArrivalUsesQuadraticPhysicalTravel()
        {
            var a=Frame(0,0,power:350);var b=Frame(0,2);
            var result=Sweep().Preview(A,a,B,b);
            double acceleration=a.PlanarAccelerationBound;
            double expected=Math.Sqrt(2*(.5-Contact().ContactTolerance)/acceleration);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
            Assert.That(result.ClearUntilSeconds,Is.EqualTo(expected).Within(2e-6));
            Assert.That(result.ClearUntilSeconds,Is.LessThanOrEqualTo(expected));
        }

        [Test]
        public void MidStepStopDoesNotInventTravelIntoTheAthleteAhead()
        {
            var a=Frame(0,0,speed:.1,brake:500);var b=Frame(0,1.52);
            var result=Sweep().Preview(A,a,B,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(result.ClearUntilSeconds,Is.EqualTo(1));
            Assert.That(a.State.Z,Is.LessThan(.01));
        }

        [Test]
        public void ParallelClosePassUsesActualBoxesNotPermanentTurningCircles()
        {
            var a=Frame(0,0,speed:20);var b=Frame(.701,0,speed:20);
            var result=Sweep().Preview(A,a,B,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(result.IntervalChecks,Is.LessThan(512));
        }

        [Test]
        public void CoMovingPackRowsAreExcludedBeforeIntervalSearch()
        {
            var a=Frame(0,0,speed:20,duration:.05);
            var b=Frame(0,3,speed:20,duration:.05);
            var result=Sweep().Preview(A,a,B,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(result.IntervalChecks,Is.Zero,"Shared forward speed cannot close the gap between rows.");
        }

        [Test]
        public void StoppedAndDistantTurningBodiesCanBeExcluded()
        {
            var a=Frame(0,0,yaw:-.7,input:1000);var b=Frame(3,2,yaw:.9,input:-1000);
            Assert.That(Sweep().Preview(A,a,B,b).Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(Sweep().Preview(A,Frame(0,0),B,Frame(0,2)).Status,Is.EqualTo(SkiContactSweepStatus.Clear));
        }

        [Test]
        public void WorkExhaustionAndInitialOverlapNeverBecomeClear()
        {
            var a=Frame(-3,0,Math.PI/2,speed:20);var b=Frame(0,0);
            var limited=Sweep(PhysiologyContractTests.ReplaceNumber(SweepJson,"maximum_interval_checks","1")).Preview(A,a,B,b);
            Assert.That(limited.Status,Is.EqualTo(SkiContactSweepStatus.WorkLimit));Assert.That(limited.ClearUntilSeconds,Is.Zero);
            Assert.That(limited.IntervalChecks,Is.EqualTo(1));
            var shallow=Sweep(PhysiologyContractTests.ReplaceNumber(SweepJson,"maximum_depth","1")).Preview(A,a,B,b);
            Assert.That(shallow.Status,Is.EqualTo(SkiContactSweepStatus.WorkLimit));
            var overlap=Sweep().Preview(A,b,B,b);
            Assert.That(overlap.Status,Is.EqualTo(SkiContactSweepStatus.InitialProximity));Assert.That(overlap.ClearUntilSeconds,Is.Zero);
        }

        [Test]
        public void SwapRepeatabilityAndHotQueriesAllocateNothing()
        {
            var sweep=Sweep();var a=Frame(0,0,yaw:1.2,input:1000);var b=Frame(1.177,0);
            var expected=sweep.Preview(A,a,B,b);var reverse=sweep.Preview(B,b,A,a);
            Assert.That(reverse,Is.EqualTo(expected));
            SkiContactSweepPreview actual=default;
            for(int i=0;i<10;i++)sweep.Preview(A,a,B,b);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++)actual=sweep.Preview(A,a,B,b);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(bytes,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
            Assert.That(sweep.ContextJson,Is.EqualTo(Sweep().ContextJson));
        }

        [Test]
        public void IndependentAnalyticAcceleratingArcFitsTheIntegrationReserve()
        {
            foreach(int panels in new[]{1,8,32})foreach(double origin in new[]{0.0,900000.0})
            {
                var definition=ManualSkiForceTrajectoryTests.HeadingDefinition();
                string json=PhysiologyContractTests.ReplaceNumber(definition.ToJson(),"high_speed_response_multiplier","1");
                json=PhysiologyContractTests.ReplaceNumber(json,"path_integration_substeps",panels.ToString());
                var travel=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(new SkiState(0,5),700,Flat(),1);
                const double yaw=1.2,heading=.2;
                var frame=new ManualSkiSteeringSystem(ManualSkiSteeringDefinition.FromJson(json),1).Preview(new ManualSkiHeadingState(origin,origin,heading,yaw),1000,travel,1,100);
                for(int i=1;i<=20;i++)
                {
                    double t=i/20.0,end=heading+yaw*t,a=travel.Acceleration;
                    double x=5*(Math.Cos(heading)-Math.Cos(end))/yaw+a*(-t*Math.Cos(end)/yaw+(Math.Sin(end)-Math.Sin(heading))/(yaw*yaw));
                    double z=5*(Math.Sin(end)-Math.Sin(heading))/yaw+a*(t*Math.Sin(end)/yaw+(Math.Cos(end)-Math.Cos(heading))/(yaw*yaw));
                    var sample=frame.Sample(t);
                    double dx=sample.X-origin-x,dz=sample.Z-origin-z;
                    Assert.That(Math.Sqrt(dx*dx+dz*dz),Is.LessThanOrEqualTo(frame.PositionErrorBoundMeters));
                }
            }
        }

        [Test]
        public void ClearPrefixesStaySeparatedAcrossFixedTurningAndAccelerationMatrix()
        {
            var sweep=Sweep();var geometry=new ManualSkiContactSystem(Body(),Contact());int clear=0,possible=0;
            foreach(double yaw in new[]{-.8,0,.8})foreach(int input in new[]{-1000,0,1000})
            foreach(double speed in new[]{0.0,4,12})foreach(double offset in new[]{.71,1.1,2,4})
            {
                var a=Frame(0,0,yaw:yaw,input:input,speed:speed,power:350,duration:.05);
                var b=Frame(offset,.2,yaw:-yaw,input:-input,speed:speed,power:0,duration:.05);
                var result=sweep.Preview(A,a,B,b);
                if(result.Status==SkiContactSweepStatus.Clear)clear++;else possible++;
                for(int i=0;i<=128;i++)
                {
                    double fraction=i/128.0;
                    if(fraction*.05>=result.ClearUntilSeconds)break;
                    var gap=geometry.Preview(new ManualSkiContactBody(A,a.Sample(fraction),default,75),
                        new ManualSkiContactBody(B,b.Sample(fraction),default,75)).GapMeters;
                    Assert.That(gap,Is.GreaterThan(Contact().ContactTolerance),$"yaw={yaw}, input={input}, speed={speed}, offset={offset}, t={fraction*.05}");
                }
            }
            Assert.That(clear,Is.GreaterThan(0));Assert.That(possible,Is.GreaterThan(0));
        }

        [Test]
        public void ExistingParallelOrDepartingContactDoesNotCauseAnAutomaticZeroTimeStop()
        {
            var sweep=Sweep();var a=Frame(0,0,speed:6,duration:.05);var b=Frame(.7,0,speed:6,duration:.05);
            Assert.That(sweep.Preview(A,a,B,b).Status,Is.EqualTo(SkiContactSweepStatus.InitialProximity));
            var continuing=sweep.PreviewExistingContact(A,a,B,b);
            Assert.That(continuing.IsContactContinuation,Is.True);Assert.That(continuing.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(continuing.ClearUntilSeconds,Is.EqualTo(.05));
            Assert.That(sweep.PreviewExistingContact(B,b,A,a),Is.EqualTo(continuing));
            var departing=Frame(0,1.5,speed:8,duration:.05);
            Assert.That(sweep.PreviewExistingContact(A,a,B,departing).Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(sweep.ContinuationContextJson,Is.Not.EqualTo(sweep.ContextJson));
        }

        [Test]
        public void ExistingContactStillRejectsTurningIntoAnotherBodyOrRealPenetration()
        {
            var sweep=Sweep();var a=Frame(0,0,duration:.05);var turning=Frame(.7,0,input:-1000,duration:.05);
            var result=sweep.PreviewExistingContact(A,a,B,turning);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
            Assert.That(result.ClearUntilSeconds,Is.LessThan(.002));
            Assert.That(sweep.PreviewExistingContact(A,a,B,Frame(.699,0,duration:.05)).Status,Is.EqualTo(SkiContactSweepStatus.InitialProximity));
            Assert.Catch(()=>sweep.PreviewExistingContact(A,a,B,Frame(2,0,duration:.05)));
            // Even steering away can swing the rear of a ski into its neighbour
            // before the centre has translated sideways. Do not exempt that contact.
            var moving=Frame(0,0,speed:6,duration:.05);var noseAway=Frame(.7,0,input:1000,speed:6,duration:.05);
            Assert.That(Gap(moving,noseAway,.2),Is.LessThan(-Contact().ContactTolerance));
            Assert.That(sweep.PreviewExistingContact(A,moving,B,noseAway).Status,Is.EqualTo(SkiContactSweepStatus.PotentialContact));
        }

        [Test] public void DistantRotatingBodiesUseTheConservativeBroadPhase()
        {
            var a=Frame(0,0,yaw:1.2,input:1000);var b=Frame(100,100,speed:20);
            var result=Sweep().Preview(A,a,B,b);
            Assert.That(result.Status,Is.EqualTo(SkiContactSweepStatus.Clear));
            Assert.That(result.IntervalChecks,Is.EqualTo(0));
            Assert.That(result.ClearUntilSeconds,Is.EqualTo(a.DurationSeconds));
        }
        [Test]
        public void InvalidOrUnboundedInputsAreRejectedAndDefaultReceiptIsInvalid()
        {
            var sweep=Sweep();var a=Frame(0,0);var b=Frame(2,0,duration:.5);
            Assert.Catch(()=>sweep.Preview(A,a,A,a));Assert.Catch(()=>sweep.Preview(A,a,B,b));
            Assert.Catch(()=>sweep.Preview(A,a,B,default));
            var legacy=new ManualSkiSteeringSystem(ManualSkiForceTrajectoryTests.HeadingDefinition(),1).Preview(new ManualSkiHeadingState(0,0,0),0,1,1,4);
            Assert.Catch(()=>sweep.Preview(A,a,B,legacy));Assert.Catch(()=>{var unused=legacy.PositionErrorBoundMeters;});
            Assert.Catch(()=>a.VelocityAt(double.NaN));
            foreach(var pair in new[]{("schema_version","2"),("maximum_depth","41"),("maximum_interval_checks","0"),("time_tolerance_s","0")})
                Assert.Catch(()=>SkiContactSweepDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(SweepJson,pair.Item1,pair.Item2)));
            Assert.Catch(()=>SkiContactSweepDefinition.FromJson(SweepJson.Replace("\"maximum_depth\"","\"missing\"")));
            Assert.That(default(SkiContactSweepPreview).IsValid,Is.False);
        }
    }
}
