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
    public sealed class SprintJunctionSnowTests
    {
        private readonly struct Face
        {
            public readonly Vector3 A,B,C,N;
            public Face(Vector3 a,Vector3 b,Vector3 c){A=a;B=b;C=c;N=Vector3.Cross(b-a,c-a);}
            public bool Covers(Vector3 p,out float height)
            {
                height=0;if(N.y<=1e-9f)return false;
                double area=-N.y;
                double u=Cross(p-A,C-A)/area,v=Cross(B-A,p-A)/area;
                if(u< -1e-6||v< -1e-6||u+v>1+1e-6)return false;
                height=(float)(A.y+u*(B.y-A.y)+v*(C.y-A.y));return true;
            }
            private static double Cross(Vector3 a,Vector3 b)=>(double)a.x*b.z-(double)a.z*b.x;
        }
        [Serializable]private sealed class Result{public int exposed_faces;public float largest_exposed_area,total_exposed_area;public List<Vector3> broad_faces=new List<Vector3>();public List<SnowEdgeDiagnostic> edges;}

        [UnityTest,Timeout(180000)]public IEnumerator ReturnForkDoesNotExposeBroadVerticalSnowFaces()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            string output=Path.GetFullPath("artifacts/m16-5-snow-gaps/"+(File.Exists("artifacts/m16-5-snow-gaps/phase.txt")?File.ReadAllText("artifacts/m16-5-snow-gaps/phase.txt").Trim():"after"));Directory.CreateDirectory(output);
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest")),ProductionVenueHost.ReadResource);
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_profile"));
            var route=new RaceRoute(plan.Track,RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+"sprint_route")),profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource(prefix+"environment"));
            string source=plan.Track.ToJson();var root=new GameObject("Actual Sprint return surface regression");BenchmarkRenderTarget target=null;
            try
            {
                var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route,sharedApron:plan.SharedApron,snowShoulders:ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(recipe.terrain.snow_shoulders_resource),plan));
                var camObject=new GameObject("Snow junction camera");camObject.transform.SetParent(root.transform,false);var camera=camObject.AddComponent<Camera>();camera.enabled=false;camera.fieldOfView=58;camera.farClipPlane=650;
                var environment=root.AddComponent<ProductionVenueEnvironment>();environment.Initialize(plan,recipe,ProductionVenueHost.ReadResource,root.AddComponent<RacePresentationFeed>(),camera,apron:course.Apron);environment.AutomaticRendering=false;
                course.JoinGround(environment,environment.TerrainGridSpacing,environment.TerrainGridOrigin);
                var quality=root.AddComponent<RenderQualityPresentation>();quality.Initialize(camera);quality.Apply("high");
                target=new BenchmarkRenderTarget(1548,954);
                foreach(var view in new[]{("sprint_extension",235d),("sprint_extension",248.779d),("main",139.43d)})
                {
                    var sample=plan.Visual.Get(view.Item1).Sample(view.Item2);var p=new Vector3((float)sample.X,(float)sample.Y,(float)sample.Z);var f=new Vector3((float)sample.ForwardX,0,(float)sample.ForwardZ);
                    camera.transform.position=p-f*8+Vector3.up*4;camera.transform.LookAt(p+f*10);
                    for(int i=0;i<5;i++){environment.Render(camera,"high",12);target.Render(camera);yield return null;}
                    target.SavePng(Path.Combine(output,view.Item1+"-"+view.Item2.ToString(System.Globalization.CultureInfo.InvariantCulture)+".png"));
                }
                var end=plan.Visual.Get("sprint_extension").Sample(plan.Track.GetLoop("sprint_extension").Length.Meters);var anchor=new Vector3((float)end.X,(float)end.Y,(float)end.Z);
                var roofs=new List<Face>();var walls=new List<Face>();
                for(int i=0;i<course.LoopCount;i++)
                {string id=course.LoopId(i);Collect(course.LoopMesh(id),course.LoopRenderer(id).localToWorldMatrix,false);Collect(course.SkirtMesh(id),course.SkirtRenderer(id).localToWorldMatrix,true);}
                Collect(course.Apron.Top,course.ApronRenderer.localToWorldMatrix,false);Collect(course.ApronSkirt,course.ApronSkirtRenderer.localToWorldMatrix,true);
                var result=new Result{edges=new List<SnowEdgeDiagnostic>()};foreach(var edge in course.EdgeDiagnostics)if(edge.loop_id=="sprint_extension"&&edge.start_m>230&&!edge.tapered)result.edges.Add(edge);
                foreach(var face in walls)
                {
                    var columns=new Dictionary<Vector2,float>();foreach(var p in new[]{face.A,face.B,face.C}){var key=new Vector2(p.x,p.z);columns.TryGetValue(key,out float prior);columns[key]=Mathf.Max(prior,p.y);}
                    if(columns.Count!=2)continue;var points=new List<Vector2>(columns.Keys);float length=Vector2.Distance(points[0],points[1]);if(length<.01f)continue;
                    var center=(face.A+face.B+face.C)/3;float u=Vector2.Distance(new Vector2(center.x,center.z),points[0])/length;
                    float top=Mathf.Lerp(columns[points[0]],columns[points[1]],u);var normal=new Vector3(face.N.x,0,face.N.z).normalized;var probe=center+normal*.03f;
                    float roof=float.NegativeInfinity;foreach(var candidate in roofs)if(candidate.Covers(probe,out float h))roof=Mathf.Max(roof,h);
                    if(top-roof<=.08f)continue;float area=face.N.magnitude*.5f;result.exposed_faces++;result.total_exposed_area+=area;result.largest_exposed_area=Mathf.Max(result.largest_exposed_area,area);if(area>.005f){result.broad_faces.Add(face.A);result.broad_faces.Add(face.B);result.broad_faces.Add(face.C);}
                }
                File.WriteAllText(Path.Combine(output,"exposed-faces.json"),JsonUtility.ToJson(result,true));
                Assert.That(plan.Track.ToJson(),Is.EqualTo(source));
                Assert.That(result.largest_exposed_area,Is.LessThanOrEqualTo(.005f),"A broad vertical curtain must have an outward rounded snow roof, not drop straight to terrain at the photographed Sprint return.");
                void Collect(Mesh mesh,Matrix4x4 frame,bool join)
                {
                    var points=mesh.vertices;for(int i=0;i<points.Length;i++)points[i]=frame.MultiplyPoint3x4(points[i]);var faces=mesh.triangles;
                    for(int i=0;i<faces.Length;i+=3)
                    {
                        var face=new Face(points[faces[i]],points[faces[i+1]],points[faces[i+2]]);var center=(face.A+face.B+face.C)/3;var delta=center-anchor;delta.y=0;
                        if(delta.sqrMagnitude>12*12)continue;if(face.N.y>1e-9f)roofs.Add(face);else if(join&&Mathf.Abs(face.N.y)<1e-7f&&new Vector2(face.N.x,face.N.z).magnitude>.001f)walls.Add(face);
                    }
                }
            }
            finally{target?.Dispose();Object.DestroyImmediate(root);}
            // Reuse the independent complete-contact oracle after releasing the
            // capture venue, so a visual fix cannot leave a floating patch edge.
            new FormatSnowCutGeometryTests().ActualFormatJoinsKeepPackedBuffersAndWholeTerrainContact("sprint",0f,false);
        }
    }
}
