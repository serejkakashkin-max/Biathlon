#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
namespace Biathlon.Unity
{
    /// <summary>Small original face/shoulder variants on the retained licensed topology.
    /// Bindposes, skin weights, equipment and every gameplay datum remain untouched.</summary>
    public static class VisualUpgradeHeroAuthoring
    {
        public static void Generate()
        {
            const string root="Assets/Resources/Biathlon/Athletes/G2Hero/";
            for(int lod=0;lod<3;lod++)
            {
                var mesh=AssetDatabase.LoadAssetAtPath<Mesh>(root+"Meshes/HeroLOD"+lod+".asset");var v=mesh.vertices;
                G2JointVolumeAuthoring.Apply(mesh);
                for(int variant=0;variant<3;variant++)
                {
                    var delta=new Vector3[v.Length];var zero=new Vector3[v.Length];float factor=variant-1;
                    for(int i=0;i<v.Length;i++)
                    {
                        var p=v[i];float jaw=Mathf.Exp(-Mathf.Pow((p.y-1.625f)/.045f,2));
                        if(p.y>1.54f&&p.y<1.71f&&Mathf.Abs(p.x)<.10f&&p.z>.015f)
                            delta[i]=new Vector3(AthleteFaceAuthoring.FaceSide(p.x)*factor*.004f*jaw,-factor*.002f*jaw,factor*.002f*jaw);
                        // Subtle build variation, confined to the shoulder shell and below the head.
                        else if(p.y>1.20f&&p.y<1.43f&&Mathf.Abs(p.x)>.12f&&Mathf.Abs(p.x)<.22f)
                            delta[i]=new Vector3(Mathf.Sign(p.x)*factor*.003f*Mathf.Sin((p.y-1.2f)/.23f*Mathf.PI),0,0);
                    }
                    mesh.AddBlendShapeFrame("OriginalBuild"+variant,100,delta,zero,zero);
                }
                EditorUtility.SetDirty(mesh);
            }
            foreach(string name in new[]{"SuitNavy","SuitTeal","CapNavy","Skin","GloveRubber","BootComposite","GoggleLens"})
            {
                var mat=AssetDatabase.LoadAssetAtPath<Material>(root+"Materials/"+name+".mat");
                mat.SetFloat("_Smoothness",name=="Skin"?.3f:name=="GoggleLens"?.88f:name=="BootComposite"?.32f:name=="GloveRubber"?.22f:.27f);
                if(name=="CapNavy"){mat.SetFloat("_DetailTiling",28);mat.SetFloat("_BumpScale",.48f);}
                if(name=="BootComposite")mat.SetFloat("_SurfaceKind",4);
                EditorUtility.SetDirty(mat);
            }
            AssetDatabase.SaveAssets();
            AthleteFaceAuthoring.Generate();
            AthleteCreatorAppearanceAuthoring.Generate();
        }
    }
}
#endif
