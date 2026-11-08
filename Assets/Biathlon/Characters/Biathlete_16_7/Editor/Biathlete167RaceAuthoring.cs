#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEngine;
using Biathlon.Career;

namespace Biathlon.Unity
{
    public static class Biathlete167RaceAuthoring
    {
        private const string Hero="Assets/Biathlon/Characters/Biathlete_16_7";
        private const string Output="Assets/Resources/Biathlon/Athletes/M167";

        public static string EnsureRuntimeResources()
        {
            Directory.CreateDirectory(Output);
            AssetDatabase.Refresh();
            var rig=AssetDatabase.LoadAssetAtPath<Biathlete167Rig>(Hero+"/Prefabs/Biathlete_16_7_Natural.prefab");
            if(rig==null)throw new InvalidOperationException("Candidate asset is missing.");
            var sourceBib=AssetDatabase.LoadAssetAtPath<Material>("Assets/Resources/Biathlon/Athletes/G2Hero/Materials/BibIvory.mat");
            string bibPath=Output+"/RaceBib.mat";
            var bib=AssetDatabase.LoadAssetAtPath<Material>(bibPath);
            if(bib==null){bib=new Material(sourceBib);AssetDatabase.CreateAsset(bib,bibPath);}
            bib.name="PULSE_Bib_Printed_Textile";
            bib.shader=Shader.Find("Biathlon/AthleteBib");
            bib.SetFloat("_HeroPages",1);bib.SetFloat("_BibNumber",1);
            EditorUtility.SetDirty(bib);
            MeshFilter longest=null;
            foreach(var mesh in rig.Bone(HumanBodyBones.LeftFoot).GetComponentsInChildren<MeshFilter>(true))
                if(longest==null||mesh.sharedMesh.bounds.size.sqrMagnitude>longest.sharedMesh.bounds.size.sqrMagnitude)longest=mesh;
            var deck=Array.Find(longest.GetComponent<MeshRenderer>().sharedMaterials,m=>m.name.Contains("Ski")&&!m.name.Contains("Binding"));
            if(deck==null)throw new InvalidOperationException("The authored ski deck is missing.");
            var variants=new List<Biathlete167RaceDefinition.SkiStyle>();
            for(int index=0;index<CareerSkiAppearance.Catalog.Count;index++)
            {
                var ski=CareerSkiAppearance.Catalog.GetAt(index);
                string path=Output+"/"+ski.Id+".mat";
                var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(deck);AssetDatabase.CreateAsset(material,path);}
                string texturePath=Output+"/"+ski.Id+".png";
                var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
                if(texture==null)throw new InvalidOperationException("Missing authored saved-ski texture: "+texturePath);
                material.SetTexture("_BaseMap",texture);material.SetColor("_BaseColor",Color.white);
                material.enableInstancing=true;EditorUtility.SetDirty(material);
                variants.Add(new Biathlete167RaceDefinition.SkiStyle{id=ski.Id,material=material});
            }
            string definitionPath=Output+"/RaceVisual.asset";
            var definition=AssetDatabase.LoadAssetAtPath<Biathlete167RaceDefinition>(definitionPath);
            if(definition==null){definition=ScriptableObject.CreateInstance<Biathlete167RaceDefinition>();AssetDatabase.CreateAsset(definition,definitionPath);}
            definition.athlete=rig;definition.bibMaterial=bib;definition.skis=variants.ToArray();
            definition.bodies=new[]{new Biathlete167RaceDefinition.BodyScale{id="slim",width=.96f,depth=.98f},
                new Biathlete167RaceDefinition.BodyScale{id="natural",width=1,depth=1},
                new Biathlete167RaceDefinition.BodyScale{id="athletic",width=1.04f,depth=1.02f}};
            definition.Validate();EditorUtility.SetDirty(definition);AssetDatabase.SaveAssets();
            return definitionPath;
        }

        public static string VerifyCareerCopies(string directory,string output)
        {
            if(!Path.GetFullPath(directory).StartsWith(Path.GetFullPath("artifacts/m16-7-race-integration/profile-backup"),StringComparison.OrdinalIgnoreCase))
                throw new ArgumentException("Verify the explicit backup copies only.");
            int careers=0,athletes=0,personal=0,legacy=0;
            var root=new GameObject("Career visual verification copies");
            try
            {
                foreach(string file in Directory.GetFiles(directory,"career_*.json"))
                {
                    var save=CareerSave.FromJson(File.ReadAllText(file));string before=save.ToJson();
                    for(int i=0;i<save.AthleteCount;i++)
                    {
                        var athlete=save.Athlete(i);
                        var source=AthleteRigFactory.Create(root.transform);
                        try
                        {
                            source.GetComponent<AthleteAppearancePresentation>().ApplyNation(athlete.Nation);
                            source.GetComponent<AthleteAppearancePresentation>().ApplyProfile(athlete.Appearance);
                            source.GetComponent<AthleteBibPresentation>().SetNumber(i+1);
                            var skis=source.gameObject.AddComponent<CareerSkiAppearance>();skis.Apply(source,athlete.SelectedSki);
                            var visual=Biathlete167RacePresentation.Attach(source);
                            if(visual.SelectedSkiId!=athlete.SelectedSki||visual.Number!=i+1||visual.Rig.GetComponent<Biathlete167Outfit>().Nation!=athlete.Nation)
                                throw new InvalidOperationException("Saved equipment/nationality changed in visual projection.");
                            source.GetComponent<AthleteBibPresentation>().SetLeader(CareerLeaderBib.Total);
                            if(visual.SkiDeckMaterial!=Resources.Load<Biathlete167RaceDefinition>(Biathlete167RaceDefinition.ResourcePath).Ski(athlete.SelectedSki))
                                throw new InvalidOperationException("Appearance refresh repainted the saved skis.");
                            if(!athlete.Appearance.IsLegacy)
                            {
                                var normalized=AthleteLookCatalog.Load().Edit(athlete.Appearance);
                                if(visual.AppliedProfile.ToJson()!=normalized.ToJson())throw new InvalidOperationException("Saved personal appearance changed.");
                                personal++;
                            }
                            else legacy++;
                            var face=Array.Find(AthleteFaceCatalog.Load().profiles,f=>f.id==visual.AppliedProfile.FaceId);
                            foreach(var lod in visual.Rig.Lods.GetLODs())foreach(var renderer in lod.renderers)
                                if(renderer is SkinnedMeshRenderer skin&&Mathf.Abs(skin.GetBlendShapeWeight(skin.sharedMesh.GetBlendShapeIndex(face.ShapeName))-100)>.001f)
                                    throw new InvalidOperationException("The actual mesh lost its saved face.");
                            athletes++;
                        }
                        finally{UnityEngine.Object.DestroyImmediate(source.gameObject);}
                    }
                    if(save.ToJson()!=before)throw new InvalidOperationException("A visual projection mutated the career.");
                    careers++;
                }
            }
            finally{UnityEngine.Object.DestroyImmediate(root);}
            if(careers==0)throw new InvalidOperationException("No career copies were verified.");
            string result=$"{{\"schema_version\":1,\"id\":\"m167_career_copy_projection\",\"careers\":{careers},\"athletes\":{athletes},\"personal_profiles\":{personal},\"legacy_profiles\":{legacy},\"career_state_changed\":false}}";
            File.WriteAllText(output,result);return result;
        }
    }
}
#endif
