#if UNITY_EDITOR
using System;
using System.IO;
using System.Collections.Generic;
using Biathlon.Career;
using Biathlon.Unity.Presentation;
using UnityEngine;
using UnityEditor;
using Object=UnityEngine.Object;

namespace Biathlon.Unity
{
    /// <summary>Bakes the approved Hero geometry and face morphs once, never during race loading.</summary>
    public static class RangeCoachesAuthoring
    {
        public const string Root="Assets/Resources/Biathlon/Stadium/Coaches/";
        public static void Generate()
        {
            var d=RangeCoachesDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/Venues/woodland_basin_v1/range_coaches_v1").text);
            Directory.CreateDirectory(Root);AssetDatabase.Refresh();
            var shader=Shader.Find("Biathlon/RangeCoachSurface");
            if(shader==null||ShaderUtil.ShaderHasError(shader))throw new InvalidOperationException("Coach shader must compile before baking.");
            var material=new Material(shader){name="Coaches original athlete textiles",enableInstancing=true};
            material.SetTexture("_BumpMap",Resources.Load<Texture2D>("Biathlon/Athletes/G2Hero/Textures/SuitNormal"));
            material.SetTexture("_SkinBumpMap",Resources.Load<Texture2D>("Biathlon/Athletes/G2Hero/Textures/SkinNormal"));
            material.SetTexture("_RoughnessMap",Resources.Load<Texture2D>("Biathlon/Athletes/G2Hero/Textures/SuitRoughness"));
            material.SetFloat("_Smoothness",.26f);material.SetFloat("_BumpScale",.16f);
            var shared=Save(material,Root+"CoachSurface.mat");
            for(int i=0;i<d.coaches.Length;i++)Bake(d.coaches[i],i,shared);
            AssetDatabase.SaveAssets();AssetDatabase.Refresh();
        }
        private static void Bake(RangeCoachesDefinition.Coach c,int index,Material material)
        {
            var rig=AthleteRigFactory.Create(null);var art=new GameObject(c.id);var meshes=new List<Mesh>();
            try
            {
                foreach(var b in rig.GetComponentsInChildren<Behaviour>(true))b.enabled=false;
                var skin=rig.GetComponentInChildren<SkinnedMeshRenderer>();var detail=rig.GetComponent<AthleteSkinDetailBinding>();
                var appearance=rig.GetComponent<AthleteAppearancePresentation>();
                var profile=AppearanceProfile.CreateDetailed(c.face,c.skin,index%2==0?"natural":"athletic",1850,6500,"short_swept",c.hair,index%2==0?"grey":"brown",c.headwear,"raised","ice","carbon","graphite","national");
                var lods=new LOD[3];Vector3 eye=default,notebook=default;
                for(int level=0;level<3;level++)
                {
                    detail.Prepare(level==0?0:level==1?20:100,level==0,true);
                    appearance.ApplyCosmeticSelection(c.face,AthleteFaceCatalog.Load().SkinIndex(c.skin),index%2==0?1:2);appearance.ApplyProfile(profile);
                    rig.ResetPose();
                    // A slightly roomier winter jacket, using the original body/limb geometry.
                    rig.Bone(AthleteBone.Spine).localScale=new Vector3(1.08f,1,1.10f);
                    rig.Bone(AthleteBone.Chest).localScale=new Vector3(1.05f,1,1.06f);
                    foreach(var b in new[]{AthleteBone.LeftUpperArm,AthleteBone.RightUpperArm,AthleteBone.LeftLowerArm,AthleteBone.RightLowerArm})rig.Bone(b).localScale=new Vector3(1.15f,1,1.15f);
                    rig.Rotate(AthleteBone.Spine,Quaternion.Euler(10+index%3,0,0));rig.Rotate(AthleteBone.Chest,Quaternion.Euler(6,0,0));
                    rig.Rotate(AthleteBone.Neck,Quaternion.Euler(-6,0,0));rig.Rotate(AthleteBone.Head,Quaternion.Euler(-10-index%3,0,0));
                    var focus=rig.Bone(AthleteBone.Head).TransformPoint(new Vector3(.031f,.075f,.091f));
                    Arm(false,new Vector3(-.13f,1.30f,.26f));Arm(true,focus+new Vector3(.11f,-.09f,.19f));
                    var baked=new Mesh{name=c.id+" LOD"+level};skin.BakeMesh(baked);meshes.Add(baked);
                    var source=skin.sharedMesh;var vertices=baked.vertices;var normals=baked.normals;var tangents=baked.tangents;
                    var colors=new Color[vertices.Length];var metadata=new List<Vector4>();baked.GetUVs(2,metadata);var hairData=new List<Vector2>();var originalRest=new List<Vector3>();source.GetUVs(1,originalRest);
                    if(metadata.Count!=vertices.Length)throw new InvalidOperationException("Approved Hero cosmetic channels required.");
                    var indices=new List<int>();var flags=new bool[vertices.Length];float age=Mathf.InverseLerp(35,75,c.age),height=c.height_mm/1850f;
                    var coat=Color.Lerp(new Color(.045f,.065f,.08f),AthleteAppearancePresentation.NationColor(c.nation),.46f);
                    var skinColor=AthleteFaceCatalog.Load().SkinColor(AthleteFaceCatalog.Load().SkinIndex(c.skin));
                    for(int sub=0;sub<source.subMeshCount;sub++)
                    {
                        if(sub==7)continue; // Winter staff have uncovered eyes, no race goggles or bib lettering.
                        var triangles=source.GetTriangles(sub);
                        var rest=new List<Vector3>();source.GetUVs(1,rest);
                        for(int t=0;t<triangles.Length;t+=3)
                        {
                            // Race rifle harness is a separate original body piece in the rubber slot.
                            // Its bind-space vertices identify art only; glove/boot geometry stays intact.
                            bool harness=sub==4;
                            for(int v=0;v<3&&harness;v++){var p=rest[triangles[t+v]];harness&=p.y>1.16f&&p.y<1.60f&&Mathf.Abs(p.x)<.22f;}
                            if(!harness){indices.Add(triangles[t]);indices.Add(triangles[t+1]);indices.Add(triangles[t+2]);}
                        }
                        foreach(int v in triangles)
                        {
                            if(flags[v])continue;flags[v]=true;
                            var color=sub==3?skinColor:sub==4||sub==5?new Color(.045f,.055f,.065f):sub==6?AthleteAppearancePresentation.NationColor(c.nation):coat;
                            color=color.linear;
                            color.a=c.headwear=="none"?1:c.headwear=="headband"?.5f:0;colors[v]=color;
                            var m=metadata[v];m.z=sub==3?1:sub==4?3:sub==5?4:0;m.w=c.beard?age:-age;metadata[v]=m;
                        }
                    }
                    float bottom=float.PositiveInfinity;
                    for(int v=0;v<vertices.Length;v++)
                    {
                        var rest=originalRest[v];
                        if(c.beard&&metadata[v].z==1&&metadata[v].x<.5f&&rest.y<1.677f&&rest.y>1.59f&&rest.z>.03f)
                            vertices[v]+=normals[v]*(.004f*Mathf.SmoothStep(0,1,Mathf.InverseLerp(1.677f,1.64f,rest.y)));
                        hairData.Add(new Vector2(c.hair=="silver"?.9f:.22f,0));
                        vertices[v]=skin.transform.TransformPoint(vertices[v])*height;normals[v]=skin.transform.TransformDirection(normals[v]).normalized;
                        if(flags[v])bottom=Mathf.Min(bottom,vertices[v].y);
                        if(tangents.Length==vertices.Length){var tangent=skin.transform.TransformDirection(new Vector3(tangents[v].x,tangents[v].y,tangents[v].z));tangents[v]=new Vector4(tangent.x,tangent.y,tangent.z,tangents[v].w);}
                    }
                    for(int v=0;v<vertices.Length;v++)vertices[v]-=Vector3.up*bottom;
                    baked.vertices=vertices;baked.normals=normals;baked.tangents=tangents;baked.colors=colors;baked.SetUVs(2,metadata);baked.SetUVs(3,hairData);
                    baked.subMeshCount=1;baked.SetTriangles(indices,0);baked.bindposes=Array.Empty<Matrix4x4>();baked.boneWeights=Array.Empty<BoneWeight>();baked.ClearBlendShapes();baked.RecalculateBounds();
                    var saved=Save(baked,Root+c.id+"_LOD"+level+".asset");meshes.Remove(baked);
                    var go=new GameObject("Static coach LOD"+level);go.transform.SetParent(art.transform,false);go.AddComponent<MeshFilter>().sharedMesh=saved;var renderer=go.AddComponent<MeshRenderer>();renderer.sharedMaterial=material;
                    lods[level]=new LOD(level==0?.075f:level==1?.022f:.0025f,new Renderer[]{renderer});
                    if(level==0){eye=focus*height-Vector3.up*bottom;notebook=(rig.Bone(AthleteBone.LeftHand).position+new Vector3(.06f,-.09f,.025f))*height-Vector3.up*bottom;}
                    void Arm(bool right,Vector3 target)
                    {
                        AthleteLocomotionDriver.SolveTwoBone(rig.Bone(right?AthleteBone.RightUpperArm:AthleteBone.LeftUpperArm),rig.Bone(right?AthleteBone.RightLowerArm:AthleteBone.LeftLowerArm),rig.Bone(right?AthleteBone.RightHand:AthleteBone.LeftHand),target,new Vector3(right?.35f:-.35f,-.3f,-.25f),Quaternion.identity);
                    }
                }
                art.AddComponent<RangeCoachArt>().Configure(eye,notebook);var lod=art.AddComponent<LODGroup>();lod.SetLODs(lods);lod.RecalculateBounds();
                PrefabUtility.SaveAsPrefabAsset(art,Root+c.id+".prefab");
            }
            finally{foreach(var mesh in meshes)if(mesh!=null)Object.DestroyImmediate(mesh);Object.DestroyImmediate(rig.gameObject);Object.DestroyImmediate(art);}
        }
        private static T Save<T>(T value,string path) where T:Object
        {
            var old=AssetDatabase.LoadAssetAtPath<T>(path);
            if(old==null){AssetDatabase.CreateAsset(value,path);return value;}
            EditorUtility.CopySerialized(value,old);Object.DestroyImmediate(value);EditorUtility.SetDirty(old);return old;
        }
    }
}
#endif
