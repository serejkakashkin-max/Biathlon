using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Owned groomed surfaces for separate authored loops. Rendering never changes course data.</summary>
    public sealed class ProductionCoursePresentation : MonoBehaviour
    {
        private GameObject generated;
        private readonly List<SnowEdgeDiagnostic> edgeDiagnostics=new List<SnowEdgeDiagnostic>();
        public IReadOnlyList<SnowEdgeDiagnostic> EdgeDiagnostics=>edgeDiagnostics;
        private readonly List<Mesh> ownedMeshes=new List<Mesh>();
        private readonly Dictionary<string,int> indices=new Dictionary<string,int>(StringComparer.Ordinal);
        private string[] loopIds;
        private bool[] closedLoops;
        private bool[] roundBranchEnds;
        private Mesh[] meshes;
        private Mesh[] nativeMeshes;
        private bool[][] bankPanels;
        private float moundHeight,moundSpacing;
        private ProductionSnowShoulderDefinition.NaturalProfile natural;
        public int RimClearanceAdjustments {get;private set;}
        public int CoveredCurtainFaces {get;private set;}
        public string SnowBankPolicyId {get;private set;}
        private Mesh[] drawMeshes;
        private MeshRenderer apronRenderer,apronSkirtRenderer;
        private Mesh apronSkirt;
        public RangeApronDrawPlan Apron {get;private set;}
        public Mesh ApronSkirt=>apronSkirt;
        public MeshRenderer ApronRenderer=>apronRenderer;
        public MeshRenderer ApronSkirtRenderer=>apronSkirtRenderer;
        private double[][] nativeDistances;
        private bool[][] visiblePanels;
        private MeshRenderer[] renderers;
        private Matrix4x4 authoredRenderFrame;
        private Mesh[] skirts;
        private MeshRenderer[] skirtRenderers;
        private Material material;
        public bool Initialized=>generated!=null;
        public string VenueId {get;private set;}
        public string TrackSnapshotJson {get;private set;}
        public string VisualSnapshotJson {get;private set;}
        public string PresentationRouteId {get;private set;}
        public string PresentationRouteSnapshotJson {get;private set;}
        public string PresentationRequirementsSnapshotJson {get;private set;}
        public int VisibleLoopCount {get;private set;}
        public int LoopCount=>loopIds?.Length??0;
        public Material GroomedMaterial=>material;
        public bool GroundJoined=>skirts!=null;
        public float MaximumEdgeGapMeters {get;private set;}
        public float SnowShoulderWidthMeters {get;private set;}
        public int TaperedSnowPanelCount {get;private set;}
        public int VerticalSnowPanelCount {get;private set;}
        public int RoundedSnowCutCount {get;private set;}
        public string LoopId(int index)=>loopIds[index];
        public Mesh LoopMesh(int index)=>meshes[index];
        public Mesh LoopMesh(string loopId)=>meshes[Index(loopId)];
        public Mesh NativeLoopMesh(string loopId)=>nativeMeshes[Index(loopId)];
        public Mesh DrawLoopMesh(string loopId)=>drawMeshes[Index(loopId)];
        public int NativeRowCount(string loopId)=>nativeDistances[Index(loopId)].Length;
        public double NativeDistance(string loopId,int row)=>nativeDistances[Index(loopId)][row];
        public bool PanelVisible(string loopId,int panel)=>visiblePanels[Index(loopId)][panel];
        public MeshRenderer LoopRenderer(string loopId)=>renderers[Index(loopId)];
        public Mesh SkirtMesh(string loopId)
        {if(!GroundJoined)throw new InvalidOperationException("Join the actual terrain first.");return skirts[Index(loopId)];}
        public MeshRenderer SkirtRenderer(string loopId)
        {if(!GroundJoined)throw new InvalidOperationException("Join the actual terrain first.");return skirtRenderers[Index(loopId)];}
        private int Index(string loopId)
        {
            if(loopId==null||!indices.TryGetValue(loopId,out var index))throw new ArgumentException("Unknown authored loop.",nameof(loopId));
            return index;
        }
        public void Initialize(VenueAuthoringPlan plan,
            string materialResource="Biathlon/Terrain/G2Snow/WinterBlend",string blendResource="Biathlon/terrain_blend_v1",RaceRoute route=null,PhysicalSharedApronDefinition sharedApron=null,ProductionSnowShoulderDefinition snowShoulders=null,ManualFinishRunoutDefinition finishRunout=null)
        {
            if(Initialized||ownedMeshes.Count!=0||material!=null)throw new InvalidOperationException("Initialize the production course once.");
            if(plan==null)throw new ArgumentNullException(nameof(plan));
            if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-6f)
                throw new ArgumentException("Enabled unit-scale course root required.");
            snowShoulders?.RequireBinding(plan);
            if(snowShoulders!=null&&sharedApron==null)throw new ArgumentException("Optional snow banks require the explicit shared apron owner.");
            var usage=route==null?null:RouteUsage(plan,route);
            VenueManifest.ResourceId(materialResource);VenueManifest.ResourceId(blendResource);
            var template=Resources.Load<Material>(materialResource);
            if(template==null)throw new FileNotFoundException("Missing groomed course material: "+materialResource);
            if(template.shader==null||template.shader.name!="Biathlon/Snow")throw new ArgumentException("Native snow material required.",nameof(materialResource));
            var text=Resources.Load<TextAsset>(blendResource);
            if(text==null)throw new FileNotFoundException("Missing groomed course definition: "+blendResource);
            var blend=TerrainBlendDefinition.FromJson(text.text);
            SnowShoulderWidthMeters=snowShoulders!=null&&snowShoulders.width_m>0?snowShoulders.width_m:blend.edge_width_m;
            natural=snowShoulders?.natural;moundHeight=snowShoulders?.mound_height_m??0;moundSpacing=snowShoulders?.mound_spacing_m??0;
            try
            {
                // Build every mesh before exposing any generated objects. Each stable loop owns one mesh.
                loopIds=new string[plan.Visual.Count];closedLoops=new bool[loopIds.Length];roundBranchEnds=new bool[loopIds.Length];meshes=new Mesh[loopIds.Length];renderers=new MeshRenderer[loopIds.Length];
                bankPanels=snowShoulders==null?null:new bool[loopIds.Length][];
                SnowBankPolicyId=snowShoulders?.id;nativeMeshes=new Mesh[loopIds.Length];nativeDistances=new double[loopIds.Length][];visiblePanels=new bool[loopIds.Length][];
                for(int i=0;i<loopIds.Length;i++)
                {
                    string id=plan.Visual.Get(i).LoopId;var loop=plan.Track.GetLoop(id);
                    var mesh=TrackRibbonBuilder.Build(plan.Track,plan.Visual,id,out var distances);ownedMeshes.Add(mesh);
                    Groom(mesh,loop.Closed);loopIds[i]=id;closedLoops[i]=loop.Closed;nativeMeshes[i]=mesh;nativeDistances[i]=distances;indices.Add(id,i);
                    roundBranchEnds[i]=!loop.Closed&&IsClosedRaceBranch(plan,id);
                    var selected=new bool[distances.Length-1];int count=0;
                    for(int panel=0;panel<selected.Length;panel++)
                    {
                        bool keep=usage==null;
                        if(finishRunout!=null&&route!=null)
                        {
                            var last=route.GetSection(route.SectionCount-1);
                            keep|=last.LoopId==id&&distances[panel+1]>last.LoopEnd.Meters&&
                                distances[panel]<last.LoopEnd.Meters+finishRunout.DistanceMeters;
                        }
                        if(usage!=null&&usage.TryGetValue(id,out var intervals))foreach(var interval in intervals)
                            if(distances[panel+1]>interval.Item1/1000d&&distances[panel]<interval.Item2/1000d){keep=true;break;}
                        selected[panel]=keep;if(keep)count++;
                    }
                    if(bankPanels!=null)
                    {
                        bankPanels[i]=new bool[selected.Length];
                        for(int panel=0;panel<selected.Length;panel++)bankPanels[i][panel]=snowShoulders.ContainsPanel(id,distances[panel],distances[panel+1]);
                    }
                    var surface=mesh;
                    if(natural!=null&&!IsMatPath(plan,id))
                    {surface=SnowRibbonRim.Refine(mesh,distances,loop.Closed,natural.rim_direction_window_m,natural.rim_maximum_displacement_m);ownedMeshes.Add(surface);}
                    visiblePanels[i]=selected;meshes[i]=count==selected.Length?surface:SelectDrawMesh(surface,selected);
                    if(meshes[i]!=surface)ownedMeshes.Add(meshes[i]);if(count>0)VisibleLoopCount++;
                }
                drawMeshes=meshes;
                if(sharedApron!=null)
                {
                    var paid=new Dictionary<string,Mesh>(StringComparer.Ordinal);
                    for(int i=0;i<loopIds.Length;i++)paid.Add(loopIds[i],meshes[i]);
                    // The packed apron feather retains its original material
                    // definition; wider decorative banks are a separate surface.
                    Apron=RangeApronDrawPlan.Create(sharedApron,plan,paid,blend.edge_width_m);ownedMeshes.Add(Apron.Top);
                    drawMeshes=new Mesh[meshes.Length];
                    for(int i=0;i<meshes.Length;i++){drawMeshes[i]=Apron.OutsideDrawMesh(loopIds[i],meshes[i]);ownedMeshes.Add(drawMeshes[i]);}
                }
                material=new Material(template){name=plan.Manifest.Id+" groomed course"};blend.Configure(material);
                TrackSnapshotJson=plan.Track.ToJson();VisualSnapshotJson=plan.Visual.ToJson();VenueId=plan.Manifest.Id;
                PresentationRouteId=route?.Id;PresentationRouteSnapshotJson=route?.PlanSnapshotJson;PresentationRequirementsSnapshotJson=route?.RequirementsSnapshotJson;
                generated=new GameObject("Production course "+VenueId);generated.transform.SetParent(transform,false);
                // Visual coordinates already belong to the authored world, regardless of host placement.
                generated.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                for(int i=0;i<loopIds.Length;i++)
                {
                    var go=new GameObject("Course surface "+loopIds[i]);go.transform.SetParent(generated.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=drawMeshes[i];renderers[i]=go.AddComponent<MeshRenderer>();
                    renderers[i].sharedMaterial=material;renderers[i].enabled=VisibleLoopCount==loopIds.Length||HasVisiblePanel(i);
                }
                authoredRenderFrame=renderers[0].localToWorldMatrix;
                if(Apron!=null)
                {
                    var go=new GameObject("Continuous range apron "+Apron.Id);go.transform.SetParent(generated.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=Apron.Top;apronRenderer=go.AddComponent<MeshRenderer>();apronRenderer.sharedMaterial=material;
                    ValidateRendered(Apron.Top,authoredRenderFrame,true);
                    for(int i=0;i<drawMeshes.Length;i++)ValidateRendered(drawMeshes[i],authoredRenderFrame,false);
                }
            }
            catch{Shutdown();throw;}
        }
        public void ConfigureSnowBanks(VenueAuthoringPlan plan,ProductionSnowShoulderDefinition policy)
        {
            if(!Initialized||GroundJoined||bankPanels!=null)throw new InvalidOperationException("Configure optional snow banks once before terrain joining.");
            if(plan==null||policy==null||Apron==null||plan.SharedApron==null||plan.Manifest.Id!=VenueId||
                plan.Track.ToJson()!=TrackSnapshotJson||plan.Visual.ToJson()!=VisualSnapshotJson||
                plan.SharedApron.ToJson()!=Apron.SharedApronSnapshotJson)
                throw new ArgumentException("Snow banks require the exact owned compact presentation family.");
            policy.RequireBinding(plan);var next=new bool[meshes.Length][];
            for(int i=0;i<next.Length;i++)
            {
                next[i]=new bool[visiblePanels[i].Length];
                for(int panel=0;panel<next[i].Length;panel++)next[i][panel]=policy.ContainsPanel(loopIds[i],nativeDistances[i][panel],nativeDistances[i][panel+1]);
            }
            bankPanels=next;SnowBankPolicyId=policy.id;
            if(policy.width_m>0)SnowShoulderWidthMeters=policy.width_m;
            natural=policy.natural;moundHeight=policy.mound_height_m;moundSpacing=policy.mound_spacing_m;
            if(natural!=null)for(int i=0;i<meshes.Length;i++)
            {
                if(IsMatPath(plan,loopIds[i]))continue;
                var refined=SnowRibbonRim.Refine(nativeMeshes[i],nativeDistances[i],closedLoops[i],natural.rim_direction_window_m,natural.rim_maximum_displacement_m);ownedMeshes.Add(refined);
                meshes[i]=SelectDrawMesh(refined,visiblePanels[i]);ownedMeshes.Add(meshes[i]);
                drawMeshes[i]=Apron.OutsideDrawMesh(loopIds[i],meshes[i]);ownedMeshes.Add(drawMeshes[i]);
                renderers[i].GetComponent<MeshFilter>().sharedMesh=drawMeshes[i];
            }
        }
        private static bool IsMatPath(VenueAuthoringPlan plan,string loopId)
        {if(plan.Paths!=null)for(int lane=0;lane<plan.Paths.Count;lane++)if(plan.Paths.GetLane(lane).MatAnchor.LoopId==loopId)return true;return false;}
        private static bool IsClosedRaceBranch(VenueAuthoringPlan plan,string loopId)
        {
            if(plan.Junctions==null||plan.Connections==null||IsMatPath(plan,loopId))return false;
            bool enters=false,leaves=false;var loop=plan.Track.GetLoop(loopId);
            for(int i=0;i<plan.Connections.Count;i++)
            {
                var connection=plan.Connections.Get(i);PhysicalJunctionPort from=null,to=null;
                for(int j=0;j<plan.Junctions.Count;j++)
                {
                    var junction=plan.Junctions.Get(j);if(junction.Id!=connection.JunctionId)continue;
                    for(int p=0;p<junction.PortCount;p++)
                    {var port=junction.GetPort(p);if(port.Id==connection.FromPortId)from=port;if(port.Id==connection.ToPortId)to=port;}
                    break;
                }
                if(from==null||to==null)continue;
                if(to.Anchor.LoopId==loopId&&to.Anchor.Distance.Millimeters==0&&ClosedRace(from.Anchor.LoopId))enters=true;
                if(from.Anchor.LoopId==loopId&&from.Anchor.Distance.Millimeters==loop.Length.Millimeters&&ClosedRace(to.Anchor.LoopId))leaves=true;
            }
            return enters&&leaves;
            bool ClosedRace(string id){var other=plan.Track.GetLoop(id);return other.Closed&&other.Kind==TrackLoopKind.Race;}
        }
        private bool HasVisiblePanel(int index){foreach(bool active in visiblePanels[index])if(active)return true;return false;}
        private static Mesh SelectDrawMesh(Mesh source,bool[] selected)
        {
            var original=source.triangles;var chosen=new List<int>();
            for(int i=0;i<selected.Length;i++)if(selected[i])for(int j=0;j<6;j++)chosen.Add(original[i*6+j]);
            var mesh=new Mesh{name=source.name+" live route selection",indexFormat=source.indexFormat};
            try{mesh.vertices=source.vertices;mesh.uv=source.uv;mesh.colors=source.colors;mesh.normals=source.normals;mesh.triangles=chosen.ToArray();mesh.RecalculateBounds();return mesh;}
            catch{DestroyOwned(mesh);throw;}
        }
        internal static Dictionary<string,List<(long,long)>> RouteUsage(VenueAuthoringPlan plan,RaceRoute route)
        {
            if(route.TrackId!=plan.Track.Id||route.TrackSnapshotJson!=plan.Track.ToJson())throw new ArgumentException("Presentation requires the exact live route track snapshot.",nameof(route));
            if(plan.Formats!=null)
            {
                VenueFormatBinding match=null;
                for(int i=0;i<plan.Formats.Count;i++){var candidate=plan.Formats.Get(i);if(candidate.Route.Id==route.Id&&candidate.Profile.Id==route.CompetitionProfileId)match=candidate;}
                if(match==null||match.Route.ToJson()!=route.PlanSnapshotJson||match.Profile.ToRouteRequirements().ToJson()!=route.RequirementsSnapshotJson)
                    throw new ArgumentException("Presentation requires the exact bound route plan and profile requirements, including all occurrences.",nameof(route));
            }
            else
            {
                var rebuilt=new RaceRoute(plan.Track,RaceRoutePlan.FromJson(route.PlanSnapshotJson),CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson),plan.Junctions,plan.Connections);
                if(rebuilt.SectionCount!=route.SectionCount)throw new ArgumentException("Live route occurrences differ.",nameof(route));
                for(int i=0;i<route.SectionCount;i++)
                {var a=route.GetSection(i);var b=rebuilt.GetSection(i);if(a.LapIndex!=b.LapIndex||a.LapId!=b.LapId||a.LoopId!=b.LoopId||!a.Start.Equals(b.Start)||!a.LoopStart.Equals(b.LoopStart)||!a.LoopEnd.Equals(b.LoopEnd)||a.EnterConnectionId!=b.EnterConnectionId||a.HasRange!=b.HasRange)throw new ArgumentException("Live route occurrences differ.",nameof(route));}
            }
            var usage=new Dictionary<string,List<(long,long)>>(StringComparer.Ordinal);var baseLoops=new HashSet<string>(StringComparer.Ordinal);
            var requirements=CompetitionRouteRequirements.FromJson(route.RequirementsSnapshotJson);
            for(int i=0;i<route.SectionCount;i++)
            {
                var section=route.GetSection(i);baseLoops.Add(section.LoopId);
                // A required physical range visit replaces this logical clock interval.
                // Add the actual lane slices below, rather than also drawing the unused
                // main-loop bypass (whose tight hairpin can intersect its own ribbon).
                if(plan.Paths!=null&&section.HasRange&&requirements.RequiresRange(section.LapIndex)&&section.LoopId==plan.Paths.MainLoopId)
                {
                    if(section.LoopStart.Millimeters>plan.Paths.Entry.Millimeters||section.LoopEnd.Millimeters<plan.Paths.Exit.Millimeters)
                        throw new ArgumentException("Physical range overlay must be wholly contained by its route slice.",nameof(route));
                    if(section.LoopStart.Millimeters<plan.Paths.Entry.Millimeters)Add(section.LoopId,section.LoopStart.Millimeters,plan.Paths.Entry.Millimeters);
                    if(plan.Paths.Exit.Millimeters<section.LoopEnd.Millimeters)Add(section.LoopId,plan.Paths.Exit.Millimeters,section.LoopEnd.Millimeters);
                }
                else Add(section.LoopId,section.LoopStart.Millimeters,section.LoopEnd.Millimeters);
            }
            if(plan.Paths!=null)
                for(int lane=0;lane<plan.Paths.Count;lane++)for(int s=0;s<plan.Paths.GetLane(lane).SliceCount;s++)
                {var slice=plan.Paths.GetLane(lane).GetSlice(s);if(baseLoops.Contains(slice.LoopId))Add(slice.LoopId,slice.Start.Millimeters,slice.End.Millimeters);else Whole(slice.LoopId);}
            else if(plan.Lanes!=null)for(int lane=0;lane<plan.Lanes.Count;lane++)Whole(plan.Lanes.GetLane(lane));
            Whole(plan.Track.Penalty.LoopId);if(plan.StartApproach!=null)Whole(plan.StartApproach.LoopId);
            foreach(var list in usage.Values)
            {
                list.Sort((a,b)=>a.Item1.CompareTo(b.Item1));int written=0;
                foreach(var interval in list.ToArray())
                    if(written>0&&interval.Item1<=list[written-1].Item2)list[written-1]=(list[written-1].Item1,Math.Max(list[written-1].Item2,interval.Item2));else list[written++]=interval;
                list.RemoveRange(written,list.Count-written);
            }
            return usage;
            void Whole(string id)=>Add(id,0,plan.Track.GetLoop(id).Length.Millimeters);
            void Add(string id,long first,long last)
            {
                if(first<0||last<=first||last>plan.Track.GetLoop(id).Length.Millimeters)throw new ArgumentException("Positive native presentation interval required.",nameof(route));
                if(!usage.TryGetValue(id,out var list)){list=new List<(long,long)>();usage.Add(id,list);}list.Add((first,last));
            }
        }
        /// <summary>Cold snow shoulders on straight segments outside every groomed ribbon.
        /// Bends and overlapping corridors retain their exact terrain-following vertical joins.
        /// Tapered faces are clipped against actual terrain triangles and their contact contour.
        /// No ribbon vertex, terrain sample or authoritative corridor width is changed.</summary>
        public void JoinGround(ProductionVenueEnvironment environment,float terrainGridSpacing)
            =>JoinGround(environment,terrainGridSpacing,environment!=null&&environment.Initialized?environment.TerrainGridOrigin:Vector2.zero);
        public void JoinGround(ProductionVenueEnvironment environment,float terrainGridSpacing,Vector2 terrainGridOrigin)
        {
            if(!Initialized)throw new InvalidOperationException("Initialize the course before joining terrain.");
            if(GroundJoined)throw new InvalidOperationException("The course already owns its terrain joins.");
            if(environment==null)throw new ArgumentNullException(nameof(environment));
            if(!environment.Initialized||environment.VenueId!=VenueId)throw new ArgumentException("Initialized terrain for the same venue required.",nameof(environment));
            if(!float.IsFinite(terrainGridSpacing)||terrainGridSpacing<.1f||terrainGridSpacing>100||
                !float.IsFinite(terrainGridOrigin.x)||!float.IsFinite(terrainGridOrigin.y))throw new ArgumentException("Actual terrain grid required.");
            if(terrainGridSpacing!=environment.TerrainGridSpacing||!terrainGridOrigin.Equals(environment.TerrainGridOrigin))
                throw new ArgumentException("Join sampling must match the environment's exact terrain grid.");
            for(int i=0;i<meshes.Length;i++)
            {
                var renderer=renderers[i];var filter=renderer==null?null:renderer.GetComponent<MeshFilter>();
                if(filter==null||filter.sharedMesh!=drawMeshes[i])throw new ArgumentException("The actual groomed mesh must retain its owned renderer.");
                // A transformed owner can leave a small composite-matrix residual even
                // after the generated anchor is placed at authored world zero. Require
                // the exact original shared frame and untouched child pose, not an
                // assumed identity matrix or an approximate matrix comparison.
                var pose=renderer.transform;
                if(pose.parent!=generated.transform||!pose.localPosition.Equals(Vector3.zero)||
                    !pose.localRotation.Equals(Quaternion.identity)||!pose.localScale.Equals(Vector3.one)||
                    !renderer.localToWorldMatrix.Equals(authoredRenderFrame))
                    throw new ArgumentException("Groomed renderers must retain their exact owned shared render frame for terrain joins.");
            }
            if((Apron==null)!=(environment.Apron==null)||Apron!=null&&!ReferenceEquals(Apron,environment.Apron))
                throw new ArgumentException("Course and terrain require the exact shared apron draw plan.");
            if(Apron!=null)RequireOwnedFrame(apronRenderer,Apron.Top);
            var prepared=new Mesh[meshes.Length];var objects=new GameObject[meshes.Length];
            var priorMeshes=(Mesh[])meshes.Clone();var priorDrawMeshes=(Mesh[])drawMeshes.Clone();
            int priorOwned=ownedMeshes.Count,priorRimAdjustments=RimClearanceAdjustments;
            Mesh preparedApron=null;GameObject apronObject=null;float maximum=0;int tapered=0,vertical=0;
            try
            {
                var ground=new JoinGroundSurface(environment,terrainGridSpacing,terrainGridOrigin,authoredRenderFrame);
                if(natural!=null)ConstrainRimsToActualTerrain(ground);
                Dictionary<Mesh,bool[]> selection=null;
                if(PresentationRouteId!=null){selection=new Dictionary<Mesh,bool[]>();for(int i=0;i<meshes.Length;i++)selection.Add(meshes[i],visiblePanels[i]);}
                Mesh[] groomedMeshes=meshes;
                if(Apron!=null)
                {
                    groomedMeshes=new Mesh[meshes.Length+1];Array.Copy(meshes,groomedMeshes,meshes.Length);groomedMeshes[meshes.Length]=Apron.Top;
                    if(selection!=null)selection.Add(Apron.Top,new[]{true});
                }
                Dictionary<Mesh,bool[]> curved=null;
                if(bankPanels!=null){curved=new Dictionary<Mesh,bool[]>();for(int i=0;i<meshes.Length;i++)curved.Add(meshes[i],bankPanels[i]);if(Apron!=null)curved.Add(Apron.Top,new[]{false});}
                var groomed=new JoinRibbonSurface(groomedMeshes,terrainGridSpacing,terrainGridOrigin,selection,curved);
                for(int i=0;i<meshes.Length;i++)
                {
                    prepared[i]=BuildBankedSkirtCore(meshes[i],closedLoops[i],ground,groomed,SnowShoulderWidthMeters,out float gap,out int slopeCount,out int wallCount,selection==null?null:visiblePanels[i],bankPanels==null?null:bankPanels[i],moundHeight,moundSpacing,(side,panel,reason,taperedPanel,wall)=>edgeDiagnostics.Add(new SnowEdgeDiagnostic {
                        loop_id=loopIds[i],side=side,panel=panel,start_m=nativeDistances[i][panel],end_m=nativeDistances[i][panel+1],
                        width_m=SnowShoulderWidthMeters,reason=reason,tapered=taperedPanel,vertical_emitted=wall}),natural,roundBranchEnds[i]);
                    if(Apron!=null)
                    {
                        var original=prepared[i];prepared[i]=Apron.OutsideJoinMesh(loopIds[i],original);DestroyOwned(original);
                        ValidateRendered(prepared[i],authoredRenderFrame,false);
                    }
                    maximum=Mathf.Max(maximum,gap);tapered+=slopeCount;vertical+=wallCount;
                }
                if(Apron!=null)
                {
                    preparedApron=BuildVisibleSkirt(Apron.Top,false,ground,groomed,SnowShoulderWidthMeters,out float gap,out int slopeCount,out int wallCount,null);
                    ValidateRendered(preparedApron,authoredRenderFrame,false);
                    maximum=Mathf.Max(maximum,gap);tapered+=slopeCount;vertical+=wallCount;
                }
                if(natural!=null)
                {
                    var coverage=new SnowJoinCoverage();int covered=0;
                    bool rounded=false;foreach(var edge in edgeDiagnostics)rounded|=edge.side==2&&edge.tapered;
                    if(rounded)
                    {
                        var roofs=new List<Mesh>(prepared);if(preparedApron!=null)roofs.Add(preparedApron);coverage.CaptureRoofs(roofs);
                        for(int i=0;i<prepared.Length;i++)
                        {var prior=prepared[i];prepared[i]=coverage.RetainExteriorCurtains(prior,out int removed);covered+=removed;if(prepared[i]!=prior)DestroyOwned(prior);}
                    }
                    for(int i=0;i<prepared.Length;i++)
                    {var prior=prepared[i];prepared[i]=coverage.RetainUncovered(prior,out int removed);covered+=removed;if(prepared[i]!=prior)DestroyOwned(prior);}
                    if(preparedApron!=null)
                    {var prior=preparedApron;preparedApron=coverage.RetainUncovered(prior,out int removed);covered+=removed;if(preparedApron!=prior)DestroyOwned(prior);}
                    CoveredCurtainFaces=covered;
                }
                RoundedSnowCutCount=0;foreach(var edge in edgeDiagnostics)if(edge.side==2&&edge.tapered)RoundedSnowCutCount++;
                // A failed preparation does not alter the existing groomed surfaces or publish partial joins.
                var nextRenderers=new MeshRenderer[meshes.Length];
                for(int i=0;i<meshes.Length;i++)
                {
                    var go=new GameObject("Course snow edge "+loopIds[i]);objects[i]=go;go.SetActive(false);go.transform.SetParent(generated.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=prepared[i];nextRenderers[i]=go.AddComponent<MeshRenderer>();nextRenderers[i].sharedMaterial=material;nextRenderers[i].enabled=HasVisiblePanel(i);
                }
                if(Apron!=null)
                {
                    apronObject=new GameObject("Continuous range apron outer join "+Apron.Id);apronObject.SetActive(false);apronObject.transform.SetParent(generated.transform,false);
                    apronObject.AddComponent<MeshFilter>().sharedMesh=preparedApron;apronSkirtRenderer=apronObject.AddComponent<MeshRenderer>();apronSkirtRenderer.sharedMaterial=material;
                    RequireOwnedFrame(apronSkirtRenderer,preparedApron);
                }
                skirts=prepared;skirtRenderers=nextRenderers;apronSkirt=preparedApron;MaximumEdgeGapMeters=maximum;
                TaperedSnowPanelCount=tapered;VerticalSnowPanelCount=vertical;
                foreach(var mesh in prepared)ownedMeshes.Add(mesh);
                foreach(var go in objects)go.SetActive(true);
                if(preparedApron!=null)ownedMeshes.Add(preparedApron);if(apronObject!=null)apronObject.SetActive(true);
            }
            catch
            {
                foreach(var go in objects)DestroyOwned(go);
                foreach(var mesh in prepared)DestroyOwned(mesh);DestroyOwned(apronObject);DestroyOwned(preparedApron);apronSkirtRenderer=null;
                meshes=priorMeshes;drawMeshes=priorDrawMeshes;
                for(int i=0;i<renderers.Length;i++)renderers[i].GetComponent<MeshFilter>().sharedMesh=drawMeshes[i];
                for(int i=ownedMeshes.Count-1;i>=priorOwned;i--){if(Array.IndexOf(prepared,ownedMeshes[i])<0&&ownedMeshes[i]!=preparedApron)DestroyOwned(ownedMeshes[i]);ownedMeshes.RemoveAt(i);}
                skirts=null;skirtRenderers=null;apronSkirt=null;RimClearanceAdjustments=priorRimAdjustments;CoveredCurtainFaces=RoundedSnowCutCount=0;edgeDiagnostics.Clear();
                throw;
            }
        }
        private void ConstrainRimsToActualTerrain(JoinGroundSurface ground)
        {
            for(int loop=0;loop<meshes.Length;loop++)
            {
                if(Fits(meshes[loop]))continue;
                var original=SelectDrawMesh(nativeMeshes[loop],visiblePanels[loop]);
                if(!Fits(original)){DestroyOwned(original);throw new ArgumentException("Original ribbon lacks required actual terrain clearance: "+loopIds[loop]);}
                float low=0,high=1;Mesh best=original;
                // Bounded search changes only the presentation rotation limit. Every
                // trial keeps the exact original centers/heights/full road widths.
                for(int step=0;step<8;step++)
                {
                    float scale=(low+high)*.5f;
                    var full=SnowRibbonRim.Refine(nativeMeshes[loop],nativeDistances[loop],closedLoops[loop],natural.rim_direction_window_m,natural.rim_maximum_displacement_m*scale);
                    var selected=SelectDrawMesh(full,visiblePanels[loop]);DestroyOwned(full);
                    if(Fits(selected)){DestroyOwned(best);best=selected;low=scale;}else{DestroyOwned(selected);high=scale;}
                }
                meshes[loop]=best;ownedMeshes.Add(best);
                drawMeshes[loop]=Apron.OutsideDrawMesh(loopIds[loop],best);ownedMeshes.Add(drawMeshes[loop]);
                renderers[loop].GetComponent<MeshFilter>().sharedMesh=drawMeshes[loop];RimClearanceAdjustments++;
            }
            bool Fits(Mesh mesh)
            {
                var points=mesh.vertices;var indices=mesh.triangles;
                for(int face=0;face<indices.Length;face+=3)
                {
                    var triangle=new JoinGroundTriangle(points[indices[face]],points[indices[face+1]],points[indices[face+2]]);
                    double expected=Math.Abs(triangle.Area);if(expected<1e-12)continue;double covered=0;
                    foreach(var terrain in ground.Over(triangle.A,triangle.B,triangle.C))
                    {
                        double sign=terrain.Area<0?-1:1;var polygon=new List<JoinVertex>{triangle.A,triangle.B,triangle.C};
                        polygon=ClipJoin(polygon,p=>sign*Cross(terrain.A,terrain.B,p));polygon=ClipJoin(polygon,p=>sign*Cross(terrain.B,terrain.C,p));polygon=ClipJoin(polygon,p=>sign*Cross(terrain.C,terrain.A,p));
                        for(int i=1;i<polygon.Count-1;i++)covered+=Math.Abs(Cross(polygon[0],polygon[i],polygon[i+1]));
                        foreach(var point in polygon)if(triangle.Height(point)-terrain.Height(point)<natural.rim_minimum_clearance_m)return false;
                    }
                    if(Math.Abs(covered-expected)>Math.Max(1e-6,expected*1e-5))return false;
                }
                return true;
            }
        }
        private void RequireOwnedFrame(MeshRenderer renderer,Mesh mesh)
        {
            var filter=renderer==null?null:renderer.GetComponent<MeshFilter>();var pose=renderer==null?null:renderer.transform;
            if(filter==null||filter.sharedMesh!=mesh||pose.parent!=generated.transform||!pose.localPosition.Equals(Vector3.zero)||
                !pose.localRotation.Equals(Quaternion.identity)||!pose.localScale.Equals(Vector3.one)||!renderer.localToWorldMatrix.Equals(authoredRenderFrame))
                throw new ArgumentException("Apron must retain its owned exact common native renderer frame.");
        }
        private static void ValidateRendered(Mesh mesh,Matrix4x4 frame,bool top)
        {
            var points=mesh.vertices;var faces=mesh.triangles;
            for(int i=0;i<faces.Length;i+=3)
            {
                var a=frame.MultiplyPoint3x4(points[faces[i]]);var b=frame.MultiplyPoint3x4(points[faces[i+1]]);var c=frame.MultiplyPoint3x4(points[faces[i+2]]);
                var normal=Vector3.Cross(b-a,c-a);
                if(!float.IsFinite(normal.x)||!float.IsFinite(normal.y)||!float.IsFinite(normal.z)||normal.sqrMagnitude<1e-16f||top&&normal.y<=1e-12f)
                    throw new ArgumentException("Apron composite requires finite nondegenerate actual rendered faces.");
            }
        }
        private readonly struct JoinVertex
        {
            internal readonly double X,Y,Z,Blend,Travel;
            internal JoinVertex(Vector3 point,double blend=0,double travel=0)
            {X=point.x;Y=point.y;Z=point.z;Blend=blend;Travel=travel;}
            private JoinVertex(double x,double y,double z,double blend,double travel)
            {X=x;Y=y;Z=z;Blend=blend;Travel=travel;}
            internal JoinVertex WithExactHeight(double height)=>new JoinVertex(X,height,Z,Blend,Travel);
            internal JoinVertex Offset(Vector3 delta)=>new JoinVertex(X+delta.x,Y+delta.y,Z+delta.z,Blend,Travel);
            internal static JoinVertex Lerp(JoinVertex a,JoinVertex b,double t)
                =>new JoinVertex(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,
                    a.Blend+(b.Blend-a.Blend)*t,a.Travel+(b.Travel-a.Travel)*t);
        }
        private readonly struct JoinGroundTriangle
        {
            internal readonly JoinVertex A,B,C;
            internal readonly double Area;
            internal readonly Color ColorA,ColorB,ColorC;
            internal readonly Vector3 NormalA,NormalB,NormalC;
            internal JoinGroundTriangle(Vector3 a,Vector3 b,Vector3 c)
            {A=new JoinVertex(a);B=new JoinVertex(b);C=new JoinVertex(c);Area=Cross(A,B,C);ColorA=ColorB=ColorC=Color.white;NormalA=NormalB=NormalC=Vector3.up;}
            internal JoinGroundTriangle(Vector3 a,Vector3 b,Vector3 c,Color ca,Color cb,Color cc,Vector3 na,Vector3 nb,Vector3 nc):this(a,b,c)
            {ColorA=ca;ColorB=cb;ColorC=cc;NormalA=na;NormalB=nb;NormalC=nc;}
            internal void Attributes(JoinVertex p,out Color color,out Vector3 normal)
            {
                float u=(float)(((p.X-A.X)*(C.Z-A.Z)-(p.Z-A.Z)*(C.X-A.X))/Area);
                float v=(float)(((B.X-A.X)*(p.Z-A.Z)-(B.Z-A.Z)*(p.X-A.X))/Area);
                color=ColorA*(1-u-v)+ColorB*u+ColorC*v;normal=(NormalA*(1-u-v)+NormalB*u+NormalC*v).normalized;
            }
            internal double Height(JoinVertex point)
            {
                double u=((point.X-A.X)*(C.Z-A.Z)-(point.Z-A.Z)*(C.X-A.X))/Area;
                double v=((B.X-A.X)*(point.Z-A.Z)-(B.Z-A.Z)*(point.X-A.X))/Area;
                return A.Y+u*(B.Y-A.Y)+v*(C.Y-A.Y);
            }
            internal bool Contains(JoinVertex point)
            {double sign=Area<0?-1:1;return sign*Cross(A,B,point)>=-1e-7&&sign*Cross(B,C,point)>=-1e-7&&sign*Cross(C,A,point)>=-1e-7;}
        }
        private static double Cross(JoinVertex a,JoinVertex b,JoinVertex c)
            =>(b.X-a.X)*(c.Z-a.Z)-(b.Z-a.Z)*(c.X-a.X);
        private static List<JoinVertex> ClipJoin(List<JoinVertex> polygon,Func<JoinVertex,double> side)
        {
            var next=new List<JoinVertex>(8);if(polygon.Count==0)return next;
            var prior=polygon[polygon.Count-1];double previous=side(prior);
            foreach(var current in polygon)
            {
                double value=side(current);
                if((previous>=0)!=(value>=0))next.Add(JoinVertex.Lerp(prior,current,previous/(previous-value)));
                if(value>=0)next.Add(current);
                prior=current;previous=value;
            }
            return next;
        }
        /// <summary>Cold index of the terrain renderer's actual geometry in its verified render frame.
        /// Sampling outside covered cells fails rather than extrapolating a terrain edge.</summary>
        private sealed class JoinGroundSurface
        {
            private readonly Dictionary<(int,int),List<JoinGroundTriangle>> cells=new Dictionary<(int,int),List<JoinGroundTriangle>>();
            internal readonly double spacing;
            internal readonly Vector2 origin;
            private readonly Matrix4x4? renderFrame;
            internal JoinGroundSurface(ProductionVenueEnvironment environment,float spacing,Vector2 origin)
                :this(environment,spacing,origin,null){}
            internal JoinGroundSurface(ProductionVenueEnvironment environment,float spacing,Vector2 origin,Matrix4x4 sharedRenderFrame)
                :this(environment,spacing,origin,(Matrix4x4?)sharedRenderFrame){}
            private JoinGroundSurface(ProductionVenueEnvironment environment,float spacing,Vector2 origin,Matrix4x4? sharedRenderFrame)
            {
                this.spacing=spacing;this.origin=origin;
                renderFrame=sharedRenderFrame;
                var filters=environment.GetComponentsInChildren<MeshFilter>(true);
                for(int tile=0;tile<environment.TerrainTileCount;tile++)
                {
                    var mesh=environment.TileMesh(tile);MeshRenderer owner=null;
                    foreach(var filter in filters)if(filter.sharedMesh==mesh){owner=filter.GetComponent<MeshRenderer>();break;}
                    if(owner==null)throw new ArgumentException("The actual terrain mesh must have its owned renderer.");
                    var vertices=mesh.vertices;var indices=mesh.triangles;var weights=mesh.colors;var normals=mesh.normals;var world=owner.localToWorldMatrix;
                    if(sharedRenderFrame.HasValue)
                    {
                        // Equal renderer matrices map terrain, ribbons and emitted joins
                        // together. Work in that exact common buffer frame to avoid
                        // projection/inverse rounding and applying the transform twice.
                        if(!world.Equals(sharedRenderFrame.Value))
                            throw new ArgumentException("Terrain and groomed surfaces must retain one exact shared render frame.");
                    }
                    else for(int i=0;i<vertices.Length;i++)vertices[i]=world.MultiplyPoint3x4(vertices[i]);
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var triangle=new JoinGroundTriangle(vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]],
                            weights[indices[i]],weights[indices[i+1]],weights[indices[i+2]],normals[indices[i]],normals[indices[i+1]],normals[indices[i+2]]);
                        if(!Finite(triangle.A)||!Finite(triangle.B)||!Finite(triangle.C)||!double.IsFinite(triangle.Area)||Math.Abs(triangle.Area)<1e-12)
                            throw new ArgumentException("Actual terrain triangles require finite positions and projected area.");
                        int x=(int)Math.Round((Math.Min(triangle.A.X,Math.Min(triangle.B.X,triangle.C.X))-origin.x)/spacing);
                        int z=(int)Math.Round((Math.Min(triangle.A.Z,Math.Min(triangle.B.Z,triangle.C.Z))-origin.y)/spacing);
                        if(!cells.TryGetValue((x,z),out var cell)){cell=new List<JoinGroundTriangle>(2);cells.Add((x,z),cell);}
                        cell.Add(triangle);if(cell.Count>2)throw new ArgumentException("Actual terrain cells must not overlap.");
                    }
                }
                bool Finite(JoinVertex p)=>double.IsFinite(p.X)&&double.IsFinite(p.Y)&&double.IsFinite(p.Z);
            }
            internal Vector3 Rendered(Vector3 point)=>renderFrame.HasValue?renderFrame.Value.MultiplyPoint3x4(point):point;
            internal IEnumerable<JoinGroundTriangle> Over(JoinVertex a,JoinVertex b,JoinVertex c)
            {
                // Adjacent cells are included for floating-point transforms at a shared grid boundary.
                int x0=(int)Math.Floor((Math.Min(a.X,Math.Min(b.X,c.X))-origin.x)/spacing)-1;
                int x1=(int)Math.Floor((Math.Max(a.X,Math.Max(b.X,c.X))-origin.x)/spacing)+1;
                int z0=(int)Math.Floor((Math.Min(a.Z,Math.Min(b.Z,c.Z))-origin.y)/spacing)-1;
                int z1=(int)Math.Floor((Math.Max(a.Z,Math.Max(b.Z,c.Z))-origin.y)/spacing)+1;
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)if(cells.TryGetValue((x,z),out var cell))foreach(var triangle in cell)yield return triangle;
            }
            internal void Attributes(Vector3 point,out Color color,out Vector3 normal)
            {
                var p=new JoinVertex(point);
                foreach(var triangle in Over(p,p,p))if(triangle.Contains(p)){triangle.Attributes(p,out color,out normal);return;}
                throw new ArgumentException("Actual terrain must cover snow attributes.");
            }
            internal float Height(Vector3 point)
            {
                var vertex=new JoinVertex(point);
                foreach(var triangle in Over(vertex,vertex,vertex))if(triangle.Contains(vertex))return (float)triangle.Height(vertex);
                throw new ArgumentException("Actual terrain must cover the complete snow shoulder. Missing point="+point.ToString("R"));
            }
        }
        private sealed class JoinRibbonSurface
        {
            private readonly Dictionary<(int,int),List<JoinGroundTriangle>> cells=new Dictionary<(int,int),List<JoinGroundTriangle>>();
            private readonly Dictionary<(int,int),List<PotentialShoulder>> shoulders=new Dictionary<(int,int),List<PotentialShoulder>>();
            private readonly Mesh[] ribbons;private readonly Dictionary<Mesh,bool[]> selection,curved;private bool shouldersReady;
            private readonly Dictionary<(Mesh,int),float[]> naturalWidths=new Dictionary<(Mesh,int),float[]>();
            internal float[] NaturalWidths(Mesh ribbon,int side)=>naturalWidths[(ribbon,side)];
            internal readonly struct PotentialShoulder
            {
                internal readonly Mesh Ribbon;internal readonly int Side,Segment,Count;internal readonly JoinGroundTriangle Face;
                internal PotentialShoulder(Mesh ribbon,int side,int segment,int count,JoinGroundTriangle face)
                {Ribbon=ribbon;Side=side;Segment=segment;Count=count;Face=face;}
            }
            private readonly double spacing;private readonly Vector2 origin;
            internal JoinRibbonSurface(Mesh[] ribbons,float spacing,Vector2 origin)
                :this(ribbons,spacing,origin,null){}
            internal JoinRibbonSurface(Mesh[] ribbons,float spacing,Vector2 origin,Dictionary<Mesh,bool[]> selection)
                :this(ribbons,spacing,origin,selection,null){}
            internal JoinRibbonSurface(Mesh[] ribbons,float spacing,Vector2 origin,Dictionary<Mesh,bool[]> selection,Dictionary<Mesh,bool[]> curved)
            {
                this.ribbons=ribbons;this.spacing=spacing;this.origin=origin;this.selection=selection;this.curved=curved;
                foreach(var ribbon in ribbons)
                {
                    var vertices=ribbon.vertices;var indices=ribbon.triangles;
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var triangle=new JoinGroundTriangle(vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]);
                        if(Math.Abs(triangle.Area)<1e-12)continue;
                        foreach(var key in Keys(triangle.A,triangle.B,triangle.C))
                        {if(!cells.TryGetValue(key,out var cell)){cell=new List<JoinGroundTriangle>();cells.Add(key,cell);}cell.Add(triangle);}
                    }
                }
            }
            internal void PreparePotentialShoulders(float width,ProductionSnowShoulderDefinition.NaturalProfile natural=null)
            {
                if(shouldersReady)return;shouldersReady=true;
                // First constrain ALL roads against the actual packed footprints. The
                // neighbor index must describe these feasible shoulders, not impossible
                // full-width candidates that would reject valid banks beside a bend.
                if(natural!=null)foreach(var ribbon in ribbons)
                {
                    var points=ribbon.vertices;int count=points.Length/2-1;
                    bool closed=points[0].Equals(points[count*2])&&points[1].Equals(points[count*2+1]);
                    for(int side=0;side<2;side++)
                    {
                        var widths=new float[count+1];var edges=new Vector3[count+1];var directions=new Vector3[count+1];
                        double travel=0,perimeter=0;for(int i=0;i<count;i++)perimeter+=Vector3.Distance(points[i*2+side],points[(i+1)*2+side]);
                        for(int row=0;row<=count;row++)
                        {
                            edges[row]=points[row*2+side];var across=edges[row]-(points[row*2]+points[row*2+1])*.5f;across.y=0;directions[row]=across.normalized;
                            if(row>0)travel+=Vector3.Distance(edges[row-1],edges[row]);
                            double phase=perimeter>0?travel/perimeter*Math.PI*2:0;
                            // Low-frequency closed variation never exceeds the allowed width.
                            widths[row]=width*(1-natural.width_variation*(.5f+.5f*(float)Math.Sin(phase*3+side)));
                        }
                        for(int pass=0;pass<8;pass++)
                        {
                            bool changed=false;
                            for(int segment=0;segment<count;segment++)
                            {
                                if(selection!=null&&!selection[ribbon][segment])continue;
                                var a=new JoinVertex(edges[segment]);var b=new JoinVertex(edges[segment+1]);
                                var oa=new JoinVertex(edges[segment]+directions[segment]*widths[segment]);var ob=new JoinVertex(edges[segment+1]+directions[segment+1]*widths[segment+1]);
                                if(Clear(a,b,oa)&&Clear(b,ob,oa))continue;
                                if(Mathf.Max(widths[segment],widths[segment+1])<=natural.minimum_width_m)continue;
                                widths[segment]=Mathf.Max(natural.minimum_width_m,widths[segment]*.5f);widths[segment+1]=Mathf.Max(natural.minimum_width_m,widths[segment+1]*.5f);changed=true;
                            }
                            if(closed)widths[0]=widths[count]=Mathf.Min(widths[0],widths[count]);
                            if(!changed)break;
                        }
                        for(int pass=0;pass<2;pass++)
                        {
                            for(int row=1;row<=count;row++)widths[row]=Mathf.Min(widths[row],widths[row-1]+Vector3.Distance(edges[row],edges[row-1])*width/natural.end_fade_m);
                            for(int row=count-1;row>=0;row--)widths[row]=Mathf.Min(widths[row],widths[row+1]+Vector3.Distance(edges[row],edges[row+1])*width/natural.end_fade_m);
                            if(closed)widths[0]=widths[count]=Mathf.Min(widths[0],widths[count]);
                        }
                        naturalWidths.Add((ribbon,side),widths);
                    }
                }
                foreach(var ribbon in ribbons)
                {
                    var points=ribbon.vertices;int count=points.Length/2-1;
                    // The shared apron keeps the legacy full-width perimeter. Its
                    // neighbor footprint must not use adaptive widths it never renders.
                    bool adaptive=natural!=null;
                    if(adaptive&&curved!=null&&curved.TryGetValue(ribbon,out var allowed))
                    {adaptive=false;foreach(bool enabled in allowed)if(enabled){adaptive=true;break;}}
                    for(int side=0;side<2;side++)for(int segment=0;segment<count;segment++)
                    {
                        if(selection!=null&&!selection[ribbon][segment])continue;
                        var a=points[segment*2+side];var b=points[(segment+1)*2+side];var delta=b-a;delta.y=0;
                        if(delta.sqrMagnitude<1e-12f)continue;
                        var normal=new Vector3(delta.z,0,-delta.x).normalized*(side==0?-1:1);var outerA=a+normal*width;var outerB=b+normal*width;
                        if(adaptive||curved!=null&&curved.TryGetValue(ribbon,out var mask)&&mask[segment])
                        {
                            var first=a-(points[segment*2]+points[segment*2+1])*.5f;first.y=0;
                            var last=b-(points[(segment+1)*2]+points[(segment+1)*2+1])*.5f;last.y=0;
                            outerA=a+first.normalized*(adaptive?naturalWidths[(ribbon,side)][segment]:width);
                            outerB=b+last.normalized*(adaptive?naturalWidths[(ribbon,side)][segment+1]:width);
                        }
                        // Buried fork rims cannot reserve an impossible shoulder and block an exposed neighbor.
                        if(adaptive&&(!Clear(new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))||
                            !Clear(new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA))))continue;
                        Capture(new JoinGroundTriangle(a,b,outerA));Capture(new JoinGroundTriangle(b,outerB,outerA));
                        void Capture(JoinGroundTriangle face)
                        {
                            var candidate=new PotentialShoulder(ribbon,side,segment,count,face);
                            foreach(var key in Keys(face.A,face.B,face.C))
                            {if(!shoulders.TryGetValue(key,out var list)){list=new List<PotentialShoulder>();shoulders.Add(key,list);}list.Add(candidate);}
                        }
                    }
                }
                bool Clear(JoinVertex a,JoinVertex b,JoinVertex c)
                {
                    foreach(var triangle in Over(a,b,c))
                    {
                        double sign=triangle.Area<0?-1:1;var overlap=new List<JoinVertex>{a,b,c};
                        overlap=ClipJoin(overlap,p=>sign*Cross(triangle.A,triangle.B,p));
                        overlap=ClipJoin(overlap,p=>sign*Cross(triangle.B,triangle.C,p));
                        overlap=ClipJoin(overlap,p=>sign*Cross(triangle.C,triangle.A,p));
                        double area=0;for(int i=1;i<overlap.Count-1;i++)area+=Math.Abs(Cross(overlap[0],overlap[i],overlap[i+1]));
                        if(area>1e-10)return false;
                    }
                    return true;
                }
            }
            internal IEnumerable<JoinGroundTriangle> OtherShoulders(Mesh ribbon,int side,int segment,bool closed,JoinVertex a,JoinVertex b,JoinVertex c)
            {
                var seen=new HashSet<PotentialShoulder>();
                foreach(var key in Keys(a,b,c))if(shoulders.TryGetValue(key,out var candidates))foreach(var candidate in candidates)
                {
                    int difference=Math.Abs(candidate.Segment-segment);
                    if(candidate.Ribbon==ribbon&&candidate.Side==side&&(difference<=1||closed&&difference==candidate.Count-1))continue;
                    if(seen.Add(candidate))yield return candidate.Face;
                }
            }
            private IEnumerable<(int,int)> Keys(JoinVertex a,JoinVertex b,JoinVertex c)
            {
                int x0=(int)Math.Floor((Math.Min(a.X,Math.Min(b.X,c.X))-origin.x)/spacing),x1=(int)Math.Floor((Math.Max(a.X,Math.Max(b.X,c.X))-origin.x)/spacing);
                int z0=(int)Math.Floor((Math.Min(a.Z,Math.Min(b.Z,c.Z))-origin.y)/spacing),z1=(int)Math.Floor((Math.Max(a.Z,Math.Max(b.Z,c.Z))-origin.y)/spacing);
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)yield return (x,z);
            }
            internal List<JoinGroundTriangle> Over(JoinVertex a,JoinVertex b,JoinVertex c)
            {
                var result=new List<JoinGroundTriangle>();var seen=new HashSet<JoinGroundTriangle>();
                foreach(var key in Keys(a,b,c))if(cells.TryGetValue(key,out var cell))foreach(var triangle in cell)if(seen.Add(triangle))result.Add(triangle);
                return result;
            }
        }
        private static Mesh BuildSkirt(Mesh ribbon,bool closed,JoinGroundSurface ground,JoinRibbonSurface groomed,float width,
            out float maximumGap,out int taperedCount,out int verticalCount)
            =>BuildVisibleSkirt(ribbon,closed,ground,groomed,width,out maximumGap,out taperedCount,out verticalCount,null);
        private static Mesh BuildVisibleSkirt(Mesh ribbon,bool closed,JoinGroundSurface ground,JoinRibbonSurface groomed,float width,
            out float maximumGap,out int taperedCount,out int verticalCount,bool[] selected)
            =>BuildBankedSkirt(ribbon,closed,ground,groomed,width,out maximumGap,out taperedCount,out verticalCount,selected,null);
        private static Mesh BuildBankedSkirt(Mesh ribbon,bool closed,JoinGroundSurface ground,JoinRibbonSurface groomed,float width,
            out float maximumGap,out int taperedCount,out int verticalCount,bool[] selected,bool[] curved,float moundHeight=0,float moundSpacing=0)
        {
            return BuildBankedSkirtCore(ribbon,closed,ground,groomed,width,out maximumGap,out taperedCount,out verticalCount,selected,curved,moundHeight,moundSpacing,null);
        }
        private static Mesh BuildBankedSkirtCore(Mesh ribbon,bool closed,JoinGroundSurface ground,JoinRibbonSurface groomed,float width,
            out float maximumGap,out int taperedCount,out int verticalCount,bool[] selected,bool[] curved,float moundHeight,float moundSpacing,
            Action<int,int,string,bool,bool> diagnostic,ProductionSnowShoulderDefinition.NaturalProfile natural=null,bool roundOpenEnds=false)
        {
            var source=ribbon.vertices;var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            var sourceNormals=natural==null?null:ribbon.normals;var seamNormals=new List<Vector3>();
            bool publishingTaper=false;Vector3 rimNormalFirst=Vector3.up,rimNormalLast=Vector3.up;double rimTravelFirst=0,rimTravelLast=0;
            int nativeCount=source.Length/2-1;
            if(selected!=null&&selected.Length!=nativeCount)throw new ArgumentException("Native panel selection must match the original rows.");
            if(curved!=null&&curved.Length!=nativeCount)throw new ArgumentException("Snow bank mask must preserve every original native row.");
            bool Active(int panel)=>selected==null||selected[panel];
            var clipCapRows=new bool[nativeCount+1];
            if(natural!=null&&selected!=null&&(closed||roundOpenEnds))
            {
                var distance=new double[nativeCount+1];
                for(int row=1;row<=nativeCount;row++)distance[row]=distance[row-1]+Vector3.Distance((source[(row-1)*2]+source[(row-1)*2+1])*.5f,(source[row*2]+source[row*2+1])*.5f);
                for(int cut=0;cut<=nativeCount;cut++)
                {
                    bool before=cut>0?Active(cut-1):closed&&Active(nativeCount-1),after=cut<nativeCount?Active(cut):closed&&Active(0);
                    if(before==after)continue;
                    for(int row=0;row<=nativeCount;row++)
                    {double gap=Math.Abs(distance[row]-distance[cut]);if(closed)gap=Math.Min(gap,distance[nativeCount]-gap);if(gap<=natural.end_fade_m)clipCapRows[row]=true;}
                }
            }
            groomed.PreparePotentialShoulders(width,natural);
            var vertexIndices=new Dictionary<(Vector3,Vector2),int>();float maximum=0;int slopes=0,walls=0;
            var originalCurtains=new Dictionary<(Vector3,Vector3,bool),Vector3[]>();
            var roundedClosures=new HashSet<(Vector3,Vector3,bool)>();
            for(int side=0;side<2;side++)
            {
                var path=new List<Vector3>();var outward=new List<Vector3>();
                for(int row=side;row<source.Length;row+=2)
                {path.Add(source[row]);var across=source[row]-(source[row-side]+source[row-side+1])*.5f;across.y=0;outward.Add(across.normalized);}
                int count=path.Count-1;double bankPerimeter=0;for(int row=0;row<count;row++)bankPerimeter+=Vector3.Distance(path[row],path[row+1]);var eligible=new bool[count];var reasons=new string[count];var normal=new Vector3[count];var outerFirst=new Vector3[count];var outerLast=new Vector3[count];
                for(int i=0;i<count;i++)
                {
                    var delta=path[i+1]-path[i];delta.y=0;normal[i]=new Vector3(delta.z,0,-delta.x).normalized*(side==0?-1:1);
                    bool bank=natural!=null||curved!=null&&curved[i];
                    outerFirst[i]=path[i]+(bank?outward[i]:normal[i])*width;
                    outerLast[i]=path[i+1]+(bank?outward[i+1]:normal[i])*width;
                }
                if(natural!=null)
                {
                    var rowWidths=(float[])groomed.NaturalWidths(ribbon,side).Clone();
                    // Shared row widths keep neighboring edges bit-identical. A narrow gap
                    // constrains both incident panels rather than creating a vertical step.
                    for(int pass=0;pass<8;pass++)
                    {
                        bool any=false;
                        for(int i=0;i<count;i++)
                        {
                            if(!Active(i))continue;
                            var a=path[i];var b=path[i+1];var oa=a+outward[i]*rowWidths[i];var ob=b+outward[i+1]*rowWidths[i+1];
                            bool clear=ClearFootprint(new JoinVertex(a),new JoinVertex(b),new JoinVertex(oa))&&
                                ClearFootprint(new JoinVertex(b),new JoinVertex(ob),new JoinVertex(oa))&&
                                ClearOtherShoulders(side,i,new JoinVertex(a),new JoinVertex(b),new JoinVertex(oa))&&
                                ClearOtherShoulders(side,i,new JoinVertex(b),new JoinVertex(ob),new JoinVertex(oa));
                            if(!clear&&Mathf.Max(rowWidths[i],rowWidths[i+1])>natural.minimum_width_m)
                            {rowWidths[i]=Mathf.Max(natural.minimum_width_m,rowWidths[i]*.5f);rowWidths[i+1]=Mathf.Max(natural.minimum_width_m,rowWidths[i+1]*.5f);any=true;}
                        }
                        if(closed)rowWidths[0]=rowWidths[count]=Mathf.Min(rowWidths[0],rowWidths[count]);
                        if(!any)break;
                    }
                    // Lipschitz envelope prevents isolated teeth where available space changes.
                    for(int pass=0;pass<2;pass++)
                    {
                        for(int row=1;row<=count;row++)rowWidths[row]=Mathf.Min(rowWidths[row],rowWidths[row-1]+Vector3.Distance(path[row],path[row-1])*width/natural.end_fade_m);
                        for(int row=count-1;row>=0;row--)rowWidths[row]=Mathf.Min(rowWidths[row],rowWidths[row+1]+Vector3.Distance(path[row],path[row+1])*width/natural.end_fade_m);
                        if(closed)rowWidths[0]=rowWidths[count]=Mathf.Min(rowWidths[0],rowWidths[count]);
                    }
                    for(int i=0;i<count;i++){outerFirst[i]=path[i]+outward[i]*rowWidths[i];outerLast[i]=path[i+1]+outward[i+1]*rowWidths[i+1];}
                }
                for(int i=0;i<count;i++)
                {
                    if(!Active(i))continue;
                    var delta=path[i+1]-path[i];delta.y=0;
                    bool straight=Vector3.Dot(normal[i],outward[i])>1-1e-6f&&Vector3.Dot(normal[i],outward[i+1])>1-1e-6f;
                    if(i>0)straight&=Parallel(path[i]-path[i-1],delta);else if(closed)straight&=Parallel(path[count]-path[count-1],delta);
                    if(i+1<count)straight&=Parallel(delta,path[i+2]-path[i+1]);else if(closed)straight&=Parallel(delta,path[1]-path[0]);
                    // Approximate tangent agreement alone can create a slit/overlap at a
                    // very small turn. Both panels must publish the exact same outer seam.
                    if(i>0)straight&=SharedOuter(i-1,i,path[i]);else if(closed)straight&=SharedOuter(count-1,i,path[i]);
                    if(i+1<count)straight&=SharedOuter(i,i+1,path[i+1]);else if(closed)straight&=SharedOuter(i,0,path[i+1]);
                    bool bank=natural!=null||curved!=null&&curved[i];
                    if((!straight&&!bank)||delta.sqrMagnitude<1e-12f){reasons[i]=!straight&&!bank?"curved_outside_policy":"degenerate_edge";continue;}
                    var a=path[i];var b=path[i+1];var outerA=outerFirst[i];var outerB=outerLast[i];
                    bool roadClear=ClearFootprint(new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))&&ClearFootprint(new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA));
                    bool terrainCovered=roadClear&&CoveredFootprint(new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))&&CoveredFootprint(new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA));
                    bool shoulderClear=terrainCovered&&ClearOtherShoulders(side,i,new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))&&ClearOtherShoulders(side,i,new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA));
                    eligible[i]=shoulderClear;reasons[i]=!roadClear?"groomed_overlap":!terrainCovered?"terrain_coverage":!shoulderClear?"neighbor_shoulder_overlap":"accepted";
                    // Curved neighbors share the exact row chord. Its complete terrain
                    // contour must be admissible before either adjacent panel is published.
                    if(natural==null&&bank&&eligible[i]&&(!ClearCurtain(a,Foot(outerA))||!ClearCurtain(b,Foot(outerB)))){eligible[i]=false;reasons[i]="terrain_crosses_row_chord";}
                }
                // A row-based curved contour is shared bit-exactly by its two panels.
                // Optional-to-legacy transitions also require the same published seam.
                for(int i=0;i+1<count;i++)if(eligible[i]&&eligible[i+1]&&!outerLast[i].Equals(outerFirst[i+1]))
                {eligible[i]=false;eligible[i+1]=false;reasons[i]=reasons[i+1]="outer_seam_mismatch";}
                bool SharedOuter(int first,int second,Vector3 pivot)=>(pivot+normal[first]*width).Equals(pivot+normal[second]*width);
                // Validate the actual float triangles before selecting transition caps. A
                // float clipping result can differ from its ideal footprint near a tile seam.
                var profiles=new JoinVertex[count+1][];var panels=new List<JoinVertex[]>[count];var crestFirst=new Vector3[count];var crestLast=new Vector3[count];var crested=new bool[count];double prepareTravel=0;
                if(natural!=null)
                {
                    var rowTravel=new double[count+1];for(int row=1;row<=count;row++)rowTravel[row]=rowTravel[row-1]+Vector3.Distance(path[row-1],path[row]);
                    var fade=new float[count+1];for(int row=0;row<=count;row++)fade[row]=natural.end_fade_m;
                    for(int row=0;row<=count;row++)
                    {
                        bool before=row>0?eligible[row-1]:closed&&eligible[count-1];bool after=row<count?eligible[row]:closed&&eligible[0];
                        if(!before||!after)fade[row]=0;
                    }
                    for(int pass=0;pass<2;pass++)
                    {
                        for(int row=1;row<=count;row++)fade[row]=Mathf.Min(fade[row],fade[row-1]+(float)(rowTravel[row]-rowTravel[row-1]));
                        for(int row=count-1;row>=0;row--)fade[row]=Mathf.Min(fade[row],fade[row+1]+(float)(rowTravel[row+1]-rowTravel[row]));
                        if(closed)fade[0]=fade[count]=Mathf.Min(fade[0],fade[count]);
                    }
                    for(int row=0;row<=count;row++)
                    {
                        var edge=path[row];var outer=Foot(row<count?outerFirst[row]:outerLast[count-1]);
                        profiles[row]=new JoinVertex[natural.cross_sections+1];
                        float f=Mathf.SmoothStep(0,1,fade[row]/natural.end_fade_m);
                        float available=Vector3.Distance(new Vector3(edge.x,0,edge.z),new Vector3(outer.x,0,outer.z))/width;
                        for(int k=0;k<=natural.cross_sections;k++)
                        {
                            float u=(float)k/natural.cross_sections;var point=Vector3.Lerp(edge,outer,u);
                            float baseY=ground.Height(point);float smooth=u*u*(3-2*u);
                            point.y=Mathf.Max(baseY,Mathf.Lerp(edge.y,outer.y,smooth));
                            float wave=Mathf.Sin(Mathf.PI*u);point.y+=Crest(rowTravel[row],side,bankPerimeter)*wave*wave*f*available;
                            if(k==0)point=edge;if(k==natural.cross_sections)point=outer;
                            profiles[row][k]=new JoinVertex(point,smooth,rowTravel[row]);
                        }
                    }
                    if(closed)profiles[count]=profiles[0];
                }
                for(int i=0;i<count;i++)
                {
                    var a=path[i];var b=path[i+1];double next=prepareTravel+Vector3.Distance(a,b);
                    if(eligible[i])
                    {
                        var outerA=Foot(outerFirst[i]);var outerB=Foot(outerLast[i]);var panel=new List<JoinVertex[]>();
                        var first=new JoinVertex(a,0,prepareTravel);var last=new JoinVertex(b,0,next);
                        var outerFirstVertex=new JoinVertex(outerA,1,prepareTravel);var outerLastVertex=new JoinVertex(outerB,1,next);
                        bool mounds=moundHeight>0&&curved!=null&&curved[i];
                        bool accepted;
                        if(natural!=null)
                        {
                            accepted=true;
                            for(int k=0;k<natural.cross_sections&&accepted;k++)
                            {
                                var a0=profiles[i][k];var a1=profiles[i][k+1];var b0=profiles[i+1][k];var b1=profiles[i+1][k+1];
                                accepted=PrepareTaper(a0,b0,a1,panel)&&PrepareTaper(b0,b1,a1,panel);
                            }
                        }
                        else if(mounds)
                        {
                            // Static snow crests use the exact existing row seams. At a
                            // transition the crest fades to the original terrain chord,
                            // so the retained cap still closes the complete bank.
                            var ridgeA=Vector3.Lerp(a,outerA,.45f);var ridgeB=Vector3.Lerp(b,outerB,.45f);
                            bool prior=i>0?eligible[i-1]&&curved[i-1]:closed&&eligible[count-1]&&curved[count-1];
                            bool following=i+1<count?eligible[i+1]&&curved[i+1]:closed&&eligible[0]&&curved[0];
                            ridgeA.y+=prior?Crest(prepareTravel,side,bankPerimeter):0;ridgeB.y+=following?Crest(next,side,bankPerimeter):0;
                            crestFirst[i]=ridgeA;crestLast[i]=ridgeB;crested[i]=true;
                            var crestA=new JoinVertex(ridgeA,.45,prepareTravel);var crestB=new JoinVertex(ridgeB,.45,next);
                            accepted=PrepareTaper(first,last,crestA,panel)&&PrepareTaper(last,crestB,crestA,panel)&&
                                PrepareTaper(crestA,crestB,outerFirstVertex,panel)&&PrepareTaper(crestB,outerLastVertex,outerFirstVertex,panel);
                        }
                        else accepted=PrepareTaper(first,last,outerFirstVertex,panel)&&PrepareTaper(last,outerLastVertex,outerFirstVertex,panel);
                        if(accepted)panels[i]=panel;else {eligible[i]=false;reasons[i]="float_triangulation_or_coverage";}
                    }
                    prepareTravel=next;
                }
                // A transition cap's top chord can cross a convex terrain contour even
                // when both endpoints and the clipped shoulder itself are valid. Settle
                // all newly exposed transitions before publishing any panel or cap.
                bool changed;
                do
                {
                    changed=false;
                    for(int i=0;i<count;i++)if(eligible[i])
                    {
                        bool previous=i>0?eligible[i-1]:closed&&eligible[count-1],following=i+1<count?eligible[i+1]:closed&&eligible[0];
                        if(!previous&&!ClearProfile(i,path[i],Foot(outerFirst[i]))||
                            !following&&!ClearProfile(i+1,path[i+1],Foot(outerLast[i])))
                        {eligible[i]=false;changed=true;reasons[i]="terrain_crosses_transition_cap";}
                    }
                }while(changed);
                bool ClearProfile(int row,Vector3 a,Vector3 b)
                {
                    if(natural==null)return ClearCurtain(a,b);
                    // These roofs are already clipped and certified against every
                    // actual terrain face. Close their exposed positive portions,
                    // rather than rejecting a whole bank because its end chord
                    // passes below a convex ground ridge near a format junction.
                    if(clipCapRows[row])return true;
                    for(int k=0;k<natural.cross_sections;k++)if(!ClearCurtain(Point(profiles[row][k]),Point(profiles[row][k+1])))return false;
                    return true;
                }
                void EmitProfileCap(int row,Vector3 edge,Vector3 outer,Vector3 crest,bool crested,bool reverse,double distance)
                {
                    if(natural==null){Cap(edge,outer,crest,crested,reverse,distance);return;}
                    bool formatCut=(closed||roundOpenEnds)&&selected!=null&&
                        (row>0?Active(row-1):closed&&Active(count-1))!=(row<count?Active(row):closed&&Active(0));
                    var across=source[row*2+1]-source[row*2];across.y=0;
                    bool paidBefore=row>0?Active(row-1):closed&&Active(count-1);
                    var cutOutward=new Vector3(-across.z,0,across.x).normalized*(paidBefore?1:-1);
                    for(int k=0;k<natural.cross_sections;k++)
                        if(formatCut)RoundCut(Point(profiles[row][k]),Point(profiles[row][k+1]),reverse,cutOutward);
                        else if(clipCapRows[row])ClippedCurtain(Point(profiles[row][k]),Point(profiles[row][k+1]),reverse,distance,distance);
                        else Curtain(Point(profiles[row][k]),Point(profiles[row][k+1]),reverse,distance,distance);
                }
                double travel=0;
                for(int i=0;i<count;i++)
                {
                    var a=path[i];var b=path[i+1];double next=travel+Vector3.Distance(a,b);
                    if(!Active(i)){travel=next;continue;}
                    if(!eligible[i]){bool emitted=Curtain(a,b,side==1,travel,next,true,true);if(emitted)walls++;diagnostic?.Invoke(side,i,reasons[i]??"rejected",false,emitted);}
                    else
                    {
                        var outerA=Foot(outerFirst[i]);var outerB=Foot(outerLast[i]);
                        publishingTaper=natural!=null;
                        if(publishingTaper){rimNormalFirst=sourceNormals[i*2+side];rimNormalLast=sourceNormals[(i+1)*2+side];rimTravelFirst=travel;rimTravelLast=next;}
                        foreach(var face in panels[i])
                        {
                            int first=Index(face[0],Point(face[0])),second=Index(face[1],Point(face[1])),third=Index(face[2],Point(face[2]));
                            triangles.Add(first);triangles.Add(second);triangles.Add(third);
                        }
                        publishingTaper=false;
                        slopes++;diagnostic?.Invoke(side,i,"accepted",true,false);
                        bool previous=i>0?eligible[i-1]:closed&&eligible[count-1],following=i+1<count?eligible[i+1]:closed&&eligible[0];
                        if(!previous)EmitProfileCap(i,a,outerA,crestFirst[i],crested[i],side==1,travel);
                        if(!following)EmitProfileCap(i+1,b,outerB,crestLast[i],crested[i],side==0,next);
                    }
                    travel=next;
                }
            }
            // Across-track caps belong only to true cuts in the selected native panels.
            // The original closed seam is continuous when both incident panels are paid.
            for(int row=0;row<=nativeCount;row++)
            {
                bool previous=row>0?Active(row-1):closed&&Active(nativeCount-1);
                bool following=row<nativeCount?Active(row):closed&&Active(0);
                if(previous==following||closed&&row==0&&previous||closed&&row==nativeCount&&following)continue;
                var first=source[row*2];var last=source[row*2+1];
                if(natural!=null&&(closed||roundOpenEnds)&&selected!=null)
                {
                    if(RoundCut(first,last,!previous))diagnostic?.Invoke(2,Math.Min(row,nativeCount-1),"rounded_format_cut",true,false);
                }
                else if(Curtain(first,last,!previous,0,Vector3.Distance(first,last),true))walls++;
            }
            foreach(var edge in roundedClosures)ClippedCurtain(edge.Item1,edge.Item2,edge.Item3,0,0);
            // Clipped zero-clearance pieces can leave unused points. Keep the published mesh
            // compact so every vertex has a finite, meaningful normal.
            var used=new bool[vertices.Count];foreach(int index in triangles)used[index]=true;
            var compact=new int[vertices.Count];int destination=0;
            for(int i=0;i<vertices.Count;i++)if(used[i]){compact[i]=destination;vertices[destination]=vertices[i];uv[destination]=uv[i];colors[destination]=colors[i];seamNormals[destination]=seamNormals[i];destination++;}
            vertices.RemoveRange(destination,vertices.Count-destination);uv.RemoveRange(destination,uv.Count-destination);colors.RemoveRange(destination,colors.Count-destination);
            for(int i=0;i<triangles.Count;i++)triangles[i]=compact[triangles[i]];
            var mesh=new Mesh{name=ribbon.name+" snow shoulder and retained edge joins",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            try
            {
                mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);
                var summed=new Dictionary<Vector3,Vector3>();
                for(int i=0;i<triangles.Count;i+=3)
                {var a=vertices[triangles[i]];var b=vertices[triangles[i+1]];var c=vertices[triangles[i+2]];var normal=Vector3.Cross(b-a,c-a);Sum(a,normal);Sum(b,normal);Sum(c,normal);}
                var normals=new Vector3[vertices.Count];for(int i=0;i<normals.Length;i++)
                {
                    var normal=summed[vertices[i]];double square=(double)normal.x*normal.x+(double)normal.y*normal.y+(double)normal.z*normal.z;
                    if(!double.IsFinite(square)||square<=0)throw new ArgumentException("Snow joins require finite nonzero face normals: "+ribbon.name+" at "+vertices[i].ToString("R"));
                    // Vector3.normalized rounds very small, valid accumulated face areas to zero.
                    normals[i]=normal/(float)Math.Sqrt(square);
                }
                if(natural!=null)for(int i=0;i<normals.Length;i++)
                {
                    float blend=uv[i].x/width;
                    if(blend<.25f&&seamNormals[i].sqrMagnitude>.0001f)
                        normals[i]=Vector3.Slerp(seamNormals[i],normals[i],Mathf.SmoothStep(0,1,blend*4));
                    if(blend>.75f){ground.Attributes(vertices[i],out _,out var groundNormal);normals[i]=Vector3.Slerp(normals[i],groundNormal,Mathf.SmoothStep(0,1,(blend-.75f)*4));}
                }
                mesh.normals=normals;mesh.RecalculateBounds();maximumGap=maximum;taperedCount=slopes;verticalCount=walls;return mesh;
                void Sum(Vector3 point,Vector3 normal){summed.TryGetValue(point,out var prior);summed[point]=prior+normal;}
            }
            catch{DestroyOwned(mesh);throw;}

            bool RoundCut(Vector3 a,Vector3 b,bool reverse,Vector3? sharedOutward=null)
            {
                // A format selects native main-loop panels, exposing a transverse end
                // which the longitudinal banks cannot cover. Round only the free snow
                // beside that exact end; the packed buffers and all race geometry stay
                // unchanged. Retain a curtain only where no rounded profile fits.
                var delta=b-a;delta.y=0;
                // All pieces of the same native cut must use its original full
                // cross-row direction. Renormalizing short bank chords separately
                // changes the shared foot by float bits and leaves opposite walls.
                var outward=sharedOutward??new Vector3(-delta.z,0,delta.x).normalized*(reverse?-1:1);
                var first=new JoinVertex(a);var last=new JoinVertex(b);
                var cuts=new SortedSet<double>{0,1};
                int divisions=Math.Max(1,(int)Math.Ceiling(delta.magnitude/(width/natural.cross_sections)));
                for(int i=1;i<divisions;i++)cuts.Add((double)i/divisions);
                foreach(var face in groomed.Over(first,last,last))
                    foreach(var edge in new[]{(face.A,face.B),(face.B,face.C),(face.C,face.A)})
                    {
                        double start=Cross(edge.Item1,edge.Item2,first),end=Cross(edge.Item1,edge.Item2,last);
                        if((start<0)!=(end<0))
                        {
                            double t=start/(start-end);var point=JoinVertex.Lerp(first,last,t);
                            double dx=edge.Item2.X-edge.Item1.X,dz=edge.Item2.Z-edge.Item1.Z;
                            double dot=(point.X-edge.Item1.X)*dx+(point.Z-edge.Item1.Z)*dz;
                            if(t>0&&t<1&&dot>=0&&dot<=dx*dx+dz*dz)cuts.Add(t);
                        }
                    }
                // Keep exact intersection coordinates for footprint admission. Rounding
                // a cut to Vector3 first can move its base a float bit inside its own
                // packed ribbon and falsely reject every available snow-bank width.
                var rows=new List<JoinVertex>();foreach(double t in cuts)rows.Add(JoinVertex.Lerp(first,last,t));
                // A neighboring road can touch just one end of a strip. Let that
                // endpoint pinch to zero, keeping a certified free-space fan at
                // the other end instead of rejecting the whole rectangular bank.
                var widths=new float[rows.Count];for(int row=0;row<widths.Length;row++)widths[row]=width;
                // Constrain both incident fans independently before changing shared
                // row widths. A greedy choice of one fan can erase the free end of
                // the next strip and leave an avoidable rectangular notch.
                for(int row=0;row+1<rows.Count;row++)if(!Free(row,width,width))
                {widths[row]=Mathf.Min(widths[row],Fan(row,width,true));widths[row+1]=Mathf.Min(widths[row+1],Fan(row,width,false));}
                for(int pass=0;pass<natural.cross_sections;pass++)
                {
                    bool changed=false;
                    for(int row=0;row+1<rows.Count;row++)
                    {
                        if(Free(row,widths[row],widths[row+1]))continue;
                        float firstWidth=Shrink(widths[row]),lastWidth=Shrink(widths[row+1]);
                        changed|=firstWidth!=widths[row]||lastWidth!=widths[row+1];widths[row]=firstWidth;widths[row+1]=lastWidth;
                    }
                    if(!changed)break;
                }
                var profiles=new JoinVertex[rows.Count][];var panels=new List<JoinVertex[]>[rows.Count-1];
                for(int attempt=0;attempt<=natural.cross_sections;attempt++)
                {
                    for(int row=0;row<rows.Count;row++)profiles[row]=Profile(Point(rows[row]),widths[row]);
                    bool changed=false;
                    for(int row=0;row+1<rows.Count;row++)
                    {
                        var pa=profiles[row];var pb=profiles[row+1];var pending=new List<JoinVertex[]>();bool accepted=true;
                        for(int k=0;k<natural.cross_sections&&accepted;k++)
                            accepted=PrepareTaper(pa[k],pb[k],pa[k+1],pending,!sharedOutward.HasValue)&&PrepareTaper(pb[k],pb[k+1],pa[k+1],pending,!sharedOutward.HasValue);
                        panels[row]=accepted?pending:null;
                        if(!accepted&&attempt<natural.cross_sections)
                        {
                            float firstWidth=Shrink(widths[row]),lastWidth=Shrink(widths[row+1]);
                            changed|=firstWidth!=widths[row]||lastWidth!=widths[row+1];widths[row]=firstWidth;widths[row+1]=lastWidth;
                        }
                    }
                    if(!changed)break;
                }
                bool emitted=false;
                for(int row=0;row+1<rows.Count;row++)
                {
                    var p=Point(rows[row]);var q=Point(rows[row+1]);if((q-p).sqrMagnitude<1e-10f)continue;
                    var pending=panels[row];var pa=profiles[row];var pb=profiles[row+1];
                    if(pending==null||pending.Count==0)
                    {if(sharedOutward.HasValue)ClippedCurtain(p,q,reverse,0,0);else Curtain(p,q,reverse,0,0,true);continue;}
                    foreach(var face in pending)
                    {triangles.Add(Index(face[0],Point(face[0])));triangles.Add(Index(face[1],Point(face[1])));triangles.Add(Index(face[2],Point(face[2])));}
                    // Equal adjacent profiles are interior faces. Cancel them before
                    // publication instead of adding opposite coplanar curtains, whose
                    // normals would cancel at the shared terrain foot.
                    for(int k=0;k<natural.cross_sections;k++)
                    {Close(pa[k],pa[k+1],!reverse);Close(pb[k],pb[k+1],reverse);}
                    if(widths[row]==0||widths[row+1]==0)Close(pa[natural.cross_sections],pb[natural.cross_sections],reverse);
                    emitted=true;
                }
                return emitted;
                bool Free(int row,float firstWidth,float lastWidth)
                {
                    var p=rows[row];var q=rows[row+1];var op=p.Offset(outward*firstWidth);var oq=q.Offset(outward*lastWidth);
                    return ClearFootprint(p,q,op)&&ClearFootprint(q,oq,op);
                }
                float Fan(int row,float available,bool firstEnd)
                {
                    if(Free(row,firstEnd?available:0,firstEnd?0:available))return available;
                    float low=0,high=available;
                    for(int pass=0;pass<natural.cross_sections;pass++)
                    {float trial=(low+high)*.5f;if(Free(row,firstEnd?trial:0,firstEnd?0:trial))low=trial;else high=trial;}
                    return low>=natural.minimum_width_m?low:0;
                }
                float Shrink(float available)=>available*.5f>=natural.minimum_width_m?available*.5f:0;
                void Close(JoinVertex firstPoint,JoinVertex lastPoint,bool reversed)
                {
                    var p=Point(firstPoint);var q=Point(lastPoint);
                    if(p.Equals(q))return;
                    if(!roundedClosures.Remove((p,q,!reversed)))roundedClosures.Add((p,q,reversed));
                }
                JoinVertex[] Profile(Vector3 edge,float available)
                {
                    var result=new JoinVertex[natural.cross_sections+1];
                    if(available==0){for(int k=0;k<result.Length;k++)result[k]=new JoinVertex(edge);return result;}
                    var outer=Foot(edge+outward*available);
                    for(int k=0;k<=natural.cross_sections;k++)
                    {
                        float t=(float)k/natural.cross_sections,smooth=t*t*(3-2*t);var point=Vector3.Lerp(edge,outer,t);
                        point.y=Mathf.Max(ground.Height(point),Mathf.Lerp(edge.y,outer.y,smooth));
                        if(k==0)point=edge;if(k==natural.cross_sections)point=outer;
                        result[k]=new JoinVertex(point,smooth,0);
                    }
                    return result;
                }
            }

            void Cap(Vector3 edge,Vector3 outer,Vector3 crest,bool crested,bool reverse,double travel)
            {if(crested){Curtain(edge,crest,reverse,travel,travel);Curtain(crest,outer,reverse,travel,travel);}else Curtain(edge,outer,reverse,travel,travel);}
            float Crest(double travel,int side,double perimeter)
            {
                double cycles=closed?travel/perimeter*Math.Max(1,Math.Round(perimeter/moundSpacing)):travel/moundSpacing;
                if(natural==null)return moundHeight*(.5f+.5f*(float)Math.Cos(cycles*2*Math.PI+side*Math.PI));
                double phase=cycles*2*Math.PI+side*1.7;
                double broad=closed?travel/perimeter*2*Math.PI:travel/(moundSpacing*3)*2*Math.PI;
                return moundHeight*(.55f+.25f*(float)Math.Cos(phase)+.12f*(float)Math.Sin(2*phase+.8)+.08f*(float)Math.Sin(broad+side));
            }
            bool Parallel(Vector3 a,Vector3 b){a.y=b.y=0;return a.sqrMagnitude>1e-12f&&b.sqrMagnitude>1e-12f&&Vector3.Dot(a.normalized,b.normalized)>1-1e-6f;}
            Vector3 Foot(Vector3 point)
            {
                try{point.y=ground.Height(point);return point;}
                catch(ArgumentException error){throw new ArgumentException("Snow join terrain lookup failed. Ribbon="+ribbon.name+" point="+point.ToString("R"),error);}
            }
            double Area(List<JoinVertex> polygon){double area=0;for(int i=1;i<polygon.Count-1;i++)area+=Math.Abs(Cross(polygon[0],polygon[i],polygon[i+1]));return area;}
            bool ClearFootprint(JoinVertex a,JoinVertex b,JoinVertex c)
            {
                foreach(var ribbonFace in groomed.Over(a,b,c))
                {
                    double sign=ribbonFace.Area<0?-1:1;var overlap=new List<JoinVertex>{a,b,c};
                    overlap=ClipJoin(overlap,p=>sign*Cross(ribbonFace.A,ribbonFace.B,p));
                    overlap=ClipJoin(overlap,p=>sign*Cross(ribbonFace.B,ribbonFace.C,p));
                    overlap=ClipJoin(overlap,p=>sign*Cross(ribbonFace.C,ribbonFace.A,p));
                    if(Area(overlap)>1e-10)return false;
                }
                return true;
            }
            bool CoveredFootprint(JoinVertex a,JoinVertex b,JoinVertex c)
            {
                // Rendered tile transforms can leave tiny gaps or overlaps even when the
                // declared grid covers the strip. Reject such a taper using actual triangles;
                // retain the original join instead of extrapolating or snapping the ground.
                double expected=Math.Abs(Cross(a,b,c)),covered=0;var pieces=new List<List<JoinVertex>>();
                foreach(var terrain in ground.Over(a,b,c))
                {
                    double sign=terrain.Area<0?-1:1;var piece=new List<JoinVertex>{a,b,c};
                    piece=ClipJoin(piece,p=>sign*Cross(terrain.A,terrain.B,p));piece=ClipJoin(piece,p=>sign*Cross(terrain.B,terrain.C,p));piece=ClipJoin(piece,p=>sign*Cross(terrain.C,terrain.A,p));
                    double area=Area(piece);if(area<1e-12)continue;covered+=area;
                    foreach(var prior in pieces)
                    {
                        var overlap=piece;double orientation=0;for(int i=1;i<prior.Count-1&&orientation==0;i++)orientation=Cross(prior[0],prior[i],prior[i+1]);
                        double priorSign=orientation<0?-1:1;
                        for(int i=0;i<prior.Count&&overlap.Count>0;i++){var first=prior[i];var last=prior[(i+1)%prior.Count];overlap=ClipJoin(overlap,p=>priorSign*Cross(first,last,p));}
                        if(Area(overlap)>1e-10)return false;
                    }
                    pieces.Add(piece);
                }
                return Math.Abs(covered-expected)<=Math.Max(1e-7,expected*1e-6);
            }
            bool ClearOtherShoulders(int side,int segment,JoinVertex a,JoinVertex b,JoinVertex c)
            {
                foreach(var other in groomed.OtherShoulders(ribbon,side,segment,closed,a,b,c))
                {
                    double sign=other.Area<0?-1:1;var overlap=new List<JoinVertex>{a,b,c};
                    overlap=ClipJoin(overlap,p=>sign*Cross(other.A,other.B,p));overlap=ClipJoin(overlap,p=>sign*Cross(other.B,other.C,p));overlap=ClipJoin(overlap,p=>sign*Cross(other.C,other.A,p));
                    if(Area(overlap)>1e-10)return false;
                }
                return true;
            }
            bool PrepareTaper(JoinVertex a,JoinVertex b,JoinVertex c,List<JoinVertex[]> pending,bool requireRibbonEdge=true)
            {
                // Authored packed edges keep their original clearance guard. Only
                // a decorative cap starting on an already terrain-clipped snow
                // profile may begin below ground; its surviving faces are clipped
                // and certified against the same actual terrain below.
                double expected=Math.Abs(Cross(a,b,c)),covered=0;
                foreach(var terrain in ground.Over(a,b,c))
                {
                    double sign=terrain.Area<0?-1:1;var polygon=new List<JoinVertex>{a,b,c};
                    polygon=ClipJoin(polygon,p=>sign*Cross(terrain.A,terrain.B,p));polygon=ClipJoin(polygon,p=>sign*Cross(terrain.B,terrain.C,p));polygon=ClipJoin(polygon,p=>sign*Cross(terrain.C,terrain.A,p));
                    covered+=Area(polygon);
                    for(int i=0;i<polygon.Count;i++)
                    {
                        var point=polygon[i];if(requireRibbonEdge&&point.Blend<1e-9)CheckGap(point.Y-terrain.Height(point));
                        if(point.Blend>1-1e-9)
                            // Keep the terrain plane in double precision until final mesh emission.
                            // An intermediate float can move a foot above that plane and clipping
                            // then publishes an exposed sliver in legacy as well as opted-in joins.
                            polygon[i]=point.WithExactHeight(terrain.Height(point));
                    }
                    for(int i=1;i<polygon.Count-1;i++)
                    {
                        var face=ClipJoin(new List<JoinVertex>{polygon[0],polygon[i],polygon[i+1]},p=>p.Y-terrain.Height(p));
                        for(int j=1;j<face.Count-1;j++)
                        {
                            var first=Point(face[0]);var second=Point(face[j]);var third=Point(face[j+1]);var normal=Vector3.Cross(second-first,third-first);float up=normal.y;
                            if(!float.IsFinite(normal.x)||!float.IsFinite(normal.y)||!float.IsFinite(normal.z))throw new ArgumentException("Snow joins require finite triangle faces.");
                            // Apply the same actual float-space nondegeneracy bound as retained
                            // vertical joins: grid clipping can otherwise leave tiny sliver faces.
                            if(normal.sqrMagnitude<1e-16f||Math.Abs(up)<1e-12f)continue;
                            // A shared native renderer translation can round distinct
                            // clipping vertices onto one projected point. Certify the
                            // planned retained face in actual renderer space too, and
                            // roll back the whole taper candidate rather than leaving
                            // a face-sized hole or relaxing the nondegeneracy bound.
                            var renderedFirst=ground.Rendered(first);var renderedSecond=ground.Rendered(second);var renderedThird=ground.Rendered(third);
                            var renderedNormal=Vector3.Cross(renderedSecond-renderedFirst,renderedThird-renderedFirst);
                            if(!float.IsFinite(renderedNormal.x)||!float.IsFinite(renderedNormal.y)||!float.IsFinite(renderedNormal.z))
                                throw new ArgumentException("Snow joins require finite rendered triangle faces.");
                            if(renderedNormal.sqrMagnitude<1e-16f||Math.Abs(renderedNormal.y)<1e-12f||(up>0)!=(renderedNormal.y>0))return false;
                            if(!CoveredFootprint(new JoinVertex(first),new JoinVertex(second),new JoinVertex(third)))return false;
                            pending.Add(up>0?new[]{face[0],face[j],face[j+1]}:new[]{face[0],face[j+1],face[j]});
                        }
                    }
                }
                return Math.Abs(covered-expected)<=Math.Max(1e-7,expected*1e-6);
            }
            bool ClearCurtain(Vector3 a,Vector3 b)
            {
                var rows=CurtainRows(a,b);for(int row=0;row<rows.Count;row++)
                {
                    if(rows[row].Item1.y-rows[row].Item2.y< -1e-4)return false;if(row==0)continue;
                    var first=new JoinVertex(rows[row-1].Item1);var last=new JoinVertex(rows[row].Item1);var intervals=new List<(double,double)>();
                    foreach(var terrain in ground.Over(first,last,last))
                    {
                        double low=0,high=1,sign=terrain.Area<0?-1:1;bool covered=true;
                        foreach(var edge in new[]{(terrain.A,terrain.B),(terrain.B,terrain.C),(terrain.C,terrain.A)})
                        {
                            double start=sign*Cross(edge.Item1,edge.Item2,first),end=sign*Cross(edge.Item1,edge.Item2,last);
                            if(start<0&&end<0){covered=false;break;}
                            if((start<0)!=(end<0)){double t=start/(start-end);if(start<0)low=Math.Max(low,t);else high=Math.Min(high,t);}
                        }
                        if(!covered||low>high)continue;
                        foreach(double t in new[]{low,high})
                        {var point=JoinVertex.Lerp(first,last,t);if(point.Y-terrain.Height(point)< -1e-4)return false;}
                        intervals.Add((low,high));
                    }
                    intervals.Sort((firstInterval,lastInterval)=>firstInterval.Item1.CompareTo(lastInterval.Item1));double endCovered=0;
                    foreach(var interval in intervals){if(interval.Item1>endCovered+1e-9)return false;endCovered=Math.Max(endCovered,interval.Item2);}if(endCovered<1-1e-9)return false;
                }
                return true;
            }
            List<(Vector3,Vector3,double)> CurtainRows(Vector3 a,Vector3 b)
            {
                var cuts=new SortedSet<double>{0,1};Cuts(a.x,b.x,ground.origin.x);Cuts(a.z,b.z,ground.origin.y);Cuts((double)a.x+a.z,(double)b.x+b.z,(double)ground.origin.x+ground.origin.y);
                var rows=new List<(Vector3,Vector3,double)>();
                foreach(double t in cuts)
                {
                    var top=new Vector3((float)(a.x+((double)b.x-a.x)*t),(float)(a.y+((double)b.y-a.y)*t),(float)(a.z+((double)b.z-a.z)*t));rows.Add((top,Foot(top),t));
                }
                return rows;
                void Cuts(double first,double last,double origin)
                {if(first==last)return;long low=(long)Math.Ceiling((Math.Min(first,last)-origin)/ground.spacing),high=(long)Math.Floor((Math.Max(first,last)-origin)/ground.spacing);for(long line=low;line<=high;line++){double t=(origin+line*ground.spacing-first)/(last-first);if(t>0&&t<1)cuts.Add(t);}}
            }
            void SimplifyFootRows(List<(Vector3,Vector3,double)> rows)
            {
                // A grid cut can round beside a short, thin original ribbon edge.
                // Retain source endpoints; remove an intermediate row only when
                // its whole replacement terrain-foot chord is independently proved
                // against every actual terrain segment at the existing contact bound.
                for(int row=rows.Count-2;row>0;row--)
                    if(GroundChord(rows[row-1].Item2,rows[row+1].Item2))rows.RemoveAt(row);
            }
            bool GroundChord(Vector3 a,Vector3 b)
            {
                var first=new JoinVertex(a);var last=new JoinVertex(b);var intervals=new List<(double,double)>();
                foreach(var terrain in ground.Over(first,last,last))
                {
                    double low=0,high=1,sign=terrain.Area<0?-1:1;bool covered=true;
                    foreach(var edge in new[]{(terrain.A,terrain.B),(terrain.B,terrain.C),(terrain.C,terrain.A)})
                    {
                        double start=sign*Cross(edge.Item1,edge.Item2,first),end=sign*Cross(edge.Item1,edge.Item2,last);
                        if(start< -1e-7&&end< -1e-7){covered=false;break;}
                        if((start<0)!=(end<0)){double t=start/(start-end);if(start<0)low=Math.Max(low,t);else high=Math.Min(high,t);}
                    }
                    if(!covered||low>high+1e-9)continue;
                    foreach(double t in new[]{low,high})
                    {var point=JoinVertex.Lerp(first,last,t);if(Math.Abs(point.Y-terrain.Height(point))>1e-4)return false;}
                    intervals.Add((low,high));
                }
                intervals.Sort((x,y)=>x.Item1.CompareTo(y.Item1));double endCovered=0;
                foreach(var interval in intervals){if(interval.Item1>endCovered+1e-9)return false;endCovered=Math.Max(endCovered,interval.Item2);}
                return endCovered>=1-1e-9;
            }
            bool Curtain(Vector3 a,Vector3 b,bool reverse,double travelA,double travelB,bool original=false,bool simplifyFoot=false)
            {
                var rows=CurtainRows(a,b);
                if(simplifyFoot)SimplifyFootRows(rows);
                foreach(var row in rows)
                {
                    double gap=row.Item1.y-row.Item2.y;
                    if(!double.IsFinite(gap)||gap< -1e-4)throw new ArgumentException("Actual ground must remain beneath the groomed course edge. Ribbon="+ribbon.name+" original="+original+" a="+a.ToString("R")+" b="+b.ToString("R")+" top="+row.Item1.ToString("R")+" ground="+row.Item2.ToString("R")+" gap="+gap.ToString("R",System.Globalization.CultureInfo.InvariantCulture));
                    CheckGap(gap);
                }
                if(original)
                {
                    // A retraced source boundary can request the same original curtain
                    // twice with opposite traversal but identical outward winding. Only
                    // suppress an exact whole top/terrain polyline duplicate, including
                    // every grid/diagonal break; caps and opposite-facing surfaces remain.
                    bool reversed=false;bool found=originalCurtains.TryGetValue((a,b,reverse),out var priorRows);
                    if(!found){found=originalCurtains.TryGetValue((b,a,!reverse),out priorRows);reversed=found;}
                    if(found&&priorRows.Length==rows.Count*2)
                    {
                        bool same=true;
                        for(int row=0;row<rows.Count;row++)
                        {int priorRow=reversed?rows.Count-1-row:row;if(!rows[row].Item1.Equals(priorRows[priorRow*2])||!rows[row].Item2.Equals(priorRows[priorRow*2+1])){same=false;break;}}
                        if(same)return false;
                    }
                    if(!found){var saved=new Vector3[rows.Count*2];for(int row=0;row<rows.Count;row++){saved[row*2]=rows[row].Item1;saved[row*2+1]=rows[row].Item2;}originalCurtains.Add((a,b,reverse),saved);}
                }
                int prior=-1,priorFoot=-1;
                foreach(var row in rows)
                {
                    var top=row.Item1;var bottom=row.Item2;double travel=travelA+(travelB-travelA)*row.Item3;int current=Index(new JoinVertex(top,0,travel),top),foot=Index(new JoinVertex(bottom,1,travel),bottom);
                    if(prior>=0)
                    {
                        if(reverse){Face(prior,current,priorFoot);Face(current,foot,priorFoot);}
                        else{Face(prior,priorFoot,current);Face(current,priorFoot,foot);}
                    }
                    prior=current;priorFoot=foot;
                }
                return true;
            }
            void ClippedCurtain(Vector3 a,Vector3 b,bool reverse,double travelA,double travelB)
            {
                var rows=CurtainRows(a,b);bool clear=true;foreach(var row in rows)clear&=row.Item1.y>=row.Item2.y;
                if(clear){Curtain(a,b,reverse,travelA,travelB);return;}
                for(int i=1;i<rows.Count;i++)
                {
                    var first=rows[i-1];var last=rows[i];double ga=(double)first.Item1.y-first.Item2.y,gb=(double)last.Item1.y-last.Item2.y;
                    CheckGap(Math.Max(0,ga));CheckGap(Math.Max(0,gb));
                    if(ga<=0&&gb<=0)continue;
                    if((ga<0)!=(gb<0))
                    {
                        double t=ga/(ga-gb);var p=JoinVertex.Lerp(new JoinVertex(first.Item1),new JoinVertex(last.Item1),t);var point=Foot(Point(p));
                        var crossing=(point,point,first.Item3+(last.Item3-first.Item3)*t);
                        if(ga<0)first=crossing;else last=crossing;
                    }
                    double ta=travelA+(travelB-travelA)*first.Item3,tb=travelA+(travelB-travelA)*last.Item3;
                    int at=Index(new JoinVertex(first.Item1,0,ta),first.Item1),af=Index(new JoinVertex(first.Item2,1,ta),first.Item2),
                        bt=Index(new JoinVertex(last.Item1,0,tb),last.Item1),bf=Index(new JoinVertex(last.Item2,1,tb),last.Item2);
                    if(reverse){Face(at,bt,af);Face(bt,bf,af);}else{Face(at,af,bt);Face(bt,af,bf);}
                }
            }
            void Face(int a,int b,int c)
            {if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude<1e-16f)return;triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            void CheckGap(double gap){if(!double.IsFinite(gap)||gap< -1e-4)throw new ArgumentException("Actual ground must remain beneath the groomed course edge.");maximum=Mathf.Max(maximum,(float)Math.Max(0,gap));}
            Vector3 Point(JoinVertex vertex)=>new Vector3((float)vertex.X,(float)vertex.Y,(float)vertex.Z);
            int Index(JoinVertex vertex,Vector3 point)
            {
                var texture=new Vector2((float)(vertex.Blend*width),(float)vertex.Travel);var key=(point,texture);if(vertexIndices.TryGetValue(key,out int index))return index;
                index=vertices.Count;vertexIndices.Add(key,index);vertices.Add(point);uv.Add(texture);float blend=(float)Math.Max(0,Math.Min(1,vertex.Blend));if(natural==null)colors.Add(new Color(0,1-blend,0,blend));
                else {ground.Attributes(point,out var groundColor,out _);colors.Add(Color.Lerp(new Color(0,1,0,0),groundColor,blend));}
                seamNormals.Add(publishingTaper?Vector3.Lerp(rimNormalFirst,rimNormalLast,(float)((vertex.Travel-rimTravelFirst)/Math.Max(1e-9,rimTravelLast-rimTravelFirst))).normalized:Vector3.zero);
                return index;
            }
        }

        private static void Groom(Mesh mesh,bool closed)
        {
            var vertices=mesh.vertices;var uv=mesh.uv;var colors=new Color[vertices.Length];
            // A closed centreline has two tangent interpretations at its boundary. Join the actual ribbon
            // edges there as well, retaining the exact authored centre and longitudinal UV distance.
            if(closed){vertices[vertices.Length-2]=vertices[0];vertices[vertices.Length-1]=vertices[1];mesh.vertices=vertices;}
            for(int i=0;i<vertices.Length;i+=2)
            {
                colors[i]=colors[i+1]=new Color(0,1,0,0); // Canonical terrain shader packed-snow channel.
                uv[i].x=0;uv[i+1].x=Vector3.Distance(vertices[i],vertices[i+1]);
            }
            // Across-track UVs are metres, so the data-driven groom spacing does not vary with track width.
            mesh.colors=colors;mesh.uv=uv;mesh.RecalculateNormals();
            if(closed)
            {
                var normals=mesh.normals;int end=normals.Length-2;
                var left=(normals[0]+normals[end]).normalized;var right=(normals[1]+normals[end+1]).normalized;
                normals[0]=normals[end]=left;normals[1]=normals[end+1]=right;mesh.normals=normals;
            }
            mesh.RecalculateBounds();
            // RecalculateBounds may round a far edge inward by one float ULP.
            // Expand only a failing culling bound; source positions remain exact.
            var bounds=mesh.bounds;
            for(int i=0;i<vertices.Length;i++)
                if(!bounds.Contains(vertices[i]))
                {
                    var delta=vertices[i]-bounds.center;var extent=bounds.extents;
                    extent.x=NextExtent(Mathf.Max(extent.x,Mathf.Abs(delta.x)));
                    extent.y=NextExtent(Mathf.Max(extent.y,Mathf.Abs(delta.y)));
                    extent.z=NextExtent(Mathf.Max(extent.z,Mathf.Abs(delta.z)));
                    bounds.extents=extent;
                }
            mesh.bounds=bounds;
            float NextExtent(float value)=>BitConverter.Int32BitsToSingle(BitConverter.SingleToInt32Bits(value)+1);
        }
        private void OnDisable(){if(generated!=null)generated.SetActive(false);}
        private void OnEnable(){if(generated!=null)generated.SetActive(true);}
        public void Shutdown()
        {
            if(generated!=null){generated.SetActive(false);generated.transform.SetParent(null);DestroyOwned(generated);generated=null;}
            foreach(var mesh in ownedMeshes)DestroyOwned(mesh);ownedMeshes.Clear();DestroyOwned(material);material=null;
            edgeDiagnostics.Clear();indices.Clear();loopIds=null;closedLoops=null;meshes=null;nativeMeshes=null;nativeDistances=null;visiblePanels=null;renderers=null;skirts=null;skirtRenderers=null;MaximumEdgeGapMeters=SnowShoulderWidthMeters=0;TaperedSnowPanelCount=VerticalSnowPanelCount=VisibleLoopCount=0;VenueId=null;TrackSnapshotJson=null;VisualSnapshotJson=null;PresentationRouteId=PresentationRouteSnapshotJson=PresentationRequirementsSnapshotJson=null;
            authoredRenderFrame=default;bankPanels=null;roundBranchEnds=null;natural=null;RimClearanceAdjustments=CoveredCurtainFaces=RoundedSnowCutCount=0;moundHeight=moundSpacing=0;SnowBankPolicyId=null;drawMeshes=null;Apron=null;apronRenderer=apronSkirtRenderer=null;apronSkirt=null;
        }
        private static void DestroyOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Shutdown();
    }
}
