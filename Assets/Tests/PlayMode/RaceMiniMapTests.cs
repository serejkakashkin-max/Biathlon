using System;
using System.IO;
using System.Collections;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class RaceMiniMapTests
    {
        private const string Prefix="Biathlon/Venues/woodland_basin_v1/";
        private GameObject root;
        private static string Read(string resource)=>ProductionVenueHost.ReadResource(resource);
        private static VenueAuthoringPlan Venue()=>new VenueAuthoringPlan(VenueManifest.FromJson(Read(Prefix+"manifest")),Read);
        private static RaceRoute Route(VenueAuthoringPlan venue,int format)
        {var b=venue.Formats.Get(format);return new RaceRoute(venue.Track,b.Route,b.Profile.ToRouteRequirements(),venue.Junctions,venue.Connections);}
        [TearDown] public void Cleanup(){if(root!=null)Object.DestroyImmediate(root);if(BiathlonAudio.Instance!=null)Object.DestroyImmediate(BiathlonAudio.Instance.gameObject);}
        [TestCase(0)][TestCase(1)][TestCase(2)] public void FormatSpecificRouteFitsWithoutStretchingAndIncludesActualLandmarks(int format)
        {
            var venue=Venue();var route=Route(venue,format);string track=venue.Track.ToJson(),visual=venue.Visual.ToJson();
            var settings=RaceMiniMapSettings.Load();var map=new RaceMiniMapGeometry(venue,route,settings);var rect=new Rect(0,0,176,155);
            Assert.That(map.Lines.Length,Is.GreaterThan(3));bool penalty=false;
            foreach(var line in map.Lines)
            {
                penalty|=line.Penalty;Assert.That(line.Points.Length,Is.GreaterThan(1));
                foreach(var p in line.Points){var ui=map.Project(p,rect,12);Assert.That(ui.x,Is.InRange(11.999f,164.001f));Assert.That(ui.y,Is.InRange(11.999f,143.001f));}
                if(line.LoopId=="sprint_extension")Assert.That(venue.Formats.Get(format).Profile.Format,Is.EqualTo(CompetitionFormat.Sprint));
                if(line.LoopId=="pursuit_shortcut")Assert.That(venue.Formats.Get(format).Profile.Format,Is.EqualTo(CompetitionFormat.Pursuit));
            }
            Assert.That(penalty,Is.True);
            var zero=map.Project(Vector2.zero,rect,12);var dx=map.Project(Vector2.right,rect,12)-zero;var dy=map.Project(Vector2.up,rect,12)-zero;
            Assert.That(dx.magnitude,Is.EqualTo(dy.magnitude).Within(.0001));Assert.That(dx.x,Is.GreaterThan(0));Assert.That(dy.y,Is.GreaterThan(0));
            var last=route.GetSection(route.SectionCount-1);var finish=venue.Visual.Get(last.LoopId).Sample(last.LoopEnd.Meters);
            Assert.That(map.Finish,Is.EqualTo(new Vector2((float)finish.X,(float)-finish.Z)));Assert.That(venue.Track.ToJson(),Is.EqualTo(track));Assert.That(venue.Visual.ToJson(),Is.EqualTo(visual));
        }
        [TestCase(0)][TestCase(1)][TestCase(2)] public void RejectsUnboundedUiDefinitions(int kind)
        {
            var s=RaceMiniMapSettings.Load();if(kind==0)s.max_athletes=10000;if(kind==1)s.sample_spacing_m=float.NaN;if(kind==2)s.width=1000;
            Assert.Throws<ArgumentException>(()=>RaceMiniMapSettings.FromJson(JsonUtility.ToJson(s)));
        }
        private ProductionVenueHost Host(bool manual=false,int count=30)
        {
            var venue=Venue();var binding=venue.Formats.Get(0);root=new GameObject("Actual route minimap acceptance");
            var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;host.UseManualHeading=manual;
            var conditions=RaceSnowConditions.FromJson(Read(host.ConditionsResource));
            host.Initialize(venue,binding.Profile,binding.Route,conditions,count:count);return host;
        }
        [UnityTest] public IEnumerator AcceptedWorldAndLegacyMarkersMoveAndWarmBufferDoesNotAllocate()
        {
            var host=Host();var map=host.GameplayHud.MiniMapGeometry;var state=new RaceMiniMapState(128);
            var first=RacePresentationSnapshot.Capture(host.Runner.Field);state.Observe(first,map,0);var before=state.Get(0).Point;
            for(int i=0;i<300;i++)host.Runner.Step();var second=RacePresentationSnapshot.Capture(host.Runner.Field);
            state.Observe(second,map,0);Assert.That(Vector2.Distance(before,state.Get(0).Point),Is.GreaterThan(2));Assert.That(state.Count,Is.EqualTo(30));Assert.That(state.Rivals,Is.EqualTo(29));
            for(int i=0;i<second.Count;i++)
            {
                var a=second.Get(i);var sampled=host.Venue.Visual.Get(a.LoopId).Sample(a.LocalNanometers/(double)SkiState.NanometersPerMeter);
                var expected=new Vector2((float)(sampled.X+sampled.ForwardZ*a.LateralMeters),(float)-(sampled.Z-sampled.ForwardX*a.LateralMeters));
                if(a.WorldPose.IsValid)expected=new Vector2((float)a.WorldPose.X,(float)-a.WorldPose.Z);
                Assert.That(state.Get(i).Point,Is.EqualTo(expected));
            }
            long tick=host.Runner.Field.Tick.Value;for(int i=0;i<20;i++){state.Observe(first,map,0);state.Observe(second,map,0);}
            long bytes=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<1000;i++){state.Observe(first,map,0);state.Observe(second,map,0);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-bytes,Is.Zero);Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));
            Assert.That(state.Observe(second,map,0),Is.False);state.Clear();Assert.That(state.Count,Is.Zero);Assert.That(state.PlayerIndex,Is.EqualTo(-1));
            yield return null;
        }
        [UnityTest] public IEnumerator ScopedWorldPoseIsShownWithoutSnappingPlayerBackToCenterline()
        {
            var host=Host(true,1);for(int i=0;i<60;i++)host.Runner.Step();var snapshot=RacePresentationSnapshot.Capture(host.Runner.Field);var actor=snapshot.Get(0);
            Assert.That(actor.WorldPose.IsValid,Is.True);var state=new RaceMiniMapState(30);state.Observe(snapshot,host.GameplayHud.MiniMapGeometry,0);
            Assert.That(state.Get(0).Point,Is.EqualTo(new Vector2((float)actor.WorldPose.X,(float)-actor.WorldPose.Z)));
            Assert.That(state.Get(0).Direction.x,Is.EqualTo((float)Math.Sin(actor.WorldPose.HeadingRadians)).Within(.0001));
            yield return null;
        }
        [UnityTest] public IEnumerator ShootingContextAndRaceResetPreserveTheClearAimingView()
        {
            var host=Host(false,1);int steps=0;
            while(host.Runner.Field.GetAthlete(0).ActiveShooting==null&&steps++<12000)host.Runner.Step();
            Assert.That(host.Runner.Field.GetAthlete(0).ActiveShooting,Is.Not.Null,"Reach a real, unedited range visit.");
            host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));long tick=host.Runner.Field.Tick.Value;host.GameplayHud.Refresh();
            Assert.That(host.GameplayHud.Model.Shooting,Is.True);Assert.That(host.GameplayHud.MiniMapVisible,Is.False);
            Assert.That(host.GameplayHud.Root.Q("minimap_panel").style.display.value,Is.EqualTo(DisplayStyle.None));
            Assert.That(host.Runner.Field.Tick.Value,Is.EqualTo(tick));
            host.Feed.ResetRace();host.GameplayHud.Refresh();Assert.That(host.GameplayHud.MiniMapState.Count,Is.Zero);Assert.That(host.GameplayHud.MiniMapVisible,Is.False);
            yield return null;
        }
        [UnityTest] public IEnumerator NativeHudLayoutAndRetainedLayersStayClearAcrossPreferencesAndTables()
        {
            var host=Host();for(int i=0;i<300;i++)host.Runner.Step();host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));var hud=host.GameplayHud;hud.Refresh();yield return null;yield return null;
            var geometry=hud.MiniMapGeometry;var state=hud.MiniMapState;
            int cameras=root.GetComponentsInChildren<Camera>().Length;
            yield return Capture(1600,900,"ru",0,false,"ski-ru");
            yield return Capture(1024,768,"en",2,true,"ski-en-large-contrast");
            // Restoring the target changes the native panel's scale/layout; settle that before measuring marker-only repaints.
            for(int i=0;i<6;i++)yield return null;
            Assert.That(hud.MiniMapGeometry,Is.SameAs(geometry));Assert.That(hud.MiniMapState,Is.SameAs(state));Assert.That(root.GetComponentsInChildren<Camera>().Length,Is.EqualTo(cameras));
            var graphic=hud.MiniMapGraphic;int paints=graphic.CoursePaints,actors=graphic.ActorPaints;
            for(int i=0;i<20;i++)host.Runner.Step();host.Feed.Publish(RacePresentationSnapshot.Capture(host.Runner.Field));hud.Refresh();yield return null;yield return null;
            Assert.That(graphic.ActorPaints,Is.GreaterThan(actors));Assert.That(graphic.CoursePaints,Is.EqualTo(paints),"Moving dots cannot rebuild the static route vector layer.");
            hud.SetCheckpointTableVisible(true);Assert.That(hud.MiniMapVisible,Is.False);hud.SetCheckpointTableVisible(false);Assert.That(hud.MiniMapVisible,Is.True);
            hud.SetBroadcastVisible(true);Assert.That(hud.MiniMapVisible,Is.False);hud.SetBroadcastVisible(false);Assert.That(hud.MiniMapVisible,Is.True);
            hud.enabled=false;Assert.That(hud.MiniMapVisible,Is.False);yield return null;hud.enabled=true;yield return null;
            Assert.That(hud.MiniMapVisible,Is.True);Assert.That(hud.MiniMapState.Count,Is.EqualTo(30));
            IEnumerator Capture(int width,int height,string locale,int size,bool contrast,string name)
            {
                var panel=(PanelSettings)typeof(GameplayHud).GetField("panel",BindingFlags.Instance|BindingFlags.NonPublic).GetValue(hud);var target=new RenderTexture(width,height,0);target.Create();panel.targetTexture=target;var previous=RenderTexture.active;Texture2D pixels=null;
                try
                {
                    hud.SetPreferences(locale,size);hud.SetAccessibility(contrast);for(int i=0;i<6;i++)yield return null;
                    Assert.That(hud.MiniMapVisible,Is.True);var screen=hud.Root.worldBound;var map=hud.Root.Q("minimap_panel").worldBound;var top=hud.Root.Q("timing_panel").worldBound;var motion=hud.Root.Q("motion_panel").worldBound;
                    Assert.That(map.yMin,Is.GreaterThanOrEqualTo(top.yMax+10));Assert.That(map.yMax,Is.LessThan(motion.yMin-10));Assert.That(map.xMax,Is.LessThan(screen.center.x));
                    float area=map.width*map.height;foreach(string key in new[]{"timing_panel","motion_panel","condition_panel","help_hint"}){var b=hud.Root.Q(key).worldBound;area+=b.width*b.height;}
                    Assert.That(area/(screen.width*screen.height),Is.LessThan(size==0?.10f:.17f),"Include the new map in the approved total HUD area budget.");
                    hud.Root.Q("minimap_panel").Query<VisualElement>().ForEach(v=>Assert.That(v.pickingMode,Is.EqualTo(PickingMode.Ignore)));
                    Assert.That(hud.MiniMapState.Rivals,Is.EqualTo(29));
                    RenderTexture.active=target;pixels=new Texture2D(width,height,TextureFormat.RGBA32,false);pixels.ReadPixels(new Rect(0,0,width,height),0,0);pixels.Apply();
                    const string output="artifacts/m16-4/minimap/views/";Directory.CreateDirectory(output);File.WriteAllBytes(output+name+".png",pixels.EncodeToPNG());
                    File.WriteAllText(output+name+"-coverage.txt",(area/(screen.width*screen.height)).ToString(System.Globalization.CultureInfo.InvariantCulture));
                }
                finally{panel.targetTexture=null;RenderTexture.active=previous;if(pixels!=null)Object.DestroyImmediate(pixels);target.Release();Object.DestroyImmediate(target);}
            }
        }
    }
}
