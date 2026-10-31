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
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class FormatSnowSeamReviewTests
    {
        [Serializable]private sealed class EdgeReport {public int schema_version=1;public string id,format;public int join_vertices,join_triangles,rounded_cuts;public double join_elapsed_ms;public SnowEdgeDiagnostic[] edges;}
        [UnityTest,Timeout(300000)]public IEnumerator ActualSprintAndPursuitForkViews()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            string phase=File.Exists("artifacts/m16-2/snow-seams/phase.txt")?File.ReadAllText("artifacts/m16-2/snow-seams/phase.txt").Trim():"after";
            Assert.That(phase=="before"||phase=="after",Is.True);
            string output=Path.GetFullPath("artifacts/m16-2/snow-seams/"+phase);Directory.CreateDirectory(output);
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            foreach(string format in new[]{"sprint","pursuit"})
            {
                var root=new GameObject("Format snow seam native review");BenchmarkRenderTarget target=null;
                try
                {
                    var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+format+"_profile"));
                    var route=new RaceRoute(plan.Track,RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+format+"_route")),profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
                    var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment"));
                    var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route,sharedApron:plan.SharedApron,
                        snowShoulders:ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(recipe.terrain.snow_shoulders_resource),plan));
                    var cameraObject=new GameObject("Seam camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.farClipPlane=650;
                    var feed=root.AddComponent<RacePresentationFeed>();var environment=root.AddComponent<ProductionVenueEnvironment>();
                    environment.Initialize(plan,recipe,ProductionVenueHost.ReadResource,feed,camera,apron:course.Apron);environment.AutomaticRendering=false;
                    var watch=System.Diagnostics.Stopwatch.StartNew();course.JoinGround(environment,environment.TerrainGridSpacing,environment.TerrainGridOrigin);watch.Stop();
                    var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
                    target=new BenchmarkRenderTarget(1280,720);
                    var views=new List<(string,double)>{("main",55d)};
                    if(format=="sprint")views.AddRange(new[]{("main",240d),("main",440d),("sprint_extension",0d),("sprint_extension",40d),("sprint_extension",210d),("sprint_extension",248.779d),("main",139.43d),("main",180.273d)});
                    else views.AddRange(new[]{("main",151.997d),("main",633.649d),("pursuit_shortcut",0d),("pursuit_shortcut",40d),("pursuit_shortcut",340d),("pursuit_shortcut",366.692d)});
                    foreach(var view in views)
                    {
                        var point=plan.Visual.Get(view.Item1).Sample(view.Item2);var p=new Vector3((float)point.X,(float)point.Y,(float)point.Z);var forward=new Vector3((float)point.ForwardX,0,(float)point.ForwardZ);
                        camera.transform.position=p-forward*14+Vector3.up*5;camera.transform.LookAt(p+forward*12);
                        for(int frame=0;frame<5;frame++){environment.Render(camera,"high",12);target.Render(camera);yield return null;}
                        target.SavePng(Path.Combine(output,format+"-"+view.Item1+"-"+view.Item2.ToString("0",System.Globalization.CultureInfo.InvariantCulture)+".png"));
                    }
                    var report=new EdgeReport{id="m16_2_"+format+"_snow_seams",format=format,join_elapsed_ms=watch.Elapsed.TotalMilliseconds,edges=new List<SnowEdgeDiagnostic>(course.EdgeDiagnostics).ToArray()};
                    report.rounded_cuts=course.RoundedSnowCutCount;
                    for(int i=0;i<course.LoopCount;i++){var mesh=course.SkirtMesh(course.LoopId(i));if(mesh!=null){report.join_vertices+=mesh.vertexCount;report.join_triangles+=mesh.triangles.Length/3;}}
                    File.WriteAllText(Path.Combine(output,format+"-edges.json"),JsonUtility.ToJson(report,true));
                    Assert.That(course.GroundJoined,Is.True);
                }
                finally{target?.Dispose();Object.DestroyImmediate(root);}
                yield return null;
            }
        }
    }
}
