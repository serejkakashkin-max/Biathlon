using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;
namespace Biathlon.PlayModeTests
{
    public class VegetationTests
    {
        [Test] public void PlacementRepeatsKeepsCanopiesOutsideRouteAndDensityIsNested()
        {
            var d=VegetationDefinition.Load();var route=new[]{new Vector3(-12,0,-80),new Vector3(8,0,0),new Vector3(-4,0,80)};
            var first=d.Place(route);var second=d.Place(route);Assert.That(first.Length,Is.InRange(400,d.max_instances));Assert.That(second.Length,Is.EqualTo(first.Length));
            int low=0,medium=0;
            for(int i=0;i<first.Length;i++){var t=first[i];Assert.That(t.Matrix,Is.EqualTo(second[i].Matrix));Assert.That(VegetationDefinition.DistanceToRoute(t.Position,route),Is.GreaterThanOrEqualTo(d.route_half_width_m+d.canopy_radius_m*t.Scale+d.wind_amplitude_m));if(t.DensityRank<d.Density("low"))low++;if(t.DensityRank<d.Density("balanced"))medium++;}
            Assert.That(low,Is.GreaterThan(100));Assert.That(medium,Is.GreaterThan(low));Assert.That(medium,Is.LessThan(first.Length));
            d.max_instances=7;Assert.That(d.Place(route).Length,Is.EqualTo(7));
            Assert.Throws<ArgumentException>(()=>d.Place(new[]{Vector3.zero,new Vector3(float.NaN,0,1)}));
            var json=Resources.Load<TextAsset>("Biathlon/vegetation_v1").text;
            Assert.Throws<ArgumentException>(()=>VegetationDefinition.FromJson(json.Replace("768","2048")));
            Assert.Throws<ArgumentException>(()=>VegetationDefinition.FromJson(json.Replace("0.16","-0.1")));
            Assert.Throws<ArgumentException>(()=>d.Density("unknown"));
        }
        [Test] public void GeneratedLodsReduceGeometryAndKeepSnowWindAndInstancingSupport()
        {
            var material=Resources.Load<Material>("Biathlon/Vegetation/Conifer");Assert.That(material,Is.Not.Null);Assert.That(material.enableInstancing,Is.True);Assert.That(material.shader.isSupported,Is.True);
            Assert.That(material.FindPass("ShadowCaster"),Is.GreaterThanOrEqualTo(0));Assert.That(material.HasProperty("_Wind"),Is.True);
            int prior=int.MaxValue;
            for(int i=0;i<3;i++){var mesh=Resources.Load<Mesh>("Biathlon/Vegetation/ConiferLOD"+i);Assert.That(mesh,Is.Not.Null);Assert.That(mesh.vertexCount,Is.LessThan(prior));prior=mesh.vertexCount;Assert.That(mesh.bounds.max.y,Is.InRange(7.9f,8.1f));Assert.That(mesh.bounds.extents.x,Is.LessThan(2.5f));Assert.That(mesh.colors.Length,Is.EqualTo(mesh.vertexCount));}
        }
        [Test] public void CullingBudgetsRewindAndWarmedPreparationHaveNoManagedAllocation()
        {
            var go=new GameObject("forest test");var camGo=new GameObject("forest camera");
            try
            {
                var cam=camGo.AddComponent<Camera>();cam.enabled=false;cam.transform.position=new Vector3(0,2,-48);cam.farClipPlane=200;
                var forest=go.AddComponent<VegetationPresentation>();forest.Initialize(VegetationPreview.Route);
                forest.Prepare(cam,"high",12);int high=forest.VisibleInstances;long triangles=forest.SubmittedTriangles;Assert.That(high,Is.GreaterThan(100));Assert.That(forest.NearCount,Is.GreaterThan(0));Assert.That(forest.FarCount,Is.GreaterThan(0));
                forest.Prepare(cam,"low",20);Assert.That(forest.VisibleInstances,Is.LessThan(high));forest.Prepare(cam,"high",12);Assert.That(forest.VisibleInstances,Is.EqualTo(high));Assert.That(forest.SubmittedTriangles,Is.EqualTo(triangles));
                for(int i=0;i<20;i++)forest.Prepare(cam,"balanced",i);
                long before=GC.GetAllocatedBytesForCurrentThread();for(int i=0;i<100;i++)forest.Prepare(cam,"balanced",i);long bytes=GC.GetAllocatedBytesForCurrentThread()-before;Assert.That(bytes,Is.Zero);
                cam.transform.position=new Vector3(0,0,-1000);forest.Prepare(cam,"high",0);Assert.That(forest.VisibleInstances,Is.Zero);
                go.transform.localScale=Vector3.one*2;Assert.Throws<ArgumentException>(()=>forest.Prepare(cam,"high",0));
                var d=VegetationDefinition.Load();Assert.That(d.Lod(d.cull_m),Is.EqualTo(-1));Assert.That(d.Lod(d.near_lod_m),Is.EqualTo(1));Assert.That(d.Lod(d.far_lod_m),Is.EqualTo(2));
            }
            finally{Object.DestroyImmediate(go);Object.DestroyImmediate(camGo);}
        }
    }
}
