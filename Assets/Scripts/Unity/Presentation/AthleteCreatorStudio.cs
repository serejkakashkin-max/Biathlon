using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Biathlon.Unity.Presentation
{
    public enum AthletePreviewPose { Idle, Sport, Skate, BackCarry }

    [Serializable] public sealed class AthleteCreatorStudioDefinition
    {
        public int schema_version,layer,texture_width,texture_height;
        public string id,background;
        public float field_of_view,full_distance,face_distance,full_focus_y,face_focus_y;
        public float key_intensity,fill_intensity,rim_intensity;
        private static AthleteCreatorStudioDefinition cached;
        public static AthleteCreatorStudioDefinition Load()
        {
            if(cached!=null)return cached;
            var asset=Resources.Load<TextAsset>("Biathlon/athlete_creator_studio_v1");
            var d=asset==null?null:JsonUtility.FromJson<AthleteCreatorStudioDefinition>(asset.text);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.layer<24||d.layer>30||
                d.texture_width<256||d.texture_width>1600||d.texture_height<256||d.texture_height>1600||
                !(d.field_of_view>=20&&d.field_of_view<=45&&d.full_distance>=3&&d.full_distance<=6&&
                d.face_distance>=.4f&&d.face_distance<=1&&d.full_focus_y>=.7f&&d.full_focus_y<=1.1f&&d.face_focus_y>=1.5f&&d.face_focus_y<=1.8f&&
                d.key_intensity>=.1f&&d.key_intensity<=50&&d.fill_intensity>=.1f&&d.fill_intensity<=50&&d.rim_intensity>=.1f&&d.rim_intensity<=50)||
                !ColorUtility.TryParseHtmlString(d.background,out _))throw new ArgumentException("Invalid character studio definition.");
            return cached=d;
        }
    }

    /// <summary>One reusable native G2 preview. Owns its camera/target/finite lights; no authority or global render changes.</summary>
    public sealed class AthleteCreatorStudio : MonoBehaviour
    {
        public AthleteRigBinding Rig {get;private set;}
        public RenderTexture Texture {get;private set;}
        public Camera PreviewCamera {get;private set;}
        public float Yaw {get;private set;}=25;
        public float Zoom {get;private set;}
        public AthletePreviewPose Pose {get;private set;}
        public bool ReducedMotion {get;set;}
        private GameObject stage;
        private AthleteCreatorStudioDefinition definition;
        private AthleteHybridLocomotionDriver motion;
        private AthleteRiflePresentation rifle;
        private AthleteGroundContact contact;
        private AthleteGripPresentation grip;
        private double started;
        private bool released;
        private float framedScale=1;
        public void Initialize()
        {
            if(released)throw new ObjectDisposedException(nameof(AthleteCreatorStudio));
            if(Rig!=null)return;
            definition=AthleteCreatorStudioDefinition.Load();started=Time.unscaledTimeAsDouble;
            stage=new GameObject("Athlete creator studio");stage.transform.SetParent(transform,false);
            // Outside every race/menu camera's far clip. Finite studio lights cannot reach the venue.
            stage.transform.position=new Vector3(10000,0,10000);
            Rig=AthleteRigFactory.Create(stage.transform);motion=Rig.GetComponent<AthleteHybridLocomotionDriver>();
            rifle=Rig.GetComponent<AthleteRiflePresentation>();contact=Rig.GetComponent<AthleteGroundContact>();grip=Rig.GetComponent<AthleteGripPresentation>();
            Rig.GetComponent<AthleteBibPresentation>().SetNumber(1);
            Rig.GetComponent<AthleteAppearancePresentation>().ApplyNation("NOR");
            foreach(var renderer in Rig.GetComponentsInChildren<SkinnedMeshRenderer>())
            {renderer.updateWhenOffscreen=true;renderer.forceMatrixRecalculationPerRender=true;renderer.reflectionProbeUsage=ReflectionProbeUsage.Off;}
            var cameraObject=new GameObject("Character studio camera",typeof(Camera));cameraObject.transform.SetParent(stage.transform,false);
            PreviewCamera=cameraObject.GetComponent<Camera>();PreviewCamera.cullingMask=1<<definition.layer;PreviewCamera.nearClipPlane=.03f;PreviewCamera.farClipPlane=12;
            PreviewCamera.clearFlags=CameraClearFlags.SolidColor;ColorUtility.TryParseHtmlString(definition.background,out var background);PreviewCamera.backgroundColor=background;
            PreviewCamera.fieldOfView=definition.field_of_view;PreviewCamera.allowHDR=true;PreviewCamera.allowMSAA=true;PreviewCamera.depth=-10;
            var data=cameraObject.AddComponent<UniversalAdditionalCameraData>();data.renderPostProcessing=false;data.volumeLayerMask=0;data.requiresColorOption=CameraOverrideOption.Off;data.requiresDepthOption=CameraOverrideOption.Off;
            Texture=new RenderTexture(definition.texture_width,definition.texture_height,24,RenderTextureFormat.ARGB32)
                {name="FROSTLINE athlete creator",antiAliasing=2,hideFlags=HideFlags.DontSave};Texture.Create();PreviewCamera.targetTexture=Texture;
            AddLight("Studio key",new Vector3(-2.4f,3.2f,3),new Color(.84f,.93f,1),definition.key_intensity,true);
            AddLight("Studio fill",new Vector3(2.8f,2.0f,2.2f),new Color(.60f,.82f,1),definition.fill_intensity,false);
            AddLight("Studio rim",new Vector3(0,2.8f,-2.5f),new Color(1,.83f,.60f),definition.rim_intensity,false);
            foreach(var item in stage.GetComponentsInChildren<Transform>(true))item.gameObject.layer=definition.layer;
            UpdateView();SamplePose(0);
        }
        private void AddLight(string label,Vector3 position,Color color,float intensity,bool shadows)
        {
            var obj=new GameObject(label,typeof(Light));obj.transform.SetParent(stage.transform,false);obj.transform.localPosition=position;
            obj.transform.LookAt(stage.transform.position+Vector3.up);var light=obj.GetComponent<Light>();light.type=LightType.Spot;light.range=12;light.spotAngle=65;
            light.innerSpotAngle=45;light.color=color;light.intensity=intensity;light.cullingMask=1<<definition.layer;
            light.shadows=shadows?LightShadows.Soft:LightShadows.None;light.shadowCustomResolution=1024;
        }
        public void Rotate(float degrees)
        {if(!float.IsFinite(degrees))throw new ArgumentException("Finite rotation required.");Initialize();Yaw=Mathf.Repeat(Yaw+degrees,360);UpdateView();}
        public void SetView(float yaw,float zoom)
        {
            if(!float.IsFinite(yaw)||!float.IsFinite(zoom))throw new ArgumentException("Finite studio view required.");
            Initialize();Yaw=Mathf.Repeat(yaw,360);Zoom=Mathf.Clamp01(zoom);UpdateView();
        }
        public void SetPose(AthletePreviewPose pose)
        {
            if(pose<AthletePreviewPose.Idle||pose>AthletePreviewPose.BackCarry)throw new ArgumentOutOfRangeException(nameof(pose));
            Initialize();Pose=pose;if(pose==AthletePreviewPose.BackCarry){Yaw=180;UpdateView();}SamplePose(0);
        }
        private void UpdateView()
        {
            if(Rig==null)return;Rig.transform.localRotation=Quaternion.Euler(0,Yaw,0);
            framedScale=Rig.transform.localScale.x;
            var focus=new Vector3(0,Mathf.Lerp(definition.full_focus_y,definition.face_focus_y,Zoom)*framedScale,0);
            PreviewCamera.transform.localPosition=focus+Vector3.forward*Mathf.Lerp(definition.full_distance*framedScale,definition.face_distance,Zoom);
            PreviewCamera.transform.LookAt(stage.transform.TransformPoint(focus));
        }
        public void SamplePose(double seconds)
        {
            if(!double.IsFinite(seconds)||seconds<0)throw new ArgumentException("Finite nonnegative preview time required.");
            Initialize();bool animate=Pose==AthletePreviewPose.Skate&&!ReducedMotion&&Zoom<.7f;
            var input=Pose==AthletePreviewPose.Sport?new LocomotionInput(3,0,.7f):animate?new LocomotionInput(6,0,.9f):new LocomotionInput(0,0,0,travelling:false);
            double time=animate?seconds:0;
            motion.Sample(Rig,input,time);rifle.Sample(Rig,0);
            contact.SampleMoving(Rig,0,motion.PoleCycle,motion.ContactState,0,time);motion.ApplySkiEdges(Rig);grip.Render(Rig);
        }
        private void LateUpdate(){if(Rig!=null){if(framedScale!=Rig.transform.localScale.x)UpdateView();SamplePose(Math.Max(0,Time.unscaledTimeAsDouble-started));}}
        private void OnDestroy()=>Release();
        public void Release()
        {
            if(released)return;released=true;
            if(PreviewCamera!=null){PreviewCamera.enabled=false;PreviewCamera.targetTexture=null;}
            if(stage!=null)Destroy(stage);Rig=null;PreviewCamera=null;stage=null;
            if(Texture!=null){Texture.Release();Destroy(Texture);Texture=null;}
        }
    }
}
