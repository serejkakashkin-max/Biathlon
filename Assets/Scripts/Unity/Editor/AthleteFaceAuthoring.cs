#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity
{
    /// <summary>Original face-only derivatives on the existing audited CC0 hero topology.</summary>
    public static class AthleteFaceAuthoring
    {
        private sealed class Frame {public string name;public float weight;public Vector3[] delta,normals,tangents;}
        public static void Generate()
        {
            var recipe=AthleteFaceCatalog.Load();
            for(int lod=0;lod<3;lod++)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(G2HeroAuthoring.DirectoryPath+"/Meshes/HeroLOD"+lod+".asset");
                if(mesh==null)throw new InvalidOperationException("Audited hero LOD missing.");
                Apply(mesh,recipe);EditorUtility.SetDirty(mesh);
            }
            AssetDatabase.SaveAssets();
        }
        public static void Apply(Mesh mesh,AthleteFaceCatalog recipe)
        {
            var vertices=mesh.vertices;var keep=new List<Frame>();
            for(int shape=0;shape<mesh.blendShapeCount;shape++)
            {
                string name=mesh.GetBlendShapeName(shape);if(name.StartsWith("FaceIdentity_",StringComparison.Ordinal))continue;
                for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
                {
                    var f=new Frame{name=name,weight=mesh.GetBlendShapeFrameWeight(shape,frame),delta=new Vector3[vertices.Length],normals=new Vector3[vertices.Length],tangents=new Vector3[vertices.Length]};
                    mesh.GetBlendShapeFrameVertices(shape,frame,f.delta,f.normals,f.tangents);keep.Add(f);
                }
            }
            var allowed=new bool[vertices.Length];foreach(int index in mesh.GetTriangles(3))allowed[index]=true;
            mesh.ClearBlendShapes();foreach(var f in keep)mesh.AddBlendShapeFrame(f.name,f.weight,f.delta,f.normals,f.tangents);
            // Preserve the artist-authored split normals. Store only the change
            // caused by a morph, rather than replacing them with Unity's base smoothing.
            Vector3[] referenceNormals;var reference=Object.Instantiate(mesh);
            try{reference.RecalculateNormals();referenceNormals=reference.normals;}
            finally{Object.DestroyImmediate(reference);}
            foreach(var profile in recipe.profiles)
            {
                var delta=new Vector3[vertices.Length];var normalDelta=new Vector3[vertices.Length];
                var posed=(Vector3[])vertices.Clone();
                for(int i=0;i<vertices.Length;i++)
                {
                    if(!allowed[i])continue;var p=vertices[i];
                    // Smoothly exclude neck, cap, lens and temples. Preserve eyes/goggle fit.
                    float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.015f,.070f,p.z));
                    float region=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.585f,1.615f,p.y))*(1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.675f,1.710f,p.y)))*front;
                    float jaw=Bell(p.y,1.626f,.032f);float cheek=Bell(p.y,1.662f,.022f);
                    float chin=Bell(p.y,1.606f,.025f)*Bell(p.x,0,.040f);
                    float nose=Bell(p.y,1.673f,.021f)*Bell(p.x,0,.023f)*Mathf.SmoothStep(0,1,Mathf.InverseLerp(.085f,.115f,p.z));
                    delta[i]=new Vector3(Mathf.Sign(p.x)*(profile.jaw_width_m*jaw+profile.cheek_width_m*cheek)+profile.nose_width_m*(p.x/.023f)*nose,
                        -profile.chin_length_m*chin,profile.chin_depth_m*chin+profile.nose_depth_m*nose)*region;
                    posed[i]+=delta[i];
                }
                var temporary=Object.Instantiate(mesh);
                try
                {
                    temporary.vertices=posed;temporary.RecalculateNormals();var changed=temporary.normals;
                    for(int i=0;i<vertices.Length;i++)if(allowed[i]&&vertices[i].y>1.585f&&vertices[i].y<1.710f)normalDelta[i]=changed[i]-referenceNormals[i];
                }
                finally{Object.DestroyImmediate(temporary);}
                mesh.AddBlendShapeFrame(profile.ShapeName,100,delta,normalDelta,null);
            }
        }
        private static float Bell(float value,float centre,float radius)=>Mathf.Exp(-Mathf.Pow((value-centre)/radius,2));
    }
}
#endif
