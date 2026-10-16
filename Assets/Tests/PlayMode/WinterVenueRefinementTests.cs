using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Unity.Profiling;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class WinterVenueRefinementTests
    {
        [Serializable] public class Review
        {
            public string phase;
            public string measurement_scope="Editor offscreen capture; frame time includes Editor/vsync; unavailable counters are -1. Not standalone performance qualification.";
            public int field, vertical_panels, tapered_panels, join_vertices, flag_excluded_trees,covered_curtain_faces,rim_clearance_adjustments;
            public float maximum_edge_gap_m;
            public SnowEdgeDiagnostic[] edges;
            public List<View> views=new List<View>();
        }
        [Serializable] public class View
        {
            public string id,tier,weather="clear",lighting;
            public bool override_wind;
            public Vector2 wind;
            public Vector3 position,target;
            public int frames;
            public bool main_available,draw_calls_available,triangles_available,gc_available,gpu_available;
            public double mean_frame_ms,mean_main_thread_ms,mean_gpu_ms,mean_draw_calls,mean_triangles,mean_gc_bytes;
            public long allocated_memory_bytes;
        }
        [UnityTest] public IEnumerator NativeWholeVenueReview()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            string phase=File.Exists("artifacts/m16-1/phase.txt")?File.ReadAllText("artifacts/m16-1/phase.txt").Trim():"review";
            Assert.That(phase,Does.Match("^[a-z0-9-]+$"));
            string output=Path.GetFullPath("artifacts/m16-1/"+phase);Directory.CreateDirectory(output);
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var root=new GameObject("M16.1 isolated native venue review");
            var report=new Review{phase=phase,field=30};
            RenderTexture rt=null;Texture2D pixels=null;
            try
            {
                var host=root.AddComponent<ProductionVenueHost>();host.AutoStart=false;host.enabled=false;
                host.Initialize(venue,CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_profile")),
                    RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"mass_start_route")),
                    RaceSnowConditions.FromJson(ProductionVenueHost.ReadResource("Biathlon/race_snow_neutral_v1")),30);
                var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment"));
                bool baseline=phase.EndsWith("-before",StringComparison.Ordinal);
                if(baseline){recipe.range.flags_resource=null;recipe.terrain.snow_shoulders_resource=prefix+"snow_shoulders_before_m16_1";}
                host.InitializeEnvironment(recipe);
                host.Environment.AutomaticRendering=false;
                report.flag_excluded_trees=host.Environment.FlagClearanceExcludedTrees;
                var course=host.Course;
                report.vertical_panels=course.VerticalSnowPanelCount;report.tapered_panels=course.TaperedSnowPanelCount;
                report.maximum_edge_gap_m=course.MaximumEdgeGapMeters;
                report.covered_curtain_faces=course.CoveredCurtainFaces;report.rim_clearance_adjustments=course.RimClearanceAdjustments;
                report.edges=new List<SnowEdgeDiagnostic>(course.EdgeDiagnostics).ToArray();
                for(int i=0;i<course.LoopCount;i++)report.join_vertices+=course.SkirtMesh(course.LoopId(i)).vertexCount;
                var camera=host.ChaseCamera;camera.GetComponent<AthleteChaseCamera>().enabled=false;
                camera.fieldOfView=58;camera.nearClipPlane=.05f;camera.farClipPlane=650;
                rt=new RenderTexture(1600,900,24);rt.Create();camera.targetTexture=rt;
                pixels=new Texture2D(1600,900,TextureFormat.RGB24,false);
                var views=new List<View>();
                AddRoute("a-stadium-bend", "main",55,6,10);
                AddRoute("b-forest-spectators", "main",325,4,8);
                AddRoute("c-wide-course", "main",480,4,10);
                AddRoute("d-snow-bank", "main",715,3,7);
                AddRoute("f-penalty", "penalty_150",65,4,8);
                AddRoute("g-close-bank", "main",120,1.3f,3);
                var center=(recipe.range.banks[0].position+recipe.range.banks[recipe.range.banks.Length-1].position)*.5f;
                var forward=recipe.range.forward;
                views.Add(new View{id="e-range",position=center-forward*66+Vector3.up*5,target=center+Vector3.up*3});
                views.Add(new View{id="h-overhead",position=center-forward*60+Vector3.up*115,target=center-forward*60});
                views.Add(new View{id="i-range-sight",position=recipe.range.banks[14].position-forward*50+Vector3.up*.1f,target=recipe.range.banks[14].position});
                var flagDefinition=RangeFlagDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"range_flags_v1"));
                var flag=flagDefinition.Positions(recipe.range,host.Environment.SampleGround)[2]+Vector3.up*10;
                views.Add(new View{id="j-flag-calm",position=flag-forward*10+Vector3.right*3,target=flag,override_wind=true,wind=Vector2.zero});
                views.Add(new View{id="k-flag-strong",position=flag-forward*10+Vector3.right*3,target=flag,override_wind=true,wind=new Vector2(8,0)});
                views.Add(new View{id="l-range-overcast",position=center-forward*66+Vector3.up*5,target=center+Vector3.up*3,weather="overcast"});
                views.Add(new View{id="m-range-snowfall",position=center-forward*66+Vector3.up*5,target=center+Vector3.up*3,weather="snowfall"});
                views.Add(new View{id="n-range-low-sun",position=center-forward*66+Vector3.up*5,target=center+Vector3.up*3,lighting="late_day"});
                var bend=views[0];views.Add(new View{id="o-bank-low-sun",position=bend.position,target=bend.target,lighting="late_day"});
                Vector2 originalWind=recipe.lighting.visual_wind;
                using(var main=ProfilerRecorder.StartNew(ProfilerCategory.Internal,"Main Thread",1))
                using(var draws=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Draw Calls Count",1))
                using(var tris=ProfilerRecorder.StartNew(ProfilerCategory.Render,"Triangles Count",1))
                using(var gc=ProfilerRecorder.StartNew(ProfilerCategory.Memory,"GC Allocated In Frame",1))
                {
                    foreach(var tier in new[]{"high","balanced","low"})
                    {
                        host.GetComponent<RenderQualityPresentation>().Apply(tier);
                        foreach(var spec in views)
                        {
                            if(tier!="high"&&spec.id!="a-stadium-bend"&&spec.id!="e-range")continue;
                            recipe.lighting.visual_wind=spec.override_wind?spec.wind:originalWind;
                            var v=new View{id=spec.id,tier=tier,weather=spec.weather,lighting=spec.lighting,position=spec.position,target=spec.target,wind=recipe.lighting.visual_wind,override_wind=spec.override_wind};
                            camera.transform.position=v.position;camera.transform.LookAt(v.target);
                            host.Environment.SetViewCamera(camera);host.Environment.SetAtmosphereForReview(v.weather);
                            host.Environment.SetLightingForReview(v.lighting);
                            void DrawFrame()
                            {
                                host.Environment.Render(camera,tier,12);
                                // The host owns a detached recipe; override only this visual
                                // cloth observation, never the shooting/weather authority.
                                if(v.override_wind)host.Environment.RangeFlags?.Render(12,v.wind);
                            }
                            for(int i=0;i<8;i++){DrawFrame();yield return null;}
                            for(int i=0;i<40;i++)
                            {
                                DrawFrame();FrameTimingManager.CaptureFrameTimings();yield return null;
                                v.frames++;v.mean_frame_ms+=Time.unscaledDeltaTime*1000;
                                v.mean_main_thread_ms+=main.Valid?main.LastValue*.000001:0;
                                v.mean_draw_calls+=draws.Valid?draws.LastValue:0;v.mean_triangles+=tris.Valid?tris.LastValue:0;
                                v.mean_gc_bytes+=gc.Valid?gc.LastValue:0;
                            }
                            v.main_available=main.Valid;v.draw_calls_available=draws.Valid&&v.mean_draw_calls>0;
                            v.triangles_available=tris.Valid&&v.mean_triangles>0;v.gc_available=gc.Valid;
                            v.mean_frame_ms/=v.frames;v.mean_main_thread_ms=v.main_available?v.mean_main_thread_ms/v.frames:-1;
                            v.mean_draw_calls=v.draw_calls_available?v.mean_draw_calls/v.frames:-1;
                            v.mean_triangles=v.triangles_available?v.mean_triangles/v.frames:-1;v.mean_gc_bytes=v.gc_available?v.mean_gc_bytes/v.frames:-1;
                            // Editor GPU frame timing does not identify this offscreen camera.
                            // Do not report a single, possibly unrelated sample as a mean.
                            v.gpu_available=false;v.mean_gpu_ms=-1;
                            v.allocated_memory_bytes=UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                            report.views.Add(v);
                            DrawFrame();yield return null;
                            if(v.override_wind&&!baseline)Assert.That(host.Environment.RangeFlags.ObservedWind,Is.EqualTo(v.wind));
                            var old=RenderTexture.active;RenderTexture.active=rt;pixels.ReadPixels(new Rect(0,0,1600,900),0,0);pixels.Apply();RenderTexture.active=old;
                            File.WriteAllBytes(Path.Combine(output,v.id+"-"+tier+".png"),pixels.EncodeToPNG());
                        }
                    }
                }
                Assert.That(host.Runner.Field.Tick.Value,Is.Zero,"Visual review must not advance authority.");
                Assert.That(host.Proxies.Count,Is.EqualTo(30));
                File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(report,true));
                void AddRoute(string id,string loop,double distance,float height,float behind)
                {
                    var p=venue.Visual.Get(loop).Sample(distance);var at=new Vector3((float)p.X,(float)p.Y,(float)p.Z);
                    var direction=new Vector3((float)p.ForwardX,0,(float)p.ForwardZ);
                    views.Add(new View{id=id,position=at-direction*behind+Vector3.up*height,target=at+direction*18+Vector3.up*.8f});
                }
            }
            finally
            {
                if(root!=null)Object.Destroy(root);
                if(rt!=null){rt.Release();Object.Destroy(rt);}if(pixels!=null)Object.Destroy(pixels);
            }
            yield return null;yield return null;
        }
    }
}
