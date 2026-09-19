using System;
using System.Collections.Generic;
using UnityEngine;
namespace Biathlon.Unity.Presentation
{
    public readonly struct BreathEnvironment
    {
        public readonly float Celsius;public readonly Vector2 Wind;
        public BreathEnvironment(float celsius,Vector2 wind){if(!float.IsFinite(celsius)||celsius< -60||celsius>40||!float.IsFinite(wind.x)||!float.IsFinite(wind.y)||wind.sqrMagnitude>1600)throw new ArgumentException("Invalid visual breath environment.");Celsius=celsius;Wind=wind;}
    }
    [Serializable] public sealed class BreathVaporSettings
    {
        public int schema_version,max_puffs,max_sources,puffs_per_exhale;public string id;
        public float full_celsius,invisible_celsius,rest_hz,loaded_hz,lifetime_s,max_gap_s,size_start_m,growth_mps,forward_mps,rise_mps,wind_fraction,opacity,fade_start_m,cull_m;public Vector3 mouth_local;
        public static BreathVaporSettings Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/breath_vapor_v1").text);
        public static BreathVaporSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<BreathVaporSettings>(json);if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.max_puffs<1||d.max_puffs>2048||d.max_sources<1||d.max_sources>512||d.puffs_per_exhale<1||d.puffs_per_exhale>6)throw new ArgumentException("Invalid vapor schema/budget.");
            Bound(d.full_celsius,-40,0);Bound(d.invisible_celsius,d.full_celsius+1,15);Bound(d.rest_hz,.1f,1);Bound(d.loaded_hz,d.rest_hz,2);Bound(d.lifetime_s,.1f,2);Bound(d.max_gap_s,.05f,.5f);Bound(d.size_start_m,.005f,.1f);Bound(d.growth_mps,0,.3f);Bound(d.forward_mps,0,1);Bound(d.rise_mps,0,.5f);Bound(d.wind_fraction,0,1);Bound(d.opacity,0,.3f);Bound(d.fade_start_m,1,20);Bound(d.cull_m,d.fade_start_m+1,40);for(int i=0;i<3;i++)Bound(d.mouth_local[i],-.2f,.2f);return d;
        }
        private static void Bound(float v,float lo,float hi){if(!float.IsFinite(v)||v<lo||v>hi)throw new ArgumentException("Invalid vapor range.");}
        public float Intensity(float celsius,float load){Bound(celsius,-60,40);Bound(load,0,1);return Mathf.InverseLerp(invisible_celsius,full_celsius,celsius)*Mathf.Lerp(.3f,1,load);}
    }
    public readonly struct VaporPuff
    {
        public readonly Vector3 Origin,Velocity;public readonly double Born;public readonly float Strength;
        public VaporPuff(Vector3 p,Vector3 velocity,double time,float strength){Origin=p;Velocity=velocity;Born=time;Strength=strength;}
    }
    public sealed class BreathVaporBuffer
    {
        private readonly BreathVaporSettings settings;
        private readonly VaporPuff[] puffs;
        private readonly Dictionary<string,Source> sources;
        private struct Source{public double Time,Phase;public bool Shooting;}
        private int cursor;private double lastTime=-1;
        public int Capacity=>puffs.Length;public long TotalEmitted {get;private set;}public long Overwritten {get;private set;}
        public VaporPuff Get(int index)=>puffs[index];
        public BreathVaporBuffer(BreathVaporSettings definition){settings=BreathVaporSettings.FromJson(JsonUtility.ToJson(definition));puffs=new VaporPuff[settings.max_puffs];sources=new Dictionary<string,Source>(settings.max_sources,StringComparer.Ordinal);}
        public void Advance(double time)
        {
            if(!double.IsFinite(time)||time<0)throw new ArgumentException("Finite nonnegative vapor time required.");
            if(time<lastTime)Clear();lastTime=time;
            for(int i=0;i<puffs.Length;i++)if(puffs[i].Strength>0&&time-puffs[i].Born>=settings.lifetime_s)puffs[i]=default;
        }
        public void Observe(string id,double time,Vector3 mouth,Vector3 forward,float load,BreathEnvironment environment,bool active,bool holding,double? observedPhase=null)
        {
            if(string.IsNullOrWhiteSpace(id)||!float.IsFinite(mouth.x)||!float.IsFinite(mouth.y)||!float.IsFinite(mouth.z)||!float.IsFinite(forward.x)||!float.IsFinite(forward.y)||!float.IsFinite(forward.z)||forward.sqrMagnitude<.1f||observedPhase.HasValue&&(!double.IsFinite(observedPhase.Value)||observedPhase.Value<0||observedPhase.Value>=1))throw new ArgumentException("Invalid vapor source.");
            float strength=settings.Intensity(environment.Celsius,load);Advance(time);
            bool exists=sources.TryGetValue(id,out var source);if(!exists&&sources.Count>=settings.max_sources)return;
            double dt=exists?time-source.Time:0;if(exists&&dt==0)return;
            bool reset=!exists||dt>settings.max_gap_s||source.Shooting!=observedPhase.HasValue;
            double phase=observedPhase??(reset?0:source.Phase+dt*Mathf.Lerp(settings.rest_hz,settings.loaded_hz,load));
            bool exhale=reset?phase<.1:observedPhase.HasValue?phase<source.Phase:phase>=1;
            phase-=Math.Floor(phase);sources[id]=new Source{Time=time,Phase=phase,Shooting=observedPhase.HasValue};
            if(!active||holding||strength<=0||!exhale)return;
            var side=Vector3.Cross(Vector3.up,forward).normalized;
            for(int i=0;i<settings.puffs_per_exhale;i++)
            {
                if(puffs[cursor].Strength>0)Overwritten++;
                float offset=i-(settings.puffs_per_exhale-1)*.5f;
                var velocity=forward.normalized*settings.forward_mps+Vector3.up*settings.rise_mps+new Vector3(environment.Wind.x,0,environment.Wind.y)*settings.wind_fraction+side*(offset*.04f);
                puffs[cursor]=new VaporPuff(mouth+side*(offset*.008f),velocity,time,strength);cursor=(cursor+1)%puffs.Length;TotalEmitted++;
            }
        }
        public void Clear(){Array.Clear(puffs,0,puffs.Length);sources.Clear();cursor=0;lastTime=-1;TotalEmitted=0;Overwritten=0;}
    }
}
