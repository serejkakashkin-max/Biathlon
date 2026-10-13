using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class SnowSpraySettings
    {
        public int schema_version,max_particles,max_emitters,ski_count,pole_count;
        public string id;
        public float interval_s,lifetime_s,near_distance_m,draw_distance_m,size_m,lift_mps,spread_mps,gravity_mps2,opacity;
        public float push_min,edge_gain,attack_gain,fresh_gain,ice_gain;
        public int performance_draw_budget,high_draw_budget,ultra_draw_budget;
        public static SnowSpraySettings Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/snow_spray_v1").text);
        public static SnowSpraySettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<SnowSpraySettings>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.max_particles<16||d.max_particles>2048||d.max_emitters<4||d.max_emitters>512||d.ski_count<1||d.ski_count>8||d.pole_count<1||d.pole_count>8)
                throw new ArgumentException("Invalid snow spray budget.");
            foreach(float v in new[]{d.interval_s,d.lifetime_s,d.near_distance_m,d.draw_distance_m,d.size_m,d.lift_mps,d.spread_mps,d.gravity_mps2,d.opacity})
                if(!float.IsFinite(v)||v<=0)throw new ArgumentException("Invalid snow spray setting.");
            if(d.near_distance_m>=d.draw_distance_m||d.opacity>1||d.size_m>.1f||d.lifetime_s>2||d.interval_s<.02f)throw new ArgumentException("Excessive snow spray.");
            if(d.push_min<=0||d.edge_gain<0||d.attack_gain<0||d.fresh_gain<1||d.fresh_gain>2||d.ice_gain<=0||d.ice_gain>1||d.performance_draw_budget<8||d.high_draw_budget<d.performance_draw_budget||d.ultra_draw_budget<d.high_draw_budget||d.ultra_draw_budget>d.max_particles)throw new ArgumentException("Invalid snow push/quality budgets.");
            return d;
        }
    }
    public readonly struct SnowSprayParticle
    {
        public readonly Vector3 Origin,Velocity,Normal;
        public readonly double Born;
        public readonly float Life,Size;
        public SnowSprayParticle(Vector3 origin,Vector3 velocity,Vector3 normal,double born,float life,float size)
        {Origin=origin;Velocity=velocity;Normal=normal;Born=born;Life=life;Size=size;}
        public Vector3 Position(double time,float gravity){float age=(float)Math.Max(0,time-Born);return Origin+Velocity*age+Vector3.down*(.5f*gravity*age*age);}
        public bool Alive(double time,float gravity)=>Life>0&&time>=Born&&time-Born<Life&&Vector3.Dot(Position(time,gravity)-Origin,Normal)>=-.005f;
    }
    /// <summary>Original reproducible visual scatter. This clock and hash never enter gameplay or shot resolution.</summary>
    public sealed class SnowSprayBuffer
    {
        private readonly SnowSpraySettings settings;
        private readonly SnowSprayParticle[] particles;
        private readonly Dictionary<(string,int),Emitter> emitters;
        private struct Emitter{public bool Contact;public double Time;public uint Seed,Sequence;}
        private int cursor;
        private double clock=double.NegativeInfinity;
        public int Capacity=>particles.Length;
        public int ActiveCount {get;private set;}
        public int TotalEmitted {get;private set;}
        public int Overwritten {get;private set;}
        public SnowSprayBuffer(SnowSpraySettings definition)
        {settings=SnowSpraySettings.FromJson(JsonUtility.ToJson(definition));particles=new SnowSprayParticle[settings.max_particles];emitters=new Dictionary<(string,int),Emitter>(settings.max_emitters);}
        public SnowSprayParticle Get(int index)=>particles[index];
        public void Clear(){Array.Clear(particles,0,particles.Length);emitters.Clear();cursor=ActiveCount=TotalEmitted=Overwritten=0;clock=double.NegativeInfinity;}
        public void Advance(double time)
        {
            if(!double.IsFinite(time)||time<0)throw new ArgumentOutOfRangeException(nameof(time));
            if(time<clock)Clear();if(time==clock)return;clock=time;ActiveCount=0;
            for(int i=0;i<particles.Length;i++){if(particles[i].Alive(time,settings.gravity_mps2))ActiveCount++;else particles[i]=default;}
        }
        public void Sample(string id,int channel,Vector3 position,Vector3 normal,Vector3 forward,double time,bool contact,float speed,float distance,bool tuck=false,float intensity=1)
        {
            if(string.IsNullOrWhiteSpace(id)||channel<0||channel>3||!Finite(position)||!Finite(normal)||!Finite(forward)||normal.sqrMagnitude<.1f||!float.IsFinite(speed)||!float.IsFinite(distance)||distance<0)
                throw new ArgumentException("Invalid snow spray observation.");
            if(!float.IsFinite(intensity)||intensity<0||intensity>2)throw new ArgumentException("Invalid snow push intensity.");
            Advance(time);var key=(id,channel);bool exists=emitters.TryGetValue(key,out var e);
            if(!exists){if(emitters.Count>=settings.max_emitters)return;e.Seed=HashId(id)^(uint)channel;}
            bool enabled=contact&&speed>.5f&&distance<settings.draw_distance_m;
            if(!enabled){e.Contact=false;emitters[key]=e;return;}
            bool pole=channel>=2;
            if(e.Contact&&(pole||time-e.Time+0.000001<settings.interval_s))return;
            normal.Normalize();forward=Vector3.ProjectOnPlane(forward,normal).normalized;
            var side=Vector3.Cross(normal,forward).normalized;
            int count=distance<settings.near_distance_m?(pole?settings.pole_count:settings.ski_count):1;
            if(tuck&&!pole)count=1;
            float strength=(pole?1:Mathf.Lerp(.45f,1,Mathf.Clamp01(speed/10)))*intensity;if(tuck&&!pole)strength*=.4f;
            for(int i=0;i<count;i++)
            {
                uint seed=e.Seed+e.Sequence++;float x=Unit(seed),y=Unit(seed+1031),z=Unit(seed+31337);
                var velocity=normal*(settings.lift_mps*(.65f+.35f*y)*strength)+side*((x*2-1)*settings.spread_mps*strength)-forward*(z*settings.spread_mps*.4f);
                if(particles[cursor].Life>0)Overwritten++;else ActiveCount++;
                particles[cursor]=new SnowSprayParticle(position,velocity,normal,time,settings.lifetime_s,settings.size_m*(.7f+.6f*z));
                cursor=(cursor+1)%particles.Length;TotalEmitted++;
            }
            e.Time=time;e.Contact=true;emitters[key]=e;
        }
        private static uint HashId(string value){unchecked{uint h=2166136261;for(int i=0;i<value.Length;i++)h=(h^value[i])*16777619;return h;}}
        private static float Unit(uint value){unchecked{value^=value>>16;value*=0x7feb352d;value^=value>>15;value*=0x846ca68b;value^=value>>16;return (value&0xffffff)/16777215f;}}
        private static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    }
}
