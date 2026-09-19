#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity
{
    public static class VegetationAuthoring
    {
        private const string Root="Assets/Resources/Biathlon/Vegetation/";
        [MenuItem("BIATHLON/Generate Vegetation Benchmark")]
        public static void Generate()
        {
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();var shader=Shader.Find("Biathlon/Conifer");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new System.InvalidOperationException("Conifer shader failed.");
            var material=AssetDatabase.LoadAssetAtPath<Material>(Root+"Conifer.mat");
            if(material==null){material=new Material(shader);AssetDatabase.CreateAsset(material,Root+"Conifer.mat");}
            material.enableInstancing=true;material.SetFloat("_SnowCoverage",VegetationDefinition.Load().snow_coverage);EditorUtility.SetDirty(material);
            for(int i=0;i<3;i++){var mesh=BuildTree(i);string path=Root+"ConiferLOD"+i+".asset";var old=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(old==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,old);Object.DestroyImmediate(mesh);EditorUtility.SetDirty(old);}}
            var prior=SceneManager.GetActiveScene();var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var preview=new GameObject("Vegetation benchmark").AddComponent<VegetationPreview>();
                var floor=GameObject.CreatePrimitive(PrimitiveType.Plane);floor.name="Snow forest floor";floor.transform.localScale=new Vector3(16,1,18);Object.DestroyImmediate(floor.GetComponent<Collider>());
                floor.GetComponent<Renderer>().sharedMaterial=Resources.Load<Material>("Biathlon/Snow/cold");
                var light=new GameObject("Winter key").AddComponent<Light>();light.type=LightType.Directional;light.intensity=1.5f;light.color=new Color(1,.96f,.9f);light.shadows=LightShadows.Soft;light.transform.rotation=Quaternion.Euler(28,-35,0);
                RenderSettings.ambientMode=AmbientMode.Trilight;RenderSettings.ambientSkyColor=new Color(.35f,.42f,.52f);RenderSettings.ambientEquatorColor=new Color(.2f,.26f,.32f);RenderSettings.ambientGroundColor=new Color(.12f,.15f,.19f);
                RenderSettings.fog=true;RenderSettings.fogMode=FogMode.Linear;RenderSettings.fogStartDistance=50;RenderSettings.fogEndDistance=135;RenderSettings.fogColor=new Color(.57f,.64f,.71f);
                var c=new GameObject("Forest benchmark camera").AddComponent<Camera>();c.tag="MainCamera";c.transform.position=new Vector3(0,2,-48);c.transform.LookAt(new Vector3(0,2,-10));c.fieldOfView=55;c.farClipPlane=180;c.clearFlags=CameraClearFlags.SolidColor;c.backgroundColor=RenderSettings.fogColor;c.gameObject.AddComponent<AudioListener>();preview.View=c;
                EditorSceneManager.SaveScene(scene,"Assets/Scenes/VegetationBenchmark.unity");AssetDatabase.SaveAssets();
            }
            finally{SceneManager.SetActiveScene(prior);EditorSceneManager.CloseScene(scene,true);}
        }
        public static Mesh BuildTree(int lod)
        {
            var v=new List<Vector3>();var colors=new List<Color>();var indices=new List<int>();
            var bark=new Color(.19f,.135f,.095f,0);
            // Closed tapered trunk. Boughs below use asymmetrical, pointed branch fans, not stacked cones.
            for(int k=0;k<8;k++){float a=k*Mathf.PI/4,b=(k+1)*Mathf.PI/4;var p=new Vector3(Mathf.Cos(a)*.16f,0,Mathf.Sin(a)*.16f);var q=new Vector3(Mathf.Cos(b)*.16f,0,Mathf.Sin(b)*.16f);Tri(p,Vector3.up*8,q,bark,v,colors,indices);}
            int levels=lod==0?14:lod==1?10:7,branches=lod==0?9:lod==1?7:5;
            for(int level=0;level<levels;level++)
            {
                float t=(float)level/(levels-1),y=.8f+t*6.8f,radius=(1-t)*1.9f+.12f;
                for(int branch=0;branch<branches;branch++)
                {
                    float angle=branch*Mathf.PI*2/branches+level*2.399963f;float variation=.82f+.18f*Mathf.Sin(level*17+branch*11);
                    var dir=new Vector3(Mathf.Cos(angle),0,Mathf.Sin(angle));var side=Vector3.Cross(Vector3.up,dir);
                    var root=Vector3.up*(y+.17f*Mathf.Sin(level*13+branch*7));var tip=root+Vector3.down*.3f+dir*radius*variation;
                    var green=new Color(.055f+.018f*t,.115f+.025f*t,.085f+.02f*t,1);
                    int clumps=lod==0?6:lod==1?4:2,segments=lod==0?8:lod==1?6:4;
                    for(int clump=0;clump<clumps;clump++)
                    {
                        float along=(clump+.5f)/clumps;var mid=Vector3.Lerp(root,tip,along)+side*((clump%2==0?1:-1)*radius*.16f*(1-along));
                        float halfLength=radius*(lod==0?.22f:lod==1?.3f:.45f),width=radius*(.11f+.12f*(1-along));
                        var top=mid+Vector3.up*(.055f+radius*.07f);var bottom=mid-Vector3.up*.1f;
                        int start=v.Count;v.Add(top);colors.Add(green);v.Add(bottom);colors.Add(green);
                        for(int k=0;k<segments;k++){float a=k*Mathf.PI*2/segments;float rag=k%2==0?1:.8f;v.Add(mid+dir*(Mathf.Cos(a)*halfLength)+side*(Mathf.Sin(a)*width*rag)+Vector3.down*(.055f*Mathf.Abs(Mathf.Sin(a))));colors.Add(green);}
                        for(int k=0;k<segments;k++){int a=start+2+k,b=start+2+(k+1)%segments;indices.Add(start);indices.Add(a);indices.Add(b);indices.Add(start+1);indices.Add(b);indices.Add(a);}
                    }
                }
            }
            var mesh=new Mesh{name="Original winter conifer LOD"+lod};mesh.SetVertices(v);mesh.SetColors(colors);mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
        private static void Tri(Vector3 a,Vector3 b,Vector3 c,Color color,List<Vector3> v,List<Color> colors,List<int> indices)
        {int start=v.Count;v.Add(a);v.Add(b);v.Add(c);colors.Add(color);colors.Add(color);colors.Add(color);indices.Add(start);indices.Add(start+1);indices.Add(start+2);}
    }
}
#endif
