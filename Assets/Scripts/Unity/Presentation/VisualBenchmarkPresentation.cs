using UnityEngine;
using UnityEngine.SceneManagement;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Integrated M12 visual review fixture, with explicit frozen/live presentation time.</summary>
    public sealed class VisualBenchmarkPresentation : MonoBehaviour
    {
        public Camera View;public Light Key;public SpectatorSeating Seating;public VegetationPresentation[] Forests;
        public VegetationPresentation ForestEdge;
        public string Tier="balanced",WeatherPreset="clear",LightingPreset="clear_cold",Shot="approach";
        public bool Animate,ShowControls=true;public double SampleTime=8;
        private RenderQualityPresentation quality;private WeatherPresentation weather;private LightingVisualDefinition lighting;
        private SpectatorCrowd crowd;private BroadcastCameraDirector director;
        public G2BenchmarkBackdrop Backdrop {get;private set;}
        public VisualBenchmarkAthletes Athletes {get;private set;}
        public G2GroundDressing GroundDressing {get;private set;}
        public G2Understory Understory {get;private set;}
        public BenchmarkReflectionPresentation Reflections {get;private set;}
        public bool Ready {get;private set;}
        public int VisibleTrees {get{int count=Backdrop!=null?Backdrop.VisibleTrees:0;foreach(var f in Forests)count+=f.VisibleInstances;return count;}}
        public int VisibleSpectators=>crowd.Visible;
        public void Initialize()
        {
            if(Ready)throw new System.InvalidOperationException("Benchmark already initialized.");
            lighting=LightingVisualDefinition.FromJson(Resources.Load<TextAsset>("Biathlon/g2_lighting_visual_v1").text);quality=gameObject.AddComponent<RenderQualityPresentation>();quality.Initialize(View);
            var sky=Resources.Load<Material>("Biathlon/Weather/G2Lighting/BenchmarkSky");if(sky==null)throw new System.InvalidOperationException("Generate G2 lighting assets.");
            weather=gameObject.AddComponent<WeatherPresentation>();weather.Initialize(View,Key,sky);crowd=gameObject.AddComponent<SpectatorCrowd>();crowd.Initialize(Seating,useSimplifiedShadows:true,useAuditedCrowd:true);
            foreach(var f in Forests)f.Initialize(new[]{new Vector3(0,0,-100),new Vector3(0,0,100)},useAuditedFir:true,groundHeight:G2BenchmarkSurface.Height,useAuditedPine:true,useSimplifiedShadows:true);
            if(ForestEdge!=null)
            {
                ForestEdge.Initialize(G2BenchmarkSurface.CopyRoute(),useAuditedFir:true,groundHeight:G2BenchmarkSurface.Height,useAuditedPine:true,useSimplifiedShadows:true,authoredInstances:G2ForestEdge.Load().Place());
                var all=new VegetationPresentation[Forests.Length+1];System.Array.Copy(Forests,all,Forests.Length);all[Forests.Length]=ForestEdge;Forests=all;
            }
            GroundDressing=gameObject.AddComponent<G2GroundDressing>();GroundDressing.Initialize();
            Understory=gameObject.AddComponent<G2Understory>();Understory.Initialize();
            Backdrop=gameObject.AddComponent<G2BenchmarkBackdrop>();Backdrop.Initialize();
            Athletes=gameObject.AddComponent<VisualBenchmarkAthletes>();Athletes.Initialize(View);Athletes.SetEnvironment(new BreathEnvironment(-12,new Vector2(1,.3f)));
            Reflections=gameObject.AddComponent<BenchmarkReflectionPresentation>();Reflections.Initialize();
            director=gameObject.AddComponent<BroadcastCameraDirector>();director.Initialize(View,BroadcastShotDefinition.Load());director.Bind("athlete",Athletes.Subject);Ready=true;Seek(SampleTime);
        }
        // Authored camera starts disabled for Edit Mode captures. Interactive play must render to the window.
        private void Start(){if(!Ready)Initialize();View.enabled=true;}
        public void Seek(double time)
        {
            if(!Ready)throw new System.InvalidOperationException("Initialize benchmark first.");SampleTime=time;
            // Position the observation first so tracking composition uses the current pose location.
            Athletes.RenderAt(time);director.Render(Shot,time);Athletes.Seek(time);RenderFrame(time);
        }
        public void RenderFrame(double time)
        {
            if(!Ready)return;SampleTime=time;Athletes.RenderAt(time);director.Render(Shot,time);
            if(Tier!=quality.TierId)quality.Apply(Tier);
            var wind=new Vector2(1,.3f);weather.Render(WeatherPreset,Tier,time,wind,VisualBenchmarkAthletes.Height(View.transform.position),lighting.Get(LightingPreset));
            Backdrop.ApplyAtmosphere(WeatherPreset);
            Backdrop.RenderTrees(View);
            foreach(var f in Forests)f.Render(View,Tier,(float)time,wind);GroundDressing.Render(View);Understory.Render(View);crowd.Render(View,Tier,(float)time);
            Reflections.Render(WeatherPreset,LightingPreset);
        }
        private void LateUpdate(){if(!Ready)return;double next=Animate?SampleTime+Time.deltaTime:SampleTime;if(next>3600)Seek(0);else RenderFrame(next);}
        private void OnGUI()
        {
            if(!ShowControls||!Ready)return;
            GUILayout.BeginArea(new Rect(12,12,680,180),GUI.skin.box);GUILayout.Label("M12 VISUAL BENCHMARK — 10 observed pose fixtures");GUILayout.Label("Presentation motion only; gameplay tests: Sprint / Mass Start");
            GUILayout.BeginHorizontal();foreach(var id in new[]{"approach","sector","athlete","range","targets","finish","rail","crowd_zoom"})if(GUILayout.Button(id)){Shot=id;Seek(SampleTime);}GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();if(GUILayout.Button(Animate?"Freeze":"Animate"))Animate=!Animate;foreach(var id in new[]{"low","balanced","high"})if(GUILayout.Button(id))Tier=id;GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();foreach(var p in lighting.presets)if(GUILayout.Button(p.id)){LightingPreset=p.id;WeatherPreset=p.recommended_weather;}if(GUILayout.Button("snow"))WeatherPreset="snowfall";GUILayout.EndHorizontal();
            GUILayout.BeginHorizontal();if(GUILayout.Button("Sprint"))SceneManager.LoadScene("PlayableSprint");if(GUILayout.Button("Mass Start"))SceneManager.LoadScene("PlayableMassStart");GUILayout.EndHorizontal();GUILayout.EndArea();
        }
    }
}
