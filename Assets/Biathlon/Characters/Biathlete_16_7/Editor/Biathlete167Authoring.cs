#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Biathlon.Unity.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using Object=UnityEngine.Object;

namespace Biathlon.Unity
{
    public static class Biathlete167Authoring
    {
        public const string Root="Assets/Biathlon/Characters/Biathlete_16_7";
        public const string Model=Root+"/Models/Biathlete_16_7.fbx";
        public const string Prefab=Root+"/Prefabs/Biathlete_16_7.prefab";
        public const string ScenePath=Root+"/Preview/Biathlete_16_7_Preview.unity";
        [Serializable] private class MaterialRecord {public string name;public float[] color;public float roughness,metallic;}
        [Serializable] private class Manifest {public MaterialRecord[] materials;}
        private static readonly (string human,string bone)[] Mapping={
            ("Hips","Hips"),("Spine","Spine02"),("Chest","Spine01"),("UpperChest","Spine"),("Neck","neck"),("Head","Head"),
            ("LeftShoulder","LeftShoulder"),("RightShoulder","RightShoulder"),("LeftUpperArm","LeftArm"),("RightUpperArm","RightArm"),
            ("LeftLowerArm","LeftForeArm"),("RightLowerArm","RightForeArm"),("LeftHand","LeftHand"),("RightHand","RightHand"),
            ("LeftUpperLeg","LeftUpLeg"),("RightUpperLeg","RightUpLeg"),("LeftLowerLeg","LeftLeg"),("RightLowerLeg","RightLeg"),
            ("LeftFoot","LeftFoot"),("RightFoot","RightFoot"),("LeftToes","LeftToeBase"),("RightToes","RightToeBase")};
        public static void Generate()
        {
            if(EditorApplication.isPlaying||EditorApplication.isCompiling)throw new InvalidOperationException("Idle compiled editor required.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            foreach(string path in Directory.GetFiles(Root+"/Textures","*.png"))
            {
                var t=(TextureImporter)AssetImporter.GetAtPath(path);if(t==null)throw new InvalidOperationException("Missing imported texture "+path);
                t.textureType=path.Contains("Normal")?TextureImporterType.NormalMap:TextureImporterType.Default;
                t.sRGBTexture=path.Contains("BaseColor");t.maxTextureSize=path.Contains("4K")?4096:2048;t.mipmapEnabled=true;t.anisoLevel=4;
                t.textureCompression=TextureImporterCompression.CompressedHQ;t.SaveAndReimport();
            }
            var manifest=JsonUtility.FromJson<Manifest>(File.ReadAllText(Root+"/Documentation/asset-manifest.json"));
            var materials=new Dictionary<string,Material>(StringComparer.Ordinal);
            foreach(var data in manifest.materials)
            {
                string path=Root+"/Materials/"+data.name+".mat";var material=AssetDatabase.LoadAssetAtPath<Material>(path);
                if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,path);}
                material.shader=Shader.Find("Universal Render Pipeline/Lit");material.name=data.name;
                // bpy shader colors are scene-linear. Unity's serialized Color
                // properties are sRGB and become linear when supplied to shaders.
                material.SetColor("_BaseColor",new Color(data.color[0],data.color[1],data.color[2],1).gamma);material.SetFloat("_Smoothness",1-data.roughness);material.SetFloat("_Metallic",data.metallic);
                if(data.name=="Anatomical_Skin_2K")
                {material.SetColor("_BaseColor",Color.white);Map(material,"_BaseMap","Skin_BaseColor_2K");Normal(material,"Skin_Normal_2K",.8f);Map(material,"_MetallicGlossMap","Skin_MetallicSmoothness_2K");material.EnableKeyword("_METALLICSPECGLOSSMAP");}
                else if(data.name=="PULSE_Tailored_Textile")
                {material.SetColor("_BaseColor",Color.white);Map(material,"_BaseMap","Suit_Panels_BaseColor_4K");Normal(material,"Textile_Normal_4K",.28f);Map(material,"_MetallicGlossMap","Textile_MetallicSmoothness_4K");material.EnableKeyword("_METALLICSPECGLOSSMAP");}
                else if(data.name.Contains("Race_Red")||data.name.Contains("Stretch")||data.name.Contains("Bib_")||data.name.Contains("Thread"))
                {Map(material,"_BaseMap","Textile_BaseColor_4K");Normal(material,"Textile_Normal_4K",.22f);material.SetFloat("_Smoothness",.33f);}
                material.SetFloat("_Cull",0);material.doubleSidedGI=true;material.enableInstancing=true;EditorUtility.SetDirty(material);AssetDatabase.SaveAssetIfDirty(material);materials[data.name]=material;
            }
            var importer=(ModelImporter)AssetImporter.GetAtPath(Model);if(importer==null)throw new InvalidOperationException("Missing generated FBX.");
            importer.importCameras=false;importer.importLights=false;importer.addCollider=false;importer.importAnimation=false;importer.optimizeGameObjects=false;
            importer.isReadable=true;importer.skinWeights=ModelImporterSkinWeights.Custom;importer.maxBonesPerVertex=4;importer.minBoneWeight=.001f;
            importer.materialImportMode=ModelImporterMaterialImportMode.ImportStandard;
            foreach(var pair in materials)importer.AddRemap(new AssetImporter.SourceAssetIdentifier(typeof(Material),pair.Key),pair.Value);
            var model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var skeleton=model.GetComponentsInChildren<Transform>(true).Select(t=>new SkeletonBone{name=t.name,position=t.localPosition,rotation=t.localRotation,scale=t.localScale}).ToArray();
            var description=new HumanDescription{human=Mapping.Select(x=>new HumanBone{humanName=x.human,boneName=x.bone,limit=new HumanLimit{useDefaultValues=true}}).ToArray(),skeleton=skeleton,
                upperArmTwist=.5f,lowerArmTwist=.5f,upperLegTwist=.5f,lowerLegTwist=.5f,armStretch=.05f,legStretch=.05f,feetSpacing=0,hasTranslationDoF=false};
            importer.animationType=ModelImporterAnimationType.Human;importer.avatarSetup=ModelImporterAvatarSetup.CreateFromThisModel;importer.humanDescription=description;importer.SaveAndReimport();
            model=AssetDatabase.LoadAssetAtPath<GameObject>(Model);
            var root=new GameObject("Biathlete_16_7");
            try
            {
                var instance=(GameObject)PrefabUtility.InstantiatePrefab(model,root.transform);instance.name="Humanoid model";
                PrefabUtility.UnpackPrefabInstance(instance,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
                foreach(var automaticGroup in instance.GetComponentsInChildren<LODGroup>(true))Object.DestroyImmediate(automaticGroup);
                var animator=instance.GetComponent<Animator>();if(animator==null||animator.avatar==null||!animator.avatar.isHuman||!animator.avatar.isValid)throw new InvalidOperationException("Humanoid import failed; inspect avatar mapping.");
                animator.applyRootMotion=false;animator.cullingMode=AnimatorCullingMode.CullUpdateTransforms;
                Transform Find(string name)=>instance.GetComponentsInChildren<Transform>(true).Single(t=>t.name==name);
                var sockets=new[]{Find("Rifle_Back_Socket"),Find("Rifle_Hand_Socket"),Find("Ski_Left_Socket"),Find("Ski_Right_Socket"),Find("Pole_Left_Socket"),Find("Pole_Right_Socket")};
                var weapon=Find("Rifle_Prop");weapon.SetParent(sockets[0],true);
                var skin=instance.GetComponentsInChildren<SkinnedMeshRenderer>(true);
                foreach(var s in skin){s.updateWhenOffscreen=false;s.quality=SkinQuality.Bone4;s.localBounds=new Bounds(new Vector3(0,1,0),new Vector3(2.2f,2.5f,2.2f));}
                var equipment=instance.GetComponentsInChildren<MeshRenderer>(true);
                var levels=new LOD[3];float[] thresholds={.32f,.13f,.015f};
                for(int i=0;i<3;i++)levels[i]=new LOD(thresholds[i],equipment.Cast<Renderer>().Concat(skin.Where(s=>s.name.EndsWith("LOD"+i,StringComparison.Ordinal))).ToArray());
                var lodGroup=root.AddComponent<LODGroup>();lodGroup.SetLODs(levels);lodGroup.fadeMode=LODFadeMode.None;lodGroup.RecalculateBounds();
                var binding=root.AddComponent<Biathlete167Rig>();binding.Configure(animator,sockets,weapon,equipment.Where(r=>r.name.Contains("Pole_Prop")).Cast<Renderer>().ToArray(),lodGroup);
                if(root.GetComponentsInChildren<Collider>(true).Length!=0)throw new InvalidOperationException("Visual prefab cannot acquire gameplay colliders.");
                PrefabUtility.SaveAsPrefabAsset(root,Prefab);
            }
            finally{Object.DestroyImmediate(root);}
            GenerateScene();
        }
        private static void Map(Material material,string property,string name)
        {var texture=AssetDatabase.LoadAssetAtPath<Texture2D>(Root+"/Textures/"+name+".png");if(texture==null)throw new InvalidDataException("Missing PBR map "+name);material.SetTexture(property,texture);}
        private static void Normal(Material material,string name,float strength){Map(material,"_BumpMap",name);material.SetFloat("_BumpScale",strength);material.EnableKeyword("_NORMALMAP");}
        private static void GenerateScene()
        {
            var previous=SceneManager.GetActiveScene();bool cleanUntitled=string.IsNullOrEmpty(previous.path)&&!previous.isDirty;
            if(string.IsNullOrEmpty(previous.path)&&previous.isDirty)throw new InvalidOperationException("Preserve the dirty untitled scene before character scene authoring.");
            var scene=EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,cleanUntitled?NewSceneMode.Single:NewSceneMode.Additive);SceneManager.SetActiveScene(scene);
            try
            {
                var prefab=AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);var hero=(GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
                foreach(var child in hero.GetComponentsInChildren<Transform>(true))child.gameObject.layer=28;
                var camera=new GameObject("Studio camera").AddComponent<Camera>();camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=new Color(.035f,.055f,.075f);camera.nearClipPlane=.04f;camera.farClipPlane=40;
                camera.cullingMask=~(1<<30);
                camera.transform.position=new Vector3(0,1.1f,3.7f);camera.transform.LookAt(new Vector3(0,1,0));
                var floor=GameObject.CreatePrimitive(PrimitiveType.Cylinder);floor.name="Neutral studio plinth";Object.DestroyImmediate(floor.GetComponent<Collider>());floor.transform.localScale=new Vector3(3,.04f,3);floor.transform.position=Vector3.down*.04f;
                string mp=Root+"/Materials/Studio_Platform.mat";var material=AssetDatabase.LoadAssetAtPath<Material>(mp);if(material==null){material=new Material(Shader.Find("Universal Render Pipeline/Lit"));AssetDatabase.CreateAsset(material,mp);}material.color=new Color(.065f,.085f,.105f);material.SetFloat("_Smoothness",.32f);AssetDatabase.SaveAssetIfDirty(material);floor.GetComponent<Renderer>().sharedMaterial=material;
                void Light(string name,Vector3 position,float power,Color color,float angle)
                {var light=new GameObject(name).AddComponent<Light>();light.type=LightType.Spot;light.transform.position=position;light.transform.LookAt(new Vector3(0,1,0));light.intensity=power;light.range=12;light.spotAngle=angle;light.color=color;light.shadows=LightShadows.Soft;light.shadowBias=.025f;}
                Light("Soft key",new Vector3(-2,3.4f,3),24,new Color(1,.91f,.83f),75);Light("Cool fill",new Vector3(2,2.5f,2),12,new Color(.7f,.83f,1),80);Light("Rim",new Vector3(.8f,3,-2),30,new Color(.85f,.94f,1),70);
                var sun=new GameObject("Studio broad fill").AddComponent<Light>();sun.type=LightType.Directional;sun.intensity=.65f;sun.transform.rotation=Quaternion.Euler(35,-25,0);sun.shadows=LightShadows.Soft;
                var probe=new GameObject("Local studio reflection").AddComponent<ReflectionProbe>();probe.transform.position=Vector3.up;probe.mode=ReflectionProbeMode.Realtime;probe.refreshMode=ReflectionProbeRefreshMode.OnAwake;probe.timeSlicingMode=ReflectionProbeTimeSlicingMode.NoTimeSlicing;probe.resolution=128;probe.size=Vector3.one*8;probe.intensity=1;
                probe.cullingMask=~(1<<28);
                string softboxPath=Root+"/Materials/Studio_Softbox.mat";var softboxMaterial=AssetDatabase.LoadAssetAtPath<Material>(softboxPath);
                if(softboxMaterial==null){softboxMaterial=new Material(Shader.Find("Universal Render Pipeline/Unlit"));AssetDatabase.CreateAsset(softboxMaterial,softboxPath);}softboxMaterial.SetColor("_BaseColor",Color.white*2);softboxMaterial.SetFloat("_Cull",0);AssetDatabase.SaveAssetIfDirty(softboxMaterial);
                foreach(var position in new[]{new Vector3(-2,2.7f,3),new Vector3(2,2.5f,2),new Vector3(1,3,-2)})
                {var card=GameObject.CreatePrimitive(PrimitiveType.Quad);card.name="Studio reflection softbox";Object.DestroyImmediate(card.GetComponent<Collider>());card.layer=30;card.transform.position=position;card.transform.LookAt(Vector3.up);card.transform.localScale=new Vector3(1.4f,2,1);card.GetComponent<Renderer>().sharedMaterial=softboxMaterial;}
                var studio=new GameObject("PULSE character studio").AddComponent<Biathlete167Studio>();studio.Configure(hero.GetComponent<Biathlete167Rig>(),camera,Resources.Load<PanelSettings>("Biathlon/UI/front_end_panel_v1"));
                EditorSceneManager.SaveScene(scene,ScenePath);
            }
            finally{if(cleanUntitled)EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects,NewSceneMode.Single);else{EditorSceneManager.CloseScene(scene,true);if(previous.IsValid())SceneManager.SetActiveScene(previous);}}
        }
    }
}
#endif
