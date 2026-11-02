using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class ManualAiSteeringControllerTests
    {
        private const string PolicyJson="{\"schema_version\":1,\"id\":\"manual_ai_test_v1\",\"lookahead_base_m\":2,"+
            "\"lookahead_speed_seconds\":0.5,\"lookahead_max_m\":8,\"heading_response_seconds\":0.6,\"yaw_rate_feedback\":0.4}";
        private const string SteeringJson="{\"schema_version\":1,\"id\":\"manual_steering_test_v1\",\"maximum_yaw_rate_radps\":1.2,"+
            "\"yaw_acceleration_radps2\":2.4,\"neutral_yaw_deceleration_radps2\":3,\"high_speed_reference_mps\":12,"+
            "\"high_speed_response_multiplier\":0.55,\"minimum_speed_for_grip_limit_mps\":0.5,\"path_integration_substeps\":8}";
        private static (RaceRoute route,TrackVisualDefinition visual,ManualRouteProjectionMap map,ManualAiSteeringController controller) Create(string format="mass_start")
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+format+"_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+format+"_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var map=new ManualRouteProjectionMap(route,venue.Visual);
            return (route,venue.Visual,map,new ManualAiSteeringController(map,
                ManualAiSteeringDefinition.FromJson(PolicyJson),ManualSkiSteeringDefinition.FromJson(SteeringJson)));
        }
        private static long Nano(double metres)=>(long)Math.Round(metres*SkiState.NanometersPerMeter);

        [Test] public void StoppedAthleteFacingAwayCanTurnBackTowardTheOwnedTarget()
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(point.X,point.Z,Math.Atan2(point.ForwardX,point.ForwardZ)+Math.PI);
            Assert.That(f.map.TryProject(pose,0,Nano(10),.001,false,out var at),Is.True);
            var plan=f.controller.Plan(pose,at,0,false);
            Assert.That(Math.Abs(plan.HeadingErrorRadians),Is.GreaterThan(3));
            double yaw=f.controller.PhysicalPursuitYaw(plan,pose,default);
            Assert.That(Math.Abs(yaw),Is.GreaterThan(.5),"A target behind the skis must not produce sin(pi) zero steering.");
            Assert.That(Math.Sign(yaw),Is.EqualTo(Math.Sign(plan.HeadingErrorRadians)));
        }

        [Test] public void OwnedEndpointRetainsFittingParallelLineInsteadOfLateCentreTurn()
        {
            var f=Create();var section=f.route.GetSection(0);var loop=f.visual.Get(section.LoopId);
            int segment=1;while(segment<loop.Count&&loop.Get(segment).Distance-loop.Get(segment-1).Distance<6)segment++;
            Assert.That(segment,Is.LessThan(loop.Count));
            double local=(loop.Get(segment-1).Distance+loop.Get(segment).Distance)*.5;
            var p=loop.Sample(local);double yaw=Math.Atan2(p.ForwardX,p.ForwardZ);
            var pose=new ManualSkiHeadingState(p.X+p.ForwardZ*.04,p.Z-p.ForwardX*.04,yaw);
            long station=Nano(section.Start.Meters+local-section.LoopStart.Meters);
            Assert.That(f.map.TryProject(pose,0,station,.001,false,out var at),Is.True);
            var centre=f.controller.PlanOwnedTarget(pose,at,0,false,0,.5,.01);
            var parallel=f.controller.PlanParallelEndpoint(pose,centre,.5);
            Assert.That(Math.Abs(centre.HeadingErrorRadians),Is.GreaterThan(1));
            // World-coordinate subtraction at this 1 cm endpoint has a larger
            // floating angular error than a metre-scale chord; intent is zero.
            Assert.That(parallel.HeadingErrorRadians,Is.EqualTo(0).Within(1e-9));
            Assert.That(parallel.SteeringInput,Is.Zero);
            Assert.That(parallel.TargetLateralMeters,Is.EqualTo(.04).Within(1.0/SkiState.NanometersPerMeter));
            Assert.That(parallel.Target,Is.EqualTo(centre.Target));
            Assert.That(parallel.LookaheadMeters,Is.EqualTo(centre.LookaheadMeters));
            Assert.That(f.controller.PlanParallelEndpoint(pose,centre,.02).TargetLateralMeters,Is.EqualTo(.02));
            Assert.That(pose.X,Is.EqualTo(p.X+p.ForwardZ*.04));
            Assert.Throws<ArgumentException>(()=>f.controller.PlanParallelEndpoint(default,centre,.5));
            Assert.Throws<ArgumentException>(()=>f.controller.PlanParallelEndpoint(pose,default,.5));
            Assert.Throws<ArgumentOutOfRangeException>(()=>f.controller.PlanParallelEndpoint(pose,centre,-1));
        }

        [TestCase(0d)][TestCase(4d)][TestCase(12d)]
        public void PhysicalPursuitInvertsTheActualSharedSpeedResponse(double speed)
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(point.X,point.Z,Math.Atan2(point.ForwardX,point.ForwardZ)-.05);
            Assert.That(f.map.TryProject(pose,0,Nano(10),.001,false,out var at),Is.True);
            var plan=f.controller.Plan(pose,at,speed,false);
            var velocity=new SkiPlanarVelocity(speed*Math.Sin(pose.HeadingRadians),speed*Math.Cos(pose.HeadingRadians));
            var definition=ManualSkiSteeringDefinition.FromJson(SteeringJson);
            double dx=plan.Target.X+plan.Target.ForwardZ*plan.TargetLateralMeters-pose.X;
            double dz=plan.Target.Z-plan.Target.ForwardX*plan.TargetLateralMeters-pose.Z;
            double desired=2*Math.Max(speed,definition.MinimumSpeedForGripLimit)*Math.Sin(plan.HeadingErrorRadians)/Math.Sqrt(dx*dx+dz*dz);
            double target=desired*(1+ManualAiSteeringDefinition.FromJson(PolicyJson).YawRateFeedback);
            int intent=f.controller.PhysicalPursuitIntent(plan,pose,velocity);
            var achieved=ManualSkiSteeringSystem.GetRequest(definition,intent,speed,1000);
            double full=ManualSkiSteeringSystem.GetRequest(definition,1000,speed,1000).RequestedYawRate;
            Assert.That(Math.Abs(target),Is.LessThan(full));
            Assert.That(achieved.RequestedYawRate,Is.EqualTo(target).Within(full/2000+1e-12));
            Assert.That(new ManualSkiSteeringSystem(definition,50).GetRequest(intent,speed,1000).RequestedYawRate,
                Is.EqualTo(achieved.RequestedYawRate),"Intent inversion must not invent a tick-frequency dependency.");
        }
        [Test] public void OffCentrePursuitUsesActualChordRatherThanCountedLookahead()
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            double yaw=Math.Atan2(point.ForwardX,point.ForwardZ);
            var pose=new ManualSkiHeadingState(point.X+point.ForwardZ,point.Z-point.ForwardX,yaw);
            Assert.That(f.map.TryProject(pose,0,Nano(10),.001,false,out var at),Is.True);
            var plan=f.controller.Plan(pose,at,1,false);
            double dx=plan.Target.X-pose.X,dz=plan.Target.Z-pose.Z;
            double chord=Math.Sqrt(dx*dx+dz*dz);
            Assert.That(Math.Abs(chord-plan.LookaheadMeters),Is.GreaterThan(.1));
            var definition=ManualSkiSteeringDefinition.FromJson(SteeringJson);
            double desired=2*Math.Sin(plan.HeadingErrorRadians)/chord;
            double expected=desired*(1+ManualAiSteeringDefinition.FromJson(PolicyJson).YawRateFeedback);
            int command=f.controller.PhysicalPursuitIntent(plan,pose,new SkiPlanarVelocity(Math.Sin(yaw),Math.Cos(yaw)));
            double full=ManualSkiSteeringSystem.GetRequest(definition,1000,1,1000).RequestedYawRate;
            Assert.That(Math.Abs(expected),Is.LessThan(full));
            Assert.That(ManualSkiSteeringSystem.GetRequest(definition,command,1,1000).RequestedYawRate,
                Is.EqualTo(expected).Within(full/2000+1e-12));
        }
        [Test] public void ActualSlipChangesPursuitIntentWithoutChangingAcceptedPoseOrVelocity()
        {
            var f=Create();var point=f.visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(point.X,point.Z,Math.Atan2(point.ForwardX,point.ForwardZ));
            Assert.That(f.map.TryProject(pose,0,Nano(10),.001,false,out var at),Is.True);
            var plan=f.controller.Plan(pose,at,4,false);
            var right=new SkiPlanarVelocity(4*Math.Sin(pose.HeadingRadians+.15),4*Math.Cos(pose.HeadingRadians+.15));
            var left=new SkiPlanarVelocity(4*Math.Sin(pose.HeadingRadians-.15),4*Math.Cos(pose.HeadingRadians-.15));
            int expected=f.controller.PhysicalPursuitIntent(plan,pose,right);
            Assert.That(expected,Is.LessThan(0));
            Assert.That(f.controller.PhysicalPursuitIntent(plan,pose,left),Is.GreaterThan(0));
            for(int i=0;i<100;i++)f.controller.PhysicalPursuitIntent(plan,pose,right);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)if(f.controller.PhysicalPursuitIntent(plan,pose,right)!=expected)Assert.Fail("Changed physical intent.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            Assert.That(pose.X,Is.EqualTo(point.X).Within(1e-9));Assert.That(pose.Z,Is.EqualTo(point.Z).Within(1e-9));
        }

        [Test] public void ShortOwnedFiringPathTargetsItsEndpointWithoutLookingThroughTheLifecycleGate()
        {
            var f=new ManualRacePathCatalogTests.Fixture();var c=f.Catalog;int at=c.GetFiringOccurrence(0);
            long arc=c.Get(at).LengthNanometers-Nano(.1);var pose=f.At(at,arc);
            Assert.That(c.TryProject(pose,at,arc,.01,out var observation),Is.True);
            var controller=new ManualAiSteeringController(c.ProjectionGeometry,ManualAiSteeringDefinition.FromJson(PolicyJson),
                ManualSkiSteeringDefinition.FromJson(SteeringJson));
            var plan=controller.PlanOwnedTarget(pose,c.GetGeometryObservation(observation),12,false,0,1,.1);
            Assert.That(plan.IsValid,Is.True);Assert.That(plan.LookaheadMeters,Is.EqualTo(.1));
            Assert.That(plan.Target.SectionIndex,Is.EqualTo(at));
            Assert.That(plan.Target.RouteNanometers,Is.EqualTo(c.GetGeometryObservation(observation).RouteNanometers+Nano(.1)));
            Assert.Throws<ArgumentOutOfRangeException>(()=>controller.PlanOwnedTarget(pose,c.GetGeometryObservation(observation),12,false,0,1,0));
        }

        [Test] public void CornerEffortPolicyIsVersionedBoundedAndAllocationFree()
        {
            var json=PhysiologyContractTests.ProjectFile(
                "Assets/Resources/Biathlon/manual_ai_corner_effort_v1.json");
            var policy=ManualAiCornerEffortDefinition.FromJson(json);
            Assert.That(ManualAiCornerEffortDefinition.FromJson(policy.ToJson()).ToJson(),
                Is.EqualTo(policy.ToJson()));
            foreach(var field in new[]{"schema_version","id","bend_lookahead_m",
                "bend_angle_threshold_rad","bend_power_cap_w"})
                Assert.Catch(()=>ManualAiCornerEffortDefinition.FromJson(
                    json.Replace("\""+field+"\"","\"missing_"+field+"\"")),field);
            Assert.Catch(()=>ManualAiCornerEffortDefinition.FromJson(
                json.Replace("\"bend_lookahead_m\": 70","\"bend_lookahead_m\": 0")));
            Assert.Catch(()=>ManualAiCornerEffortDefinition.FromJson(
                json.Replace("\"bend_power_cap_w\": 300","\"bend_power_cap_w\": -1")));
            var f=Create();var first=f.route.GetSection(0);
            Assert.That(f.map.TrySampleAhead(0,Nano(first.Start.Meters),5,false,out var start),Is.True);
            var pose=new ManualSkiHeadingState(start.X,start.Z,
                Math.Atan2(start.ForwardX,start.ForwardZ));
            Assert.That(f.map.TryProject(pose,0,start.RouteNanometers,.01,false,
                out var observation),Is.True);
            var planner=new ManualAiCornerEffortPlanner(f.map,policy);
            var plan=planner.Plan(observation,400,false);
            Assert.That(plan.IsValid&&plan.BendObserved,Is.True);
            Assert.That(plan.RequestedPowerWatts,Is.EqualTo(300));
            Assert.That(planner.Plan(observation,200,false).RequestedPowerWatts,
                Is.EqualTo(200),"A tactical cap cannot increase an athlete's request.");
            Assert.That(planner.ContextJson,Does.Contain(policy.Id));
            var endPoint=f.visual.Get(first.LoopId).Sample(first.LoopEnd.Meters-1);
            var endPose=new ManualSkiHeadingState(endPoint.X,endPoint.Z,
                Math.Atan2(endPoint.ForwardX,endPoint.ForwardZ));
            Assert.That(f.map.TryProject(endPose,0,Nano(first.Start.Meters+first.Length.Meters-1),
                .01,false,out var nearEnd),Is.True);
            var noUnadmittedLookahead=planner.Plan(nearEnd,400,false);
            Assert.That(noUnadmittedLookahead.IsValid,Is.False,
                "Missing a target across a sealed section is not proof of a straight road.");
            for(int i=0;i<100;i++)planner.Plan(observation,400,false);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
                if(planner.Plan(observation,400,false).RequestedPowerWatts!=300)
                    Assert.Fail("AI corner effort changed for the same observation.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }

        [TestCase(200.0,false,false)]
        [TestCase(300.0,false,false)]
        [TestCase(400.0,false,false)]
        [TestCase(400.0,false,true)]
        public void ProductionCandidateOnlyAcceptsContinuouslyClearWoodlandMotion(double deliveredPowerWatts,
            bool expectedBoundaryRejection,bool adaptiveCornerEffort)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            // The fixed 940m single-section witness predates the course shortening.
            string Read(string id)=>PhysiologyContractTests.ProjectFile(id.StartsWith(root,StringComparison.Ordinal)?"dotnet/Biathlon.Tests/Fixtures/woodland_d518195/"+id.Substring(root.Length)+".json":"Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var map=new ManualRouteProjectionMap(route,venue.Visual);
            var heading=ManualSkiSteeringDefinition.FromJson(Read("Biathlon/manual_ski_steering_v2"));
            var controller=new ManualAiSteeringController(map,
                ManualAiSteeringDefinition.FromJson(Read("Biathlon/manual_ai_steering_v2")),heading);
            var effortPlanner=new ManualAiCornerEffortPlanner(map,
                ManualAiCornerEffortDefinition.FromJson(Read("Biathlon/manual_ai_corner_effort_v1")));
            var motion=new ManualSkiMotionSystem(
                SkiPhysicsDefinition.FromJson(Read("Biathlon/ski_physics_provisional_v1")),heading,
                CorneringDefinition.FromJson(Read("Biathlon/cornering_provisional_v1")),20);
            var tube=new ManualRouteTubeSweep(map,venue.Occupancy,SkiContactSweepDefinition.FromJson(
                "{\"schema_version\":1,\"id\":\"manual_ai_force_probe_work_v1\",\"maximum_depth\":32,"+
                "\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}"));
            var first=route.GetSection(0);
            Assert.That(map.TrySampleAhead(0,Nano(first.Start.Meters),5,false,out var target),Is.True);
            var pose=new ManualSkiHeadingState(target.X,target.Z,Math.Atan2(target.ForwardX,target.ForwardZ));
            var ski=new SkiState(0,4);long station=target.RouteNanometers;
            double bodyRadius=.5*Math.Sqrt(venue.Occupancy.LongitudinalSeparation*
                venue.Occupancy.LongitudinalSeparation+venue.Occupancy.LateralSeparation*
                venue.Occupancy.LateralSeparation)+venue.Occupancy.EdgeClearance;
            double maximumLateral=0,maximumSpeed=0;int reducedTicks=0;
            var sampler=new TrackSampler(venue.Track.GetLoop(first.LoopId));
            for(int tick=0;tick<8000&&station<Nano(940);tick++)
            {
                Assert.That(map.TryProject(pose,0,station,2,false,
                    out var observed),Is.True,"Selected route projection failed at tick "+tick+
                    " station_m="+station/1e9+" speed_mps="+ski.SpeedMetersPerSecond+
                    " x="+pose.X+" z="+pose.Z+" yaw="+pose.HeadingRadians);
                var surface=sampler.Sample(new TrackDistance((long)Math.Floor(observed.LocalMeters*1000)));
                maximumLateral=Math.Max(maximumLateral,Math.Abs(observed.LateralMeters));
                Assert.That(Math.Abs(observed.LateralMeters)+bodyRadius,Is.LessThan(surface.WidthMeters*.5),
                    "Full body left the course at tick "+tick);
                var plan=controller.Plan(pose,observed,ski.SpeedMetersPerSecond,false);
                Assert.That(plan.IsValid,Is.True,"AI target missing at tick "+tick);
                double actualPower=deliveredPowerWatts;
                if(adaptiveCornerEffort)
                {
                    var effortPlan=effortPlanner.Plan(observed,deliveredPowerWatts,false);
                    if(effortPlan.IsValid)
                    {
                        actualPower=effortPlan.RequestedPowerWatts;
                        if(actualPower<deliveredPowerWatts)reducedTicks++;
                    }
                    else Assert.That(observed.RouteNanometers,Is.GreaterThan(Nano(925)),
                        "Only an explicitly sealed next occurrence may lack this 70 m lookahead.");
                }
                var frame=motion.Preview(ski,pose,observed,surface,
                    new ManualSkiMotionInput(plan.SteeringInput,actualPower,70,70));
                var continuous=tube.Preview(new ManualSkiWorldTrajectory(frame.Heading),0,
                    observed.RouteNanometers,2,false);
                if(expectedBoundaryRejection&&continuous.Status!=ManualRouteTubeStatus.Clear)
                {
                    foreach(double trialPower in new[]{300.0,200.0,0.0})
                    {
                        var slower=motion.Preview(ski,pose,observed,surface,
                            new ManualSkiMotionInput(plan.SteeringInput,trialPower,70,70));
                        var checkedTrial=tube.Preview(new ManualSkiWorldTrajectory(slower.Heading),0,
                            observed.RouteNanometers,2,false);
                        TestContext.WriteLine("alternate_w="+trialPower+" status="+checkedTrial.Status+
                            " clear_s="+checkedTrial.ClearUntilSeconds);
                    }
                    Assert.That(observed.RouteNanometers,Is.InRange(Nano(65),Nano(80)),
                        "The high-power limit must be a measured local course boundary.");
                    Assert.That(continuous.ClearUntilSeconds,Is.LessThan(frame.Heading.DurationSeconds));
                    TestContext.WriteLine("manual_force_high_power_rejected_at_m="+
                        observed.RouteNanometers/1e9+" clear_s="+continuous.ClearUntilSeconds);
                    return;
                }
                Assert.That(continuous.Status,Is.EqualTo(ManualRouteTubeStatus.Clear),
                    "Continuous edge proof failed at tick "+tick+" station_m="+
                    observed.RouteNanometers/1e9+" edge="+continuous.SourceEdgeId+
                    " clear_s="+continuous.ClearUntilSeconds);
                pose=frame.Heading.State;ski=frame.Travel.Output.State;
                maximumSpeed=Math.Max(maximumSpeed,ski.SpeedMetersPerSecond);
                station=observed.RouteNanometers;
            }
            TestContext.WriteLine("manual_force_candidate_power_w="+deliveredPowerWatts+
                " station_m="+station/1e9+
                " maximum_lateral_m="+maximumLateral+" maximum_speed_mps="+maximumSpeed+
                " reduced_ticks="+reducedTicks);
            Assert.That(station,Is.GreaterThanOrEqualTo(Nano(940)));
            if(adaptiveCornerEffort)Assert.That(reducedTicks,Is.GreaterThan(0));
            Assert.That(expectedBoundaryRejection,Is.False,
                "The diagnostic high-power input must not silently bypass course proof.");
        }

        [TestCase(4.0)]
        [TestCase(7.0)]
        public void ProductionCandidateSteersFirstWoodlandBendsThroughSharedYawKernel(double speed)
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            // Retain this captured kinematic-controller probe on its exact geometry.
            string Read(string id)=>PhysiologyContractTests.ProjectFile(id.StartsWith(root,StringComparison.Ordinal)?"dotnet/Biathlon.Tests/Fixtures/woodland_d518195/"+id.Substring(root.Length)+".json":"Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var map=new ManualRouteProjectionMap(route,venue.Visual);
            var physical=ManualSkiSteeringDefinition.FromJson(Read("Biathlon/manual_ski_steering_v2"));
            var policy=ManualAiSteeringDefinition.FromJson(Read("Biathlon/manual_ai_steering_v2"));
            Assert.That(ManualSkiSteeringDefinition.FromJson(physical.ToJson()).ToJson(),Is.EqualTo(physical.ToJson()));
            Assert.That(ManualAiSteeringDefinition.FromJson(policy.ToJson()).ToJson(),Is.EqualTo(policy.ToJson()));
            var controller=new ManualAiSteeringController(map,policy,physical);
            var solver=new ManualSkiSteeringSystem(physical,20);
            double bodyRadius=.5*Math.Sqrt(venue.Occupancy.LongitudinalSeparation*
                venue.Occupancy.LongitudinalSeparation+venue.Occupancy.LateralSeparation*
                venue.Occupancy.LateralSeparation)+venue.Occupancy.EdgeClearance;
            var first=route.GetSection(0);
            Assert.That(map.TrySampleAhead(0,Nano(first.Start.Meters),5,false,out var target),Is.True);
            var pose=new ManualSkiHeadingState(target.X,target.Z,Math.Atan2(target.ForwardX,target.ForwardZ));
            long station=target.RouteNanometers;
            double maximumLateral=0;
            for(int tick=0;tick<5000&&station<Nano(940);tick++)
            {
                Assert.That(map.TryProject(pose,0,station,.6,false,out var observed),Is.True,
                    "The controller must retain a local, explicitly selected occurrence at tick "+tick);
                var plan=controller.Plan(pose,observed,speed,false);
                Assert.That(plan.IsValid,Is.True,"Missing forward target at tick "+tick);
                maximumLateral=Math.Max(maximumLateral,Math.Abs(observed.LateralMeters));
                var surface=new TrackSampler(venue.Track.GetLoop(observed.LoopId))
                    .Sample(new TrackDistance((long)Math.Floor(observed.LocalMeters*1000)));
                Assert.That(Math.Abs(observed.LateralMeters)+bodyRadius,Is.LessThan(surface.WidthMeters*.5),
                    "The full body left the authored corridor at tick "+tick);
                pose=solver.Preview(pose,plan.SteeringInput,speed/20,speed,3).State;
                station=observed.RouteNanometers;
            }
            TestContext.WriteLine("manual_ai_candidate_speed_mps="+speed+" station_m="+
                station/(double)SkiState.NanometersPerMeter+
                " max_lateral_m="+maximumLateral);
            Assert.That(station,Is.GreaterThanOrEqualTo(Nano(940)));
            Assert.That(controller.ContextJson,Does.Contain(policy.Id));
        }

        [Test] public void PolicyIsVersionedValidatedAndBoundInReplayContext()
        {
            var policy=ManualAiSteeringDefinition.FromJson(PolicyJson);
            Assert.That(ManualAiSteeringDefinition.FromJson(policy.ToJson()).ToJson(),Is.EqualTo(policy.ToJson()));
            string context=Create().controller.ContextJson;
            Assert.That(context.Contains("manual_ai_signed_yaw_intent_v6_behind_target_recovery"),Is.True);
            Assert.That(context.Contains("physical_pursuit_actual_velocity_and_shared_speed_response_v2_actual_chord"),Is.True);
            foreach(var field in new[]{"schema_version","id","lookahead_base_m","lookahead_speed_seconds",
                "lookahead_max_m","heading_response_seconds","yaw_rate_feedback"})
                Assert.Catch(()=>ManualAiSteeringDefinition.FromJson(PolicyJson.Replace("\""+field+"\"","\"missing_"+field+"\"")),field);
            Assert.Catch(()=>ManualAiSteeringDefinition.FromJson(PolicyJson.Replace("\"lookahead_max_m\":8","\"lookahead_max_m\":1")));
            Assert.Catch(()=>ManualAiSteeringDefinition.FromJson(PolicyJson.Replace("\"heading_response_seconds\":0.6","\"heading_response_seconds\":0")));
        }
        [Test] public void OffCentreAiRequestsMirroredPhysicalYawWithoutMovingEitherWorldPose()
        {
            var f=Create();var section=f.route.GetSection(0);var loop=f.visual.Get(section.LoopId);
            int segment=1;while(segment<loop.Count&&loop.Get(segment).Distance-loop.Get(segment-1).Distance<6)segment++;
            Assert.That(segment,Is.LessThan(loop.Count));
            double local=(loop.Get(segment-1).Distance+loop.Get(segment).Distance)*.5;
            var p=loop.Sample(local);double yaw=Math.Atan2(p.ForwardX,p.ForwardZ);
            var left=new ManualSkiHeadingState(p.X-p.ForwardZ,p.Z+p.ForwardX,yaw);
            var right=new ManualSkiHeadingState(p.X+p.ForwardZ,p.Z-p.ForwardX,yaw);
            long station=Nano(section.Start.Meters+local-section.LoopStart.Meters);
            Assert.That(f.map.TryProject(left,0,station,.01,false,out var leftObservation),Is.True);
            Assert.That(f.map.TryProject(right,0,station,.01,false,out var rightObservation),Is.True);
            var leftPlan=f.controller.Plan(left,leftObservation,0,false);
            var rightPlan=f.controller.Plan(right,rightObservation,0,false);
            Assert.That(leftPlan.IsValid&&rightPlan.IsValid,Is.True);
            Assert.That(leftPlan.SteeringInput,Is.GreaterThan(0));
            Assert.That(rightPlan.SteeringInput,Is.LessThan(0));
            Assert.That(leftPlan.SteeringInput,Is.EqualTo(-rightPlan.SteeringInput).Within(1));
            Assert.That(left,Is.EqualTo(new ManualSkiHeadingState(p.X-p.ForwardZ,p.Z+p.ForwardX,yaw)));
            Assert.That(right,Is.EqualTo(new ManualSkiHeadingState(p.X+p.ForwardZ,p.Z-p.ForwardX,yaw)));
            Assert.That(leftPlan.Target.SectionIndex,Is.Zero);
        }
        [Test] public void SpeedLookaheadIsBoundedAndUnadmittedBranchReturnsNoCommand()
        {
            var f=Create("sprint");var first=f.route.GetSection(0);var loop=f.visual.Get(first.LoopId);
            double local=first.LoopStart.Meters+10;var p=loop.Sample(local);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            long station=Nano(first.Start.Meters+10);
            Assert.That(f.map.TryProject(pose,0,station,.01,false,out var observation),Is.True);
            Assert.That(f.controller.Plan(pose,observation,0,false).LookaheadMeters,Is.EqualTo(2));
            Assert.That(f.controller.Plan(pose,observation,100,false).LookaheadMeters,Is.EqualTo(8));
            int edge=0;while(edge+1<f.route.SectionCount&&f.route.GetSection(edge).LoopId==f.route.GetSection(edge+1).LoopId)edge++;
            var section=f.route.GetSection(edge);var before=f.visual.Get(section.LoopId).Sample(section.LoopEnd.Meters-.5);
            var near=new ManualSkiHeadingState(before.X,before.Z,Math.Atan2(before.ForwardX,before.ForwardZ));
            long nearStation=Nano(section.Start.Meters+section.Length.Meters-.5);
            Assert.That(f.map.TryProject(near,edge,nearStation,.01,false,out var nearObservation),Is.True);
            Assert.That(f.controller.Plan(near,nearObservation,0,false).IsValid,Is.False);
            var admitted=f.controller.Plan(near,nearObservation,0,true);
            Assert.That(admitted.IsValid,Is.True);
            Assert.That(admitted.Target.SectionIndex,Is.EqualTo(edge+1));
        }
        [Test] public void PassingLineChangesYawWithoutSlidingOrGrantingAdmission()
        {
            var f=Create();var first=f.route.GetSection(0);
            var p=f.visual.Get(first.LoopId).Sample(first.LoopStart.Meters+10);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            Assert.That(f.map.TryProject(pose,0,Nano(first.Start.Meters+10),.01,false,
                out var observation),Is.True);
            var neutral=f.controller.Plan(pose,observation,0,false);
            var right=f.controller.Plan(pose,observation,0,false,1.5,2);
            var left=f.controller.Plan(pose,observation,0,false,-1.5,2);
            Assert.That(right.IsValid&&left.IsValid,Is.True);
            Assert.That(right.SteeringInput,Is.GreaterThan(neutral.SteeringInput));
            Assert.That(left.SteeringInput,Is.LessThan(neutral.SteeringInput));
            Assert.That(right.TargetLateralMeters,Is.EqualTo(1.5));
            Assert.That(left.TargetLateralMeters,Is.EqualTo(-1.5));
            Assert.That(f.controller.Plan(pose,observation,0,false,5,1).TargetLateralMeters,
                Is.EqualTo(1));
            Assert.That(pose,Is.EqualTo(new ManualSkiHeadingState(p.X,p.Z,
                Math.Atan2(p.ForwardX,p.ForwardZ))));
            Assert.That(f.controller.ContextJson,Does.Contain("manual_ai_bounded_pass_line_yaw_v2"));
            for(int i=0;i<100;i++)f.controller.Plan(pose,observation,0,false,1.5,2);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
                if(f.controller.Plan(pose,observation,0,false,1.5,2).SteeringInput!=right.SteeringInput)
                    Assert.Fail("Passing-line yaw changed across identical observations.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
        }
        [Test] public void IntentIsDeterministicAllocationFreeAndDoesNotReplacePhysicalSteering()
        {
            var f=Create();var first=f.route.GetSection(0);var p=f.visual.Get(first.LoopId).Sample(first.LoopStart.Meters+10);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ),.2);
            Assert.That(f.map.TryProject(pose,0,Nano(first.Start.Meters+10),.01,false,out var observation),Is.True);
            var expected=f.controller.Plan(pose,observation,5,false);
            Assert.That(expected.IsValid,Is.True);
            Assert.That(expected.SteeringInput,Is.InRange(-ManualSkiCommand.SteeringUnits,ManualSkiCommand.SteeringUnits));
            for(int i=0;i<100;i++)f.controller.Plan(pose,observation,5,false);
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)
            {
                var actual=f.controller.Plan(pose,observation,5,false);
                if(actual.SteeringInput!=expected.SteeringInput||actual.Target.RouteNanometers!=expected.Target.RouteNanometers)
                    Assert.Fail("Signed AI steering intent changed across identical fixed-tick observations.");
            }
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);
            var physical=new ManualSkiSteeringSystem(ManualSkiSteeringDefinition.FromJson(SteeringJson),20)
                .Preview(pose,expected.SteeringInput,.25,5,4);
            Assert.That(physical.IsValid,Is.True);
            Assert.That(pose.X,Is.EqualTo(p.X).Within(1e-9));Assert.That(pose.Z,Is.EqualTo(p.Z).Within(1e-9));
        }
    }
}
