using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Domain;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Owned groomed surfaces for separate authored loops. Rendering never changes course data.</summary>
    public sealed class ProductionCoursePresentation : MonoBehaviour
    {
        private GameObject generated;
        private readonly List<Mesh> ownedMeshes=new List<Mesh>();
        private readonly Dictionary<string,int> indices=new Dictionary<string,int>(StringComparer.Ordinal);
        private string[] loopIds;
        private bool[] closedLoops;
        private Mesh[] meshes;
        private MeshRenderer[] renderers;
        private Mesh[] skirts;
        private MeshRenderer[] skirtRenderers;
        private Material material;
        public bool Initialized=>generated!=null;
        public string VenueId {get;private set;}
        public string TrackSnapshotJson {get;private set;}
        public string VisualSnapshotJson {get;private set;}
        public int LoopCount=>loopIds?.Length??0;
        public Material GroomedMaterial=>material;
        public bool GroundJoined=>skirts!=null;
        public float MaximumEdgeGapMeters {get;private set;}
        public float SnowShoulderWidthMeters {get;private set;}
        public int TaperedSnowPanelCount {get;private set;}
        public int VerticalSnowPanelCount {get;private set;}
        public string LoopId(int index)=>loopIds[index];
        public Mesh LoopMesh(int index)=>meshes[index];
        public Mesh LoopMesh(string loopId)=>meshes[Index(loopId)];
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
            string materialResource="Biathlon/Terrain/G2Snow/WinterBlend",string blendResource="Biathlon/terrain_blend_v1")
        {
            if(Initialized||ownedMeshes.Count!=0||material!=null)throw new InvalidOperationException("Initialize the production course once.");
            if(plan==null)throw new ArgumentNullException(nameof(plan));
            if(!isActiveAndEnabled||(transform.lossyScale-Vector3.one).sqrMagnitude>1e-6f)
                throw new ArgumentException("Enabled unit-scale course root required.");
            VenueManifest.ResourceId(materialResource);VenueManifest.ResourceId(blendResource);
            var template=Resources.Load<Material>(materialResource);
            if(template==null)throw new FileNotFoundException("Missing groomed course material: "+materialResource);
            if(template.shader==null||template.shader.name!="Biathlon/Snow")throw new ArgumentException("Native snow material required.",nameof(materialResource));
            var text=Resources.Load<TextAsset>(blendResource);
            if(text==null)throw new FileNotFoundException("Missing groomed course definition: "+blendResource);
            var blend=TerrainBlendDefinition.FromJson(text.text);
            SnowShoulderWidthMeters=blend.edge_width_m;
            try
            {
                // Build every mesh before exposing any generated objects. Each stable loop owns one mesh.
                loopIds=new string[plan.Visual.Count];closedLoops=new bool[loopIds.Length];meshes=new Mesh[loopIds.Length];renderers=new MeshRenderer[loopIds.Length];
                for(int i=0;i<loopIds.Length;i++)
                {
                    string id=plan.Visual.Get(i).LoopId;var loop=plan.Track.GetLoop(id);
                    var mesh=TrackRibbonBuilder.Build(plan.Track,plan.Visual,id);ownedMeshes.Add(mesh);
                    Groom(mesh,loop.Closed);loopIds[i]=id;closedLoops[i]=loop.Closed;meshes[i]=mesh;indices.Add(id,i);
                }
                material=new Material(template){name=plan.Manifest.Id+" groomed course"};blend.Configure(material);
                TrackSnapshotJson=plan.Track.ToJson();VisualSnapshotJson=plan.Visual.ToJson();VenueId=plan.Manifest.Id;
                generated=new GameObject("Production course "+VenueId);generated.transform.SetParent(transform,false);
                // Visual coordinates already belong to the authored world, regardless of host placement.
                generated.transform.SetPositionAndRotation(Vector3.zero,Quaternion.identity);
                for(int i=0;i<loopIds.Length;i++)
                {
                    var go=new GameObject("Course surface "+loopIds[i]);go.transform.SetParent(generated.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=meshes[i];renderers[i]=go.AddComponent<MeshRenderer>();
                    renderers[i].sharedMaterial=material;
                }
            }
            catch{Shutdown();throw;}
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
            var prepared=new Mesh[meshes.Length];var objects=new GameObject[meshes.Length];float maximum=0;int tapered=0,vertical=0;
            try
            {
                var ground=new JoinGroundSurface(environment,terrainGridSpacing,terrainGridOrigin);
                var groomed=new JoinRibbonSurface(meshes,terrainGridSpacing,terrainGridOrigin);
                for(int i=0;i<meshes.Length;i++)
                {prepared[i]=BuildSkirt(meshes[i],closedLoops[i],ground,groomed,SnowShoulderWidthMeters,out float gap,out int slopeCount,out int wallCount);maximum=Mathf.Max(maximum,gap);tapered+=slopeCount;vertical+=wallCount;}
                // A failed preparation does not alter the existing groomed surfaces or publish partial joins.
                var nextRenderers=new MeshRenderer[meshes.Length];
                for(int i=0;i<meshes.Length;i++)
                {
                    var go=new GameObject("Course snow edge "+loopIds[i]);objects[i]=go;go.SetActive(false);go.transform.SetParent(generated.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=prepared[i];nextRenderers[i]=go.AddComponent<MeshRenderer>();nextRenderers[i].sharedMaterial=material;
                }
                skirts=prepared;skirtRenderers=nextRenderers;MaximumEdgeGapMeters=maximum;
                TaperedSnowPanelCount=tapered;VerticalSnowPanelCount=vertical;
                foreach(var mesh in prepared)ownedMeshes.Add(mesh);
                foreach(var go in objects)go.SetActive(true);
            }
            catch
            {
                foreach(var go in objects)DestroyOwned(go);
                foreach(var mesh in prepared)DestroyOwned(mesh);
                throw;
            }
        }
        private readonly struct JoinVertex
        {
            internal readonly double X,Y,Z,Blend,Travel;
            internal JoinVertex(Vector3 point,double blend=0,double travel=0)
            {X=point.x;Y=point.y;Z=point.z;Blend=blend;Travel=travel;}
            private JoinVertex(double x,double y,double z,double blend,double travel)
            {X=x;Y=y;Z=z;Blend=blend;Travel=travel;}
            internal static JoinVertex Lerp(JoinVertex a,JoinVertex b,double t)
                =>new JoinVertex(a.X+(b.X-a.X)*t,a.Y+(b.Y-a.Y)*t,a.Z+(b.Z-a.Z)*t,
                    a.Blend+(b.Blend-a.Blend)*t,a.Travel+(b.Travel-a.Travel)*t);
        }
        private readonly struct JoinGroundTriangle
        {
            internal readonly JoinVertex A,B,C;
            internal readonly double Area;
            internal JoinGroundTriangle(Vector3 a,Vector3 b,Vector3 c)
            {A=new JoinVertex(a);B=new JoinVertex(b);C=new JoinVertex(c);Area=Cross(A,B,C);}
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
        /// <summary>Cold index of the terrain renderer's actual geometry, including its world transform.
        /// Sampling outside covered cells fails rather than extrapolating a terrain edge.</summary>
        private sealed class JoinGroundSurface
        {
            private readonly Dictionary<(int,int),List<JoinGroundTriangle>> cells=new Dictionary<(int,int),List<JoinGroundTriangle>>();
            internal readonly double spacing;
            internal readonly Vector2 origin;
            internal JoinGroundSurface(ProductionVenueEnvironment environment,float spacing,Vector2 origin)
            {
                this.spacing=spacing;this.origin=origin;
                var filters=environment.GetComponentsInChildren<MeshFilter>(true);
                for(int tile=0;tile<environment.TerrainTileCount;tile++)
                {
                    var mesh=environment.TileMesh(tile);Transform owner=null;
                    foreach(var filter in filters)if(filter.sharedMesh==mesh){owner=filter.transform;break;}
                    if(owner==null)throw new ArgumentException("The actual terrain mesh must have its owned renderer.");
                    var vertices=mesh.vertices;var indices=mesh.triangles;
                    for(int i=0;i<vertices.Length;i++)vertices[i]=owner.TransformPoint(vertices[i]);
                    for(int i=0;i<indices.Length;i+=3)
                    {
                        var triangle=new JoinGroundTriangle(vertices[indices[i]],vertices[indices[i+1]],vertices[indices[i+2]]);
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
            internal IEnumerable<JoinGroundTriangle> Over(JoinVertex a,JoinVertex b,JoinVertex c)
            {
                // Adjacent cells are included for floating-point transforms at a shared grid boundary.
                int x0=(int)Math.Floor((Math.Min(a.X,Math.Min(b.X,c.X))-origin.x)/spacing)-1;
                int x1=(int)Math.Floor((Math.Max(a.X,Math.Max(b.X,c.X))-origin.x)/spacing)+1;
                int z0=(int)Math.Floor((Math.Min(a.Z,Math.Min(b.Z,c.Z))-origin.y)/spacing)-1;
                int z1=(int)Math.Floor((Math.Max(a.Z,Math.Max(b.Z,c.Z))-origin.y)/spacing)+1;
                for(int z=z0;z<=z1;z++)for(int x=x0;x<=x1;x++)if(cells.TryGetValue((x,z),out var cell))foreach(var triangle in cell)yield return triangle;
            }
            internal float Height(Vector3 point)
            {
                var vertex=new JoinVertex(point);
                foreach(var triangle in Over(vertex,vertex,vertex))if(triangle.Contains(vertex))return (float)triangle.Height(vertex);
                throw new ArgumentException("Actual terrain must cover the complete snow shoulder.");
            }
        }
        private sealed class JoinRibbonSurface
        {
            private readonly Dictionary<(int,int),List<JoinGroundTriangle>> cells=new Dictionary<(int,int),List<JoinGroundTriangle>>();
            private readonly Dictionary<(int,int),List<PotentialShoulder>> shoulders=new Dictionary<(int,int),List<PotentialShoulder>>();
            private readonly Mesh[] ribbons;private bool shouldersReady;
            internal readonly struct PotentialShoulder
            {
                internal readonly Mesh Ribbon;internal readonly int Side,Segment,Count;internal readonly JoinGroundTriangle Face;
                internal PotentialShoulder(Mesh ribbon,int side,int segment,int count,JoinGroundTriangle face)
                {Ribbon=ribbon;Side=side;Segment=segment;Count=count;Face=face;}
            }
            private readonly double spacing;private readonly Vector2 origin;
            internal JoinRibbonSurface(Mesh[] ribbons,float spacing,Vector2 origin)
            {
                this.ribbons=ribbons;this.spacing=spacing;this.origin=origin;
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
            internal void PreparePotentialShoulders(float width)
            {
                if(shouldersReady)return;shouldersReady=true;
                foreach(var ribbon in ribbons)
                {
                    var points=ribbon.vertices;int count=points.Length/2-1;
                    for(int side=0;side<2;side++)for(int segment=0;segment<count;segment++)
                    {
                        var a=points[segment*2+side];var b=points[(segment+1)*2+side];var delta=b-a;delta.y=0;
                        if(delta.sqrMagnitude<1e-12f)continue;
                        var normal=new Vector3(delta.z,0,-delta.x).normalized*(side==0?-1:1);var outerA=a+normal*width;var outerB=b+normal*width;
                        Capture(new JoinGroundTriangle(a,b,outerA));Capture(new JoinGroundTriangle(b,outerB,outerA));
                        void Capture(JoinGroundTriangle face)
                        {
                            var candidate=new PotentialShoulder(ribbon,side,segment,count,face);
                            foreach(var key in Keys(face.A,face.B,face.C))
                            {if(!shoulders.TryGetValue(key,out var list)){list=new List<PotentialShoulder>();shoulders.Add(key,list);}list.Add(candidate);}
                        }
                    }
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
        {
            var source=ribbon.vertices;var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            groomed.PreparePotentialShoulders(width);
            var vertexIndices=new Dictionary<(Vector3,Vector2),int>();float maximum=0;int slopes=0,walls=0;
            var originalCurtains=new Dictionary<(Vector3,Vector3,bool),Vector3[]>();
            for(int side=0;side<2;side++)
            {
                var path=new List<Vector3>();var outward=new List<Vector3>();
                for(int row=side;row<source.Length;row+=2)
                {path.Add(source[row]);var across=source[row]-(source[row-side]+source[row-side+1])*.5f;across.y=0;outward.Add(across.normalized);}
                int count=path.Count-1;var eligible=new bool[count];var normal=new Vector3[count];
                for(int i=0;i<count;i++)
                {var delta=path[i+1]-path[i];delta.y=0;normal[i]=new Vector3(delta.z,0,-delta.x).normalized*(side==0?-1:1);}
                for(int i=0;i<count;i++)
                {
                    var delta=path[i+1]-path[i];delta.y=0;
                    bool straight=Vector3.Dot(normal[i],outward[i])>1-1e-6f&&Vector3.Dot(normal[i],outward[i+1])>1-1e-6f;
                    if(i>0)straight&=Parallel(path[i]-path[i-1],delta);else if(closed)straight&=Parallel(path[count]-path[count-1],delta);
                    if(i+1<count)straight&=Parallel(delta,path[i+2]-path[i+1]);else if(closed)straight&=Parallel(delta,path[1]-path[0]);
                    // Approximate tangent agreement alone can create a slit/overlap at a
                    // very small turn. Both panels must publish the exact same outer seam.
                    if(i>0)straight&=SharedOuter(i-1,i,path[i]);else if(closed)straight&=SharedOuter(count-1,i,path[i]);
                    if(i+1<count)straight&=SharedOuter(i,i+1,path[i+1]);else if(closed)straight&=SharedOuter(i,0,path[i+1]);
                    if(!straight||delta.sqrMagnitude<1e-12f)continue;
                    var a=path[i];var b=path[i+1];var outerA=a+normal[i]*width;var outerB=b+normal[i]*width;
                    eligible[i]=ClearFootprint(new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))&&
                        ClearFootprint(new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA))&&
                        CoveredFootprint(new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))&&
                        CoveredFootprint(new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA))&&
                        ClearOtherShoulders(side,i,new JoinVertex(a),new JoinVertex(b),new JoinVertex(outerA))&&
                        ClearOtherShoulders(side,i,new JoinVertex(b),new JoinVertex(outerB),new JoinVertex(outerA));
                }
                bool SharedOuter(int first,int second,Vector3 pivot)=>(pivot+normal[first]*width).Equals(pivot+normal[second]*width);
                // Validate the actual float triangles before selecting transition caps. A
                // float clipping result can differ from its ideal footprint near a tile seam.
                var panels=new List<JoinVertex[]>[count];double prepareTravel=0;
                for(int i=0;i<count;i++)
                {
                    var a=path[i];var b=path[i+1];double next=prepareTravel+Vector3.Distance(a,b);
                    if(eligible[i])
                    {
                        var outerA=Foot(a+normal[i]*width);var outerB=Foot(b+normal[i]*width);var panel=new List<JoinVertex[]>();
                        if(PrepareTaper(new JoinVertex(a,0,prepareTravel),new JoinVertex(b,0,next),new JoinVertex(outerA,1,prepareTravel),panel)&&
                            PrepareTaper(new JoinVertex(b,0,next),new JoinVertex(outerB,1,next),new JoinVertex(outerA,1,prepareTravel),panel))panels[i]=panel;
                        else eligible[i]=false;
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
                        if(!previous&&!ClearCurtain(path[i],Foot(path[i]+normal[i]*width))||
                            !following&&!ClearCurtain(path[i+1],Foot(path[i+1]+normal[i]*width)))
                        {eligible[i]=false;changed=true;}
                    }
                }while(changed);
                double travel=0;
                for(int i=0;i<count;i++)
                {
                    var a=path[i];var b=path[i+1];double next=travel+Vector3.Distance(a,b);
                    if(!eligible[i]){if(Curtain(a,b,side==1,travel,next,true))walls++;}
                    else
                    {
                        var outerA=Foot(a+normal[i]*width);var outerB=Foot(b+normal[i]*width);
                        foreach(var face in panels[i])
                        {
                            int first=Index(face[0],Point(face[0])),second=Index(face[1],Point(face[1])),third=Index(face[2],Point(face[2]));
                            triangles.Add(first);triangles.Add(second);triangles.Add(third);
                        }
                        slopes++;
                        bool previous=i>0?eligible[i-1]:closed&&eligible[count-1],following=i+1<count?eligible[i+1]:closed&&eligible[0];
                        if(!previous)Curtain(a,outerA,side==1,travel,travel);
                        if(!following)Curtain(b,outerB,side==0,next,next);
                    }
                    travel=next;
                }
            }
            if(!closed)
            {if(Curtain(source[0],source[1],true,0,Vector3.Distance(source[0],source[1]),true))walls++;
                if(Curtain(source[source.Length-2],source[source.Length-1],false,0,Vector3.Distance(source[source.Length-2],source[source.Length-1]),true))walls++;}
            // Clipped zero-clearance pieces can leave unused points. Keep the published mesh
            // compact so every vertex has a finite, meaningful normal.
            var used=new bool[vertices.Count];foreach(int index in triangles)used[index]=true;
            var compact=new int[vertices.Count];int destination=0;
            for(int i=0;i<vertices.Count;i++)if(used[i]){compact[i]=destination;vertices[destination]=vertices[i];uv[destination]=uv[i];colors[destination]=colors[i];destination++;}
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
                    if(!double.IsFinite(square)||square<=0)throw new ArgumentException("Snow joins require finite nonzero face normals.");
                    // Vector3.normalized rounds very small, valid accumulated face areas to zero.
                    normals[i]=normal/(float)Math.Sqrt(square);
                }
                mesh.normals=normals;mesh.RecalculateBounds();maximumGap=maximum;taperedCount=slopes;verticalCount=walls;return mesh;
                void Sum(Vector3 point,Vector3 normal){summed.TryGetValue(point,out var prior);summed[point]=prior+normal;}
            }
            catch{DestroyOwned(mesh);throw;}

            bool Parallel(Vector3 a,Vector3 b){a.y=b.y=0;return a.sqrMagnitude>1e-12f&&b.sqrMagnitude>1e-12f&&Vector3.Dot(a.normalized,b.normalized)>1-1e-6f;}
            Vector3 Foot(Vector3 point){point.y=ground.Height(point);return point;}
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
            bool PrepareTaper(JoinVertex a,JoinVertex b,JoinVertex c,List<JoinVertex[]> pending)
            {
                double expected=Math.Abs(Cross(a,b,c)),covered=0;
                foreach(var terrain in ground.Over(a,b,c))
                {
                    double sign=terrain.Area<0?-1:1;var polygon=new List<JoinVertex>{a,b,c};
                    polygon=ClipJoin(polygon,p=>sign*Cross(terrain.A,terrain.B,p));polygon=ClipJoin(polygon,p=>sign*Cross(terrain.B,terrain.C,p));polygon=ClipJoin(polygon,p=>sign*Cross(terrain.C,terrain.A,p));
                    covered+=Area(polygon);
                    for(int i=0;i<polygon.Count;i++)
                    {
                        var point=polygon[i];if(point.Blend<1e-9)CheckGap(point.Y-terrain.Height(point));
                        if(point.Blend>1-1e-9)polygon[i]=new JoinVertex(new Vector3((float)point.X,(float)terrain.Height(point),(float)point.Z),1,point.Travel);
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
            bool Curtain(Vector3 a,Vector3 b,bool reverse,double travelA,double travelB,bool original=false)
            {
                var rows=CurtainRows(a,b);
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
            void Face(int a,int b,int c)
            {if(Vector3.Cross(vertices[b]-vertices[a],vertices[c]-vertices[a]).sqrMagnitude<1e-16f)return;triangles.Add(a);triangles.Add(b);triangles.Add(c);}
            void CheckGap(double gap){if(!double.IsFinite(gap)||gap< -1e-4)throw new ArgumentException("Actual ground must remain beneath the groomed course edge.");maximum=Mathf.Max(maximum,(float)Math.Max(0,gap));}
            Vector3 Point(JoinVertex vertex)=>new Vector3((float)vertex.X,(float)vertex.Y,(float)vertex.Z);
            int Index(JoinVertex vertex,Vector3 point)
            {
                var texture=new Vector2((float)(vertex.Blend*width),(float)vertex.Travel);var key=(point,texture);if(vertexIndices.TryGetValue(key,out int index))return index;
                index=vertices.Count;vertexIndices.Add(key,index);vertices.Add(point);uv.Add(texture);float blend=(float)Math.Max(0,Math.Min(1,vertex.Blend));colors.Add(new Color(0,1-blend,0,blend));return index;
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
        }
        private void OnDisable(){if(generated!=null)generated.SetActive(false);}
        private void OnEnable(){if(generated!=null)generated.SetActive(true);}
        public void Shutdown()
        {
            if(generated!=null){generated.SetActive(false);generated.transform.SetParent(null);DestroyOwned(generated);generated=null;}
            foreach(var mesh in ownedMeshes)DestroyOwned(mesh);ownedMeshes.Clear();DestroyOwned(material);material=null;
            indices.Clear();loopIds=null;closedLoops=null;meshes=null;renderers=null;skirts=null;skirtRenderers=null;MaximumEdgeGapMeters=SnowShoulderWidthMeters=0;TaperedSnowPanelCount=VerticalSnowPanelCount=0;VenueId=null;TrackSnapshotJson=null;VisualSnapshotJson=null;
        }
        private static void DestroyOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Shutdown();
    }
}
