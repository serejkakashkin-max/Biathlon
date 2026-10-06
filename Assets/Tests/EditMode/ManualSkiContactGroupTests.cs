using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiContactGroupTests
    {
        private const string Json="{\"schema_version\":1,\"id\":\"contact_group_test_v1\",\"maximum_passes\":128,\"closing_speed_tolerance_mps\":0.000001}";
        private static OccupancyDefinition Body()=>OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static ManualSkiContactGroup Group(string json=Json)=>new ManualSkiContactGroup(Body(),
            SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"group_contact_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}"),SkiContactGroupDefinition.FromJson(json));
        private static ManualSkiContactBody Actor(string id,double x,double z,double vx,double vz,double heading=0,double yaw=0,double mass=75)
            =>new ManualSkiContactBody(new AthleteId(id),new ManualSkiHeadingState(x,z,heading,yaw),new SkiPlanarVelocity(vx,vz),mass);

        [Test]
        public void ThreeWayRearContactSharesMomentumWithoutArtificialFullStop()
        {
            var bodies=new[]{Actor("a",0,0,0,8),Actor("b",0,1.5,0,0),Actor("c",0,3,0,0)};
            var motion=new SkiContactMotion[3];var result=Group().Preview(bodies,3,motion,new int[3]);
            Assert.That(result.Status,Is.EqualTo(SkiContactGroupStatus.Resolved));
            Assert.That(result.Passes,Is.GreaterThan(1));Assert.That(result.Impulses,Is.GreaterThan(2));
            Assert.That(result.MaximumClosingSpeed,Is.LessThanOrEqualTo(1e-6));
            foreach(var item in motion)Assert.That(item.Velocity.Z,Is.EqualTo(8.0/3).Within(2e-6));
            AssertConservation(bodies,motion);
            Assert.That(bodies[0].Velocity.Z,Is.EqualTo(8));Assert.That(bodies[1].Velocity.Z,Is.Zero);
        }

        [Test]
        public void InsufficientPassesExposeResidualClosingInsteadOfGrantingAdmission()
        {
            var bodies=new[]{Actor("a",0,0,0,8),Actor("b",0,1.5,0,0),Actor("c",0,3,0,0)};
            var motion=new SkiContactMotion[3];
            var result=Group(PhysiologyContractTests.ReplaceNumber(Json,"maximum_passes","1")).Preview(bodies,3,motion,new int[3]);
            Assert.That(result.Status,Is.EqualTo(SkiContactGroupStatus.WorkLimit));
            Assert.That(result.MaximumClosingSpeed,Is.EqualTo(2));Assert.That(result.Passes,Is.EqualTo(1));
            AssertConservation(bodies,motion);
        }

        [Test]
        public void CanonicalIdOrderMakesAllInputPermutationsExactlyIdentical()
        {
            var source=new[]{Actor("b",0,0,.8,8),Actor("c",.7,0,-.5,6),Actor("a",0,1.5,.2,3)};
            var expected=new SkiContactMotion[3];var group=Group();var baseline=group.Preview(source,3,expected,new int[3]);
            Assert.That(baseline.Status,Is.EqualTo(SkiContactGroupStatus.Resolved));
            foreach(var order in new[]{new[]{0,2,1},new[]{1,0,2},new[]{1,2,0},new[]{2,0,1},new[]{2,1,0}})
            {
                var bodies=new[]{source[order[0]],source[order[1]],source[order[2]]};var output=new SkiContactMotion[3];
                var result=group.Preview(bodies,3,output,new int[3]);
                Assert.That(result,Is.EqualTo(baseline));
                for(int i=0;i<3;i++)Assert.That(output[i],Is.EqualTo(expected[order[i]]));
                AssertConservation(bodies,output);
            }
        }

        [Test]
        public void AngularGroupRetainsRawSpinBetweenImpulsesAndConservesMomentum()
        {
            double yaw=.4;var encoded=new ManualSkiHeadingState(0,0,yaw);
            double x=.35+.75*Math.Sin(encoded.HeadingRadians)+.35*Math.Cos(encoded.HeadingRadians);
            var bodies=new[]{Actor("a",0,0,.3,6,yaw:.25,mass:60),Actor("b",x,0,-.5,5,heading:yaw,yaw:.35,mass:90),Actor("c",0,1.5,.2,3,yaw:-.12,mass:70)};
            var output=new SkiContactMotion[3];var result=Group().Preview(bodies,3,output,new int[3]);
            Assert.That(result.Status,Is.EqualTo(SkiContactGroupStatus.Resolved));
            Assert.That(result.Impulses,Is.GreaterThan(0));AssertConservation(bodies,output);
            bool unrounded=false;
            foreach(var motion in output)unrounded|=motion.YawRate!=Math.Round(motion.YawRate,9);
            Assert.That(unrounded,Is.True,"Pair iteration must not repeatedly quantize physical angular momentum.");
        }

        [Test]
        public void FortyActorFieldKeepsUninvolvedAthletesUnchanged()
        {
            var bodies=new ManualSkiContactBody[40];
            for(int i=0;i<40;i++)bodies[i]=Actor("athlete_"+i,i*3,0,0,6);
            bodies[1]=Actor("athlete_1",.7,0,-.5,6);
            var output=new SkiContactMotion[40];var result=Group().Preview(bodies,40,output,new int[40]);
            Assert.That(result.Status,Is.EqualTo(SkiContactGroupStatus.Resolved));Assert.That(result.Impulses,Is.EqualTo(1));
            for(int i=2;i<40;i++){Assert.That(output[i].Velocity,Is.EqualTo(bodies[i].Velocity));Assert.That(output[i].YawRate,Is.Zero);}
            AssertConservation(bodies,output);
        }

        [Test]
        public void PenetrationIsReportedWithoutMovingOrTeleportingAnyBody()
        {
            var bodies=new[]{Actor("a",0,0,0,8),Actor("b",.1,0,0,0)};var output=new SkiContactMotion[2];
            var result=Group().Preview(bodies,2,output,new int[2]);
            Assert.That(result.Status,Is.EqualTo(SkiContactGroupStatus.Penetrating));Assert.That(result.Impulses,Is.Zero);
            Assert.That(bodies[0].Pose.X,Is.Zero);Assert.That(bodies[1].Pose.X,Is.EqualTo(.1));
        }

        [Test]
        public void RepeatedGroupResolutionDoesNotAllocateOrChangeInputs()
        {
            var bodies=new[]{Actor("a",0,0,0,8),Actor("b",0,1.5,0,0),Actor("c",0,3,0,0)};
            var output=new SkiContactMotion[3];var order=new int[3];var group=Group();
            var expected=group.Preview(bodies,3,output,order);SkiContactGroupPreview actual=default;
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<100;i++)actual=group.Preview(bodies,3,output,order);
            long bytes=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(bytes,Is.Zero);Assert.That(actual,Is.EqualTo(expected));
            Assert.That(group.ContextJson,Is.EqualTo(Group().ContextJson));
        }

        [Test]
        public void InvalidBodiesBuffersAndDefinitionsFailBeforeWritingOutputs()
        {
            var a=Actor("a",0,0,0,0);var bodies=new[]{a,a};var output=new[]{new SkiContactMotion(new SkiPlanarVelocity(1,2),3),default(SkiContactMotion)};
            var group=Group();Assert.Catch(()=>group.Preview(bodies,2,output,new int[2]));
            Assert.That(output[0].YawRate,Is.EqualTo(3));
            Assert.Catch(()=>group.Preview(bodies,1,output,new int[0]));Assert.Catch(()=>group.Preview(new ManualSkiContactBody[1],1,output,new int[1]));
            foreach(var pair in new[]{("schema_version","2"),("maximum_passes","0"),("closing_speed_tolerance_mps","0.1")})
                Assert.Catch(()=>SkiContactGroupDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(Json,pair.Item1,pair.Item2)));
            Assert.Catch(()=>SkiContactGroupDefinition.FromJson(Json.Replace("\"maximum_passes\"","\"missing\"")));
            Assert.That(default(SkiContactGroupPreview).IsValid,Is.False);
        }

        private static void AssertConservation(ManualSkiContactBody[] bodies,SkiContactMotion[] output)
        {
            double beforeX=0,beforeZ=0,afterX=0,afterZ=0,beforeMoment=0,afterMoment=0,beforeEnergy=0,afterEnergy=0;
            var d=Body();double inertiaScale=(d.LongitudinalSeparation*d.LongitudinalSeparation+d.LateralSeparation*d.LateralSeparation)/12;
            for(int i=0;i<bodies.Length;i++)
            {
                var b=bodies[i];var v=b.Velocity;var next=output[i];double m=b.MassKilograms,inertia=m*inertiaScale,w=b.Pose.YawRateRadiansPerSecond;
                beforeX+=m*v.X;beforeZ+=m*v.Z;afterX+=m*next.Velocity.X;afterZ+=m*next.Velocity.Z;
                beforeMoment+=m*(b.Pose.Z*v.X-b.Pose.X*v.Z)+inertia*w;
                afterMoment+=m*(b.Pose.Z*next.Velocity.X-b.Pose.X*next.Velocity.Z)+inertia*next.YawRate;
                beforeEnergy+=.5*(m*(v.X*v.X+v.Z*v.Z)+inertia*w*w);
                afterEnergy+=.5*(m*(next.Velocity.X*next.Velocity.X+next.Velocity.Z*next.Velocity.Z)+inertia*next.YawRate*next.YawRate);
            }
            Assert.That(afterX,Is.EqualTo(beforeX).Within(1e-8));Assert.That(afterZ,Is.EqualTo(beforeZ).Within(1e-8));
            Assert.That(afterMoment,Is.EqualTo(beforeMoment).Within(1e-8));Assert.That(afterEnergy,Is.LessThanOrEqualTo(beforeEnergy+1e-8));
        }
    }
}
