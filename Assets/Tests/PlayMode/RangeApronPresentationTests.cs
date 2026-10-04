using System;
using System.Collections.Generic;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    // Draft focused tests for the imported compact content. No execution claim outside Unity.
    public sealed class RangeApronPresentationTests
    {
        [TestCase(0,45f)][TestCase(1,0f)][TestCase(2,31f)]
        public void ActualCompactFormatKeepsPaidGeometryAndOwnsOnePackedApronWithContinuousContact(int format,float yaw)
        {
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
            Assert.That(plan.SharedApron,Is.Not.Null,"Run this focused acceptance only after the compact bundle is imported.");
            var binding=plan.Formats.Get(format);var route=new RaceRoute(plan.Track,binding.Route,binding.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            var root=new GameObject("Compact apron native contact");root.transform.SetPositionAndRotation(new Vector3(200,70,-80),Quaternion.Euler(0,yaw,0));
            ProductionCoursePresentation course=null;ProductionVenueEnvironment terrain=null;
            string snapshots=plan.Track.ToJson()+plan.Visual.ToJson()+plan.Paths.ToJson()+plan.SharedApron.ToJson()+plan.Manifest.ToJson();
            try
            {
                course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route,sharedApron:plan.SharedApron);
                var apron=course.Apron;Assert.That(apron,Is.Not.Null);Assert.That(apron.SharedApronSnapshotJson,Is.EqualTo(plan.SharedApron.ToJson()));
                var cameraObject=new GameObject("Apron contact camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var feed=root.AddComponent<RacePresentationFeed>();terrain=root.AddComponent<ProductionVenueEnvironment>();
                var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
                terrain.Initialize(plan,recipe,ProductionVenueHost.ReadResource,feed,camera,apron:apron);
                course.JoinGround(terrain,terrain.TerrainGridSpacing,terrain.TerrainGridOrigin);
                var frame=course.ApronRenderer.localToWorldMatrix;
                Assert.That(course.ApronRenderer.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(apron.Top));
                Assert.That(course.ApronSkirtRenderer.localToWorldMatrix,Is.EqualTo(frame));
                Assert.That(course.ApronSkirtRenderer.GetComponent<MeshFilter>().sharedMesh,Is.SameAs(course.ApronSkirt));
                Faces(apron.Top,frame,true);Faces(course.ApronSkirt,frame,false);
                for(int lane=0;lane<plan.Paths.Count;lane++)
                {
                    string id=plan.Paths.GetLane(lane).MatAnchor.LoopId;var paid=course.NativeLoopMesh(id);var original=TrackRibbonBuilder.Build(plan.Track,plan.Visual,id);
                    try{CollectionAssert.AreEqual(original.vertices,paid.vertices);CollectionAssert.AreEqual(original.triangles,paid.triangles);}
                    finally{Object.DestroyImmediate(original);}
                    foreach(var point in paid.vertices)Assert.That(apron.ContainsPaidTop(point),Is.True,"All paid original path points remain covered at their original height.");
                    Assert.That(course.LoopRenderer(id).enabled,Is.True);Assert.That(course.DrawLoopMesh(id).triangles,Is.Empty,"Covered paid top belongs to the continuous surface.");
                    Assert.That(course.LoopRenderer(id).GetComponent<MeshFilter>().sharedMesh,Is.SameAs(course.DrawLoopMesh(id)));
                }
                for(int loop=0;loop<course.LoopCount;loop++)
                {
                    string id=course.LoopId(loop);Assert.That(course.LoopRenderer(id).localToWorldMatrix,Is.EqualTo(frame));
                    Faces(course.DrawLoopMesh(id),frame,false);Faces(course.SkirtMesh(id),frame,false);
                    // No retained triangle can be wholly inside the area replaced by the composite.
                    var mesh=course.SkirtMesh(id);var p=mesh.vertices;var t=mesh.triangles;
                    for(int i=0;i<t.Length;i+=3)Assert.That(apron.Contains(p[t[i]])&&apron.Contains(p[t[i+1]])&&apron.Contains(p[t[i+2]]),Is.False);
                }
                // This bounded stadium has a flat authored ground plane. Prove that actual
                // rendered terrain covers the apron and the entire perimeter join at that plane.
                var center=new Vector3((apron.MinX+apron.MaxX)*.5f,apron.Height,(apron.MinZ+apron.MaxZ)*.5f);
                float ground=terrain.SampleGround(center),actualGround=frame.MultiplyPoint3x4(new Vector3(center.x,ground,center.z)).y;
                float actualTop=frame.MultiplyPoint3x4(center).y;Assert.That(actualTop-actualGround,Is.GreaterThanOrEqualTo(.025f));
                int inside=0,near=0;var filters=terrain.GetComponentsInChildren<MeshFilter>(true);
                for(int tile=0;tile<terrain.TerrainTileCount;tile++)
                {
                    var mesh=terrain.TileMesh(tile);MeshRenderer owner=null;foreach(var filter in filters)if(filter.sharedMesh==mesh){owner=filter.GetComponent<MeshRenderer>();break;}
                    Assert.That(owner,Is.Not.Null);Assert.That(owner.localToWorldMatrix,Is.EqualTo(frame));var p=mesh.vertices;var weights=mesh.colors;
                    foreach(var point in p)
                    {
                        if(point.x<apron.MinX-course.SnowShoulderWidthMeters-terrain.TerrainGridSpacing||point.x>apron.MaxX+course.SnowShoulderWidthMeters+terrain.TerrainGridSpacing||
                            point.z<apron.MinZ-course.SnowShoulderWidthMeters-terrain.TerrainGridSpacing||point.z>apron.MaxZ+course.SnowShoulderWidthMeters+terrain.TerrainGridSpacing)continue;
                        Assert.That(frame.MultiplyPoint3x4(point).y,Is.EqualTo(actualGround),"Actual native apron contact tiles retain their flat original height.");near++;
                    }
                    for(int v=0;v<p.Length;v++)if(apron.Contains(p[v])){Assert.That(weights[v],Is.EqualTo(new Color(0,1,0,0)));inside++;}
                }
                Assert.That(inside,Is.GreaterThan(0));Assert.That(near,Is.GreaterThan(0));
                foreach(var p in course.ApronSkirt.vertices){float y=frame.MultiplyPoint3x4(p).y;Assert.That(y,Is.GreaterThanOrEqualTo(actualGround-1e-5f));Assert.That(y,Is.LessThanOrEqualTo(actualTop));}
                Perimeter(apron,course.ApronSkirt);
                Assert.That(snapshots,Is.EqualTo(plan.Track.ToJson()+plan.Visual.ToJson()+plan.Paths.ToJson()+plan.SharedApron.ToJson()+plan.Manifest.ToJson()));
                foreach(var collider in root.GetComponentsInChildren<Collider>(true))Assert.That(collider.enabled,Is.False,"Deferred collider cleanup cannot leave gameplay physics active.");
            }
            finally{course?.Shutdown();terrain?.Shutdown();Object.DestroyImmediate(root);}
        }
        private static void Faces(Mesh mesh,Matrix4x4 frame,bool top)
        {
            var p=mesh.vertices;var t=mesh.triangles;
            for(int i=0;i<t.Length;i+=3)
            {
                var a=frame.MultiplyPoint3x4(p[t[i]]);var b=frame.MultiplyPoint3x4(p[t[i+1]]);var c=frame.MultiplyPoint3x4(p[t[i+2]]);var cross=Vector3.Cross(b-a,c-a);
                Assert.That(float.IsFinite(cross.x)&&float.IsFinite(cross.y)&&float.IsFinite(cross.z)&&cross.sqrMagnitude>=1e-16f,Is.True,"Retained actual rendered faces keep the existing nondegeneracy bound.");
                if(top)Assert.That(cross.y,Is.GreaterThan(1e-12f));
            }
        }
        private static void Perimeter(RangeApronDrawPlan apron,Mesh join)
        {
            var corners=apron.Top.vertices;var points=join.vertices;var indices=join.triangles;
            foreach(var side in new[]{(0,2),(2,3),(3,1),(1,0)})
            {
                var a=corners[side.Item1];var b=corners[side.Item2];var delta=b-a;double length=delta.sqrMagnitude;
                var intervals=new List<(double,double)>();
                for(int face=0;face<indices.Length;face+=3)for(int edge=0;edge<3;edge++)
                {
                    var first=points[indices[face+edge]];var last=points[indices[face+(edge+1)%3]];
                    if(first.y!=apron.Height||last.y!=apron.Height)continue;
                    double start=Vector3.Dot(first-a,delta)/length,end=Vector3.Dot(last-a,delta)/length;
                    if(Vector3.Distance(first,a+delta*(float)start)>2e-4||Vector3.Distance(last,a+delta*(float)end)>2e-4)continue;
                    intervals.Add((Math.Max(0,Math.Min(start,end)),Math.Min(1,Math.Max(start,end))));
                }
                intervals.Sort((x,y)=>x.Item1.CompareTo(y.Item1));double covered=0;double tolerance=2e-4/Math.Sqrt(length);
                foreach(var interval in intervals){Assert.That(interval.Item1,Is.LessThanOrEqualTo(covered+tolerance),"Apron outer boundary cannot lose a ground join.");covered=Math.Max(covered,interval.Item2);}
                Assert.That(covered,Is.GreaterThanOrEqualTo(1-tolerance));
            }
        }
    }
}
