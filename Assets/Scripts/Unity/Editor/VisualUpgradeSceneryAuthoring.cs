#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    public static class VisualUpgradeSceneryAuthoring
    {
        public static void Generate()
        {
            const string root="Assets/Resources/Biathlon/Terrain/WinterDressing/";Directory.CreateDirectory(root);AssetDatabase.Refresh();
            Save(Tube(false),root+"FallenLog.asset");Save(Tube(true),root+"Stump.asset");Save(Drift(),root+"SnowDrift.asset");
            var wood=AssetDatabase.LoadAssetAtPath<Material>(root+"Wood.mat");if(wood==null){wood=new Material(Shader.Find("Biathlon/WinterWood"));AssetDatabase.CreateAsset(wood,root+"Wood.mat");}
            wood.shader=Shader.Find("Biathlon/WinterWood");wood.enableInstancing=true;
            wood.SetTexture("_Bark",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Biathlon/Vegetation/G2Fir/Textures/BarkAlbedo.png"));
            wood.SetTexture("_Normal",AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Resources/Biathlon/Vegetation/G2Fir/Textures/BarkNormal.png"));EditorUtility.SetDirty(wood);
            var snow=AssetDatabase.LoadAssetAtPath<Material>(root+"SnowDrift.mat");
            var source=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Biathlon/Terrain/G2Snow/WinterBlend.mat");
            if(snow==null){snow=new Material(source);AssetDatabase.CreateAsset(snow,root+"SnowDrift.mat");}else EditorUtility.CopySerialized(source,snow);
            Biathlon.Unity.Presentation.SnowMaterialDefinition.Load().Configure(snow,"cold");snow.enableInstancing=true;EditorUtility.SetDirty(snow);AssetDatabase.SaveAssets();
        }
        private static void Save(Mesh mesh,string path)
        {var saved=AssetDatabase.LoadAssetAtPath<Mesh>(path);if(saved==null)AssetDatabase.CreateAsset(mesh,path);else{EditorUtility.CopySerialized(mesh,saved);UnityEngine.Object.DestroyImmediate(mesh);EditorUtility.SetDirty(saved);}}
        private static Mesh Tube(bool stump)
        {
            var vertices=new List<Vector3>();var uv=new List<Vector2>();var triangles=new List<int>();const int sides=14,rings=7;
            for(int ring=0;ring<rings;ring++)for(int side=0;side<=sides;side++)
            {
                float t=ring/(float)(rings-1),a=side/(float)sides*Mathf.PI*2;
                float radius=(stump?.29f:.21f)*(1-.25f*t)*(1+.06f*Mathf.Sin(a*5+ring));
                var point=stump?new Vector3(Mathf.Cos(a)*radius,t*.65f,Mathf.Sin(a)*radius):new Vector3((t-.5f)*3,radius*Mathf.Cos(a)+.24f+.035f*Mathf.Sin(t*4),radius*Mathf.Sin(a));
                vertices.Add(point);uv.Add(new Vector2(side/(float)sides,t*(stump?1:4)));
                if(ring>0&&side<sides){int p=(ring-1)*(sides+1)+side,q=ring*(sides+1)+side;triangles.AddRange(new[]{p,q,p+1,p+1,q,q+1});}
            }
            for(int end=0;end<2;end++)
            {
                int center=vertices.Count;Vector3 c=Vector3.zero;for(int i=0;i<sides;i++)c+=vertices[end*(rings-1)*(sides+1)+i];c/=sides;vertices.Add(c);uv.Add(new Vector2(.5f,.5f));
                for(int i=0;i<sides;i++){int first=end*(rings-1)*(sides+1)+i;triangles.AddRange(end==0?new[]{center,first+1,first}:new[]{center,first,first+1});}
            }
            var mesh=new Mesh{name=stump?"Original winter stump":"Original fallen winter log"};mesh.SetVertices(vertices);mesh.SetUVs(0,uv);mesh.SetTriangles(triangles,0);mesh.RecalculateNormals();mesh.RecalculateTangents();mesh.RecalculateBounds();return mesh;
        }
        private static Mesh Drift()
        {
            const int columns=20,rows=8;var v=new Vector3[(columns+1)*(rows+1)];var uv=new Vector2[v.Length];var colors=new Color[v.Length];var indices=new List<int>();
            for(int y=0;y<=rows;y++)for(int x=0;x<=columns;x++)
            {
                float a=x/(float)columns*Mathf.PI*2,b=y/(float)rows*Mathf.PI;int i=y*(columns+1)+x;
                v[i]=new Vector3(Mathf.Cos(a)*Mathf.Sin(b)*2.3f,Mathf.Cos(b)*.65f+.32f,Mathf.Sin(a)*Mathf.Sin(b)*1.2f);
                uv[i]=new Vector2(x/(float)columns,y/(float)rows);colors[i]=new Color(1,0,0,0);
                if(x<columns&&y<rows){int n=i+columns+1;indices.AddRange(new[]{i,n,i+1,i+1,n,n+1});}
            }
            var mesh=new Mesh{name="Original wind-deposited snow drift",vertices=v,uv=uv,colors=colors};mesh.SetTriangles(indices,0);mesh.RecalculateNormals();mesh.RecalculateBounds();return mesh;
        }
    }
}
#endif
