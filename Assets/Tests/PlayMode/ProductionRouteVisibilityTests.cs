using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Biathlon.Domain;
using Biathlon.Simulation;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using Object=UnityEngine.Object;

namespace Biathlon.PlayModeTests
{
    public class ProductionRouteVisibilityTests
    {
        private static VenueAuthoringPlan Production()=>new VenueAuthoringPlan(VenueManifest.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/manifest")),ProductionVenueHost.ReadResource);
        private static RaceRoute Route(VenueAuthoringPlan plan,int format)=>new RaceRoute(plan.Track,plan.Formats.Get(format).Route,plan.Formats.Get(format).Profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
        [TestCase("sprint")]
        [TestCase("pursuit")]
        [TestCase("mass_start")]
        public void ReplacedRangeBypassDoesNotDrawTheUnusedPenaltySideHairpin(string format)
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var plan=Production();string before=Snapshots(plan);
            var profile=CompetitionProfile.FromJson(ProductionVenueHost.ReadResource(prefix+format+"_profile"));
            var route=new RaceRoute(plan.Track,RaceRoutePlan.FromJson(ProductionVenueHost.ReadResource(prefix+format+"_route")),profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            var root=new GameObject("Unused range bypass regression");
            try
            {
                var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route,sharedApron:plan.SharedApron);
                double lastApproach=0;
                for(int lane=0;lane<plan.Paths.Count;lane++)for(int slice=0;slice<plan.Paths.GetLane(lane).SliceCount;slice++)
                {var path=plan.Paths.GetLane(lane).GetSlice(slice);if(path.LoopId==plan.Paths.MainLoopId)lastApproach=Math.Max(lastApproach,path.End.Meters);}
                int omitted=0;
                for(int panel=0;panel<course.NativeRowCount(plan.Paths.MainLoopId)-1;panel++)
                    if(course.NativeDistance(plan.Paths.MainLoopId,panel)>=lastApproach&&course.NativeDistance(plan.Paths.MainLoopId,panel+1)<=plan.Paths.Exit.Meters)
                    {Assert.That(course.PanelVisible(plan.Paths.MainLoopId,panel),Is.False,"The logical range clock interval is replaced by real lane paths, not an extra visible hairpin.");omitted++;}
                Assert.That(omitted,Is.GreaterThan(10));
                Assert.That(course.LoopMesh(plan.Track.Penalty.LoopId).triangles,Is.EqualTo(course.NativeLoopMesh(plan.Track.Penalty.LoopId).triangles));
                Assert.That(course.LoopMesh(plan.StartApproach.LoopId).triangles,Is.EqualTo(course.NativeLoopMesh(plan.StartApproach.LoopId).triangles));
                Assert.That(Snapshots(plan),Is.EqualTo(before));
            }
            finally{Object.DestroyImmediate(root);}
        }
        [Test]
        public void ARealNonShootingLapRetainsItsMainBypassDespiteOtherLapsVisitingTheRange()
        {
            const string prefix="Biathlon/Venues/woodland_basin_v1/";
            var manifest=VenueManifest.FromJson(ProductionVenueHost.ReadResource(prefix+"manifest"));
            var unboundManifest=Regex.Replace(manifest.ToJson(),",\\\"format_bindings_resource\\\":\\\"[^\\\"]*\\\"","");
            var plan=new VenueAuthoringPlan(VenueManifest.FromJson(unboundManifest),ProductionVenueHost.ReadResource);
            Assert.That(plan.Formats,Is.Null,"The control uses an explicitly different route family, not a changed bound live route.");
            var profile=CompetitionProfile.FromJson(Regex.Replace(ProductionVenueHost.ReadResource(prefix+"mass_start_profile"),"\\\"distance_m\\\"\\s*:\\s*[0-9.]+","\"distance_m\":4800.025"));
            var laps=new List<string>();
            for(int i=1;i<=5;i++)laps.Add("{\"id\":\"lap_"+i+"\",\"slices\":[{\"loop_id\":\"main\",\"start_m\":0,\"end_m\":960.005}]}");
            var definition=RaceRoutePlan.FromJson("{\"schema_version\":2,\"id\":\"range_bypass_control_v1\",\"track_id\":\""+plan.Track.Id+"\",\"competition_profile_id\":\""+profile.Id+"\",\"laps\":["+string.Join(",",laps)+"]}");
            var route=new RaceRoute(plan.Track,definition,profile.ToRouteRequirements(),plan.Junctions,plan.Connections);
            Assert.That(route.GetSection(4).HasRange,Is.True);Assert.That(profile.ToRouteRequirements().RequiresRange(4),Is.False);
            var root=new GameObject("Non-shooting bypass negative control");
            try
            {
                var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route);
                SameArrays(course.NativeLoopMesh(plan.Paths.MainLoopId),course.LoopMesh(plan.Paths.MainLoopId));
                PaidCoverage(course,plan,route);
            }
            finally{Object.DestroyImmediate(root);}
        }
        private static string Snapshots(VenueAuthoringPlan plan)=>plan.Track.ToJson()+plan.Visual.ToJson()+plan.Paths.ToJson()+plan.Formats.ToJson()+plan.StartApproach.ToJson()+plan.StartContinuation.ToJson()+plan.Occupancy.ToJson();
        private static void SameArrays(Mesh a,Mesh b)
        {CollectionAssert.AreEqual(a.vertices,b.vertices);CollectionAssert.AreEqual(a.uv,b.uv);CollectionAssert.AreEqual(a.colors,b.colors);CollectionAssert.AreEqual(a.normals,b.normals);CollectionAssert.AreEqual(a.triangles,b.triangles);}
        private sealed class MeshArrays
        {
            private readonly Vector3[] vertices,normals;private readonly Vector2[] uv;private readonly Color[] colors;private readonly int[] indices;
            internal MeshArrays(Mesh mesh){vertices=mesh.vertices;normals=mesh.normals;uv=mesh.uv;colors=mesh.colors;indices=mesh.triangles;}
            internal void AssertUnchanged(Mesh mesh)
            {CollectionAssert.AreEqual(vertices,mesh.vertices);CollectionAssert.AreEqual(normals,mesh.normals);CollectionAssert.AreEqual(uv,mesh.uv);CollectionAssert.AreEqual(colors,mesh.colors);CollectionAssert.AreEqual(indices,mesh.triangles);}
        }
        private static void ExactRenderedTerrainBorders(ProductionVenueEnvironment environment,ProductionEnvironmentDefinition recipe,Matrix4x4? sharedFrame=null)
        {
            Assert.That(environment.TerrainTileCount,Is.EqualTo(recipe.terrain.tiles.Length));var filters=environment.GetComponentsInChildren<MeshFilter>(true);
            var borders=new Dictionary<Vector2,Vector3>();int shared=0;
            for(int tile=0;tile<environment.TerrainTileCount;tile++)
            {
                var source=recipe.terrain.tiles[tile];var mesh=environment.TileMesh(tile);MeshRenderer renderer=null;
                foreach(var filter in filters)if(filter.sharedMesh==mesh){renderer=filter.GetComponent<MeshRenderer>();break;}
                Assert.That(renderer,Is.Not.Null);var matrix=renderer.localToWorldMatrix;
                var expectedFrame=sharedFrame??Matrix4x4.identity;
                Assert.That(matrix,Is.EqualTo(expectedFrame),"All terrain buffers share the exact renderer frame; the existing yaw31 fixture still requires identity.");
                var points=mesh.vertices;int columns=(int)Math.Ceiling(source.size.x/recipe.terrain.grid_spacing_m),rows=(int)Math.Ceiling(source.size.z/recipe.terrain.grid_spacing_m);
                Assert.That(points.Length,Is.EqualTo((columns+1)*(rows+1)));
                for(int z=0;z<=rows;z++)for(int x=0;x<=columns;x++)
                {
                    var actualBuffer=points[z*(columns+1)+x];var originalLocal=new Vector3((float)x/columns*source.size.x-source.size.x*.5f,0,(float)z/rows*source.size.z-source.size.z*.5f);
                    var expected=source.center+originalLocal;expected.y=environment.SampleGround(expected);
                    Assert.That(actualBuffer,Is.EqualTo(expected),"The absolute buffer preserves original Build vertices plus the authored tile center, including every original terrain height.");
                    if(x!=0&&x!=columns&&z!=0&&z!=rows)continue;
                    var actual=matrix.MultiplyPoint3x4(actualBuffer);Assert.That(actual,Is.EqualTo(expectedFrame.MultiplyPoint3x4(expected)));var key=new Vector2(expected.x,expected.z);
                    if(borders.TryGetValue(key,out var prior)){Assert.That(actual,Is.EqualTo(prior),"Shared rendered tile borders must coincide exactly, without a gap/overlap tolerance.");shared++;}else borders.Add(key,actual);
                }
            }
            Assert.That(shared,Is.GreaterThan(0));
        }
        private static void PaidCoverage(ProductionCoursePresentation course,VenueAuthoringPlan plan,RaceRoute route)
        {
            var requirements=CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
            var masks=new Dictionary<string,bool[]>(StringComparer.Ordinal);
            var paidLoops=new HashSet<string>(StringComparer.Ordinal){plan.StartApproach.LoopId,plan.Track.Penalty.LoopId};
            var routeLoops=new HashSet<string>(StringComparer.Ordinal);
            for(int section=0;section<route.SectionCount;section++){var occurrence=route.GetSection(section);paidLoops.Add(occurrence.LoopId);routeLoops.Add(occurrence.LoopId);}
            for(int lane=0;lane<plan.Paths.Count;lane++)for(int slice=0;slice<plan.Paths.GetLane(lane).SliceCount;slice++)paidLoops.Add(plan.Paths.GetLane(lane).GetSlice(slice).LoopId);
            Assert.That(course.VisibleLoopCount,Is.EqualTo(paidLoops.Count));
            for(int loop=0;loop<course.LoopCount;loop++)
            {
                string id=course.LoopId(loop);var native=course.NativeLoopMesh(id);var draw=course.LoopMesh(id);
                var selected=ProductionTerrainContactTests.DrawnSourcePanels(native,draw);masks.Add(id,selected);
                for(int panel=0;panel<selected.Length;panel++)
                {
                    bool expected=PaidPanel(id,course.NativeDistance(id,panel),course.NativeDistance(id,panel+1));
                    Assert.That(course.PanelVisible(id,panel),Is.EqualTo(selected[panel]));
                    Assert.That(selected[panel],Is.EqualTo(expected),"Every drawn/excluded original face must match the current typed paid union: "+id+" panel "+panel);
                }
                Assert.That(course.LoopRenderer(id).enabled,Is.EqualTo(draw.triangles.Length>0));
            }
            for(int section=0;section<route.SectionCount;section++)
            {
                var occurrence=route.GetSection(section);
                if(PhysicalVisit(occurrence))
                {
                    if(occurrence.LoopStart.Meters<plan.Paths.Entry.Meters)Cover(occurrence.LoopId,occurrence.LoopStart.Meters,plan.Paths.Entry.Meters);
                    if(plan.Paths.Exit.Meters<occurrence.LoopEnd.Meters)Cover(occurrence.LoopId,plan.Paths.Exit.Meters,occurrence.LoopEnd.Meters);
                }
                else Cover(occurrence.LoopId,occurrence.LoopStart.Meters,occurrence.LoopEnd.Meters);
            }
            for(int lane=0;lane<plan.Paths.Count;lane++)for(int slice=0;slice<plan.Paths.GetLane(lane).SliceCount;slice++)
            {var occurrence=plan.Paths.GetLane(lane).GetSlice(slice);Cover(occurrence.LoopId,occurrence.Start.Meters,occurrence.End.Meters);}
            // Every configured private path is available, including lanes not presently occupied.
            var auxiliary=new HashSet<string>(StringComparer.Ordinal){plan.StartApproach.LoopId,plan.Track.Penalty.LoopId};
            for(int lane=0;lane<plan.Paths.Count;lane++)for(int slice=0;slice<plan.Paths.GetLane(lane).SliceCount;slice++)
            {var occurrence=plan.Paths.GetLane(lane).GetSlice(slice);if(occurrence.LoopId!=plan.Paths.MainLoopId)auxiliary.Add(occurrence.LoopId);}
            foreach(string id in auxiliary){SameArrays(course.NativeLoopMesh(id),course.LoopMesh(id));Assert.That(course.LoopRenderer(id).enabled,Is.True);}
            Assert.That(plan.LaneCount,Is.EqualTo(plan.Paths.Count));
            bool PaidPanel(string id,double first,double last)
            {
                if(id==plan.StartApproach.LoopId||id==plan.Track.Penalty.LoopId)return true;
                for(int section=0;section<route.SectionCount;section++)
                {
                    var occurrence=route.GetSection(section);
                    if(occurrence.LoopId==id&&last>occurrence.LoopStart.Meters&&first<occurrence.LoopEnd.Meters&&
                        (!PhysicalVisit(occurrence)||first<plan.Paths.Entry.Meters||last>plan.Paths.Exit.Meters))return true;
                }
                for(int lane=0;lane<plan.Paths.Count;lane++)for(int slice=0;slice<plan.Paths.GetLane(lane).SliceCount;slice++)
                {var occurrence=plan.Paths.GetLane(lane).GetSlice(slice);if(occurrence.LoopId==id&&(!routeLoops.Contains(id)||last>occurrence.Start.Meters&&first<occurrence.End.Meters))return true;}
                return false;
            }
            bool PhysicalVisit(RouteSection section)=>section.HasRange&&requirements.RequiresRange(section.LapIndex)&&section.LoopId==plan.Paths.MainLoopId;
            void Cover(string id,double start,double end)
            {
                int covered=0;var selected=masks[id];
                for(int panel=0;panel<selected.Length;panel++)if(course.NativeDistance(id,panel+1)>start&&course.NativeDistance(id,panel)<end)
                {Assert.That(selected[panel],Is.True,"No paid route/path occurrence may lose its original face: "+id+" "+start+".."+end);covered++;}
                Assert.That(covered,Is.GreaterThan(0));
            }
        }
        [UnityTest] public IEnumerator NullRouteRetainsEveryNativeSurfaceAndPreciseBuilderRows()
        {
            var plan=Production();string before=Snapshots(plan);var root=new GameObject("Null route native presentation");
            try
            {
                var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan);
                Assert.That(course.PresentationRouteId,Is.Null);Assert.That(course.PresentationRouteSnapshotJson,Is.Null);Assert.That(course.PresentationRequirementsSnapshotJson,Is.Null);
                Assert.That(course.VisibleLoopCount,Is.EqualTo(course.LoopCount));
                for(int loop=0;loop<course.LoopCount;loop++)
                {
                    string id=course.LoopId(loop);Assert.That(course.LoopMesh(id),Is.SameAs(course.NativeLoopMesh(id)));
                    var rows=course.NativeRowCount(id);Assert.That(rows*2,Is.EqualTo(course.LoopMesh(id).vertexCount));
                    var expectedRows=new SortedSet<double>{0,plan.Track.GetLoop(id).Length.Meters};
                    var authored=plan.Visual.Get(id);for(int knot=0;knot<authored.Count;knot++)expectedRows.Add(authored.Get(knot).Distance);
                    var typedLoop=plan.Track.GetLoop(id);for(int segment=0;segment<typedLoop.SegmentCount;segment++)expectedRows.Add(typedLoop.GetSegment(segment).StartDistance.Meters);
                    int steps=(int)Math.Ceiling(typedLoop.Length.Meters/plan.Visual.SampleSpacing);for(int sample=1;sample<steps;sample++)expectedRows.Add(typedLoop.Length.Meters*sample/steps);
                    Assert.That(rows,Is.EqualTo(expectedRows.Count));int expectedRow=0;foreach(double distance in expectedRows)Assert.That(course.NativeDistance(id,expectedRow++),Is.EqualTo(distance));
                    Assert.That(course.NativeDistance(id,0),Is.Zero);Assert.That(course.NativeDistance(id,rows-1),Is.EqualTo(plan.Track.GetLoop(id).Length.Meters));
                    for(int row=1;row<rows;row++){Assert.That(course.NativeDistance(id,row),Is.GreaterThan(course.NativeDistance(id,row-1)));Assert.That(course.PanelVisible(id,row-1),Is.True);}
                }
                Assert.That(Snapshots(plan),Is.EqualTo(before));
                course.Shutdown();yield return null;Assert.That(course.VisibleLoopCount,Is.Zero);Assert.That(root.transform.childCount,Is.Zero);
            }
            finally{root.GetComponent<ProductionCoursePresentation>()?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ActualFormatsSelectOriginalPaidFacesAndPreserveAllTypedAuxiliaryPaths()
        {
            var plan=Production();string before=Snapshots(plan);var root=new GameObject("Actual route source preservation");
            var baselineObject=new GameObject("Original venue-wide baseline");ProductionCoursePresentation baseline=null;
            try
            {
                baseline=baselineObject.AddComponent<ProductionCoursePresentation>();baseline.Initialize(plan);
                for(int format=0;format<plan.Formats.Count;format++)
                {
                    var route=Route(plan,format);string routeBefore=route.PlanSnapshotJson,requirementsBefore=route.RequirementsSnapshotJson;
                    var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:route);
                    Assert.That(course.PresentationRouteId,Is.EqualTo(route.Id));Assert.That(course.PresentationRouteSnapshotJson,Is.EqualTo(routeBefore));Assert.That(course.PresentationRequirementsSnapshotJson,Is.EqualTo(requirementsBefore));
                    PaidCoverage(course,plan,route);
                    for(int loop=0;loop<course.LoopCount;loop++){string id=course.LoopId(loop);SameArrays(baseline.NativeLoopMesh(id),course.NativeLoopMesh(id));}
                    bool sprint=plan.Formats.Get(format).Profile.Format==CompetitionFormat.Sprint;
                    bool mass=plan.Formats.Get(format).Profile.Format==CompetitionFormat.MassStart;
                    Assert.That(course.LoopRenderer("sprint_extension").enabled,Is.EqualTo(sprint));Assert.That(course.LoopRenderer("pursuit_shortcut").enabled,Is.EqualTo(!mass&&!sprint));
                    if(sprint)
                    {
                        int excluded=0;for(int panel=0;panel<course.NativeRowCount("main")-1;panel++)if(!course.PanelVisible("main",panel))excluded++;
                        Assert.That(excluded,Is.GreaterThan(0),"The current sprint detour must exclude its untravelled main faces.");
                        Assert.That(course.PanelVisible("main",0),Is.True);Assert.That(course.PanelVisible("main",course.NativeRowCount("main")-2),Is.True);
                    }
                    Assert.That(route.PlanSnapshotJson,Is.EqualTo(routeBefore));Assert.That(route.RequirementsSnapshotJson,Is.EqualTo(requirementsBefore));Assert.That(Snapshots(plan),Is.EqualTo(before));
                    course.Shutdown();Object.Destroy(course);yield return null;
                }
            }
            finally{baseline?.Shutdown();foreach(var course in root.GetComponents<ProductionCoursePresentation>())course.Shutdown();Object.Destroy(root);Object.Destroy(baselineObject);}
        }
        [UnityTest] public IEnumerator SameIdGeometryPlanAndRequirementsRejectAtomicallyAndAllowRetry()
        {
            var plan=Production();var binding=plan.Formats.Get(0);var requirements=binding.Profile.ToRouteRequirements();
            var changedTrack=TrackDefinition.FromJson(Regex.Replace(plan.Track.ToJson(),"\\\"width_m\\\"\\s*:\\s*6(?=[,}])","\"width_m\":5"));
            var changedJunctions=PhysicalJunctionDefinition.FromJson(plan.Junctions.ToJson(),changedTrack);
            var changedConnections=PhysicalConnectionDefinition.FromJson(plan.Connections.ToJson(),changedJunctions);
            var wrongTrack=new RaceRoute(changedTrack,binding.Route,requirements,changedJunctions,changedConnections);
            var changedPlan=RaceRoutePlan.FromJson(Regex.Replace(binding.Route.ToJson(),"\\\"id\\\"\\s*:\\s*\\\"lap_1\\\"","\"id\":\"changed_lap\""));
            var wrongPlan=new RaceRoute(plan.Track,changedPlan,requirements,plan.Junctions,plan.Connections);
            var changedRequirements=CompetitionRouteRequirements.FromJson(Regex.Replace(requirements.ToJson(),"\\\"range_required_on_laps\\\"\\s*:\\s*\\[[^\\]]*\\]","\"range_required_on_laps\":[]"));
            var wrongRequirements=new RaceRoute(plan.Track,binding.Route,changedRequirements,plan.Junctions,plan.Connections);
            Assert.That(wrongTrack.TrackId,Is.EqualTo(plan.Track.Id));Assert.That(wrongPlan.Id,Is.EqualTo(binding.Route.Id));Assert.That(wrongRequirements.CompetitionProfileId,Is.EqualTo(binding.Profile.Id));
            Assert.That(wrongTrack.TrackSnapshotJson,Is.Not.EqualTo(plan.Track.ToJson()));Assert.That(wrongPlan.PlanSnapshotJson,Is.Not.EqualTo(binding.Route.ToJson()));Assert.That(wrongRequirements.RequirementsSnapshotJson,Is.Not.EqualTo(requirements.ToJson()));
            var root=new GameObject("Exact route binding atomicity");
            try
            {
                var course=root.AddComponent<ProductionCoursePresentation>();
                foreach(var invalid in new[]{wrongTrack,wrongPlan,wrongRequirements})
                {
                    Assert.Throws<ArgumentException>(()=>course.Initialize(plan,route:invalid));Assert.That(course.Initialized,Is.False);Assert.That(course.LoopCount,Is.Zero);
                    Assert.That(course.VisibleLoopCount,Is.Zero);Assert.That(course.GroomedMaterial,Is.Null);Assert.That(course.PresentationRouteId,Is.Null);Assert.That(root.transform.childCount,Is.Zero);
                }
                course.Initialize(plan,route:Route(plan,0));Assert.That(course.Initialized,Is.True);course.Shutdown();yield return null;Assert.That(root.transform.childCount,Is.Zero);
            }
            finally{root.GetComponent<ProductionCoursePresentation>()?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator AllActualLiveUnionsKeepStrictWholeFaceContactCutsAndClosedSeam()
        {
            var plan=Production();string before=Snapshots(plan);var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));string recipeBefore=recipe.ToJson();
            var root=new GameObject("Actual selected route terrain contact");root.transform.SetPositionAndRotation(new Vector3(83,21,-53),Quaternion.Euler(0,31,0));
            ProductionVenueEnvironment environment=null;
            try
            {
                var cameraObject=new GameObject("Selected route contact camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var feed=root.AddComponent<RacePresentationFeed>();environment=root.AddComponent<ProductionVenueEnvironment>();environment.Initialize(plan,recipe,ProductionVenueHost.ReadResource,feed,camera);
                ExactRenderedTerrainBorders(environment,recipe);
                var terrain=new MeshArrays[environment.TerrainTileCount];for(int tile=0;tile<terrain.Length;tile++)terrain[tile]=new MeshArrays(environment.TileMesh(tile));
                for(int format=0;format<plan.Formats.Count;format++)
                {
                    var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:Route(plan,format));PaidCoverage(course,plan,Route(plan,format));
                    if(format==0)
                    {
                        var renderer=course.LoopRenderer("main");var filter=renderer.GetComponent<MeshFilter>();var paid=filter.sharedMesh;
                        filter.sharedMesh=course.LoopMesh(plan.Track.Penalty.LoopId);Assert.Throws<ArgumentException>(()=>course.JoinGround(environment,recipe.terrain.grid_spacing_m));Assert.That(course.GroundJoined,Is.False);filter.sharedMesh=paid;
                        renderer.transform.localPosition=Vector3.right;Assert.Throws<ArgumentException>(()=>course.JoinGround(environment,recipe.terrain.grid_spacing_m));Assert.That(course.GroundJoined,Is.False);renderer.transform.localPosition=Vector3.zero;
                    }
                    course.JoinGround(environment,recipe.terrain.grid_spacing_m);var world=new ProductionTerrainContactTests.JoinedWorld(environment,course);
                    ProductionTerrainContactTests.AssertDrawnRibbonGroundClearance(environment,course,world);
                    for(int loop=0;loop<course.LoopCount;loop++)
                    {
                        string id=course.LoopId(loop);if(course.LoopMesh(id).triangles.Length==0)
                        {Assert.That(course.SkirtMesh(id).triangles,Is.Empty);Assert.That(course.SkirtRenderer(id).enabled,Is.False);continue;}
                        ProductionTerrainContactTests.AssertJoinedGeometry(environment,course,id,course.NativeLoopMesh(id).vertices,plan.Track.GetLoop(id).Closed,world);
                    }
                    var original=course.NativeLoopMesh("main");var drawn=course.LoopMesh("main");Assert.That(drawn.vertices[0],Is.EqualTo(drawn.vertices[drawn.vertexCount-2]));
                    Assert.That(drawn.uv[0],Is.EqualTo(original.uv[0]));Assert.That(drawn.uv[drawn.vertexCount-2],Is.EqualTo(original.uv[original.vertexCount-2]));
                    for(int tile=0;tile<terrain.Length;tile++)terrain[tile].AssertUnchanged(environment.TileMesh(tile));
                    Assert.That(Snapshots(plan),Is.EqualTo(before));Assert.That(recipe.ToJson(),Is.EqualTo(recipeBefore));course.Shutdown();Object.Destroy(course);yield return null;
                    Assert.That(environment.Initialized,Is.True,"Course shutdown preserves environment-owned terrain.");
                }
            }
            finally{foreach(var course in root.GetComponents<ProductionCoursePresentation>())course.Shutdown();environment?.Shutdown();Object.Destroy(root);}
        }
        [UnityTest] public IEnumerator ActualYaw45SharedFrameKeepsRenderedContactAndRejectsMovedChildren()
        {
            var plan=Production();string before=Snapshots(plan);var recipe=ProductionEnvironmentDefinition.FromJson(ProductionVenueHost.ReadResource("Biathlon/Venues/woodland_basin_v1/environment"));string recipeBefore=recipe.ToJson();
            var root=new GameObject("Actual yaw45 shared renderer frame");root.transform.SetPositionAndRotation(new Vector3(200,70,-80),Quaternion.Euler(0,45,0));
            ProductionVenueEnvironment environment=null;
            try
            {
                var cameraObject=new GameObject("Yaw45 renderer contact camera");cameraObject.transform.SetParent(root.transform,false);var camera=cameraObject.AddComponent<Camera>();camera.enabled=false;
                var feed=root.AddComponent<RacePresentationFeed>();environment=root.AddComponent<ProductionVenueEnvironment>();environment.Initialize(plan,recipe,ProductionVenueHost.ReadResource,feed,camera);
                var terrain=new MeshArrays[environment.TerrainTileCount];for(int tile=0;tile<terrain.Length;tile++)terrain[tile]=new MeshArrays(environment.TileMesh(tile));
                for(int format=0;format<plan.Formats.Count;format++)
                {
                    var course=root.AddComponent<ProductionCoursePresentation>();course.Initialize(plan,route:Route(plan,format));PaidCoverage(course,plan,Route(plan,format));
                    var frame=course.LoopRenderer("main").localToWorldMatrix;ExactRenderedTerrainBorders(environment,recipe,frame);
                    if(format==0)
                    {
                        var renderer=course.LoopRenderer("main");renderer.transform.localPosition=Vector3.right*1e-6f;
                        Assert.Throws<ArgumentException>(()=>course.JoinGround(environment,recipe.terrain.grid_spacing_m));Assert.That(course.GroundJoined,Is.False);renderer.transform.localPosition=Vector3.zero;
                        Assert.That(renderer.localToWorldMatrix,Is.EqualTo(frame));
                        var filters=environment.GetComponentsInChildren<MeshFilter>(true);MeshRenderer moved=null;
                        foreach(var filter in filters)if(filter.sharedMesh==environment.TileMesh(0)){moved=filter.GetComponent<MeshRenderer>();break;}
                        Assert.That(moved,Is.Not.Null);moved.transform.localPosition=Vector3.right;
                        Assert.Throws<ArgumentException>(()=>course.JoinGround(environment,recipe.terrain.grid_spacing_m));Assert.That(course.GroundJoined,Is.False);moved.transform.localPosition=Vector3.zero;
                        Assert.That(moved.localToWorldMatrix,Is.EqualTo(frame));
                    }
                    course.JoinGround(environment,recipe.terrain.grid_spacing_m);var world=new ProductionTerrainContactTests.JoinedWorld(environment,course);
                    ProductionTerrainContactTests.AssertDrawnRibbonGroundClearance(environment,course,world);
                    for(int loop=0;loop<course.LoopCount;loop++)
                    {
                        string id=course.LoopId(loop);Assert.That(course.LoopRenderer(id).localToWorldMatrix,Is.EqualTo(frame));Assert.That(course.SkirtRenderer(id).localToWorldMatrix,Is.EqualTo(frame));
                        if(course.LoopMesh(id).triangles.Length==0){Assert.That(course.SkirtMesh(id).triangles,Is.Empty);continue;}
                        ProductionTerrainContactTests.AssertJoinedGeometry(environment,course,id,course.NativeLoopMesh(id).vertices,plan.Track.GetLoop(id).Closed,world);
                    }
                    ExactRenderedTerrainBorders(environment,recipe,frame);for(int tile=0;tile<terrain.Length;tile++)terrain[tile].AssertUnchanged(environment.TileMesh(tile));
                    Assert.That(Snapshots(plan),Is.EqualTo(before));Assert.That(recipe.ToJson(),Is.EqualTo(recipeBefore));course.Shutdown();Object.Destroy(course);yield return null;
                }
            }
            finally{foreach(var course in root.GetComponents<ProductionCoursePresentation>())course.Shutdown();environment?.Shutdown();Object.Destroy(root);}
        }
    }
}
