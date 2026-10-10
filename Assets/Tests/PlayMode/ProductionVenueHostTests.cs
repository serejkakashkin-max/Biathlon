using System;
using System.Collections;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class ProductionVenueHostTests
    {
        [UnityTest] public IEnumerator WoodlandMassStartManualEnvironmentJoinsAndRendersAllQualityTiers()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var root=new GameObject("M13 final dense environment check");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                    RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),30);
                host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment")));
                Assert.That(host.Course.GroundJoined,Is.True);Assert.That(host.Environment.TreeCount,Is.GreaterThan(100));
                Assert.That(host.Proxies.Count,Is.EqualTo(30));
                var finish=root.GetComponent<FinishCorridorPresentation>();
                Assert.That(finish.LaneCount,Is.EqualTo(3));
                Assert.That(finish.FinishPoint.z,Is.GreaterThan(60),"The finish must stay on the stadium's outside branch.");
                var paint=finish.transform.Find("Finish lanes").GetComponent<MeshFilter>().sharedMesh;
                foreach(var normal in paint.normals)Assert.That(normal.y,Is.GreaterThan(.99f),"Finish paint faces the player above the snow.");
                foreach(var tier in new[]{"high","balanced","low"})
                {
                    host.GetComponent<RenderQualityPresentation>().Apply(tier);
                    host.ChaseCamera.transform.SetPositionAndRotation(new Vector3(60,25,28),Quaternion.LookRotation(new Vector3(0,-.3f,-1)));
                    host.Environment.Render(host.ChaseCamera,tier,0);host.Proxies.Render(1);
                    TestContext.WriteLine("m13_broadcast tier="+tier+" visible_trees="+host.Environment.VisibleTrees+" forest_triangles="+host.Environment.SubmittedForestTriangles);
                    yield return null;
                }
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator WoodlandHostUsesSignedSteeringAndDisplaysTheAcceptedWorldBody()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var root=new GameObject("Manual Woodland player acceptance");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                    RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:1);
                Assert.That(host.ManualStepper,Is.Not.Null);Assert.That(host.Runner.UsesManualScopedCandidate,Is.True);
                var id=host.Runner.Field.GetAthlete(0).AthleteId;
                for(int i=0;i<100;i++)
                {
                    var tick=host.Runner.Field.Tick.Next();host.Step(new ManualSkiCommand(tick,id,800,i>=92?250:0),
                        new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
                }
                host.Proxies.Render(1);
                var actual=host.ManualStepper.World.GetActor(0);var proxy=host.Proxies.GetProxy(id.Value);
                Assert.That(proxy.position.x,Is.EqualTo(actual.Pose.X).Within(.02));
                Assert.That(proxy.position.z,Is.EqualTo(actual.Pose.Z).Within(.02));
                Assert.That(Vector3.Angle(proxy.forward,new Vector3((float)Math.Sin(actual.Pose.HeadingRadians),0,(float)Math.Cos(actual.Pose.HeadingRadians))),Is.LessThan(1));
                Assert.That(host.Feed.Current.Get(0).WorldPose.IsValid,Is.True);
                Assert.That(host.LastRangeAssistance.Active,Is.False);
                bool gentleReturn=false;
                for(int i=0;i<20;i++)
                {
                    var tick=host.Runner.Field.Tick.Next();host.Step(new ManualSkiCommand(tick,id,800,0),
                        new ShootingDeviceCommand(new WeaponAimCommand(tick,id,0,0),false,false));
                    int intent=host.Runner.GetLastManualSteering(0);
                    if(i<5)Assert.That(intent,Is.Zero);else gentleReturn|=intent!=0;
                    Assert.That(Math.Abs(intent),Is.LessThanOrEqualTo(220));
                }
                Assert.That(gentleReturn,Is.True,"Production default must request a gradual physical return after release.");yield return null;
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ExplicitPathPlayerHostCompletesAndUsesTheCurrentCorridorForInputExtent()
        {
            // Adapter contract fixture only; no production geometry or 30-mat acceptance claim.
            var venue=PathPresentationFixture.Plan();
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource("Biathlon/sprint_test_v1").Replace("\"distance_m\":3000","\"distance_m\":900"));
            var route=RaceRoutePlan.TimeTrial("production_host_contract_route",venue.Track.Id,"main",profile.ToRouteRequirements());
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1").Replace("snow_test_packed","packed"));
            var root=new GameObject("Production path host contract test");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,profile,route,conditions,count:1);
                Assert.That(host.Course.PresentationRouteId,Is.EqualTo(host.Runner.Field.Route.Id));
                Assert.That(host.Course.PresentationRouteSnapshotJson,Is.EqualTo(host.Runner.Field.Route.PlanSnapshotJson));
                Assert.That(host.Course.PresentationRequirementsSnapshotJson,Is.EqualTo(host.Runner.Field.Route.RequirementsSnapshotJson));
                var body=venue.Occupancy;double wide=3-body.EdgeMargin;
                Assert.That(host.CurrentLateralExtent,Is.EqualTo(wide));Assert.That(host.Mats.Count,Is.EqualTo(2));
                Assert.That(host.Mats.LaneId(1),Is.EqualTo("zulu_mat"));
                Assert.That(host.Proxies.Count,Is.EqualTo(1));Assert.That(host.ChaseCamera,Is.Not.Null);
                var id=host.Runner.Field.GetAthlete(0).AthleteId;bool privateWidth=false;int aiming=0;
                while(!host.Runner.Field.IsComplete&&host.Runner.Field.Tick.Value<50000)
                {
                    var shooting=host.Runner.Field.GetAthlete(0).ActiveShooting;
                    bool trigger=shooting?.Phase==ShootingPhase.Aiming;
                    var tick=host.Runner.Field.Tick.Next();host.Step(new PlayerCommand(tick,id,800,0),
                        new ShootingDeviceCommand(new WeaponAimCommand(tick,id,1000,1000),trigger,false));
                    var actor=host.Runner.Field.GetAthlete(0);var point=host.Runner.Field.GetPhysicalPoint(0);
                    if(point.LoopId=="mat_1")
                    {
                        privateWidth=true;Assert.That(host.CurrentLateralExtent,Is.EqualTo(1-body.EdgeMargin));
                        Assert.That(host.Feed.Current.Get(0).LoopId,Is.EqualTo(point.LoopId));
                    }
                    if(actor.ActiveShooting?.Phase==ShootingPhase.Aiming)
                    {
                        host.Proxies.Render(1);aiming++;
                        Assert.That(Vector3.Angle(host.Proxies.GetProxy(id.Value).forward,new Vector3(.6f,0,.8f)),Is.LessThan(.03f));
                        Assert.That(actor.Motion.SpeedMetersPerSecond,Is.Zero);
                    }
                }
                Assert.That(privateWidth,Is.True);Assert.That(aiming,Is.GreaterThan(0));
                Assert.That(host.Runner.Field.IsComplete,Is.True);Assert.That(host.Feed.Current.Complete,Is.True);
                Assert.That(host.Runner.Field.GetAthlete(0).TotalPenaltyLoops,Is.EqualTo(10));
                Assert.Throws<InvalidOperationException>(()=>host.Initialize(venue,profile,route,conditions,count:1));
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ProductionSprintThirtyOneRejectsBeforeHostStateComponentsOrChildren()
        {
            string venueRoot="Biathlon/Venues/woodland_basin_v1/";
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(venueRoot+"manifest")),ProductionVenueHost.ReadResource);
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(venueRoot+"sprint_profile"));
            var route=RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(venueRoot+"sprint_route"));
            var conditions=RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1"));
            Assert.That(profile.FieldCapacity,Is.EqualTo(30));
            Assert.That(venue.Formats.Get(1).Profile.ToJson(),Is.EqualTo(profile.ToJson()));
            Assert.That(venue.Formats.Get(1).Route.ToJson(),Is.EqualTo(route.ToJson()));
            var root=new GameObject("Production cap atomic host rejection");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                int before=root.GetComponents<Component>().Length;
                Assert.Throws<ArgumentException>(()=>host.Initialize(venue,profile,route,conditions,count:31));
                Assert.That(host.Runner,Is.Null);Assert.That(host.Venue,Is.Null);Assert.That(host.Feed,Is.Null);
                Assert.That(host.Proxies,Is.Null);Assert.That(host.Mats,Is.Null);Assert.That(host.Course,Is.Null);
                Assert.That(host.ChaseCamera,Is.Null);Assert.That(host.Environment,Is.Null);
                Assert.That(root.GetComponents<Component>().Length,Is.EqualTo(before));Assert.That(root.transform.childCount,Is.Zero);
                yield return null;
            }
            finally{Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator InvalidPlayerSlotRejectsBeforeAddingComponentsOrChildren()
        {
            var root=new GameObject("Production host rejection test");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                Assert.Throws<ArgumentOutOfRangeException>(()=>host.Initialize(null,null,null,null,count:1,playerIndex:1));
                Assert.That(host.Runner,Is.Null);Assert.That(root.transform.childCount,Is.Zero);
                Assert.That(root.GetComponents<Component>().Length,Is.EqualTo(2));yield return null;
            }
            finally{Object.Destroy(root);}
        }
    }
}
