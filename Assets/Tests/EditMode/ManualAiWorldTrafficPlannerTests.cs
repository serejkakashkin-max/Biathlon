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
    }
}
