using System;
using System.Collections;
using System.IO;
using System.Linq;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class CourseTimingPresentationTests
    {
        private static VenueAuthoringPlan Production(bool extended=false)=>new VenueAuthoringPlan(
            VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource,
            extended?ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/broadcast_checkpoints_v1"):null);

        [UnityTest]
        public IEnumerator WoodlandLiveSplitAndPortalUseTheSameRealSprintCrossing()
        {
            var plan=Production(true);
            var binding=Enumerable.Range(0,plan.Formats.Count).Select(plan.Formats.Get)
                .Single(x=>x.Profile.Format==CompetitionFormat.Sprint);
            var root=new GameObject("Live timing on Woodland Basin");
            const string capturePath="artifacts/m16-8-broadcast/";
            bool capture=File.Exists(capturePath+"capture.flag");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();
                host.AutoStart=false;host.enabled=false;host.InputPreferences=new PlayerSettings();
                const int featured = 4;
                host.Initialize(plan,binding.Profile,binding.Route,
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:5,playerIndex:featured);
                host.Proxies.enabled=false;
                if(capture)
                    host.InitializeEnvironment(ProductionEnvironmentDefinition.FromJson(
                        ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment")));
                var field=host.Runner.Field;
                var layout=new CheckpointLayout(field.Track,field.Route);
                var portals=host.GetComponent<CourseTimingPresentation>();
                int gate=Enumerable.Range(0,portals.TimingLines).Single(i=>portals.CheckpointId(i)=="forest_straight_split");
                var line=layout.Get(0);
                Assert.That(line.CheckpointId,Is.EqualTo(portals.CheckpointId(gate)));
                var hud=host.GameplayHud;
                hud.SetPreferences("ru",0);
                var roster=CareerFactory.Create(
                    CareerRosterDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/career_roster_v2")),
                    CareerCalendar.FromJson(ProductionVenueHost.ReadResource("Biathlon/woodland_career_calendar_v1")),
                    plan.Manifest.Id,"Александр Волков","RUS","appearance_default",new SeededRandomSource(16));
                hud.SetBroadcastRoster(roster);
                for(int i=0;i<8000&&field.GetAthlete(featured).RouteNanometers<line.RouteNanometers-16000000000L;i++)host.Runner.Step();
                Publish();
                var view=LiveSplit.Capture(field,layout,featured,host.Feed.Current.Get(featured),60,6);
                Assert.That(view.Visible&&!view.Crossed&&view.LeaderSeconds.HasValue,Is.True);
                var leader=hud.Root.Q<TimingAthleteRow>("live_split_rival_0");
                Assert.That(leader.AthleteId,Is.EqualTo(field.Timing.Leader(0).Athlete.Value));
                Assert.That(leader.Q<Label>("timing_elapsed").text,Is.EqualTo(GameplayHudModel.FormatClock(field.Timing.Leader(0).ElapsedTime.Seconds)));
                Assert.That(hud.Root.Q<Label>("live_split_player_name").text,Is.EqualTo(roster.Athlete(featured).Name));
                Assert.That(hud.Root.Q<NationFlagGraphic>("live_split_player_flag").Nation,Is.EqualTo(roster.Athlete(featured).Nation));
                AssertRivals();
                Assert.That(hud.Root.Q<Label>("live_split_heading").text,Does.Contain(portals.DistanceLabel(gate).Replace('.',',').Replace("KM","КМ")));
                if(capture)yield return Capture("approach",false);
                for(int i=0;i<2000&&field.GetAthlete(featured).Checkpoints.Count==0;i++)host.Runner.Step();
                Publish();
                var frozen=hud.Root.Q<Label>("live_split_time").text;
                Assert.That(frozen,Is.EqualTo(GameplayHudModel.FormatClock(field.GetAthlete(featured).Checkpoints.Get(0).ElapsedTime.Seconds)));
                Assert.That(hud.Root.Q<Label>("live_split_stage").text,Does.Contain("МЕСТО"));
                AssertRivals();
                if(capture)yield return Capture("crossed",true);
                for(int i=0;i<field.TickFrequency;i++)host.Runner.Step();
                Publish();
                Assert.That(hud.Root.Q<Label>("live_split_time").text,Is.EqualTo(frozen));
                yield return null;

                void AssertRivals()
                {
                    var rows = Enumerable.Range(0,3).Select(i=>hud.Root.Q<TimingAthleteRow>("live_split_rival_"+i)).ToArray();
                    Assert.That(rows.Select(row=>row.AthleteId).Distinct().Count(),Is.EqualTo(3));
                    foreach(var row in rows)
                    {
                        Assert.That(row.style.display.value,Is.EqualTo(DisplayStyle.Flex));
                        Assert.That(row.AthleteId,Is.Not.EqualTo(roster.Athlete(featured).Id));
                        var rival=Enumerable.Range(0,roster.AthleteCount).Select(roster.Athlete).Single(a=>a.Id==row.AthleteId);
                        Assert.That(row.Q<Label>("timing_athlete").text,Is.EqualTo(rival.Name));
                        Assert.That(row.Q<NationFlagGraphic>().Nation,Is.EqualTo(rival.Nation));
                    }
                }

                void Publish()
                {
                    host.Feed.Publish(RacePresentationSnapshot.Capture(field));
                    host.Proxies.Render(1);hud.Refresh();
                }
                IEnumerator Capture(string name,bool crossed)
                {
                    var camera=host.ChaseCamera;
                    var follow=camera.GetComponent<AthleteChaseCamera>();
                    if(follow!=null)follow.enabled=false;
                    var frame=portals.GateTransform(gate);
                    camera.transform.position=frame.TransformPoint(new Vector3(crossed?3.2f:0,2.2f,crossed?8:-23));
                    camera.transform.LookAt(frame.TransformPoint(new Vector3(0,2,0)));
                    camera.enabled=true;
                    for(int i=0;i<3;i++)
                    {
                        host.Environment.Render(camera,"high",(float)(field.Tick.Value/(double)field.TickFrequency));
                        yield return new WaitForEndOfFrame();
                    }
                    var image=ScreenCapture.CaptureScreenshotAsTexture();
                    File.WriteAllBytes(capturePath+name+".png",image.EncodeToPNG());
                    Object.Destroy(image);
                }
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void LegacyMeshPlacementsKeepUniformWidthAndExplicitFinishWidthSurvivesRoundTrip()
        {
            var fixture=new ProductionEnvironmentFixture();
            Assert.That(fixture.Recipe.mesh_assets[0].EffectiveLateralScale,Is.EqualTo(1));
            Assert.That(ProductionEnvironmentDefinition.FromJson(fixture.Recipe.ToJson()).mesh_assets[0].EffectiveLateralScale,Is.EqualTo(1));
            fixture.Recipe.mesh_assets[0].lateral_scale=1.4f;
            Assert.That(ProductionEnvironmentDefinition.FromJson(fixture.Recipe.ToJson()).mesh_assets[0].EffectiveLateralScale,Is.EqualTo(1.4f));
            fixture.Recipe.mesh_assets[0].lateral_scale=-1;
            Assert.Throws<ArgumentException>(()=>ProductionEnvironmentDefinition.FromJson(fixture.Recipe.ToJson()));
        }

        [TestCase(false), TestCase(true)]
        public void TimingStationsPaintExactAuthorityAnchorsAndLeaveTheSkiCorridorClear(bool extended)
        {
            var plan=Production(extended);var root=new GameObject("Timing portal placement check");
            root.transform.SetPositionAndRotation(new Vector3(30,17,-22),Quaternion.Euler(0,18,0));
            try
            {
                var binding=Enumerable.Range(0,plan.Formats.Count).Select(plan.Formats.Get).Single(x=>x.Id=="mass_start");
                var route=new RaceRoute(plan.Track,binding.Route,binding.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
                var view=root.AddComponent<CourseTimingPresentation>();view.Initialize(plan,route);
                Assert.That(view.TimingLines,Is.EqualTo(extended?5:3));
                Assert.That(Enumerable.Range(0,view.TimingLines).Count(i=>view.UsesExistingStadiumPortal(i)),Is.EqualTo(1));
                var expected=new[]{"climb_top","range_entry_split","range_exit_split"};
                if(extended)expected=expected.Concat(new[]{"forest_straight_split","ridge_straight_split"}).ToArray();
                CollectionAssert.AreEqual(expected,Enumerable.Range(0,view.TimingLines).Select(view.CheckpointId).ToArray());
                Assert.That(plan.Track.GetCheckpoint(2).Anchor.Distance.Meters,Is.GreaterThan(plan.Track.Penalty.Rejoin.Distance.Meters),"IBU §9.2.7 timing follows the penalty loop.");
                Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty,"Timing art never adds a physical stopper, even in its creation frame.");
                for(int index=0;index<view.TimingLines;index++)
                {
                    var marker=plan.Track.GetCheckpoint(index);var sample=plan.Visual.Get(marker.Anchor.LoopId).Sample(marker.Anchor.Distance.Meters);
                    Assert.That(Vector3.Distance(view.GateTransform(index).position,new Vector3((float)sample.X,(float)sample.Y,(float)sample.Z)),Is.LessThan(.001f));
                    var mesh=view.GateMesh(index);var points=mesh.vertices;float half=view.CorridorHalfWidth(index);
                    foreach(int submesh in new[]{0,1})foreach(int vertex in mesh.GetTriangles(submesh))
                        if(points[vertex].y<3)Assert.That(Mathf.Abs(points[vertex].x),Is.GreaterThan(half+1),"Every low native support face stays beyond the actual corridor.");
                    var paint=mesh.GetTriangles(2).Distinct().Select(v=>points[v]).ToArray();
                    Assert.That(paint.Length,Is.EqualTo(4));Assert.That(paint.Min(p=>p.x),Is.EqualTo(-half));Assert.That(paint.Max(p=>p.x),Is.EqualTo(half));
                    Assert.That(paint.Average(p=>p.z),Is.EqualTo(0).Within(1e-6));
                    foreach(var point in paint)Assert.That(point.y,Is.InRange(.02f,.04f),"The split is a paint line at the snow surface, not a raised timing mat.");
                    Assert.That(mesh.vertexCount,Is.LessThan(200));
                }
                Assert.That(typeof(CourseTimingPresentation).GetMethod("Update",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic),Is.Null);
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void StadiumSplitReusesActualNativeGantryWithOneDistanceSign()
        {
            var plan=Production();var root=new GameObject("Retained stadium timing gate check");
            try
            {
                var view=root.AddComponent<CourseTimingPresentation>();view.Initialize(plan);
                int station=Enumerable.Range(0,view.TimingLines).Single(view.UsesExistingStadiumPortal);
                Assert.That(view.CheckpointId(station),Is.EqualTo("range_exit_split"));
                var mesh=view.GateMesh(station);
                Assert.That(mesh.vertexCount,Is.EqualTo(4),"Only snow paint is added beneath the already existing portal.");
                Assert.That(mesh.GetTriangles(0),Is.Empty);Assert.That(mesh.GetTriangles(1),Is.Empty);
                Assert.That(view.GateTransform(station).GetComponentsInChildren<TextMesh>().Length,Is.EqualTo(1),"Reuse the frame with one cumulative kilometre label.");
                var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
                var native=recipe.mesh_assets.Where(part=>part.id.StartsWith("timing_gantry_",StringComparison.Ordinal)).ToArray();
                Assert.That(native.Length,Is.EqualTo(6));
                foreach(var part in native)
                    Assert.That(Vector3.Distance(part.position,view.GateTransform(station).position),Is.LessThan(.001f),
                        "The authority stripe coincides with every original gantry mesh placement.");
                var paint=mesh.GetTriangles(2).Distinct().Select(v=>view.GateTransform(station).TransformPoint(mesh.vertices[v])).ToArray();
                var center=(paint[0]+paint[1]+paint[2]+paint[3])*.25f;
                Assert.That(Vector2.Distance(new Vector2(center.x,center.z),new Vector2(native[0].position.x,native[0].position.z)),Is.LessThan(.001f));
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void TimingFeetFollowActualSlopedSnowWithoutMovingTheAuthorityStripe()
        {
            var fixture=new ProductionEnvironmentFixture(p=>-.2f+.015f*p.x);var root=new GameObject("Grounded timing portals");
            try
            {
                var cameraObject=new GameObject("Detached timing camera");cameraObject.transform.SetParent(root.transform,false);
                var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var feed=root.AddComponent<RacePresentationFeed>();var environment=root.AddComponent<ProductionVenueEnvironment>();
                environment.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);
                var view=root.AddComponent<CourseTimingPresentation>();view.Initialize(fixture.Plan);
                var stripeBefore=Enumerable.Range(0,view.TimingLines).Select(i=>view.GateMesh(i).GetTriangles(2).Select(v=>view.GateMesh(i).vertices[v]).ToArray()).ToArray();
                view.GroundSupports(environment);
                for(int index=0;index<view.TimingLines;index++)
                {
                    var mesh=view.GateMesh(index);var points=mesh.vertices;var frame=view.GateTransform(index);
                    CollectionAssert.AreEqual(stripeBefore[index],mesh.GetTriangles(2).Select(v=>points[v]).ToArray());
                    foreach(int side in new[]{-1,1})
                    {
                        var foot=mesh.GetTriangles(1).Distinct().Select(v=>points[v]).Where(p=>p.x*side>view.CorridorHalfWidth(index)&&p.y<3).ToArray();
                        Assert.That(foot,Is.Not.Empty);float x=(foot.Min(p=>p.x)+foot.Max(p=>p.x))*.5f;
                        float actual=frame.TransformPoint(new Vector3(x,foot.Min(p=>p.y),0)).y;
                        float snow=environment.SampleGround(frame.TransformPoint(new Vector3(x,0,0)));
                        Assert.That(actual,Is.EqualTo(snow-.08f).Within(.002f),"Each footing is slightly embedded in actual native terrain rather than floating over it.");
                    }
                }
            }
            finally{Object.DestroyImmediate(root);}
        }

        [Test] public void WidenedFinishGantryKeepsEveryActualLowSupportBeyondTheFinishShoulders()
        {
            var plan=Production();var sample=plan.Visual.Get("finish_spur").Sample(80);
            var center=new Vector3((float)sample.X,(float)sample.Y,(float)sample.Z);var right=new Vector3((float)sample.ForwardZ,0,(float)-sample.ForwardX);
            var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
            var finish=recipe.mesh_assets.Where(p=>p.id.StartsWith("finish_gantry_",StringComparison.Ordinal)).ToArray();
            Assert.That(finish.Length,Is.EqualTo(6));int supports=0;
            foreach(var part in finish)
            {
                var mesh=Resources.Load<Mesh>(part.mesh_resource);Assert.That(mesh,Is.Not.Null);
                var placement=Matrix4x4.TRS(part.position,Quaternion.Euler(0,part.yaw_deg,0),new Vector3(part.scale*part.EffectiveLateralScale,part.scale,part.scale));
                foreach(var point in mesh.vertices)
                {
                    if(point.y>3||part.id.EndsWith("_accent",StringComparison.Ordinal)&&point.y<=.032f)continue;
                    float side=Mathf.Abs(Vector3.Dot(placement.MultiplyPoint3x4(point)-center,right));
                    Assert.That(side,Is.GreaterThan(6),part.id+": nine-meter finish lane plus at least1.5m shoulder clearance.");supports++;
                }
            }
            Assert.That(supports,Is.GreaterThan(20));
        }
    }
}
