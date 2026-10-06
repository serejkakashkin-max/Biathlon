using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualSkiMotionTests
    {
        private static ManualSkiMotionSystem System() => new ManualSkiMotionSystem(SkiForceTests.Definition(),
            ManualSkiForceTrajectoryTests.HeadingDefinition(),CorneringTests.Definition(),20);
        private static (ManualSkiHeadingState pose,ManualRouteProjection observation,TrackSample surface) Slope()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var map=new ManualRouteProjectionMap(route,venue.Visual);var section=route.GetSection(0);var loop=venue.Visual.Get(section.LoopId);
            for(int i=1;i<loop.Count;i++)
            {
                double from=Math.Max(section.LoopStart.Meters,loop.Get(i-1).Distance),to=Math.Min(section.LoopEnd.Meters,loop.Get(i).Distance);
                if(to<=from||loop.Sample(to).Y-loop.Sample(from).Y<.1)continue;
                double local=(from+to)*.5;var p=loop.Sample(local);
                var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
                long station=(long)Math.Round((section.Start.Meters+local-section.LoopStart.Meters)*1e9);
                Assert.That(map.TryProject(pose,0,station,.01,false,out var observation),Is.True);
                var sample=new TrackSampler(venue.Track.GetLoop(observation.LoopId)).Sample(new TrackDistance((long)Math.Floor(observation.LocalMeters*1000)));
                return (pose,observation,sample);
            }
            throw new Exception("Woodland ascending geometry witness missing.");
        }

        [Test]
        public void ActualHeadingChangesGradeGravityAndWindWithoutAutoTurning()
        {
            var f=Slope();var system=System();var ski=new SkiState(123456789,10);
            var input=new ManualSkiMotionInput(0,0,70,70,windX:4,windZ:-3);
            var forward=system.Preview(ski,f.pose,f.observation,f.surface,input);
            var reversePose=new ManualSkiHeadingState(f.pose.X,f.pose.Z,f.pose.HeadingRadians+Math.PI);
            var crossPose=new ManualSkiHeadingState(f.pose.X,f.pose.Z,f.pose.HeadingRadians+Math.PI*.5);
            var reverse=system.Preview(ski,reversePose,f.observation,f.surface,input);
            var cross=system.Preview(ski,crossPose,f.observation,f.surface,input);
            Assert.That(forward.Travel.Output.GravityNewtons,Is.GreaterThan(0));
            Assert.That(reverse.Travel.Output.GravityNewtons,Is.EqualTo(-forward.Travel.Output.GravityNewtons).Within(1e-6));
            Assert.That(cross.Travel.Output.GravityNewtons,Is.EqualTo(0).Within(1e-6));
            Assert.That(reverse.Travel.Output.State.SpeedMetersPerSecond,Is.GreaterThan(forward.Travel.Output.State.SpeedMetersPerSecond));
            Assert.That(reverse.Headwind,Is.EqualTo(-forward.Headwind).Within(1e-8));
            Assert.That(cross.Heading.State.HeadingNanoradians,Is.EqualTo(crossPose.HeadingNanoradians));
            Assert.That(reverse.Heading.State.HeadingNanoradians,Is.EqualTo(reversePose.HeadingNanoradians));
            Assert.That(forward.TravelSurface.SnowProfileId,Is.EqualTo(f.surface.SnowProfileId));
            Assert.That(forward.TravelSurface.WidthMeters,Is.EqualTo(f.surface.WidthMeters));
            Assert.That(f.observation.RouteNanometers,Is.GreaterThan(0),"The preview does not replace/award observed station.");
        }

        [Test]
        public void ActualYawCostsSpeedAndLowSpeedTurningHasNoCurvatureSingularity()
        {
            var f=Slope();var system=System();var input=new ManualSkiMotionInput(0,0,0,0);
            var turnPose=new ManualSkiHeadingState(f.pose.X,f.pose.Z,f.pose.HeadingRadians,1.2);
            var fast=system.Preview(new SkiState(0,12),turnPose,f.observation,f.surface,input);
            var straight=system.Preview(new SkiState(0,12),f.pose,f.observation,f.surface,input);
            Assert.That(fast.Cornering.BrakingNewtons,Is.GreaterThan(0));
            Assert.That(straight.Cornering.BrakingNewtons,Is.Zero);
            double scale=1/Math.Sqrt(1+fast.TravelSurface.Grade*fast.TravelSurface.Grade);
            Assert.That(fast.Cornering.SafeSpeedMetersPerSecond,Is.EqualTo(fast.Cornering.LateralAccelerationLimit/(1.2*scale)).Within(1e-9));
            Assert.That(fast.Travel.Output.State.SpeedMetersPerSecond,Is.LessThan(straight.Travel.Output.State.SpeedMetersPerSecond));
            Assert.That(fast.Heading.ExceedsGrip,Is.True);
            foreach(double speed in new[]{0,1e-9,1e-6,.001})
            {
                var slow=system.Preview(new SkiState(0,speed),turnPose,f.observation,f.surface,input);
                Assert.That(slow.Cornering.BrakingNewtons,Is.Zero);
                Assert.That(double.IsFinite(slow.Heading.State.X),Is.True);
            }
        }

        [Test]
        public void CentreLineBendDoesNotBrakeAnAthleteWhoIsNotTurning()
        {
            var track=SyntheticTrackTests.Generated("synthetic_technical_descent_v1");
            var sample=new TrackSampler(track.GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
            Assert.That(Math.Abs(sample.CurvaturePerMeter),Is.GreaterThan(0));
            var corners=new CorneringSystem(CorneringTests.Definition(),SkiForceTests.Definition());var ski=new SkiState(0,20);
            Assert.That(corners.Evaluate(ski,sample,0,0,1,0).BrakingNewtons,Is.GreaterThan(0));
            Assert.That(corners.EvaluateWorldHeading(ski,sample,0,0,0,1,0).BrakingNewtons,Is.Zero);
        }

        [Test]
        public void ExistingTuckModifiersTradeDragForSteeringResponse()
        {
            var f=Slope();var pose=new ManualSkiHeadingState(f.pose.X,f.pose.Z,f.pose.HeadingRadians+Math.PI);var system=System();
            var ski=new SkiState(0,10);
            var normal=system.Preview(ski,pose,f.observation,f.surface,new ManualSkiMotionInput(1000,0,70,70));
            var tuck=system.Preview(ski,pose,f.observation,f.surface,new ManualSkiMotionInput(1000,0,70,70,dragAreaMultiplier:.65,steeringResponse:.75));
            Assert.That(tuck.Travel.Output.AirDragNewtons,Is.LessThan(normal.Travel.Output.AirDragNewtons));
            Assert.That(tuck.Travel.Output.State.SpeedMetersPerSecond,Is.GreaterThan(normal.Travel.Output.State.SpeedMetersPerSecond));
            Assert.That(tuck.Heading.State.YawRateRadiansPerSecond,Is.LessThan(normal.Heading.State.YawRateRadiansPerSecond));
        }

        [Test]
        public void InvalidAndStaleCourseInputsFailWithoutMutatingState()
        {
            var f=Slope();var system=System();var input=new ManualSkiMotionInput(0,350,70,70);var ski=new SkiState(0,5);
            Assert.Catch(()=>system.Preview(ski,default,f.observation,f.surface,input));
            Assert.Catch(()=>system.Preview(ski,f.pose,default,f.surface,input));
            Assert.Catch(()=>system.Preview(ski,f.pose,f.observation,default,input));
            Assert.Catch(()=>system.Preview(ski,f.pose,f.observation,f.surface,default));
            Assert.Catch(()=>system.PreviewDuration(ski,f.pose,f.observation,f.surface,input,.051));
            var wrong=new TrackSampler(SyntheticTrackTests.Generated("synthetic_flat_v1").GetLoop("main_long")).Sample(TrackDistance.FromMeters(100));
            Assert.Catch(()=>system.Preview(ski,f.pose,f.observation,wrong,input));
            Assert.Catch(()=>new ManualSkiMotionInput(1001,1,50,50));
            Assert.Catch(()=>new ManualSkiMotionInput(0,double.NaN,50,50));
            Assert.Catch(()=>new ManualSkiMotionInput(0,1,50,50,gripMultiplier:0));
            Assert.Catch(()=>new ManualSkiMotionInput(0,1,50,50,windX:double.PositiveInfinity));
            Assert.That(ski.SpeedMetersPerSecond,Is.EqualTo(5));
        }

        [Test]
        public void PreviewIsRepeatableAllocationFreeAndBindsItsDefinitions()
        {
            var f=Slope();var system=System();var input=new ManualSkiMotionInput(500,350,70,70);var ski=new SkiState(0,10);
            var expected=system.Preview(ski,f.pose,f.observation,f.surface,input);
            long before=GC.GetAllocatedBytesForCurrentThread();ManualSkiMotionFrame actual=default;
            for(int i=0;i<10000;i++)actual=system.Preview(ski,f.pose,f.observation,f.surface,input);
            long allocated=GC.GetAllocatedBytesForCurrentThread()-before;
            Assert.That(allocated,Is.Zero);Assert.That(actual.Heading.State,Is.EqualTo(expected.Heading.State));
            Assert.That(actual.Travel.Output.State,Is.EqualTo(expected.Travel.Output.State));
            Assert.That(system.ContextJson,Is.EqualTo(System().ContextJson));
            Assert.That(system.ContextJson,Does.Contain("manual_force_heading_preview_v1"));
        }
    }
}
