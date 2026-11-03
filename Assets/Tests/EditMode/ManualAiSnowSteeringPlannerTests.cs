using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualAiSnowSteeringPlannerTests
    {
        [Test] public void CompleteSearchReusesSaturatedSnowResponseAndRetainsExactGlobalChoice()
        {
            var definition=ManualScopedFieldStepperTests.Steering();var planner=new ManualAiSnowSteeringPlanner(Body,definition,20,true);
            var body=new ManualSkiContactBody(new AthleteId("saturated_snow_query"),new ManualSkiHeadingState(4,-7,.15,.35),new SkiPlanarVelocity(2,8),75);
            var motion=new SkiContactMotion(body.Velocity,.35);const double grip=.5;double target=Math.PI/7;
            var snow=new ManualSkiSteeredSnowSystem(Body,definition,20);int expected=0;double error=Math.Abs(snow.Preview(body,motion,0,grip,.05).Snow.Motion.YawRate-target);
            for(int magnitude=1;magnitude<=1000;magnitude++)foreach(int command in new[]{-magnitude,magnitude})
            {double next=Math.Abs(snow.Preview(body,motion,command,grip,.05).Snow.Motion.YawRate-target);if(next<error){expected=command;error=next;}}
            Assert.That(planner.Plan(body,motion,target,grip),Is.EqualTo(expected));
            Assert.That(planner.LastPreviewCount,Is.LessThan(150),"Identical bounded target/acceleration requests must share one physical snow evaluation per signed plateau.");
        }
        private static readonly OccupancyDefinition Body=OccupancyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("occupancy_provisional_v1.json"));
        private static ManualAiSnowSteeringPlanner Planner()=>new ManualAiSnowSteeringPlanner(Body,ManualScopedFieldStepperTests.Steering(),20);
        private static ManualSkiContactBody Witness()=>new ManualSkiContactBody(new AthleteId("sliding_curve"),
            new ManualSkiHeadingState(-102.915943415,-27.447492428,.833691583,.413986566),
            new SkiPlanarVelocity(3.1476325303678307,4.4961191769282287),75);
        [Test] public void EndpointCorrectionRetainsNominalInsideEnvelopeAndPaysSharedSnowOutsideIt()
        {
            var body=Witness();var motion=new SkiContactMotion(body.Velocity,body.Pose.YawRateRadiansPerSecond);
            var planner=Planner();var snow=new ManualSkiSteeredSnowSystem(Body,ManualScopedFieldStepperTests.Steering(),20);
            double left=snow.Preview(body,motion,-1000,2.5,.05).Snow.Motion.YawRate;
            double right=snow.Preview(body,motion,1000,2.5,.05).Snow.Motion.YawRate;
            const int nominal=-220;
            foreach(double target in new[]{left,right,(left+right)*.5})
                Assert.That(planner.CorrectUnreachedYaw(body,motion,nominal,target,2.5),Is.EqualTo(nominal));
            Assert.That(planner.CorrectUnreachedYaw(body,motion,nominal,Math.Min(left,right)-.1,2.5),Is.EqualTo(-1000));
            Assert.That(planner.CorrectUnreachedYaw(body,motion,nominal,Math.Max(left,right)+.1,2.5),Is.EqualTo(1000));
            Assert.That(body,Is.EqualTo(Witness()));
            Assert.That(planner.ContextJson,Does.Contain("nominal_retained_inside_signed_endpoint_envelope"));
            for(int i=0;i<100;i++)planner.CorrectUnreachedYaw(body,motion,nominal,-.6,2.5);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)if(planner.CorrectUnreachedYaw(body,motion,nominal,-.6,2.5)!=-1000)Assert.Fail("Changed bounded correction.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.CorrectUnreachedYaw(body,motion,1001,0,2.5));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.CorrectUnreachedYaw(body,motion,nominal,double.NaN,2.5));
        }
        [Test] public void UnreachableSpinUsesLessDeflectionForTheSameAchievedTurn()
        {
            var body=Witness();var motion=new SkiContactMotion(body.Velocity,body.Pose.YawRateRadiansPerSecond);
            var snow=new ManualSkiSteeredSnowSystem(Body,ManualScopedFieldStepperTests.Steering(),20);
            const double desired=-.6;
            var minimum=snow.Preview(body,motion,-1000,2.5,.05);
            Assert.That(minimum.Snow.Motion.YawRate,Is.GreaterThan(desired),"A command cannot erase the existing spin in one tick.");
            int command=Planner().Plan(body,motion,desired,2.5);
            Assert.That(Math.Abs(command),Is.LessThan(1000),"An actuator plateau must not request full ski deflection and incur excess corner braking.");
            Assert.That(snow.Preview(body,motion,command,2.5,.05).Snow.Motion.YawRate,
                Is.EqualTo(minimum.Snow.Motion.YawRate).Within(1e-12),"Retain the achieved correction, without inventing grip or yaw.");
            Assert.That(snow.Preview(body,motion,command,2.5,.05).Snow.DissipatedEnergyJoules,Is.GreaterThanOrEqualTo(0));
            Assert.That(body,Is.EqualTo(Witness()),"Preview must preserve accepted pose and velocity.");
        }
        [TestCase(-.35)][TestCase(0d)][TestCase(.35)]
        public void ReachableYawIsSelectedFromActualSharedSnowResponse(double spin)
        {
            var pose=new ManualSkiHeadingState(0,0,0,spin);
            var body=new ManualSkiContactBody(new AthleteId("snow_inverse"),pose,new SkiPlanarVelocity(.2,3),75);
            var motion=new SkiContactMotion(body.Velocity,spin);var snow=new ManualSkiSteeredSnowSystem(Body,ManualScopedFieldStepperTests.Steering(),20);
            double target=snow.Preview(body,motion,217,3,.05).Snow.Motion.YawRate;
            int selected=Planner().Plan(body,motion,target,3);
            double error=Math.Abs(snow.Preview(body,motion,selected,3,.05).Snow.Motion.YawRate-target);
            double best=double.PositiveInfinity,previous=double.NegativeInfinity;bool nonMonotonic=false;
            for(int i=-1000;i<=1000;i++)
            {
                double actual=snow.Preview(body,motion,i,3,.05).Snow.Motion.YawRate;
                nonMonotonic|=actual<previous-1e-12;previous=actual;
                best=Math.Min(best,Math.Abs(actual-target));
            }
            TestContext.WriteLine("spin="+spin+" target="+target+" selected="+selected+" error="+error+" best="+best+
                " ends="+snow.Preview(body,motion,-1000,3,.05).Snow.Motion.YawRate+":"+
                snow.Preview(body,motion,0,3,.05).Snow.Motion.YawRate+":"+
                snow.Preview(body,motion,1000,3,.05).Snow.Motion.YawRate);
            Assert.That(nonMonotonic,Is.True,"This shared-force witness forbids assuming globally monotonic yaw response.");
            Assert.That(error,Is.EqualTo(best).Within(1e-12));
        }
        [Test] public void NeutralRestDoesNotInventAControlCorrectionAndWarmQueriesAllocateNothing()
        {
            var body=new ManualSkiContactBody(new AthleteId("snow_neutral"),new ManualSkiHeadingState(0,0,0),default,75);
            var planner=Planner();Assert.That(planner.Plan(body,default,0,3),Is.Zero);
            var moving=Witness();var motion=new SkiContactMotion(moving.Velocity,moving.Pose.YawRateRadiansPerSecond);
            int expected=planner.Plan(moving,motion,.4,2.5);
            for(int i=0;i<100;i++)planner.Plan(moving,motion,.4,2.5);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)if(planner.Plan(moving,motion,.4,2.5)!=expected)Assert.Fail("Changed snow intent.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.That(planner.ContextJson,Does.Contain("actual_slip_spin"));
        }
        [Test] public void BothSignedSidesAndNeutralDiscontinuityRetainAttainableYaw()
        {
            var snow=new ManualSkiSteeredSnowSystem(Body,ManualScopedFieldStepperTests.Steering(),20);var planner=Planner();
            foreach(double spin in new[]{-.35,0,.35})
            {
                var body=new ManualSkiContactBody(new AthleteId("nonmonotonic_snow"),new ManualSkiHeadingState(0,0,0,spin),new SkiPlanarVelocity(.2,3),75);
                var motion=new SkiContactMotion(body.Velocity,spin);
                foreach(int witness in new[]{-900,-503,-217,-1,0,1,217,503,900})
                {
                    double target=snow.Preview(body,motion,witness,3,.05).Snow.Motion.YawRate;
                    int selected=planner.Plan(body,motion,target,3);
                    Assert.That(snow.Preview(body,motion,selected,3,.05).Snow.Motion.YawRate,
                        Is.EqualTo(target).Within(1e-12),"spin="+spin+" attainable command="+witness+" selected="+selected);
                }
            }
        }
        [Test] public void InvalidPredictionCannotProduceIntent()
        {
            var body=Witness();var planner=Planner();var motion=new SkiContactMotion(body.Velocity,.4);
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(body,motion,double.NaN,3));
            Assert.Throws<ArgumentOutOfRangeException>(()=>planner.Plan(body,motion,0,-1));
            Assert.Throws<ArgumentException>(()=>planner.Plan(default,motion,0,3));
        }
        [Test] public void CompleteSearchFindsTheAttainableInteriorWhileLegacyJournalsKeepTheirOldChoice()
        {
            var body=new ManualSkiContactBody(new AthleteId("search_revision"),new ManualSkiHeadingState(0,0,0,.35),new SkiPlanarVelocity(.2,3),75);
            var motion=new SkiContactMotion(body.Velocity,.35);var steering=ManualScopedFieldStepperTests.Steering();
            var snow=new ManualSkiSteeredSnowSystem(Body,steering,20);double target=snow.Preview(body,motion,217,3,.05).Snow.Motion.YawRate;
            var old=new ManualAiSnowSteeringPlanner(Body,steering,20,completeSearch:false);var current=Planner();
            Assert.That(old.Plan(body,motion,target,3),Is.EqualTo(563),"Exact recorded v3 policy, including its non-global search.");
            int selected=current.Plan(body,motion,target,3);
            Assert.That(snow.Preview(body,motion,selected,3,.05).Snow.Motion.YawRate,Is.EqualTo(target).Within(1e-12));
            Assert.That(current.LastPreviewCount,Is.InRange(1,2*ManualSkiCommand.SteeringUnits+1));
            Assert.That(old.SearchVersion,Is.Zero);Assert.That(current.SearchVersion,Is.EqualTo(1));
            Assert.That(old.ContextJson,Does.Contain("intent_v3"));Assert.That(current.ContextJson,Does.Contain("intent_v4"));
        }
    }
}
