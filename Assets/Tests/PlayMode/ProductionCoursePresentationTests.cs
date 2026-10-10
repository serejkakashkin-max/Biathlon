using System;
using System.Collections;
using System.IO;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class ProductionCoursePresentationTests
    {
        private static ProductionVenueEnvironment Environment(GameObject root,ProductionEnvironmentFixture fixture)
        {
            var cameraObject=new GameObject("Course join test camera");cameraObject.transform.SetParent(root.transform,false);
            var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
            var feed=root.AddComponent<RacePresentationFeed>();var environment=root.AddComponent<ProductionVenueEnvironment>();
            environment.Initialize(fixture.Plan,fixture.Recipe,fixture.Read,feed,camera);return environment;
        }
        private static float BoundaryDistance(Vector3 point,Vector3[] ribbon,bool closed)
        {
            float best=float.PositiveInfinity;
            for(int side=0;side<2;side++)for(int row=side+2;row<ribbon.Length;row+=2)Check(ribbon[row-2],ribbon[row]);
            if(!closed){Check(ribbon[0],ribbon[1]);Check(ribbon[ribbon.Length-2],ribbon[ribbon.Length-1]);}
            return best;
            void Check(Vector3 a,Vector3 b)
            {var delta=b-a;float t=delta.sqrMagnitude==0?0:Mathf.Clamp01(Vector3.Dot(point-a,delta)/delta.sqrMagnitude);best=Mathf.Min(best,Vector3.Distance(point,a+delta*t));}
        }
        private static VenueAuthoringPlan Production()
            =>new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
        private static void CheckSurface(ProductionCoursePresentation view,VenueAuthoringPlan plan,int index)
        {
            string id=plan.Visual.Get(index).LoopId;var path=plan.Visual.Get(id);var loop=plan.Track.GetLoop(id);
            var mesh=view.LoopMesh(id);var vertices=mesh.vertices;var uv=mesh.uv;var colors=mesh.colors;
            Assert.That(view.LoopId(index),Is.EqualTo(id));Assert.That(view.LoopMesh(index),Is.SameAs(mesh));
            Assert.That(view.LoopRenderer(id).sharedMaterial,Is.SameAs(view.GroomedMaterial));
            Assert.That(vertices.Length,Is.GreaterThanOrEqualTo(4));Assert.That(vertices.Length%2,Is.Zero);
            Assert.That(mesh.triangles.Length,Is.EqualTo((vertices.Length/2-1)*6));
            foreach(int vertex in mesh.triangles)Assert.That(vertex,Is.InRange(0,vertices.Length-1));
            for(int row=0;row<vertices.Length;row+=2)
            {
                double distance=Math.Min(loop.Length.Meters,uv[row].y);var point=path.Sample(distance);
                var authored=new Vector3((float)point.X,(float)point.Y,(float)point.Z);var centre=(vertices[row]+vertices[row+1])*.5f;
                Assert.That(Vector3.Distance(centre,authored),Is.LessThan(.002f),"No vertical offset or invented course centre may enter the ribbon.");
                Assert.That(vertices[row].y,Is.EqualTo(authored.y).Within(.002f));Assert.That(vertices[row+1].y,Is.EqualTo(authored.y).Within(.002f));
                Assert.That(uv[row].x,Is.Zero);Assert.That(uv[row+1].x,Is.EqualTo(Vector3.Distance(vertices[row],vertices[row+1])).Within(1e-6));
                Assert.That(colors[row],Is.EqualTo(new Color(0,1,0,0)));Assert.That(colors[row+1],Is.EqualTo(colors[row]));
                Assert.That(mesh.bounds.Contains(centre),Is.True);
                foreach(var vertex in new[]{vertices[row],vertices[row+1]})
                    Assert.That(float.IsFinite(vertex.x)&&float.IsFinite(vertex.y)&&float.IsFinite(vertex.z),Is.True);
            }
            if(loop.Closed)
            {
                Assert.That(vertices[vertices.Length-2],Is.EqualTo(vertices[0]));Assert.That(vertices[vertices.Length-1],Is.EqualTo(vertices[1]));
                Assert.That(mesh.normals[mesh.vertexCount-2],Is.EqualTo(mesh.normals[0]));Assert.That(mesh.normals[mesh.vertexCount-1],Is.EqualTo(mesh.normals[1]));
                Assert.That(uv[uv.Length-2].y,Is.EqualTo((float)loop.Length.Meters),"Closing the geometry must not reset longitudinal texture distance.");
            }
        }
        [UnityTest] public IEnumerator StableLoopSurfacesKeepActualHeightWidthAndIndependentTopology()
        {
            var plan=PathPresentationFixture.Plan();string track=plan.Track.ToJson(),visual=plan.Visual.ToJson(),paths=plan.Paths.ToJson();
            var root=new GameObject("Authored course adapter test");root.transform.SetPositionAndRotation(new Vector3(200,70,-80),Quaternion.Euler(0,45,0));
            var template=Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend");float templateBlend=template.GetFloat("_TerrainBlend");var templateColor=template.GetColor("_PackedColor");
            try
            {
                var view=root.AddComponent<ProductionCoursePresentation>();view.Initialize(plan);
                Assert.That(view.Initialized,Is.True);Assert.That(view.LoopCount,Is.EqualTo(plan.Visual.Count));
                Assert.That(view.VenueId,Is.EqualTo(plan.Manifest.Id));Assert.That(view.TrackSnapshotJson,Is.EqualTo(track));Assert.That(view.VisualSnapshotJson,Is.EqualTo(visual));
                for(int i=0;i<view.LoopCount;i++)CheckSurface(view,plan,i);
                var a=view.LoopMesh("mat_1");var b=view.LoopMesh("mat_2");Assert.That(a,Is.Not.SameAs(b));
                foreach(var vertex in a.vertices)Assert.That(vertex.y,Is.EqualTo(1));foreach(var vertex in b.vertices)Assert.That(vertex.y,Is.EqualTo(3));
                var renderer=view.LoopRenderer("mat_1");Assert.That(renderer.transform.position.sqrMagnitude,Is.LessThan(1e-8));
                Assert.That(Quaternion.Angle(renderer.transform.rotation,Quaternion.identity),Is.LessThan(.001f));
                renderer.gameObject.name="Arbitrary presentation name";Assert.That(view.LoopRenderer("mat_1"),Is.SameAs(renderer),"Stable mapping does not depend on object names.");
                Assert.That(view.GroomedMaterial,Is.Not.SameAs(template));Assert.That(view.GroomedMaterial.shader.name,Is.EqualTo("Biathlon/Snow"));
                var blend=TerrainBlendDefinition.Load();Assert.That(view.GroomedMaterial.GetFloat("_TerrainBlend"),Is.EqualTo(1));
                Assert.That(view.GroomedMaterial.GetFloat("_GroomSpacing"),Is.EqualTo(blend.groom_spacing_m));Assert.That(view.GroomedMaterial.GetColor("_PackedColor"),Is.EqualTo(blend.packed_color));
                Assert.That(template.GetFloat("_TerrainBlend"),Is.EqualTo(templateBlend));Assert.That(template.GetColor("_PackedColor"),Is.EqualTo(templateColor));
                Assert.That(plan.Track.ToJson(),Is.EqualTo(track));Assert.That(plan.Visual.ToJson(),Is.EqualTo(visual));Assert.That(plan.Paths.ToJson(),Is.EqualTo(paths));
                Assert.Throws<ArgumentException>(()=>view.LoopMesh("foreign_loop"));Assert.Throws<InvalidOperationException>(()=>view.Initialize(plan));
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            }
            finally{root.GetComponent<ProductionCoursePresentation>()?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ProductionCourseIncludesEveryActualLoopAndClosedSeamAtSourceHeight()
        {
            var plan=Production();var root=new GameObject("Production full course surfaces test");string track=plan.Track.ToJson(),visual=plan.Visual.ToJson();
            try
            {
                var view=root.AddComponent<ProductionCoursePresentation>();view.Initialize(plan);
                Assert.That(view.LoopCount,Is.EqualTo(plan.Visual.Count));Assert.That(view.LoopCount,Is.GreaterThanOrEqualTo(34));
                bool elevated=false,open=false,closed=false;
                for(int i=0;i<view.LoopCount;i++)
                {
                    CheckSurface(view,plan,i);var mesh=view.LoopMesh(i);elevated|=mesh.bounds.max.y>2;
                    if(plan.Track.GetLoop(view.LoopId(i)).Closed)closed=true;else open=true;
                    for(int j=0;j<i;j++)Assert.That(mesh,Is.Not.SameAs(view.LoopMesh(j)));
                }
                Assert.That(elevated&&open&&closed,Is.True,"Actual course surfaces include the highland, private/open paths and closed loops.");
                Assert.That(view.LoopMesh(plan.StartApproach.LoopId),Is.Not.Null);
                Assert.That(plan.Track.ToJson(),Is.EqualTo(track));Assert.That(plan.Visual.ToJson(),Is.EqualTo(visual));
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            }
            finally{root.GetComponent<ProductionCoursePresentation>()?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator OwnedResourcesSurviveDisableAndAreReleasedOnShutdownAndDestruction()
        {
            var plan=PathPresentationFixture.Plan();var root=new GameObject("Course owned resource test");var view=root.AddComponent<ProductionCoursePresentation>();
            try
            {
                view.Initialize(plan);var mesh=view.LoopMesh(0);var material=view.GroomedMaterial;var renderer=view.LoopRenderer(view.LoopId(0));
                view.enabled=false;Assert.That(renderer.gameObject.activeInHierarchy,Is.False);Assert.That(mesh!=null&&material!=null,Is.True);
                view.enabled=true;Assert.That(renderer.gameObject.activeInHierarchy,Is.True);view.Shutdown();
                Assert.That(view.Initialized,Is.False);Assert.That(view.LoopCount,Is.Zero);Assert.That(view.TrackSnapshotJson,Is.Null);
                Assert.That(root.transform.childCount,Is.Zero);Assert.That(renderer.gameObject.activeInHierarchy,Is.False);
                yield return null;Assert.That(mesh==null&&material==null,Is.True);Assert.That(Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend"),Is.Not.Null);
                view.Initialize(plan);var nextMesh=view.LoopMesh(0);var nextMaterial=view.GroomedMaterial;
                Object.Destroy(root);yield return null;yield return null;Assert.That(nextMesh==null&&nextMaterial==null,Is.True);
            }
            finally{if(root!=null){view.Shutdown();Object.Destroy(root);}}
        }
        [Test] public void MissingBindingsAndInvalidRootRejectBeforeCreatingObjects()
        {
            var plan=PathPresentationFixture.Plan();var root=new GameObject("Rejected course binding test");
            try
            {
                var view=root.AddComponent<ProductionCoursePresentation>();
                Assert.Throws<ArgumentNullException>(()=>view.Initialize(null));
                Assert.Throws<FileNotFoundException>(()=>view.Initialize(plan,"Biathlon/missing_course_material"));
                Assert.Throws<FileNotFoundException>(()=>view.Initialize(plan,blendResource:"Biathlon/missing_course_blend"));
                root.transform.localScale=Vector3.one*2;Assert.Throws<ArgumentException>(()=>view.Initialize(plan));root.transform.localScale=Vector3.one;
                view.enabled=false;Assert.Throws<ArgumentException>(()=>view.Initialize(plan));view.enabled=true;
                Assert.That(root.transform.childCount,Is.Zero);Assert.That(view.Initialized,Is.False);Assert.That(view.LoopCount,Is.Zero);Assert.That(view.GroomedMaterial,Is.Null);
            }
            finally{Object.DestroyImmediate(root);}
        }
        [UnityTest] public IEnumerator LateMeshPreparationFailureRollsBackBeforeExposureAndAllowsRetry()
        {
            var resources=PathPresentationFixture.Resources();
            resources["track"]=Regex.Replace(resources["track"],"(\\\"id\\\"\\s*:\\s*\\\"exit\\\"\\s*,\\s*\\\"length_m\\\"\\s*:\\s*)60","${1}200000");
            resources["visual"]=resources["visual"].Replace("\"distance_m\":60","\"distance_m\":200000");
            var plan=PathPresentationFixture.Plan(resources);var root=new GameObject("Late course preparation rejection test");
            try
            {
                var view=root.AddComponent<ProductionCoursePresentation>();Assert.Throws<ArgumentException>(()=>view.Initialize(plan));
                Assert.That(view.Initialized,Is.False);Assert.That(view.LoopCount,Is.Zero);Assert.That(root.transform.childCount,Is.Zero);Assert.That(view.GroomedMaterial,Is.Null);
                yield return null;view.Initialize(PathPresentationFixture.Plan());Assert.That(view.Initialized,Is.True);
            }
            finally{root.GetComponent<ProductionCoursePresentation>()?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator SnowJoinsFollowWholeTerrainEdgesWithoutChangingAnySourceSurface()
        {
            var fixture=new ProductionEnvironmentFixture(p=>-.35f-.02f*Mathf.Abs(p.x+4)-.01f*Mathf.Abs(p.z-3));
            var root=new GameObject("Actual terrain snow join geometry test");root.transform.SetPositionAndRotation(new Vector3(200,70,-80),Quaternion.Euler(0,45,0));
            ProductionVenueEnvironment environment=null;ProductionCoursePresentation view=null;
            try
            {
                environment=Environment(root,fixture);view=root.AddComponent<ProductionCoursePresentation>();view.Initialize(fixture.Plan);
                string track=fixture.Plan.Track.ToJson(),visual=fixture.Plan.Visual.ToJson(),paths=fixture.Plan.Paths.ToJson(),recipe=fixture.Recipe.ToJson();
                var ribbons=new Vector3[view.LoopCount][];var ribbonIndices=new int[view.LoopCount][];var ribbonUv=new Vector2[view.LoopCount][];
                var terrain=new Vector3[environment.TerrainTileCount][];
                for(int i=0;i<view.LoopCount;i++){ribbons[i]=view.LoopMesh(i).vertices;ribbonIndices[i]=view.LoopMesh(i).triangles;ribbonUv[i]=view.LoopMesh(i).uv;}
                for(int i=0;i<terrain.Length;i++)terrain[i]=environment.TileMesh(i).vertices;
                view.JoinGround(environment,fixture.Recipe.terrain.grid_spacing_m);
                Assert.That(view.GroundJoined,Is.True);Assert.That(view.MaximumEdgeGapMeters,Is.GreaterThan(.345f));
                for(int i=0;i<view.LoopCount;i++)
                {
                    string id=view.LoopId(i);bool closed=fixture.Plan.Track.GetLoop(id).Closed;var mesh=view.SkirtMesh(id);var vertices=mesh.vertices;var indices=mesh.triangles;var colors=mesh.colors;
                    Assert.That(mesh,Is.Not.SameAs(view.LoopMesh(i)));Assert.That(indices.Length,Is.GreaterThan(0));
                    Assert.That(view.SkirtRenderer(id).sharedMaterial,Is.SameAs(view.GroomedMaterial));
                    Assert.That(view.SkirtRenderer(id).transform.position.sqrMagnitude,Is.LessThan(1e-8f),"A rotated parent preserves authored world origin within float transform precision.");
                    ProductionTerrainContactTests.AssertJoinedGeometry(environment,view,id,ribbons[i],closed);
                    CollectionAssert.AreEqual(ribbons[i],view.LoopMesh(i).vertices);CollectionAssert.AreEqual(ribbonIndices[i],view.LoopMesh(i).triangles);CollectionAssert.AreEqual(ribbonUv[i],view.LoopMesh(i).uv);
                    foreach(var normal in mesh.normals)Assert.That(float.IsFinite(normal.x)&&float.IsFinite(normal.y)&&float.IsFinite(normal.z),Is.True);
                }
                for(int i=0;i<terrain.Length;i++)CollectionAssert.AreEqual(terrain[i],environment.TileMesh(i).vertices);
                Assert.That(fixture.Plan.Track.ToJson(),Is.EqualTo(track));Assert.That(fixture.Plan.Visual.ToJson(),Is.EqualTo(visual));Assert.That(fixture.Plan.Paths.ToJson(),Is.EqualTo(paths));Assert.That(fixture.Recipe.ToJson(),Is.EqualTo(recipe));
                Assert.Throws<InvalidOperationException>(()=>view.JoinGround(environment,fixture.Recipe.terrain.grid_spacing_m));
                yield return null;Assert.That(root.GetComponentsInChildren<Collider>(true),Is.Empty);
            }
            finally{view?.Shutdown();environment?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator SnowJoinResourcesFollowCourseLifecycleAndLeaveTerrainOwnedSeparately()
        {
            var fixture=new ProductionEnvironmentFixture(p=>-.6f);var root=new GameObject("Snow join ownership test");ProductionVenueEnvironment environment=null;
            ProductionCoursePresentation view=null;
            try
            {
                environment=Environment(root,fixture);view=root.AddComponent<ProductionCoursePresentation>();view.Initialize(fixture.Plan);view.JoinGround(environment,fixture.Recipe.terrain.grid_spacing_m);
                var mesh=view.SkirtMesh(view.LoopId(0));var renderer=view.SkirtRenderer(view.LoopId(0));var terrain=environment.TileMesh(0);
                view.enabled=false;Assert.That(renderer.gameObject.activeInHierarchy,Is.False);Assert.That(mesh!=null,Is.True);
                view.enabled=true;Assert.That(renderer.gameObject.activeInHierarchy,Is.True);view.Shutdown();
                Assert.That(view.GroundJoined,Is.False);Assert.That(view.MaximumEdgeGapMeters,Is.Zero);Assert.That(renderer.gameObject.activeInHierarchy,Is.False);
                Assert.That(environment.Initialized&&terrain!=null,Is.True);yield return null;Assert.That(mesh==null,Is.True);
                view.Initialize(fixture.Plan);view.JoinGround(environment,fixture.Recipe.terrain.grid_spacing_m);var next=view.SkirtMesh(view.LoopId(0));
                Object.Destroy(view);yield return null;yield return null;Assert.That(next==null,Is.True);Assert.That(environment.Initialized&&terrain!=null,Is.True);
            }
            finally{if(view!=null)view.Shutdown();environment?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator InvalidOrBuriedGroundDoesNotPublishPartialJoinsAndCanRetry()
        {
            var buried=new ProductionEnvironmentFixture(p=>.2f);var safe=new ProductionEnvironmentFixture(p=>-.4f);
            var root=new GameObject("Rejected snow join test");var groundRoot=new GameObject("Rejected snow join actual ground");ProductionVenueEnvironment environment=null,valid=null;
            ProductionCoursePresentation view=null;
            try
            {
                view=root.AddComponent<ProductionCoursePresentation>();Assert.Throws<InvalidOperationException>(()=>view.JoinGround(null,8));view.Initialize(buried.Plan);
                var mesh=view.LoopMesh(0);int children=root.transform.childCount;environment=Environment(root,buried);
                Assert.Throws<ArgumentNullException>(()=>view.JoinGround(null,8));Assert.Throws<ArgumentException>(()=>view.JoinGround(environment,0));
                Assert.Throws<ArgumentException>(()=>view.JoinGround(environment,8,new Vector2(float.NaN,0)));
                Assert.Throws<ArgumentException>(()=>view.JoinGround(environment,16));
                Assert.Throws<ArgumentException>(()=>view.JoinGround(environment,8,environment.TerrainGridOrigin+Vector2.one));
                int preparedChildren=root.transform.childCount;
                Assert.Throws<ArgumentException>(()=>view.JoinGround(environment,buried.Recipe.terrain.grid_spacing_m));
                Assert.That(view.GroundJoined,Is.False);Assert.That(view.MaximumEdgeGapMeters,Is.Zero);Assert.That(view.LoopMesh(0),Is.SameAs(mesh));
                Assert.That(root.transform.childCount,Is.EqualTo(preparedChildren));Assert.That(children,Is.EqualTo(1));
                Assert.Throws<InvalidOperationException>(()=>view.SkirtMesh(view.LoopId(0)));
                environment.Shutdown();yield return null;
                valid=Environment(groundRoot,safe);view.JoinGround(valid,safe.Recipe.terrain.grid_spacing_m);Assert.That(view.GroundJoined,Is.True);
            }
            finally{view?.Shutdown();environment?.Shutdown();valid?.Shutdown();Object.Destroy(root);Object.Destroy(groundRoot);}
        }
    }
}
