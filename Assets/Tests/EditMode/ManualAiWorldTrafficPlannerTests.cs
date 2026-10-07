using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualAiWorldTrafficPlannerTests
    {
        private static long Nano(double metres)=>(long)Math.Round(metres*SkiState.NanometersPerMeter);
        private static (VenueAuthoringPlan venue,RaceRoute route,ManualRouteProjectionMap map,
            ManualAiWorldTrafficPlanner planner) Create()
        {
            const string root="Biathlon/Venues/woodland_basin_v1/";
            string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(root+"manifest")),Read);
            var profile=CompetitionProfile.FromJson(Read(root+"mass_start_profile"));
            var route=new RaceRoute(venue.Track,RaceRoutePlan.FromJson(Read(root+"mass_start_route")),
                profile.ToRouteRequirements(),venue.Junctions,venue.Connections);
            var map=new ManualRouteProjectionMap(route,venue.Visual);
            var policy=ManualAiTrafficDefinition.FromJson(Read("Biathlon/manual_ai_world_traffic_v1"));
            return (venue,route,map,new ManualAiWorldTrafficPlanner(policy,venue.Occupancy));
        }
        private static ManualRouteProjection At(VenueAuthoringPlan venue,RaceRoute route,
            ManualRouteProjectionMap map,double station,double lateral)
        {
            var first=route.GetSection(0);
            var point=venue.Visual.Get(first.LoopId).Sample(first.LoopStart.Meters+station);
            var pose=new ManualSkiHeadingState(point.X+point.ForwardZ*lateral,
                point.Z-point.ForwardX*lateral,Math.Atan2(point.ForwardX,point.ForwardZ));
            Assert.That(map.TryProject(pose,0,Nano(first.Start.Meters+station),2,false,
                out var observed),Is.True);
            return observed;
        }
        [Test] public void StationaryObstructionSelectsOpenSideAsYawIntentOnly()
        {
            var f=Create();var self=At(f.venue,f.route,f.map,10,0);
            var blocked=At(f.venue,f.route,f.map,30,0);
            var selfId=new AthleteId("player");var blockerId=new AthleteId("stationary");
            var neighbors=new[]{new ManualAiTrafficNeighbor(blockerId,blocked)};
            double width=f.route.Sample(TrackDistance.FromMeters(10)).Track.WidthMeters;
            var plan=f.planner.Plan(selfId,self,neighbors,width);
            Assert.That(plan.IsValid&&plan.ObstructionObserved&&plan.HasPassingLine,Is.True);
            Assert.That(plan.Obstructor,Is.EqualTo(blockerId));
            Assert.That(plan.ForwardGapMeters,Is.EqualTo(20).Within(.01));
            Assert.That(plan.DesiredLateralMeters,Is.GreaterThan(f.venue.Occupancy.LateralSeparation));
            var p=f.venue.Visual.Get(f.route.GetSection(0).LoopId).Sample(10);
            var pose=new ManualSkiHeadingState(p.X,p.Z,Math.Atan2(p.ForwardX,p.ForwardZ));
            var yaw=new ManualAiSteeringController(f.map,
                ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                    "Assets/Resources/Biathlon/manual_ai_steering_v2.json")),
                ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                    "Assets/Resources/Biathlon/manual_ski_steering_v2.json")));
            var steering=yaw.Plan(pose,self,7,false,plan.DesiredLateralMeters,
                plan.UsableHalfWidthMeters);
            Assert.That(steering.IsValid,Is.True);
            Assert.That(steering.SteeringInput,Is.GreaterThan(0));
            Assert.That(pose.X,Is.EqualTo(p.X).Within(1e-9));
            Assert.That(pose.Z,Is.EqualTo(p.Z).Within(1e-9));
            Assert.That(f.planner.ContextJson,Does.Contain("candidate_line_only"));
        }
        [Test] public void OccupiedSideRedirectsAndTwoOccupiedSidesDoNotGrantPass()
        {
            var f=Create();var self=At(f.venue,f.route,f.map,10,0);
            var blocked=At(f.venue,f.route,f.map,30,0);
            double line=f.venue.Occupancy.LateralSeparation+.35;
            var right=At(f.venue,f.route,f.map,32,line);
            var left=At(f.venue,f.route,f.map,32,-line);
            double width=f.route.Sample(TrackDistance.FromMeters(10)).Track.WidthMeters;
            var own=new AthleteId("own");
            var group=new[]{new ManualAiTrafficNeighbor(new AthleteId("blocker"),blocked),
                new ManualAiTrafficNeighbor(new AthleteId("right"),right)};
            var opposite=f.planner.Plan(own,self,group,width);
            Assert.That(opposite.HasPassingLine,Is.True);
            Assert.That(opposite.DesiredLateralMeters,Is.LessThan(0));
            var all=new[]{group[0],group[1],
                new ManualAiTrafficNeighbor(new AthleteId("left"),left)};
            var noGap=f.planner.Plan(own,self,all,width);
            Assert.That(noGap.ObstructionObserved,Is.True);
            Assert.That(noGap.HasPassingLine,Is.False);
            Assert.That(noGap.DesiredLateralMeters,Is.EqualTo(self.LateralMeters));
        }
        [Test] public void UnrelatedOrBehindActorsDoNotProduceBypassAndChoiceAllocatesNothing()
        {
            var f=Create();var self=At(f.venue,f.route,f.map,30,0);
            var behind=At(f.venue,f.route,f.map,10,0);
            double width=f.route.Sample(TrackDistance.FromMeters(30)).Track.WidthMeters;
            var own=new AthleteId("own");var other=new[]{new ManualAiTrafficNeighbor(
                new AthleteId("behind"),behind)};
            var neutral=f.planner.Plan(own,self,other,width);
            Assert.That(neutral.ObstructionObserved,Is.False);
            Assert.That(neutral.DesiredLateralMeters,Is.EqualTo(self.LateralMeters));
            for(int i=0;i<100;i++)f.planner.Plan(own,self,other,width);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++)if(f.planner.Plan(own,self,other,width).ObstructionObserved)
                Assert.Fail("A trailing athlete became an obstruction.");
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void VersionedPolicyRejectsMissingFieldsAndNarrowCourseCannotGrantPass()
        {
            var json=PhysiologyContractTests.ProjectFile(
                "Assets/Resources/Biathlon/manual_ai_world_traffic_v1.json");
            var policy=ManualAiTrafficDefinition.FromJson(json);
            Assert.That(ManualAiTrafficDefinition.FromJson(policy.ToJson()).ToJson(),
                Is.EqualTo(policy.ToJson()));
            foreach(var field in new[]{"schema_version","id","obstruction_lookahead_m",
                "pass_line_clearance_m","prefer_right_on_tie"})
                Assert.Catch(()=>ManualAiTrafficDefinition.FromJson(json.Replace(
                    "\""+field+"\"","\"missing_"+field+"\"")),field);
            var f=Create();var self=At(f.venue,f.route,f.map,10,0);
            var blocked=At(f.venue,f.route,f.map,30,0);
            var plan=f.planner.Plan(new AthleteId("self"),self,new[]{
                new ManualAiTrafficNeighbor(new AthleteId("blocker"),blocked)},
                f.venue.Occupancy.BodyWidth+2*f.venue.Occupancy.EdgeClearance);
            Assert.That(plan.HasPassingLine,Is.False);
        }

        [Test] public void ChosenYawLinePassesStationaryWorldBodyThroughContinuousJointSweep()
        {
            var f=Create();var first=f.route.GetSection(0);
            var start=f.venue.Visual.Get(first.LoopId).Sample(10);
            var blockPoint=f.venue.Visual.Get(first.LoopId).Sample(30);
            var moverId=new AthleteId("moving");var blockId=new AthleteId("stopped");
            var pose=new ManualSkiHeadingState(start.X,start.Z,
                Math.Atan2(start.ForwardX,start.ForwardZ));
            var stopped=new ManualSkiHeadingState(blockPoint.X,blockPoint.Z,
                Math.Atan2(blockPoint.ForwardX,blockPoint.ForwardZ));
            var heading=ManualSkiSteeringDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                "Assets/Resources/Biathlon/manual_ski_steering_v2.json"));
            var ai=new ManualAiSteeringController(f.map,
                ManualAiSteeringDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                    "Assets/Resources/Biathlon/manual_ai_steering_v2.json")),heading);
            var ski=new ManualSkiMotionSystem(SkiPhysicsDefinition.FromJson(
                PhysiologyContractTests.ProjectFile(
                    "Assets/Resources/Biathlon/ski_physics_provisional_v1.json")),
                heading,CorneringDefinition.FromJson(PhysiologyContractTests.ProjectFile(
                    "Assets/Resources/Biathlon/cornering_provisional_v1.json")),20);
            var contact=SkiContactDefinition.FromJson("{\"schema_version\":1,\"id\":\"manual_pass_pair_v1\",\"restitution\":0,\"sliding_friction\":0.2,\"contact_tolerance_m\":0.000001}");
            var work=SkiContactSweepDefinition.FromJson("{\"schema_version\":1,\"id\":\"manual_pass_work_v1\",\"maximum_depth\":32,\"maximum_interval_checks\":4096,\"time_tolerance_s\":0.0000001}");
            var joint=new ManualRouteJointSweep(f.map,f.venue.Occupancy,contact,work);
            var stillBody=new ManualSkiContactBody(blockId,stopped,new SkiPlanarVelocity(0,0),75);
            var stillMotion=new ManualSkiSnowContactSystem(f.venue.Occupancy,1)
                .PreviewTrajectory(stillBody,new SkiContactMotion(new SkiPlanarVelocity(0,0),0),0,0,.05);
            var stationary=new SkiWorldMotionCandidate(blockId,
                new ManualSkiWorldTrajectory(stillMotion));
            long station=Nano(10),blockStation=Nano(30);int count=0;
            double crossingLateral=double.NaN;
            var skiState=new SkiState(0,4);
            var sampler=new TrackSampler(f.venue.Track.GetLoop(first.LoopId));
            double width=f.route.Sample(TrackDistance.FromMeters(10)).Track.WidthMeters;
            Assert.That(f.map.TryProject(stopped,0,blockStation,.01,false,
                out var blocker),Is.True);
            var neighbors=new[]{new ManualAiTrafficNeighbor(blockId,blocker)};
            Assert.That(f.map.TryProject(pose,0,station,.01,false,out var initial),Is.True);
            var bypass=f.planner.Plan(moverId,initial,neighbors,width);
            Assert.That(bypass.HasPassingLine,Is.True);
            int heldTicks=0;
            for(int tick=0;tick<220&&station<Nano(42);tick++)
            {
                Assert.That(f.map.TryProject(pose,0,station,2,false,out var observed),
                    Is.True,"Physical passing line lost its local course observation.");
                if(double.IsNaN(crossingLateral)&&observed.RouteNanometers>=blockStation)
                    crossingLateral=observed.LateralMeters;
                var surface=sampler.Sample(new TrackDistance((long)Math.Floor(observed.LocalMeters*1000)));
                bypass=f.planner.Plan(moverId,observed,neighbors,surface.WidthMeters,bypass);
                if(bypass.HasPassingLine)heldTicks++;
                if(observed.RouteNanometers<=blockStation)
                    Assert.That(bypass.HasPassingLine,Is.True,
                        "The AI must hold its selected line until the stationary body is passed.");
                var intent=ai.Plan(pose,observed,skiState.SpeedMetersPerSecond,false,bypass.DesiredLateralMeters,
                    bypass.UsableHalfWidthMeters);
                Assert.That(intent.IsValid,Is.True);
                var motion=ski.Preview(skiState,pose,observed,surface,
                    new ManualSkiMotionInput(intent.SteeringInput,200,70,70));
                var actors=new[]{new SkiWorldMotionCandidate(moverId,
                    new ManualSkiWorldTrajectory(motion.Heading)),stationary};
                var windows=new[]{new ManualRouteActorWindow(moverId,0,observed.RouteNanometers,2,false),
                    new ManualRouteActorWindow(blockId,0,blockStation,0,false)};
                var admitted=joint.Preview(actors,2,Array.Empty<SkiReachedContactPair>(),0,
                    windows,Array.Empty<SkiActorCourseEdge>(),0);
                Assert.That(admitted.Status,Is.EqualTo(ManualRouteJointStatus.Clear),
                    "Full-body/world-course passage was not proven at tick "+tick+
                    " station="+observed.RouteNanometers/1e9+" clear_s="+
                    admitted.ClearUntilSeconds);
                pose=motion.Heading.State;skiState=motion.Travel.Output.State;
                Assert.That(f.map.TryProject(pose,0,station,2,false,out var next),Is.True);
                station=next.RouteNanometers;count++;
            }
            Assert.That(station,Is.GreaterThanOrEqualTo(Nano(42)));
            Assert.That(count,Is.GreaterThan(25));
            Assert.That(heldTicks,Is.GreaterThan(25));
            Assert.That(bypass.HasPassingLine,Is.False,
                "A fully passed body must release the temporary passing line.");
            Assert.That(crossingLateral,Is.GreaterThan(f.venue.Occupancy.LateralSeparation));
            TestContext.WriteLine("world_bypass_ticks="+count+" station_m="+
                station/1e9+" crossing_lateral_m="+crossingLateral+
                " held_ticks="+heldTicks);
        }
    }
}
