using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using Object=UnityEngine.Object;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Static, grounded team-staff enclosure. No colliders, simulation writes or per-frame callbacks.</summary>
    public sealed class RangeCoachesPresentation:MonoBehaviour
    {
        private GameObject generated;
        private Mesh scenery;
        private RangeCoachArt[] coaches;
        public RangeCoachesLayout Layout{get;private set;}
        public int Count=>coaches?.Length??0;
        public int ScopeCount=>Count;
        public RangeCoachArt Coach(int index)=>coaches[index];
        public Vector3 ScopeEye(int index)=>coaches[index].Eye+Layout.Forward*.025f;
        public Vector3 ScopeDirection(int index)=>directions[index];
        private Vector3[] directions;
        private float firRadius,pineRadius,windRadius;
        public void Initialize(RangeCoachesDefinition source,ProductionEnvironmentDefinition environment,Func<Vector3,float> ground)
        {
            if(generated!=null||source==null||environment==null||ground==null)throw new ArgumentException("Fresh coach art and actual terrain required.");
            var d=RangeCoachesDefinition.FromJson(JsonUtility.ToJson(source));var layout=d.Layout(environment.range,ground);
            if(!layout.ClearsCourses(environment.route_corridors,d.route_clearance_m))throw new ArgumentException("Coaches' enclosure overlaps an athlete corridor.");
            var prefabs=new RangeCoachArt[d.coaches.Length];
            for(int i=0;i<prefabs.Length;i++)prefabs[i]=Resources.Load<RangeCoachArt>(d.art_resource+"/"+d.coaches[i].id)??throw new ArgumentException("Bake the original coach art before delivery.");
            try
            {
                Layout=layout;generated=new GameObject("Coaches' enclosure");generated.transform.SetParent(transform,false);
                firRadius=CrownRadius("G2Fir/Fir");pineRadius=CrownRadius("G2Pine/Pine");windRadius=VegetationDefinition.Load().wind_amplitude_m;
                generated.transform.SetPositionAndRotation(layout.Origin,Quaternion.LookRotation(layout.Forward));
                var build=new RangeCoachMesh();var steel=new Color(.075f,.115f,.14f);var snow=new Color(.84f,.89f,.91f);var wood=new Color(.22f,.27f,.29f);
                float front=layout.Front,back=layout.Back,top=layout.Top,half=layout.HalfWidth;
                int modules=Mathf.CeilToInt(half*2/3);
                for(int i=0;i<modules;i++)
                {
                    float x0=Mathf.Lerp(-half,half,i/(float)modules),x1=Mathf.Lerp(-half,half,(i+1f)/modules);
                    float bottom=float.PositiveInfinity;
                    foreach(float x in new[]{x0,(x0+x1)*.5f,x1})foreach(float z in new[]{back,(back+front)*.5f,front})bottom=Mathf.Min(bottom,ground(layout.World(x,0,z))-layout.Origin.y-.12f);
                    build.Box(new Vector3((x0+x1)*.5f,(bottom+top-.045f)*.5f,(front+back)*.5f),new Vector3(x1-x0,top-.045f-bottom,front-back),wood);
                    build.Box(new Vector3((x0+x1)*.5f,top-.0225f,(front+back)*.5f),new Vector3(x1-x0,.045f,front-back),snow);
                    Rail(x0,x1,front,false);Rail(x0,x1,back,true);
                }
                for(int i=0;i<=modules;i++)
                {
                    float x=Mathf.Lerp(-half,half,i/(float)modules);Post(x,front);
                    if(Mathf.Abs(x)>d.gate_width_m*.5f+.05f)Post(x,back);
                }
                foreach(float x in new[]{-half,half})foreach(float y in new[]{.35f,.65f,d.fence_height_m})build.Box(new Vector3(x,top+y,(front+back)*.5f),new Vector3(.045f,.045f,front-back),steel);
                // A real rear access opening, low steps and a clear media strip behind it.
                foreach(float x in new[]{-d.gate_width_m*.5f,d.gate_width_m*.5f})Post(x,back);
                build.Box(new Vector3(0,top*.5f-.045f,back-.22f),new Vector3(d.gate_width_m,top-.09f,.44f),snow);
                coaches=new RangeCoachArt[prefabs.Length];directions=new Vector3[coaches.Length];
                for(int i=0;i<coaches.Length;i++)
                {
                    var coach=Instantiate(prefabs[i],generated.transform,false);coach.transform.position=layout.CoachPosition(i,coaches.Length);coach.transform.rotation=generated.transform.rotation;coaches[i]=coach;
                    Vector3 eye=coach.Eye+layout.Forward*.025f;var bank=environment.range.banks[0];float nearest=float.PositiveInfinity;
                    foreach(var candidate in environment.range.banks){float distance=Mathf.Abs(Vector3.Dot(candidate.position-coach.transform.position,layout.Right));if(distance<nearest){nearest=distance;bank=candidate;}}
                    var direction=(bank.position-eye).normalized;directions[i]=direction;
                    Scope(generated.transform.InverseTransformPoint(eye),generated.transform.InverseTransformDirection(direction));
                    var notebook=generated.transform.InverseTransformPoint(coach.Notebook);
                    build.Box(notebook,new Vector3(.19f,.25f,.018f),steel);
                    build.Box(notebook+Vector3.forward*.01f,new Vector3(.16f,.21f,.003f),new Color(.70f,.76f,.74f));
                    for(int row=0;row<4;row++)build.Box(notebook+new Vector3(0,(row-1.5f)*.04f,.012f),new Vector3(.12f,.002f,.001f),steel);
                    if(i%2==0)TeamPanel(d.coaches[i].nation,Mathf.Lerp(-half+2,half-2,(i+.5f)/(coaches.Length-1f)));
                }
                scenery=build.Build("Coaches' railings, raised snow deck and twelve spotting scopes");
                var props=new GameObject("Static coaches' equipment");props.transform.SetParent(generated.transform,false);props.AddComponent<MeshFilter>().sharedMesh=scenery;props.AddComponent<MeshRenderer>().sharedMaterial=Resources.Load<Material>(d.art_resource+"/CoachSurface");
                void Post(float x,float z){build.Box(new Vector3(x,top+d.fence_height_m*.5f,z),new Vector3(.07f,d.fence_height_m+.08f,.07f),steel);build.Box(new Vector3(x,top+d.fence_height_m+.045f,z),new Vector3(.09f,.025f,.09f),snow);}
                void Rail(float x0,float x1,float z,bool rear)
                {
                    if(rear&&x0<d.gate_width_m*.5f&&x1>-d.gate_width_m*.5f)
                    {if(x0<-d.gate_width_m*.5f)Rail(x0,-d.gate_width_m*.5f,z,false);if(x1>d.gate_width_m*.5f)Rail(d.gate_width_m*.5f,x1,z,false);return;}
                    foreach(float y in new[]{.35f,.65f,d.fence_height_m})build.Box(new Vector3((x0+x1)*.5f,top+y,z),new Vector3(x1-x0,.045f,.045f),steel);
                }
                void Scope(Vector3 eye,Vector3 direction)
                {
                    var center=eye+direction*.24f;var hub=center-Vector3.up*.17f;
                    build.Tube(eye,eye+direction*.09f,.024f,steel);build.Tube(eye+direction*.08f,eye+direction*.45f,.044f,new Color(.18f,.23f,.19f));
                    build.Tube(eye+direction*.40f,eye+direction*.54f,.057f,steel);
                    build.Tube(eye+direction*.541f,eye+direction*.544f,.046f,new Color(.10f,.34f,.42f),2);
                    build.Tube(center+Vector3.right*.035f,center+Vector3.right*.075f,.022f,steel);
                    build.Tube(hub,center,.018f,steel);
                    for(int leg=0;leg<3;leg++)
                    {
                        float angle=(leg*120+30)*Mathf.Deg2Rad;var foot=new Vector3(hub.x+Mathf.Cos(angle)*.36f,top+.025f,hub.z+Mathf.Sin(angle)*.36f);
                        build.Tube(hub,foot,.014f,steel);build.Tube(foot,foot+Vector3.up*.04f,.024f,steel);
                    }
                    build.Box(new Vector3(hub.x+.55f,top+.14f,hub.z-.35f),new Vector3(.40f,.28f,.24f),new Color(.11f,.15f,.18f));
                }
                void TeamPanel(string nation,float x)
                {
                    build.Box(new Vector3(x,top+.57f,front+.028f),new Vector3(1.8f,.5f,.035f),new Color(.055f,.18f,.22f));
                    var flags=RangeFlagDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/Venues/woodland_basin_v1/range_flags_v1").text);
                    var flag=Array.Find(flags.nations,n=>n.id==nation);float width=.66f,height=width/flag.aspect;
                    Paint(0,0,1,1,RangeFlagDefinition.ColorOf(flag.background),0);
                    for(int p=0;p<flag.marks.Length;p++){var m=flag.marks[p];Paint(m.x,m.y,m.width,m.height,RangeFlagDefinition.ColorOf(m.color),(p+1)*.0004f);}
                    void Paint(float u,float v,float w,float h,Color color,float offset)=>build.Box(new Vector3(x+(u+w*.5f-.5f)*width,top+.57f+(v+h*.5f-.5f)*height,front+.050f+offset),new Vector3(w*width,h*height,.001f),color);
                }
            }
            catch{Shutdown();throw;}
        }
        public bool ClearsTree(ProductionTree tree)
        {
            // Actual all-LOD crown radius plus wind, instead of only trunk distance.
            return Layout.DistanceToFootprint(tree.position)>(tree.species=="pine"?pineRadius:firRadius)*tree.scale+windRadius+1.5f;
        }
        private static float CrownRadius(string species)
        {float radius=0;for(int lod=0;lod<3;lod++){var b=Resources.Load<Mesh>("Biathlon/Vegetation/"+species+"LOD"+lod).bounds;radius=Mathf.Max(radius,new Vector2(Mathf.Max(Mathf.Abs(b.min.x),Mathf.Abs(b.max.x)),Mathf.Max(Mathf.Abs(b.min.z),Mathf.Abs(b.max.z))).magnitude);}return radius;}
        public void Shutdown()
        {
            if(generated!=null){generated.SetActive(false);Release(generated);generated=null;}
            if(scenery!=null){Release(scenery);scenery=null;}coaches=null;directions=null;Layout=null;
        }
        private static void Release(Object value){if(Application.isPlaying)Destroy(value);else DestroyImmediate(value);}
        private void OnDestroy()=>Shutdown();
    }
    internal sealed class RangeCoachMesh
    {
        private readonly List<Vector3> vertices=new List<Vector3>(),normals=new List<Vector3>();
        private readonly List<Color> colors=new List<Color>();private readonly List<Vector4> metadata=new List<Vector4>(),tangents=new List<Vector4>();private readonly List<int> indices=new List<int>();
        public void Box(Vector3 center,Vector3 size,Color color)
        {
            Face(Vector3.right,Vector3.back,Vector3.up);Face(Vector3.left,Vector3.forward,Vector3.up);Face(Vector3.up,Vector3.right,Vector3.back);Face(Vector3.down,Vector3.right,Vector3.forward);Face(Vector3.forward,Vector3.right,Vector3.up);Face(Vector3.back,Vector3.left,Vector3.up);
            void Face(Vector3 n,Vector3 u,Vector3 v)
            {
                int start=vertices.Count;var origin=center+Vector3.Scale(n,size*.5f);var du=Vector3.Scale(u,size*.5f);var dv=Vector3.Scale(v,size*.5f);
                Add(origin-du-dv,n,color,5);Add(origin+du-dv,n,color,5);Add(origin+du+dv,n,color,5);Add(origin-du+dv,n,color,5);
                indices.AddRange(new[]{start,start+1,start+2,start,start+2,start+3});
            }
        }
        public void Tube(Vector3 a,Vector3 b,float radius,Color color,float kind=5)
        {
            const int sides=12;var axis=(b-a).normalized;var q=Quaternion.FromToRotation(Vector3.forward,axis);
            for(int i=0;i<sides;i++)
            {
                float t=i*2*Mathf.PI/sides,u=(i+1)*2*Mathf.PI/sides;var n=q*new Vector3(Mathf.Cos(t),Mathf.Sin(t),0);var m=q*new Vector3(Mathf.Cos(u),Mathf.Sin(u),0);int start=vertices.Count;
                Add(a+n*radius,n,color,kind);Add(a+m*radius,m,color,kind);Add(b+n*radius,n,color,kind);Add(b+m*radius,m,color,kind);
                indices.AddRange(new[]{start,start+1,start+2,start+1,start+3,start+2});
                start=vertices.Count;Add(a,-axis,color,kind);Add(a+m*radius,-axis,color,kind);Add(a+n*radius,-axis,color,kind);indices.AddRange(new[]{start,start+1,start+2});
                start=vertices.Count;Add(b,axis,color,kind);Add(b+n*radius,axis,color,kind);Add(b+m*radius,axis,color,kind);indices.AddRange(new[]{start,start+1,start+2});
            }
        }
        private void Add(Vector3 p,Vector3 normal,Color color,float kind)
        {
            vertices.Add(p);normals.Add(normal);color=color.linear;color.a=0;colors.Add(color);metadata.Add(new Vector4(0,0,kind,0));var t=Vector3.Cross(Mathf.Abs(normal.y)<.9f?Vector3.up:Vector3.forward,normal).normalized;tangents.Add(new Vector4(t.x,t.y,t.z,1));
        }
        public Mesh Build(string name)
        {
            var mesh=new Mesh{name=name,indexFormat=IndexFormat.UInt32};mesh.SetVertices(vertices);mesh.SetNormals(normals);mesh.SetTangents(tangents);mesh.SetColors(colors);mesh.SetUVs(2,metadata);mesh.SetTriangles(indices,0);mesh.RecalculateBounds();return mesh;
        }
    }
}
