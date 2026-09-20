using System;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class BenchmarkProfileCase
    {
        public string id,tier,shot,weather,lighting;
    }
    [Serializable] public sealed class BenchmarkProfileDefinition
    {
        public int schema_version,width,height,warmup_frames,sample_frames;
        public string id;
        public double start_time,step_seconds,frame_budget_ms,cpu_budget_ms,gpu_budget_ms;
        public int draw_call_budget,batch_budget,setpass_budget,triangle_budget;
        public BenchmarkProfileCase[] cases;
        public static BenchmarkProfileDefinition Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/benchmark_profile_v1").text);
        public static BenchmarkProfileDefinition FromJson(string json)
        {
            var d=JsonUtility.FromJson<BenchmarkProfileDefinition>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.width<640||d.width>3840||d.height<360||d.height>2160||d.warmup_frames<60||d.warmup_frames>1200||d.sample_frames<100||d.sample_frames>10000||d.cases==null||d.cases.Length<1||d.cases.Length>30)throw new ArgumentException("Invalid profiling definition.");
            if(!double.IsFinite(d.start_time)||d.start_time<0||!double.IsFinite(d.step_seconds)||d.step_seconds<=0||d.step_seconds>.1||d.start_time+(d.warmup_frames+d.sample_frames+8)*d.step_seconds>3600)throw new ArgumentException("Invalid profiling time window.");
            foreach(double budget in new[]{d.frame_budget_ms,d.cpu_budget_ms,d.gpu_budget_ms})if(!double.IsFinite(budget)||budget<=0||budget>1000)throw new ArgumentException("Invalid time budget.");
            foreach(int budget in new[]{d.draw_call_budget,d.batch_budget,d.setpass_budget,d.triangle_budget})if(budget<1||budget>100000000)throw new ArgumentException("Invalid rendering budget.");
            var shots=BroadcastShotDefinition.Load();var tiers=RenderQualityDefinition.Load();var lights=LightingVisualDefinition.Load();
            for(int i=0;i<d.cases.Length;i++)
            {
                var c=d.cases[i];if(c==null||string.IsNullOrWhiteSpace(c.id)||c.id.IndexOfAny(new[]{'/','\\',':'})>=0)throw new ArgumentException("Invalid profiling case ID.");
                for(int j=0;j<i;j++)if(c.id==d.cases[j].id)throw new ArgumentException("Duplicate profiling case.");
                shots.Get(c.shot);tiers.Get(c.tier);lights.Get(c.lighting);
                if(c.weather!="clear"&&c.weather!="overcast"&&c.weather!="snowfall")throw new ArgumentException("Unknown profiling weather.");
            }
            return d;
        }
    }
}
