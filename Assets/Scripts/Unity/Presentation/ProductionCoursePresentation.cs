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
        /// <summary>Cold, presentation-only snow faces beneath existing ribbon edges.
        /// Every terrain grid/diagonal crossing is retained so the foot follows the actual
        /// piecewise affine ground. No ribbon vertex, terrain sample or course source is changed.</summary>
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
            var prepared=new Mesh[meshes.Length];var objects=new GameObject[meshes.Length];float maximum=0;
            try
            {
                for(int i=0;i<meshes.Length;i++)
                {prepared[i]=BuildSkirt(meshes[i],closedLoops[i],environment.SampleGround,terrainGridSpacing,terrainGridOrigin,out float gap);maximum=Mathf.Max(maximum,gap);}
                // A failed preparation does not alter the existing groomed surfaces or publish partial joins.
                var nextRenderers=new MeshRenderer[meshes.Length];
                for(int i=0;i<meshes.Length;i++)
                {
                    var go=new GameObject("Course snow edge "+loopIds[i]);objects[i]=go;go.SetActive(false);go.transform.SetParent(generated.transform,false);
                    go.AddComponent<MeshFilter>().sharedMesh=prepared[i];nextRenderers[i]=go.AddComponent<MeshRenderer>();nextRenderers[i].sharedMaterial=material;
                }
                skirts=prepared;skirtRenderers=nextRenderers;MaximumEdgeGapMeters=maximum;
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
        private static Mesh BuildSkirt(Mesh ribbon,bool closed,Func<Vector3,float> ground,float spacing,Vector2 origin,out float maximumGap)
        {
            var source=ribbon.vertices;var vertices=new List<Vector3>();var uv=new List<Vector2>();var colors=new List<Color>();var triangles=new List<int>();
            var seams=new List<int>();float maximum=0;
            for(int side=0;side<2;side++)
            {
                var points=new List<Vector3>();for(int row=side;row<source.Length;row+=2)points.Add(source[row]);
                AddCurtain(points,side==1,closed);
            }
            if(!closed)
            {
                AddCurtain(new List<Vector3>{source[0],source[1]},true,false);
                AddCurtain(new List<Vector3>{source[source.Length-2],source[source.Length-1]},false,false);
            }
            var mesh=new Mesh{name=ribbon.name+" snow edge join",indexFormat=UnityEngine.Rendering.IndexFormat.UInt32};
            mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetColors(colors);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();
            if(seams.Count>0)
            {
                var normals=mesh.normals;
                for(int i=0;i<seams.Count;i+=2)for(int edge=0;edge<2;edge++)
                {int a=seams[i]+edge,b=seams[i+1]+edge;var normal=(normals[a]+normals[b]).normalized;normals[a]=normals[b]=normal;}
                mesh.normals=normals;
            }
            mesh.RecalculateBounds();maximumGap=maximum;return mesh;

            void AddCurtain(List<Vector3> path,bool reverse,bool close)
            {
                int first=vertices.Count,prior=-1;float travel=0;Vector3 previous=default;
                for(int segment=1;segment<path.Count;segment++)
                {
                    var a=path[segment-1];var b=path[segment];var cuts=new SortedSet<double>{0,1};
                    AddCuts(a.x,b.x,origin.x);AddCuts(a.z,b.z,origin.y);AddCuts((double)a.x+a.z,(double)b.x+b.z,(double)origin.x+origin.y);
                    foreach(double fraction in cuts)
                    {
                        if(segment>1&&fraction==0)continue;
                        var top=new Vector3((float)(a.x+((double)b.x-a.x)*fraction),(float)(a.y+((double)b.y-a.y)*fraction),(float)(a.z+((double)b.z-a.z)*fraction));
                        if(prior>=0&&top==previous)continue;
                        float height=ground(top);
                        if(!float.IsFinite(height)||height>top.y+1e-4f)throw new ArgumentException("Actual ground must remain beneath the groomed course edge.");
                        if(prior>=0)travel+=Vector3.Distance(previous,top);
                        var bottom=new Vector3(top.x,height,top.z);int current=vertices.Count;vertices.Add(top);vertices.Add(bottom);
                        float gap=Mathf.Max(0,top.y-height);maximum=Mathf.Max(maximum,gap);uv.Add(new Vector2(0,travel));uv.Add(new Vector2(gap,travel));
                        colors.Add(new Color(0,1,0,0));colors.Add(new Color(0,0,0,1));
                        if(prior>=0)
                        {
                            if(reverse){triangles.Add(prior);triangles.Add(current);triangles.Add(prior+1);triangles.Add(current);triangles.Add(current+1);triangles.Add(prior+1);}
                            else{triangles.Add(prior);triangles.Add(prior+1);triangles.Add(current);triangles.Add(current);triangles.Add(prior+1);triangles.Add(current+1);}
                        }
                        prior=current;previous=top;
                    }
                    void AddCuts(double start,double end,double gridOrigin)
                    {
                        if(start==end)return;
                        long low=(long)Math.Ceiling((Math.Min(start,end)-gridOrigin)/spacing),high=(long)Math.Floor((Math.Max(start,end)-gridOrigin)/spacing);
                        for(long line=low;line<=high;line++)
                        {double fraction=(gridOrigin+line*spacing-start)/(end-start);if(fraction>0&&fraction<1)cuts.Add(fraction);}
                    }
                }
                if(close&&prior>=first){seams.Add(first);seams.Add(prior);}
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
            indices.Clear();loopIds=null;closedLoops=null;meshes=null;renderers=null;skirts=null;skirtRenderers=null;MaximumEdgeGapMeters=0;VenueId=null;TrackSnapshotJson=null;VisualSnapshotJson=null;
        }
        private static void DestroyOwned(Object value){if(value==null)return;if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Shutdown();
    }
}
