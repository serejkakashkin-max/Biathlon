#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Object=UnityEngine.Object;

namespace Biathlon.Unity
{
    /// <summary>Additive CC0 eyes and cosmetic channels. The approved first13 morphs and8 draw slots remain intact.</summary>
    public static class AthleteCreatorAppearanceAuthoring
    {
        private sealed class Frame {public string Name;public float Weight;public Vector3[] Delta,Normals,Tangents;}
#pragma warning disable CS0649
        [Serializable] private sealed class EyeData {public int schema_version;public string id;public float[] vertices,normals,uv,tags,bone_weights;public int[] front,indices,headwear_indices,bone_indices;}
#pragma warning restore CS0649
        public static void Generate()
        {
            for(int lod=0;lod<3;lod++)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(G2HeroAuthoring.DirectoryPath+"/Meshes/HeroLOD"+lod+".asset");
                if(mesh==null)throw new InvalidOperationException("Approved G2 mesh missing.");
                string path="artifacts/g2-creator/eyes_lod"+lod+".json";
                if(!File.Exists(path))throw new InvalidOperationException("Run scripts/art/Build-G2CreatorEyes.py with Blender first.");
                Apply(mesh,JsonUtility.FromJson<EyeData>(File.ReadAllText(path)));EditorUtility.SetDirty(mesh);
            }
            AssetDatabase.SaveAssets();
        }
        private static void Apply(Mesh mesh,EyeData eyes)
        {
            if(eyes==null||eyes.schema_version!=1||string.IsNullOrWhiteSpace(eyes.id)||eyes.vertices==null||eyes.vertices.Length%3!=0||eyes.normals?.Length!=eyes.vertices.Length||eyes.uv?.Length!=eyes.vertices.Length/3*2||eyes.front?.Length!=eyes.vertices.Length/3||eyes.tags?.Length!=eyes.front.Length||eyes.bone_indices?.Length!=eyes.front.Length*4||eyes.bone_weights?.Length!=eyes.front.Length*4||eyes.indices==null||eyes.headwear_indices==null)throw new InvalidDataException("Invalid audited eye derivative.");
            var originalTags=new List<Vector2>();mesh.GetUVs(2,originalTags);int originalCount=mesh.vertexCount;
            for(int i=0;i<originalTags.Count;i++)if(originalTags[i].x==1){originalCount=i;break;}
            int eyeCount=eyes.vertices.Length/3,count=originalCount+eyeCount;
            var vertices=Extend(mesh.vertices,count,originalCount);var normals=Extend(mesh.normals,count,originalCount);var uv=Extend(mesh.uv,count,originalCount);var tangents=Extend(mesh.tangents,count,originalCount);var weights=Extend(mesh.boneWeights,count,originalCount);var bind=mesh.bindposes;
            var submeshes=new int[mesh.subMeshCount][];
            for(int sub=0;sub<submeshes.Length;sub++){var kept=new List<int>();foreach(int index in mesh.GetTriangles(sub))if(index<originalCount)kept.Add(index);submeshes[sub]=kept.ToArray();}
            var frames=new List<Frame>();
            for(int shape=0;shape<mesh.blendShapeCount;shape++)
            {
                string name=mesh.GetBlendShapeName(shape);if(name.StartsWith("Creator",StringComparison.Ordinal))continue;
                for(int frame=0;frame<mesh.GetBlendShapeFrameCount(shape);frame++)
                {
                    var delta=new Vector3[mesh.vertexCount];var ns=new Vector3[mesh.vertexCount];var ts=new Vector3[mesh.vertexCount];mesh.GetBlendShapeFrameVertices(shape,frame,delta,ns,ts);
                    frames.Add(new Frame{Name=name,Weight=mesh.GetBlendShapeFrameWeight(shape,frame),Delta=Extend(delta,count,originalCount),Normals=Extend(ns,count,originalCount),Tangents=Extend(ts,count,originalCount)});
                }
            }
            var tags=new Vector2[count];
            foreach(int i in submeshes[6])if(vertices[i].y>1.748f)tags[i]=new Vector2(2,Mathf.Clamp01((vertices[i].y-1.752f)/.025f));
            foreach(int i in submeshes[4]){if(vertices[i].y>.7f&&vertices[i].y<1.1f)tags[i]=new Vector2(3,0);else if(vertices[i].y<.30f)tags[i]=new Vector2(4,0);}
            foreach(int i in submeshes[5])tags[i]=new Vector2(4,0);
            for(int i=0;i<eyeCount;i++)
            {
                int at=originalCount+i;vertices[at]=new Vector3(eyes.vertices[i*3],eyes.vertices[i*3+1],eyes.vertices[i*3+2]);normals[at]=new Vector3(eyes.normals[i*3],eyes.normals[i*3+1],eyes.normals[i*3+2]);uv[at]=new Vector2(eyes.uv[i*2],eyes.uv[i*2+1]);
                var tangent=Vector3.Cross(Vector3.up,normals[at]).normalized;if(tangent.sqrMagnitude<.1f)tangent=Vector3.right;tangents[at]=new Vector4(tangent.x,tangent.y,tangent.z,1);
                weights[at]=new BoneWeight{boneIndex0=eyes.bone_indices[i*4],boneIndex1=eyes.bone_indices[i*4+1],boneIndex2=eyes.bone_indices[i*4+2],boneIndex3=eyes.bone_indices[i*4+3],weight0=eyes.bone_weights[i*4],weight1=eyes.bone_weights[i*4+1],weight2=eyes.bone_weights[i*4+2],weight3=eyes.bone_weights[i*4+3]};tags[at]=new Vector2(eyes.tags[i],eyes.front[i]);
            }
            var skinIndices=new List<int>(submeshes[3]);foreach(int i in eyes.indices){if(i<0||i>=eyeCount)throw new InvalidDataException("Eye triangle outside vertex buffer.");skinIndices.Add(originalCount+i);}submeshes[3]=skinIndices.ToArray();
            var headwearIndices=new List<int>(submeshes[6]);foreach(int i in eyes.headwear_indices){if(i<0||i>=eyeCount)throw new InvalidDataException("Headwear triangle outside vertex buffer.");headwearIndices.Add(originalCount+i);}submeshes[6]=headwearIndices.ToArray();
            // The existing raised-goggles ownership mask covers lens, frame and
            // strap. Mark it in every LOD for matching forward/depth/shadow clips.
            foreach(int i in submeshes[7])tags[i]=new Vector2(7,0);
            foreach(int i in submeshes[4])if(vertices[i].y>1.65f)tags[i]=new Vector2(7,0);
            foreach(int i in submeshes[6])if(i<originalCount&&tags[i].x!=2)tags[i]=new Vector2(7,0);
            mesh.Clear();mesh.vertices=vertices;mesh.normals=normals;mesh.uv=uv;mesh.tangents=tangents;mesh.boneWeights=weights;mesh.bindposes=bind;mesh.subMeshCount=submeshes.Length;
            for(int sub=0;sub<submeshes.Length;sub++)mesh.SetTriangles(submeshes[sub],sub);
            mesh.SetUVs(1,vertices);mesh.SetUVs(2,tags);foreach(var f in frames)mesh.AddBlendShapeFrame(f.Name,f.Weight,f.Delta,f.Normals,f.Tangents);
            foreach(string name in new[]{"CreatorShouldersNarrow","CreatorShouldersWide","CreatorHairCrew","CreatorHairShort","CreatorGogglesRaised"})
            {
                var delta=new Vector3[count];var normalDelta=new Vector3[count];
                var goggles=new bool[count];var straps=new bool[count];foreach(int i in submeshes[7])goggles[i]=true;foreach(int i in submeshes[4])if(vertices[i].y>1.65f)goggles[i]=true;foreach(int i in submeshes[6])if(i<originalCount&&tags[i].x!=2){goggles[i]=true;straps[i]=true;}
                var rotation=Quaternion.Euler(-35,0,0);var pivot=new Vector3(0,1.720f,0);
                for(int i=0;i<originalCount;i++)
                {
                    var p=vertices[i];
                    if(name=="CreatorGogglesRaised"&&goggles[i])
                    {delta[i]=straps[i]?new Vector3(p.x*.87f,p.y+.080f,p.z*(p.z>0?.88f:1))-p:pivot+rotation*(p-pivot)+new Vector3(0,.005f,.014f)-p;normalDelta[i]=straps[i]?Vector3.zero:rotation*normals[i]-normals[i];}
                    else if(name=="CreatorHairCrew"&&tags[i].x==2)
                    {float edge=1-Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.752f,1.825f,p.y));float front=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.035f,.065f,p.z));float temple=Mathf.Exp(-Mathf.Pow((Mathf.Abs(p.x)-.070f)/.018f,2));float irregular=.0012f*Mathf.Sin(p.x*370)+.0007f*Mathf.Sin(p.z*330);delta[i]=new Vector3(0,edge*(Mathf.Lerp(-.006f,.021f,front)+temple*front*.009f+irregular),0)-normals[i]*.0012f;}
                    else if(name=="CreatorHairShort"&&tags[i].x==2)
                    {float top=Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.770f,1.850f,p.y));float fore=Mathf.SmoothStep(0,1,Mathf.InverseLerp(-.035f,.06f,p.z));delta[i]=new Vector3(.010f*top,.008f*top+.011f*fore*top,.010f*fore*top);}
                    else if(name.StartsWith("CreatorShoulders",StringComparison.Ordinal)&&p.y>1.2f&&p.y<1.56f&&Mathf.Abs(p.x)<.32f)
                    {float side=Mathf.SmoothStep(0,1,Mathf.InverseLerp(.075f,.21f,Mathf.Abs(p.x)));float region=Mathf.Sin((p.y-1.2f)/.36f*Mathf.PI);delta[i]=new Vector3(Mathf.Sign(p.x)*.014f*side*region*(name.EndsWith("Wide",StringComparison.Ordinal)?1:-1),0,0);}
                }
                mesh.AddBlendShapeFrame(name,100,delta,normalDelta,null);
            }
            mesh.RecalculateBounds();
        }
        public static void Apply(Mesh mesh,string auditedEyeJson)=>Apply(mesh,JsonUtility.FromJson<EyeData>(auditedEyeJson));
        private static T[] Extend<T>(T[] source,int count,int copy){var target=new T[count];Array.Copy(source,target,Math.Min(copy,source.Length));return target;}
    }
}
#endif
