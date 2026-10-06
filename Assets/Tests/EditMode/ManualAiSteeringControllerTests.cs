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

        [Test] public void PolicyIsVersionedValidatedAndBoundInReplayContext()
        {
            var policy=ManualAiSteeringDefinition.FromJson(PolicyJson);
            Assert.That(ManualAiSteeringDefinition.FromJson(policy.ToJson()).ToJson(),Is.EqualTo(policy.ToJson()));
            Assert.That(Create().controller.ContextJson,Does.Contain("manual_ai_signed_yaw_intent_v1"));
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
