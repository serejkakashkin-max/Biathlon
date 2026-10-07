using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiForceTrajectoryTests
    {
        internal static ManualSkiSteeringDefinition HeadingDefinition() =>
            ManualSkiSteeringDefinition.FromJson("{\"schema_version\":1,\"id\":\"force_steering_test_v1\",\"maximum_yaw_rate_radps\":1.2,"
            +"\"yaw_acceleration_radps2\":2.4,\"neutral_yaw_deceleration_radps2\":3,\"high_speed_reference_mps\":12,"
            +"\"high_speed_response_multiplier\":0.55,\"minimum_speed_for_grip_limit_mps\":0.5,\"path_integration_substeps\":8}");
        private static ManualSkiSteeringSystem Steering(int frequency=20) => new ManualSkiSteeringSystem(HeadingDefinition(),frequency);
        private static TrackSample Flat() => new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));

        [Test]
        public void EncodedEndpointVelocityStartsTheNextFlatForceTickExactly()
        {
            var forces=new SkiForceSystem(SkiForceTests.Definition(),20);
            var steering=Steering();var ski=new SkiState(0,5.123456789);
            var pose=new ManualSkiHeadingState(0,0,.3);
            for(int i=0;i<200;i++)
            {
                var frame=steering.Preview(pose,i%3==0?500:i%3==1?-500:0,
                    forces.PreviewTrajectory(ski,350,Flat(),.05),1,4);
                var endpoint=frame.VelocityAt(1);
                ski=new SkiState(ski.DistanceNanometers+(long)Math.Round(frame.TravelDistanceAt(1)*1e9),frame.SlopeSpeedAt(1));
                pose=frame.State;
                var next=steering.Preview(pose,0,forces.PreviewTrajectory(ski,350,Flat(),.05),1,4);
                Assert.That(next.VelocityAt(0).X,Is.EqualTo(endpoint.X));
                Assert.That(next.VelocityAt(0).Z,Is.EqualTo(endpoint.Z));
            }
        }

        [Test]
        public void ForceTrajectoryRetainsLegacyOutputAndEveryStraightMotionPrefix()
        {
            var definition=SkiForceTests.Definition();var forces=new SkiForceSystem(definition,20);var flat=Flat();
            foreach(var scenario in new[]{(0.0,350.0,0.0),(10.0,350.0,0.0),(.01,0.0,500.0),(24.99,2000.0,0.0)})
            {
                var before=new SkiState(123456789,scenario.Item1);
                var travel=forces.PreviewTrajectory(before,scenario.Item2,flat,.05,brakingNewtons:scenario.Item3);
                var legacy=forces.Step(before,scenario.Item2,flat,brakingNewtons:scenario.Item3);
                Assert.That(travel.Output.State,Is.EqualTo(legacy.State));
                Assert.That(travel.Output.NetNewtons,Is.EqualTo(legacy.NetNewtons));
                var heading=Steering().Preview(new ManualSkiHeadingState(0,0,0),0,travel,1,4);
                for(int i=0;i<=20;i++)
                {
                    double t=.05*i/20;
                    var expected=i==0?before:forces.StepDuration(before,scenario.Item2,flat,t,brakingNewtons:scenario.Item3).State;
                    Assert.That(travel.StateAt(t),Is.EqualTo(expected));
                    Assert.That(heading.Sample(i/20.0).Z,Is.EqualTo((expected.DistanceNanometers-before.DistanceNanometers)/1e9).Within(1e-9));
                    Assert.That(heading.Sample(i/20.0).X,Is.Zero);
                }
            }
        }

        [Test]
        public void AccelerationFromRestUsesQuadraticDistanceInsteadOfUniformTickTravel()
        {
            var travel=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(default,350,Flat(),.05);
            var steering=Steering();var start=new ManualSkiHeadingState(0,0,0);
            var weighted=steering.Preview(start,0,travel,1,4);
            var uniform=steering.Preview(start,0,travel.DistanceAt(.05),travel.PeakSpeed,4);
            Assert.That(weighted.Sample(.5).Z,Is.EqualTo(travel.DistanceAt(.05)/4).Within(1e-9));
            Assert.That(uniform.Sample(.5).Z-weighted.Sample(.5).Z,Is.GreaterThan(.0001),"The old uniform kernel is not a valid acceleration prefix.");
        }

        [Test]
        public void BrakingStopsTranslationBeforeTickEndsWhilePivotMayContinue()
        {
            var travel=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(new SkiState(0,.01),0,Flat(),.05,brakingNewtons:500);
            Assert.That(travel.SpeedBoundarySeconds,Is.LessThan(.025));
            var frame=Steering().Preview(new ManualSkiHeadingState(0,0,0),1000,travel,1,4);
            var half=frame.Sample(.5);
            Assert.That(frame.State.X,Is.EqualTo(half.X));Assert.That(frame.State.Z,Is.EqualTo(half.Z));
            Assert.That(frame.State.HeadingNanoradians,Is.GreaterThan(half.HeadingNanoradians));
            Assert.That(travel.StateAt(.025),Is.EqualTo(travel.StateAt(.05)));
        }

        [Test]
        public void SpeedCapAndSlopeConversionRetainPaidTravelAndSharedPlayerAiResult()
        {
            var definition=SkiPhysicsDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(SkiForceTests.Definition().ToJson(),"maximum_speed_mps","0.1"));
            var travel=new SkiForceSystem(definition,20).PreviewTrajectory(default,2000,Flat(),.05);
            Assert.That(travel.SpeedBoundarySeconds,Is.InRange(.001,.049));
            double scale=1/Math.Sqrt(1+.15*.15);
            var start=new ManualSkiHeadingState(0,0,.3);var steering=Steering();
            var command=new ManualSkiCommand(new SimulationTick(1),new AthleteId("manual"),1000,600);
            var player=steering.Preview(start,command,travel,scale,4);
            var ai=steering.Preview(start,600,travel,scale,4);
            Assert.That(player.State,Is.EqualTo(ai.State));
            Assert.That(player.PlanarTravelDistanceMeters,Is.EqualTo(travel.DistanceAt(.05)*scale));
            Assert.That(player.PlanarTravelDistanceMeters,Is.LessThan(travel.DistanceAt(.05)));
            CheckNumericalReference(player,travel,scale,start);
        }

        [Test]
        public void AcceleratingTurnMatchesIndependentWeightedIntegrationAndMirrors()
        {
            var travel=new SkiForceSystem(SkiForceTests.Definition(),20).PreviewTrajectory(default,350,Flat(),.05);
            var steering=Steering();var start=new ManualSkiHeadingState(0,0,0);
            var right=steering.Preview(start,1000,travel,1,4);var left=steering.Preview(start,-1000,travel,1,4);
            CheckNumericalReference(right,travel,1,start);
            Assert.That(left.State.X,Is.EqualTo(-right.State.X).Within(1e-9));
            Assert.That(left.State.Z,Is.EqualTo(right.State.Z).Within(1e-9));
        }

        private static void CheckNumericalReference(ManualSkiHeadingFrame frame,SkiForceTrajectory travel,double scale,ManualSkiHeadingState start)
        {
            // Midpoint integration with many tiny panels is independent of the production
            // piecewise Simpson rule, including its angular/velocity boundary splitting.
            const int n=10000;double x=0,z=0,step=frame.DurationSeconds/n;
            for(int i=0;i<n;i++)
            {
                double t=(i+.5)*step;var pose=frame.Sample(t/frame.DurationSeconds);
                double v=travel.SpeedAt(t)*scale;
                x+=v*Math.Sin(pose.HeadingRadians)*step;z+=v*Math.Cos(pose.HeadingRadians)*step;
            }
            Assert.That(frame.State.X-start.X,Is.EqualTo(x).Within(2e-9));
            Assert.That(frame.State.Z-start.Z,Is.EqualTo(z).Within(2e-9));
        }

        [Test]
        public void GripDemandIncludesInternalMaximumOfAcceleratingAndAngularBrakingRamp()
        {
            var travel=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(new SkiState(0,.5),2000,Flat(),1);
            var frame=Steering(1).Preview(new ManualSkiHeadingState(0,0,0,1.2),0,travel,1,.1);
            double a=travel.Acceleration,t=(a*1.2-3*.5)/(6*a);
            Assert.That(t,Is.InRange(0,.4));
            double peak=(.5+a*t)*(1.2-3*t);
            Assert.That(frame.LateralAccelerationDemand,Is.EqualTo(peak).Within(1e-9));
            Assert.That(frame.LateralAccelerationDemand,Is.GreaterThan(.5*1.2));
            Assert.That(frame.ExceedsGrip,Is.True,"The preview reports excess; it does not silently erase existing yaw momentum.");
        }

        [Test]
        public void FractionalTravelDurationAndInvalidProfilesAreExplicit()
        {
            var forces=new SkiForceSystem(SkiForceTests.Definition(),20);var flat=Flat();var steering=Steering();
            var start=new ManualSkiHeadingState(1,2,0);var travel=forces.PreviewTrajectory(default,350,flat,.013);
            var frame=steering.Preview(start,500,travel,1,4);
            Assert.That(frame.DurationSeconds,Is.EqualTo(.013));
            Assert.That(frame.State.Z-2,Is.LessThan(travel.DistanceAt(.013)+1e-9));
            Assert.Catch(()=>steering.Preview(start,0,default(SkiForceTrajectory),1,4));
            Assert.Catch(()=>default(SkiForceTrajectory).DistanceAt(0));
            Assert.Catch(()=>travel.SpeedAt(double.NaN));Assert.Catch(()=>travel.StateAt(.02));
            Assert.Catch(()=>steering.Preview(start,0,travel,1.01,4));
            Assert.Catch(()=>steering.Preview(start,0,travel,0,4));
            Assert.Catch(()=>steering.Preview(start,default(ManualSkiCommand),travel,1,4));
            var longer=new SkiForceSystem(SkiForceTests.Definition(),1).PreviewTrajectory(default,350,flat,.1);
            Assert.Catch(()=>steering.Preview(start,0,longer,1,4));
        }

        [Test]
        public void SharedForceAndSteeringStreamRepeatsWithoutWarmAllocations()
        {
            var forces=new SkiForceSystem(SkiForceTests.Definition(),20);var flat=Flat();var steering=Steering();
            var ski=default(SkiState);var pose=new ManualSkiHeadingState(0,0,.3);var twin=pose;
            var warm=forces.PreviewTrajectory(ski,350,flat,.05);steering.Preview(pose,200,warm,1,4).Sample(.5);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++)
            {
                var travel=forces.PreviewTrajectory(ski,i%100<50?350:0,flat,.05);
                int intent=i%90<30?700:i%90<60?-700:0;
                var frame=steering.Preview(pose,intent,travel,1,4);
                twin=steering.Preview(twin,intent,travel,1,4).State;pose=frame.State;ski=travel.Output.State;frame.Sample(.33);
            }
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(pose,Is.EqualTo(twin));
            TestContext.WriteLine(FormattableString.Invariant($"force_heading_probe={ski.DistanceNanometers}:{pose.X:R}:{pose.Z:R}:{pose.HeadingNanoradians}"));
        }
    }
}
