using System;
using System.Collections.Generic;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public sealed class SprintCurvedSnowBankTests
    {
        [TestCase(0f)][TestCase(45f)]
        public void OptionalActualSprintBanksKeepSourceAndFullNativeProjectedContact(float yaw)
        {
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
            var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));
            Assert.That(recipe.terrain.snow_shoulders_resource,Is.Not.Null.And.Not.Empty,"The native trial requires the explicit optional recipe binding.");
            var policy=ProductionSnowShoulderDefinition.FromJson(ProductionVenueHost.ReadResource(recipe.terrain.snow_shoulders_resource),plan);
            var binding=plan.Formats.Get(1);Assert.That(binding.Profile.Format,Is.EqualTo(CompetitionFormat.Sprint));
            var route=new RaceRoute(plan.Track,binding.Route,binding.Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            var root=new GameObject("Actual Sprint curved banks");root.transform.SetPositionAndRotation(new Vector3(200,70,-80),Quaternion.Euler(0,yaw,0));
            ProductionCoursePresentation course=null;ProductionVenueEnvironment environment=null;GameObject witnessObject=null;var owned=new List<Mesh>();
            try
            {
                course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route,sharedApron:plan.SharedApron);
                var source=course.NativeLoopMesh(policy.loop_id);var positions=source.vertices;var indices=source.triangles;
                course.ConfigureSnowBanks(plan,policy);Assert.That(course.SnowBankPolicyId,Is.EqualTo(policy.id));
                Assert.Throws<InvalidOperationException>(()=>course.ConfigureSnowBanks(plan,policy));
                var cameraObject=new GameObject("Bank trial camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var feed=root.AddComponent<RacePresentationFeed>();environment=root.AddComponent<ProductionVenueEnvironment>();
                environment.Initialize(plan,recipe,ProductionVenueHost.ReadResource,feed,camera,apron:course.Apron);
                course.JoinGround(environment,environment.TerrainGridSpacing,environment.TerrainGridOrigin);
                CollectionAssert.AreEqual(positions,source.vertices);CollectionAssert.AreEqual(indices,source.triangles);
                Assert.That(course.SkirtRenderer(policy.loop_id).GetComponent<MeshFilter>().sharedMesh,Is.SameAs(course.SkirtMesh(policy.loop_id)));
                Assert.That(course.SkirtRenderer(policy.loop_id).localToWorldMatrix,Is.EqualTo(course.LoopRenderer(policy.loop_id).localToWorldMatrix));
                // The exact paid branch point used by the owner-visible wall capture.
                // At least the camera-side edge must have a real outward bank, rather than
                // only the old zero-width vertical curtain or an inactive-route exemption.
                int panel=0;while(panel+1<course.NativeRowCount(policy.loop_id)-1&&course.NativeDistance(policy.loop_id,panel+1)<18.952)panel++;
                float fraction=(float)((18.952-course.NativeDistance(policy.loop_id,panel))/(course.NativeDistance(policy.loop_id,panel+1)-course.NativeDistance(policy.loop_id,panel)));
                var sourcePoints=source.vertices;var left=Vector3.Lerp(sourcePoints[panel*2],sourcePoints[(panel+1)*2],fraction);
                var right=Vector3.Lerp(sourcePoints[panel*2+1],sourcePoints[(panel+1)*2+1],fraction);var outward=right-(left+right)*.5f;outward.y=0;
                var bankWitness=course.SkirtRenderer(policy.loop_id).localToWorldMatrix.MultiplyPoint3x4(right+outward.normalized*.2f);
                Assert.That(Covered(bankWitness,course.SkirtMesh(policy.loop_id),course.SkirtRenderer(policy.loop_id).localToWorldMatrix),Is.True,"Paid Sprint source18.952m camera-side edge has a certified nonvertical bank.");

                // Independent explicit-world contact fixture extracted from the actual native
                // buffers and actual Renderer matrices. The historical generic contact checker
                // retains every clearance, topology, whole-line and original-boundary assertion.
                // It receives no assumed identity frame or synthetic/sampled terrain heights.
                var worldRibbons=new Mesh[course.LoopCount+1];var worldNative=new Mesh[worldRibbons.Length];var ids=new string[worldRibbons.Length];
                for(int loop=0;loop<course.LoopCount;loop++)
                {
                    ids[loop]=course.LoopId(loop);worldRibbons[loop]=World(course.DrawLoopMesh(ids[loop]),course.LoopRenderer(ids[loop]).localToWorldMatrix);
                    worldNative[loop]=World(course.NativeLoopMesh(ids[loop]),course.LoopRenderer(ids[loop]).localToWorldMatrix);
                }
                ids[ids.Length-1]="actual_continuous_apron";worldRibbons[ids.Length-1]=World(course.Apron.Top,course.ApronRenderer.localToWorldMatrix);worldNative[ids.Length-1]=worldRibbons[ids.Length-1];
                int sprint=Array.IndexOf(ids,policy.loop_id);worldRibbons[sprint]=World(course.LoopMesh(policy.loop_id),course.LoopRenderer(policy.loop_id).localToWorldMatrix);
                var join=World(course.SkirtMesh(policy.loop_id),course.SkirtRenderer(policy.loop_id).localToWorldMatrix);
                witnessObject=new GameObject("Independent actual world contact fixture");var witness=witnessObject.AddComponent<ProductionCoursePresentation>();
                Set(witness,"loopIds",ids);Set(witness,"meshes",worldRibbons);Set(witness,"nativeMeshes",worldNative);
                var skirts=new Mesh[ids.Length];skirts[sprint]=join;Set(witness,"skirts",skirts);
                var mapping=(Dictionary<string,int>)typeof(ProductionCoursePresentation).GetField("indices",BindingFlags.NonPublic|BindingFlags.Instance).GetValue(witness);
                for(int i=0;i<ids.Length;i++)mapping.Add(ids[i],i);
                // Use the existing actual environment; its helper evaluates real renderer frames.
                ProductionTerrainContactTests.AssertJoinedGeometry(environment,witness,policy.loop_id,worldNative[sprint].vertices,false);
                Assert.That(join.triangles.Length,Is.GreaterThan(0));
                TestContext.WriteLine("Actual Sprint optional bank native contact PASS; source arrays immutable; yaw="+yaw+"; policy="+policy.id);

                Mesh World(Mesh mesh,Matrix4x4 frame)
                {
                    var p=mesh.vertices;for(int i=0;i<p.Length;i++)p[i]=frame.MultiplyPoint3x4(p[i]);
                    // Unity clears the index buffer when its format changes. Set it before
                    // copying indices so this explicit world witness retains actual faces.
                    var result=new Mesh{indexFormat=mesh.indexFormat,vertices=p,triangles=mesh.triangles,normals=mesh.normals,uv=mesh.uv,colors=mesh.colors};result.RecalculateBounds();owned.Add(result);return result;
                }
            }
            finally
            {
                // Explicit-world witness shares these meshes only until the checker returns.
                if(witnessObject!=null){var witness=witnessObject.GetComponent<ProductionCoursePresentation>();Set(witness,"skirts",null);Object.DestroyImmediate(witnessObject);}
                foreach(var mesh in owned)Object.DestroyImmediate(mesh);course?.Shutdown();environment?.Shutdown();Object.DestroyImmediate(root);
            }
        }
        private static void Set(object target,string name,object value)=>target.GetType().GetField(name,BindingFlags.NonPublic|BindingFlags.Instance).SetValue(target,value);
        private static bool Covered(Vector3 witness,Mesh mesh,Matrix4x4 frame)
        {
            var points=mesh.vertices;var indices=mesh.triangles;
            double Cross(Vector3 a,Vector3 b,Vector3 p)=>((double)b.x-a.x)*((double)p.z-a.z)-((double)b.z-a.z)*((double)p.x-a.x);
            for(int i=0;i<indices.Length;i+=3)
            {
                var a=frame.MultiplyPoint3x4(points[indices[i]]);var b=frame.MultiplyPoint3x4(points[indices[i+1]]);var c=frame.MultiplyPoint3x4(points[indices[i+2]]);
                double area=Cross(a,b,c);if(Math.Abs(area)<1e-12)continue;double sign=area<0?-1:1;
                if(sign*Cross(a,b,witness)>=-1e-9&&sign*Cross(b,c,witness)>=-1e-9&&sign*Cross(c,a,witness)>=-1e-9)return true;
            }
            return false;
        }
    }
}
