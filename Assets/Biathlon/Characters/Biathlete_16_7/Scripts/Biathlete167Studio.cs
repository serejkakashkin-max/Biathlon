using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Asset-only orbit studio; never opens or writes a career profile.</summary>
    public sealed class Biathlete167Studio:MonoBehaviour
    {
        [SerializeField] private Biathlete167Rig rig;
        [SerializeField] private Camera view;
        [SerializeField] private PanelSettings panel;
        private float yaw=12,distance=3.7f,height=1.02f;
        private bool rotating;
        private Label status;
        private Biathlete167Motion motion;
        private Slider phaseSlider;
        private readonly string[] phaseLabels=new string[101];
        private int shownPhase=-1;
        public Biathlete167Motion Motion=>motion;
        public Biathlete167Rig Rig=>rig;
        public Camera View=>view;
        public void Configure(Biathlete167Rig athlete,Camera camera,PanelSettings settings){rig=athlete;view=camera;panel=settings;}
        private void Start()
        {
            GameBranding.ApplyWindowTitle(GameBranding.RussianTitle);
            Application.runInBackground=true;rig.Validate();rig.SetPose(0);
            // This is a close inspection studio. Inspect the authored surface;
            // distance LOD transitions are qualified separately from anatomy.
            rig.Lods.ForceLOD(0);
            motion=rig.gameObject.AddComponent<Biathlete167Motion>();motion.Initialize(rig,rig.IsNaturalAnatomy);
            var quality=gameObject.AddComponent<RenderQualityPresentation>();quality.Initialize(view);quality.Apply("high");
            BuildUi();SetView(0);
            if(Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonHero167Review")>=0)StartCoroutine(Review());
        }
        private void Update()
        {
            if(rotating)yaw+=Time.unscaledDeltaTime*12;
            var mouse=Mouse.current;
            if(mouse!=null)
            {
                if(mouse.rightButton.isPressed){var d=mouse.delta.ReadValue();yaw+=d.x*.22f;height=Mathf.Clamp(height+d.y*.002f,.1f,1.8f);}
                float wheel=mouse.scroll.ReadValue().y;if(wheel!=0)distance=Mathf.Clamp(distance-wheel*.0015f,.38f,5.5f);
            }
            var keyboard=Keyboard.current;
            if(keyboard!=null)
            {if(keyboard.digit1Key.wasPressedThisFrame)Pose(0);if(keyboard.digit2Key.wasPressedThisFrame)Pose(1);if(keyboard.digit3Key.wasPressedThisFrame)Pose(2);if(keyboard.digit4Key.wasPressedThisFrame)Pose(3);}
            PlaceCamera();
            if(motion.Mode==Biathlete167MotionMode.SkateV2)
            {phaseSlider.SetValueWithoutNotify(motion.Phase);int value=Mathf.RoundToInt(motion.Phase*100);if(value!=shownPhase){shownPhase=value;status.text=phaseLabels[value];}}
        }
        private void PlaceCamera()
        {
            var target=rig.transform.TransformPoint(new Vector3(0,height,0));float a=yaw*Mathf.Deg2Rad;
            view.transform.position=target+new Vector3(Mathf.Sin(a)*distance,distance*.055f,Mathf.Cos(a)*distance);
            view.transform.LookAt(target);view.fieldOfView=37;
        }
        public void SetView(int index)
        {
            rotating=false;
            switch(index)
            {case 0:yaw=0;distance=3.7f;height=.98f;break;case 1:yaw=90;distance=3.7f;height=.98f;break;case 2:yaw=180;distance=3.7f;height=.98f;break;case 3:yaw=8;distance=.77f;height=1.65f;break;case 4:yaw=170;distance=1.65f;height=1.25f;break;case 5:yaw=30;distance=1.12f;height=.17f;break;default:throw new ArgumentOutOfRangeException(nameof(index));}
            PlaceCamera();
        }
        private void Pose(int index){motion.Stop();shownPhase=-1;rig.SetPose(index);status.text=new[]{"Стойка","Согнутые колени","Руки вверх","Подготовка к стрельбе"}[index];}
        private void BuildUi()
        {
            for(int i=0;i<phaseLabels.Length;i++)phaseLabels[i]=(rig.IsNaturalAnatomy?"Основной спортсмен · ":"Коньковый ход · ")+i+"%";
            var doc=gameObject.AddComponent<UIDocument>();runtimePanel=Instantiate(panel);doc.panelSettings=runtimePanel;
            var root=doc.rootVisualElement;root.style.flexGrow=1;root.pickingMode=PickingMode.Ignore;
            root.style.unityFont=Resources.Load<Font>("Biathlon/UI/Fonts/Roboto");
            var rail=new ScrollView(ScrollViewMode.Vertical);rail.style.position=Position.Absolute;rail.style.left=24;rail.style.top=24;rail.style.bottom=24;rail.style.width=264;
            rail.style.paddingLeft=20;rail.style.paddingRight=20;rail.style.paddingTop=20;rail.style.paddingBottom=20;
            rail.style.backgroundColor=new Color(.025f,.045f,.065f,.94f);rail.style.color=new Color(.87f,.93f,.95f);
            rail.style.borderLeftWidth=3;rail.style.borderLeftColor=new Color(.92f,.13f,.18f);root.Add(rail);
            Label Line(string value,int size,Color? color=null)
            {var l=new Label(value);l.style.fontSize=size;l.style.whiteSpace=WhiteSpace.Normal;l.style.marginBottom=12;l.style.color=color??new Color(.87f,.93f,.95f);rail.Add(l);return l;}
            var title=Line("BIATHLON: PULSE",25);title.style.unityFont=Resources.Load<Font>("Biathlon/UI/Fonts/Oswald");
            Line("М16.7  /  СПОРТСМЕН",12,new Color(.98f,.34f,.35f));
            Line("Рассмотрите модель",19);Line("Правая кнопка мыши — вращать\nКолесо — приблизить",13,new Color(.60f,.72f,.78f));
            void Button(string value,Action action)
            {
                var b=new Button(action){text=value};b.style.height=32;b.style.marginBottom=5;b.style.unityTextAlign=TextAnchor.MiddleLeft;
                b.style.paddingLeft=12;b.style.fontSize=14;b.style.color=new Color(.90f,.94f,.95f);b.style.backgroundColor=new Color(.09f,.15f,.19f);
                b.style.borderLeftWidth=0;b.style.borderRightWidth=0;b.style.borderTopWidth=0;b.style.borderBottomWidth=0;rail.Add(b);
            }
            string[] views={"Спереди","Сбоку","Сзади","Лицо и очки","Винтовка и ремни","Ботинки и крепления"};
            for(int i=0;i<views.Length;i++){int at=i;Button(views[i],()=>SetView(at));}
            Button("Вращать автоматически",()=>rotating=!rotating);
            Line("Проверка поз",16);
            string[] poses={"1   Стойка","2   Согнутые колени","3   Руки вверх","4   Подготовка к стрельбе"};
            for(int i=0;i<poses.Length;i++){int at=i;Button(poses[i],()=>Pose(at));}
            status=Line("Стойка",12,new Color(.60f,.72f,.78f));
            Line("Движение",16);
            Button(rig.IsNaturalAnatomy?"Движения основного спортсмена":"Коньковый ход V2",()=>{motion.SetMode(Biathlete167MotionMode.SkateV2);motion.PlaybackEnabled=true;});
            Button("Пауза / продолжить",()=>{if(motion.Mode!=Biathlete167MotionMode.SkateV2)motion.SetMode(Biathlete167MotionMode.SkateV2);motion.PlaybackEnabled=!motion.PlaybackEnabled;});
            if(motion.MainReference!=null)
            {
                var states=new[]{SkiMotionState.Flat,SkiMotionState.Uphill,SkiMotionState.MaxSprint,SkiMotionState.Tuck};
                var technique=new DropdownField("Техника",new List<string>{"Равнина","Подъём","Спринт","Стойка на спуске"},0){name="main-athlete-technique"};
                technique.RegisterValueChangedCallback(_=>{motion.MainReference.State=states[technique.index];motion.EvaluateCycle(motion.Phase);});rail.Add(technique);
            }
            phaseSlider=new Slider("Фаза",0,1){name="motion-phase"};phaseSlider.style.marginTop=8;phaseSlider.RegisterValueChangedCallback(e=>{motion.PlaybackEnabled=false;motion.EvaluateCycle(e.newValue);});rail.Add(phaseSlider);
            var speed=new Slider("Скорость",.1f,1.5f){name="motion-speed",value=1};speed.RegisterValueChangedCallback(e=>motion.PlaybackSpeed=e.newValue);rail.Add(speed);
            Button("Перейти в игру",()=>SceneManager.LoadScene("FrontEnd"));
        }
        [Serializable] private sealed class Receipt
        {public int schema_version=1;public string id="biathlete_16_7_standalone_review";public bool success,humanoid,frames_nonempty;public int width,height,poses,lod_groups;public string[] screenshots,errors;public float[] model_area_visible_fraction;public string fullscreen_mode,capture_surface;}
        private PanelSettings runtimePanel;
        private Texture2D ReadSurface(RenderTexture target)
        {
            var previous=RenderTexture.active;RenderTexture.active=target;
            try{var texture=new Texture2D(target.width,target.height,TextureFormat.RGBA32,false);texture.ReadPixels(new Rect(0,0,target.width,target.height),0,0);texture.Apply();return texture;}
            finally{RenderTexture.active=previous;}
        }
        private void OnDestroy(){if(runtimePanel!=null)Destroy(runtimePanel);}
        private IEnumerator Review()
        {
            var args=Environment.GetCommandLineArgs();int at=Array.IndexOf(args,"-biathlonHero167Output");
            string output=at>=0&&at+1<args.Length?args[at+1]:Path.Combine(Application.dataPath,"../Hero167Review");Directory.CreateDirectory(output);
            var errors=new List<string>();var shots=new List<string>();Application.LogCallback log=(m,s,t)=>{if(t==LogType.Error||t==LogType.Exception||t==LogType.Assert)errors.Add(m);};Application.logMessageReceived+=log;
            // Hidden Windows swapchains can suppress the automatic world pass.
            // Explicitly submit the actual camera, then composite the native UI
            // from its own transparent target. UI clearing cannot erase the world.
            var world=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);world.Create();
            var overlay=new RenderTexture(Screen.width,Screen.height,24,RenderTextureFormat.ARGB32);overlay.Create();
            var target=new RenderTexture(Screen.width,Screen.height,0,RenderTextureFormat.ARGB32);target.Create();
            var composite=new Material(Resources.Load<Shader>("Biathlon/UI/LiveShootingReviewComposite"));composite.SetTexture("_Overlay",overlay);
            var previousPanel=runtimePanel.targetTexture;bool previousClear=runtimePanel.clearColor;Color previousColor=runtimePanel.colorClearValue;
            LiveShootingPlayerReview.ConfigureCapturePanel(runtimePanel,overlay);
            var request=new UnityEngine.Rendering.Universal.UniversalRenderPipeline.SingleCameraRequest{destination=world};
            bool nonempty=true;var fractions=new List<float>();
            void Save(string name)
            {
                Graphics.Blit(world,target,composite);
                var texture=ReadSurface(target);var pixels=texture.GetRawTextureData<byte>();int visible=0,total=0;
                // The rail is deliberately excluded: a UI-only black world is a failure.
                for(int y=0;y<texture.height;y+=16)for(int x=texture.width/4;x<texture.width;x+=16)
                {int k=(y*texture.width+x)*4;total++;if(pixels[k]+pixels[k+1]+pixels[k+2]>90)visible++;}
                float fraction=(float)visible/total;fractions.Add(fraction);
                if(fraction<=.01f){nonempty=false;errors.Add("Empty model area: "+name);}
                File.WriteAllBytes(Path.Combine(output,name),texture.EncodeToPNG());Destroy(texture);shots.Add(name);
            }
            try
            {
            for(int i=0;i<20;i++)yield return null;
            string[] names={"front","side","back","face","rifle","boots"};
            for(int i=0;i<6;i++)
            {
                SetView(i);yield return null;yield return new WaitForEndOfFrame();UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(view,request);
                Save(names[i]+".png");
            }
            SetView(0);
            for(int i=1;i<=3;i++)
            {
                // GPU skinning is updated later than coroutine Update resumes.
                // Submit only after this frame's skinning and native UI render.
                Pose(i);yield return null;yield return new WaitForEndOfFrame();UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(view,request);
                Save("pose"+i+".png");
            }
            Pose(0);
            for(int angle=0;angle<2;angle++)for(int i=0;i<12;i++)
            {
                motion.PlaybackEnabled=false;motion.EvaluateCycle(i/12f);SetView(angle);
                yield return null;yield return new WaitForEndOfFrame();UnityEngine.Rendering.RenderPipeline.SubmitRenderRequest(view,request);
                Save("skate-"+(angle==0?"front":"side")+i.ToString("00")+".png");
            }
            motion.Stop();
            }
            finally
            {
                Application.logMessageReceived-=log;runtimePanel.targetTexture=previousPanel;runtimePanel.clearColor=previousClear;runtimePanel.colorClearValue=previousColor;
                foreach(var surface in new[]{world,overlay,target}){surface.Release();Destroy(surface);}Destroy(composite);
            }
            var report=new Receipt{success=errors.Count==0,humanoid=rig.Animator.avatar.isHuman&&rig.Animator.avatar.isValid,width=Screen.width,height=Screen.height,fullscreen_mode=Screen.fullScreenMode.ToString(),poses=4,screenshots=shots.ToArray(),errors=errors.ToArray(),frames_nonempty=nonempty,model_area_visible_fraction=fractions.ToArray(),lod_groups=rig.GetComponentsInChildren<LODGroup>(true).Length,capture_surface="explicit native camera request plus native UI on separate24bit stencil surface, GPU composition; same screen resolution/quality"};
            report.success&=report.humanoid&&shots.Count==33&&nonempty&&report.lod_groups==1;
            File.WriteAllText(Path.Combine(output,"review.json"),JsonUtility.ToJson(report,true));Application.Quit(report.success?0:1);
        }
    }
}
