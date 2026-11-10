using System;
using System.Collections;
using System.Collections.Generic;
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
        public IEnumerator WoodlandMiniWindowPagesThroughAllArrivalsAndFreezesThreeAtThePlayersCrossing()
        {
            var plan=Production(true);
            var binding=Enumerable.Range(0,plan.Formats.Count).Select(plan.Formats.Get)
                .Single(x=>x.Profile.Format==CompetitionFormat.Sprint);
            var root=new GameObject("Live timing on Woodland Basin");
            const string capturePath="artifacts/m16-8-split-scroll/";
            bool capture=File.Exists(capturePath+"capture.flag");
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();
                host.AutoStart=false;host.enabled=false;host.InputPreferences=new PlayerSettings();
                const int featured = 7;
                host.Initialize(plan,binding.Profile,binding.Route,
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),count:8,playerIndex:featured);
                host.Proxies.enabled=false;
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
                for(int i=0;i<8000&&field.GetAthlete(featured).RouteNanometers<line.RouteNanometers-59000000000L;i++)host.Runner.Step();
                Publish();
                var view=LiveSplit.Capture(field,layout,featured,host.Feed.Current.Get(featured),60,6);
                Assert.That(view.Visible&&!view.Crossed&&view.LeaderSeconds.HasValue,Is.True);
                var strip=hud.Root.Q("checkpoint_notice");
                var cards=strip.Query<TimingAthleteRow>().ToList();
                Assert.That(cards,Has.Count.EqualTo(3));
                var standings=CheckpointStandings.Capture(field,0);
                Assert.That(standings.Count,Is.EqualTo(7));
                var shownStarts=new List<int>();
                var shownRanks=new HashSet<int>();
                ObservePage();
                Assert.That(hud.Root.Q<Label>("live_split_heading").text,Does.Contain(portals.DistanceLabel(gate).Replace('.',',').Replace("KM","КМ")));
                yield return null;
                Assert.That(strip.worldBound.width,Is.GreaterThan(strip.worldBound.height*8));
                Assert.That(strip.worldBound.center.y,Is.GreaterThan(hud.Root.worldBound.height*.75f));
                foreach(var row in cards)
                    Assert.That(row.worldBound.width,Is.GreaterThan(100));

                bool capturedLaterPage=false;
                for(int i=0;i<2000&&field.GetAthlete(featured).Checkpoints.Count==0;i++)
                {
                    host.Runner.Step();
                    if(i%5!=0||field.GetAthlete(featured).Checkpoints.Count!=0)continue;
                    Publish();
                    ObservePage();
                    if(capture&&!capturedLaterPage&&shownStarts.Last()==4)
                    {
                        capturedLaterPage=true;
                        yield return Capture("approach");
                    }
                }

                Assert.That(shownStarts.Take(3),Is.EqualTo(new[]{1,4,5}),"Show full pages in order and fill the short final page.");
                Assert.That(shownRanks.OrderBy(rank=>rank),Is.EqualTo(Enumerable.Range(1,7)));
                var own=field.GetAthlete(featured).Checkpoints.Get(0);
                var atCrossing=CheckpointStandings.Capture(field,0,own.GlobalTime);
                int ownIndex=Enumerable.Range(0,atCrossing.Count).Single(i=>atCrossing.Get(i).AthleteId==own.Athlete.Value);
                var ownResult=atCrossing.Get(ownIndex);
                int resultStart=Math.Clamp(ownIndex-1,0,atCrossing.Count-3);
                Publish();
                AssertResult();
                yield return null;
                if(capture)yield return Capture("crossed");
                for(int i=0;i<3*field.TickFrequency;i++)host.Runner.Step();
                Publish();
                AssertResult();
                yield return null;

                void AssertResult()
                {
                    Assert.That(strip.style.display.value,Is.EqualTo(DisplayStyle.Flex));
                    for(int i=0;i<cards.Count;i++)
                    {
                        var result=atCrossing.Get(resultStart+i);
                        AssertCard(cards[i],result,result.AthleteId==own.Athlete.Value);
                    }
                    Assert.That(hud.Root.Q<Label>("live_split_stage").text,Is.EqualTo($"МЕСТО {ownResult.Rank} / {atCrossing.Count}"));
                    Assert.That(hud.Root.Q("live_split_clock").style.display.value,Is.EqualTo(DisplayStyle.None));
                }

                void ObservePage()
                {
                    int start=Enumerable.Range(0,standings.Count).Single(i=>standings.Get(i).AthleteId==cards[0].AthleteId);
                    for(int i=0;i<cards.Count;i++)
                    {
                        var result=standings.Get(start+i);
                        AssertCard(cards[i],result,false);
                        shownRanks.Add(result.Rank);
                    }
                    int rank=standings.Get(start).Rank;
                    if(shownStarts.Count==0||shownStarts.Last()!=rank)shownStarts.Add(rank);
                }

                void AssertCard(TimingAthleteRow card,CheckpointStanding result,bool player)
                {
                    Assert.That(card.style.display.value,Is.EqualTo(DisplayStyle.Flex));
                    Assert.That(card.AthleteId,Is.EqualTo(result.AthleteId));
                    var racer=Enumerable.Range(0,roster.AthleteCount).Select(roster.Athlete).Single(a=>a.Id==result.AthleteId);
                    Assert.That(card.Q<Label>("timing_athlete").text,Is.EqualTo(player?"ВЫ · "+racer.Name:racer.Name));
                    Assert.That(card.Q<NationFlagGraphic>().Nation,Is.EqualTo(racer.Nation));
                    Assert.That(card.Q<Label>("timing_elapsed").text,Is.EqualTo(GameplayHudModel.FormatClock(result.Seconds)));
                    Assert.That(card.Q<Label>("timing_rank").text,Is.EqualTo(result.Rank.ToString("00")));
                }

                void Publish()
                {
                    host.Feed.Publish(RacePresentationSnapshot.Capture(field));
                    host.Proxies.Render(1);hud.Refresh();
                }
                IEnumerator Capture(string name)
                {
                    var panel=Object.FindObjectsByType<UIDocument>(FindObjectsSortMode.None)
                        .Single(document=>document.rootVisualElement==hud.Root).panelSettings;
                    var prior=RenderTexture.active;
                    var target=new RenderTexture(1600,900,24);
                    Texture2D image=null;
                    try
                    {
                        target.Create();
                        LiveShootingPlayerReview.ConfigureCapturePanel(panel,target);
                        yield return null;yield return null;yield return null;
                        RenderTexture.active=target;
                        image=new Texture2D(target.width,target.height,TextureFormat.RGB24,false);
                        image.ReadPixels(new Rect(0,0,target.width,target.height),0,0);
                        image.Apply();
                        File.WriteAllBytes(capturePath+name+".png",image.EncodeToPNG());
                    }
                    finally
                    {
                        panel.targetTexture=null;
                        panel.clearColor=false;
                        RenderTexture.active=prior;
                        if(image!=null)Object.Destroy(image);
                        target.Release();
                        Object.Destroy(target);
                    }
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
