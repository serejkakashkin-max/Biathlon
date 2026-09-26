using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class BenchmarkBackdropSettings
    {
        public int schema_version,segments,rings,tree_rows,tree_columns;public string id;
        public float inner_radius_m,outer_radius_m,height_m,clear_fog_scale,overcast_fog_scale;
        public float tree_depth_m=85;
        public Vector3 centre;public Vector3[] service_tents;
        public static BenchmarkBackdropSettings Load()
        {
            var d=JsonUtility.FromJson<BenchmarkBackdropSettings>(Resources.Load<TextAsset>("Biathlon/g2_backdrop_v1").text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.segments<64||d.segments>192||d.rings<8||d.rings>32||
                !(d.inner_radius_m>=125&&d.outer_radius_m>d.inner_radius_m+100&&d.outer_radius_m<=600)||
                !(d.height_m>=20&&d.height_m<=100)||!float.IsFinite(d.centre.sqrMagnitude)||d.centre.y!=0||
                !(d.clear_fog_scale>=1&&d.clear_fog_scale<=3&&d.overcast_fog_scale>=1&&d.overcast_fog_scale<=2)||d.service_tents==null||d.service_tents.Length>6)
                throw new ArgumentException("Invalid benchmark backdrop definition.");
            if(d.tree_rows<1||d.tree_rows>16||d.tree_columns<32||d.tree_columns>384||!float.IsFinite(d.tree_depth_m)||d.tree_depth_m<20||d.tree_depth_m>d.outer_radius_m-d.inner_radius_m-20)throw new ArgumentException("Invalid distant forest layout.");
            foreach(var p in d.service_tents)if(!float.IsFinite(p.sqrMagnitude)||p.y!=0||VegetationDefinition.DistanceToRoute(p,G2BenchmarkSurface.CopyRoute())<9||p.z>0||p.sqrMagnitude>10000)throw new ArgumentException("Service tent inside protected event space.");
            return d;
        }
    }
    /// <summary>Original benchmark-only scenery, outside the venue route. No colliders or gameplay reads.</summary>
    public sealed class G2BenchmarkBackdrop : MonoBehaviour
    {
        private readonly List<Mesh> meshes=new List<Mesh>();private readonly List<Material> materials=new List<Material>();
        private BenchmarkBackdropSettings settings;
        private Matrix4x4[] trees,visibleTrees;private readonly Plane[] planes=new Plane[6];private Mesh treeMesh;private Material treeMaterial;private MaterialPropertyBlock treeProperties;
        public int VisibleTrees {get;private set;}
        public int TreeCount=>trees?.Length??0;
        public Matrix4x4 TreeTransform(int index)=>trees[index];
        public Mesh Landscape {get;private set;}
        public int TriangleCount {get;private set;}
        public void Initialize()
        {
            if(settings!=null)throw new InvalidOperationException("Backdrop already initialized.");settings=BenchmarkBackdropSettings.Load();
            var snow=new Material(Resources.Load<Material>("Biathlon/Terrain/G2Snow/WinterBlend"));snow.SetFloat("_TerrainBlend",0);snow.SetFloat("_DetailWeight",.12f);snow.SetFloat("_MacroScale",18);materials.Add(snow);
            Landscape=BuildLandscape(settings);AddMesh("Distant winter slopes",Landscape,snow);
            treeMesh=Resources.Load<Mesh>("Biathlon/Vegetation/G2Fir/FirLOD2");treeMaterial=Resources.Load<Material>("Biathlon/Vegetation/G2Fir/Fir");
            trees=new Matrix4x4[settings.tree_rows*settings.tree_columns];visibleTrees=new Matrix4x4[trees.Length];treeProperties=new MaterialPropertyBlock();treeProperties.SetFloat("_SnowCoverage",.72f);
            for(int row=0;row<settings.tree_rows;row++)for(int col=0;col<settings.tree_columns;col++)
            {
                // Jittered strata plus broad groves: no repeated radial rows on the skyline.
                uint seed=VegetationDefinition.Mix((uint)(row*settings.tree_columns+col+917));
                float Jitter(uint salt)=>(VegetationDefinition.Mix(seed^salt)&0xffff)/65535f;
                float phase=(col+Jitter(31))*Mathf.PI*2/settings.tree_columns;
                phase+=.13f*Mathf.Sin(phase*5+row*.21f);
                float radius=settings.inner_radius_m+12+(row+Jitter(73))/settings.tree_rows*settings.tree_depth_m;
                float scale=.7f+.55f*Jitter(109);
                var p=settings.centre+new Vector3(Mathf.Sin(phase)*radius,0,Mathf.Cos(phase)*radius);
                p.y=SurfaceHeight(settings,p)-treeMesh.bounds.min.y*scale-.12f;
                trees[row*settings.tree_columns+col]=Matrix4x4.TRS(p,Quaternion.Euler(0,Jitter(211)*360,0),Vector3.one*scale);
            }
            var cloth=new Material(Resources.Load<Material>("Biathlon/GreyboxLit"));cloth.SetColor("_BaseColor",new Color(.12f,.28f,.32f));materials.Add(cloth);
            var roofs=new List<Vector3>();var roofIndices=new List<int>();var walls=new List<Vector3>();var wallIndices=new List<int>();
            foreach(var c in settings.service_tents)
            {
                var a=c+new Vector3(-2,2.5f,-2);var b=c+new Vector3(2,2.5f,-2);var e=c+new Vector3(2,2.5f,2);var f=c+new Vector3(-2,2.5f,2);
                var peak=c+new Vector3(0,3.7f,0);Tri(roofs,roofIndices,a,peak,b);Tri(roofs,roofIndices,b,peak,e);Tri(roofs,roofIndices,e,peak,f);Tri(roofs,roofIndices,f,peak,a);
                // Front opening faces the route; the other three panels form a service shelter.
                Quad(walls,wallIndices,c+new Vector3(-2,0,-2),a,f,c+new Vector3(-2,0,2));
                Quad(walls,wallIndices,c+new Vector3(-2,0,2),f,e,c+new Vector3(2,0,2));
                Quad(walls,wallIndices,c+new Vector3(-2,0,-2),c+new Vector3(2,0,-2),b,a);
            }
            AddMesh("Service shelter roofs",Make(roofs,roofIndices),snow);AddMesh("Service shelter canvas",Make(walls,wallIndices),cloth);
        }
        public static Mesh BuildLandscape(BenchmarkBackdropSettings d)
        {
            var v=new Vector3[(d.rings+1)*(d.segments+1)];var indices=new int[d.rings*d.segments*6];int k=0;
            for(int r=0;r<=d.rings;r++)for(int s=0;s<=d.segments;s++)
            {
                float t=(float)r/d.rings,angle=s==d.segments?0:s*Mathf.PI*2/d.segments;
                float radius=Mathf.Lerp(d.inner_radius_m,d.outer_radius_m,t);
                float y=Height(d,t,angle);
                v[r*(d.segments+1)+s]=d.centre+new Vector3(Mathf.Sin(angle)*radius,y,Mathf.Cos(angle)*radius);
                if(r==d.rings||s==d.segments)continue;int a=r*(d.segments+1)+s,b=a+d.segments+1;
                indices[k++]=a;indices[k++]=b;indices[k++]=a+1;indices[k++]=a+1;indices[k++]=b;indices[k++]=b+1;
            }
            var mesh=new Mesh{name="Original distant winter basin",vertices=v,triangles=indices};mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static float Height(BenchmarkBackdropSettings d,float t,float angle)
        {
            float peaks=.57f+.22f*Mathf.Sin(angle*3+.8f)+.14f*Mathf.Sin(angle*7-1.2f)+.07f*Mathf.Sin(angle*17+t*11);
            return -1+Mathf.Pow(Mathf.Max(0,Mathf.Sin(t*Mathf.PI)),1.35f)*d.height_m*peaks;
        }
        public static float SurfaceHeight(BenchmarkBackdropSettings d,Vector3 worldPosition)
        {
            var local=worldPosition-d.centre;float radius=new Vector2(local.x,local.z).magnitude,angle=Mathf.Atan2(local.x,local.z);
            float row=Mathf.InverseLerp(d.inner_radius_m,d.outer_radius_m,radius)*d.rings;
            float col=Mathf.Repeat(angle/(2*Mathf.PI),1)*d.segments;
            int r=Mathf.Min(d.rings-1,Mathf.FloorToInt(row)),c=Mathf.FloorToInt(col);float v=row-r,u=col-c;
            Vector3 Vertex(int dr,int dc)
            {
                float t=(float)(r+dr)/d.rings,a=((c+dc)%d.segments)*2*Mathf.PI/d.segments,rad=Mathf.Lerp(d.inner_radius_m,d.outer_radius_m,t);
                return new Vector3(Mathf.Sin(a)*rad,Height(d,t,a),Mathf.Cos(a)*rad);
            }
            // Interpolate in Cartesian XZ, exactly as the rendered mesh does. Polar fractions
            // interpolate a different surface and leave roots hovering on sloped cells.
            var a=Vertex(0,0);var b=Vertex(1,0);var e=Vertex(0,1);var f=Vertex(1,1);
            var weights=Barycentric(a,b,e,local);
            return d.centre.y+(weights.x>=0&&weights.y>=0&&weights.z>=0?weights.x*a.y+weights.y*b.y+weights.z*e.y:HeightInTriangle(e,b,f,local));
        }
        private static Vector3 Barycentric(Vector3 a,Vector3 b,Vector3 c,Vector3 p)
        {
            float det=(b.z-c.z)*(a.x-c.x)+(c.x-b.x)*(a.z-c.z);
            float x=((b.z-c.z)*(p.x-c.x)+(c.x-b.x)*(p.z-c.z))/det;
            float y=((c.z-a.z)*(p.x-c.x)+(a.x-c.x)*(p.z-c.z))/det;
            return new Vector3(x,y,1-x-y);
        }
        private static float HeightInTriangle(Vector3 a,Vector3 b,Vector3 c,Vector3 p){var w=Barycentric(a,b,c,p);return w.x*a.y+w.y*b.y+w.z*c.y;}
        public void RenderTrees(Camera camera)
        {
            GeometryUtility.CalculateFrustumPlanes(camera,planes);VisibleTrees=0;
            foreach(var tree in trees)
            {
                Vector3 p=tree.GetColumn(3);var bounds=new Bounds(p+Vector3.up*5,new Vector3(8,12,8));
                if(GeometryUtility.TestPlanesAABB(planes,bounds))visibleTrees[VisibleTrees++]=tree;
            }
            if(VisibleTrees==0)return;
            var args=new RenderParams(treeMaterial){camera=camera,matProps=treeProperties,worldBounds=new Bounds(settings.centre,new Vector3(settings.outer_radius_m*2,220,settings.outer_radius_m*2)),shadowCastingMode=ShadowCastingMode.Off,receiveShadows=false};
            for(int i=0;i<VisibleTrees;i+=500)Graphics.RenderMeshInstanced(args,treeMesh,0,visibleTrees,Math.Min(500,VisibleTrees-i),i);
        }
        public void ApplyAtmosphere(string weather)
        {
            float scale=weather=="clear"?settings.clear_fog_scale:weather=="overcast"?settings.overcast_fog_scale:1;
            // Called once after the weather owner has restored its base values this frame.
            RenderSettings.fogStartDistance*=scale;RenderSettings.fogEndDistance*=scale;
        }
        private void AddMesh(string label,Mesh mesh,Material material)
        {
            meshes.Add(mesh);TriangleCount+=(int)mesh.GetIndexCount(0)/3;var go=new GameObject(label);go.transform.SetParent(transform,false);
            go.AddComponent<MeshFilter>().sharedMesh=mesh;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;renderer.shadowCastingMode=ShadowCastingMode.Off;
        }
        private static Mesh Make(List<Vector3> vertices,List<int> indices){var mesh=new Mesh();mesh.SetVertices(vertices);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;}
        private static void Tri(List<Vector3> v,List<int> i,Vector3 a,Vector3 b,Vector3 c){int n=v.Count;v.Add(a);v.Add(b);v.Add(c);i.Add(n);i.Add(n+1);i.Add(n+2);}
        private static void Quad(List<Vector3> v,List<int> i,Vector3 a,Vector3 b,Vector3 c,Vector3 d){Tri(v,i,a,b,c);Tri(v,i,a,c,d);Tri(v,i,c,b,a);Tri(v,i,d,c,a);}
        private void OnDestroy(){foreach(var mesh in meshes)if(mesh!=null)Destroy(mesh);foreach(var material in materials)if(material!=null)Destroy(material);}
    }
}
