using System;
using System.Collections.Generic;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    [Serializable] public sealed class SnowTrackSettings
    {
        public int schema_version, max_marks, max_emitters;
        public string id;
        public float sample_interval_s,lifetime_s,fade_start_m,draw_distance_m,min_step_m,max_step_m,max_gap_s;
        public float ski_width_m,pole_width_m,surface_offset_m,ski_contact_height_m,pole_contact_height_m;
        public Color color;
        public static SnowTrackSettings Load()=>FromJson(Resources.Load<TextAsset>("Biathlon/snow_tracks_v1").text);
        public static SnowTrackSettings FromJson(string json)
        {
            var d=JsonUtility.FromJson<SnowTrackSettings>(json);
            if(d==null||d.schema_version!=1||string.IsNullOrWhiteSpace(d.id)||d.max_marks<16||d.max_marks>8192||d.max_emitters<4||d.max_emitters>512)
                throw new ArgumentException("Invalid snow track budget.");
            foreach(float v in new[]{d.sample_interval_s,d.lifetime_s,d.fade_start_m,d.draw_distance_m,d.min_step_m,d.max_step_m,d.max_gap_s,d.ski_width_m,d.pole_width_m,d.surface_offset_m,d.ski_contact_height_m,d.pole_contact_height_m})
                if(!float.IsFinite(v)||v<=0)throw new ArgumentException("Invalid snow track setting.");
            if(d.fade_start_m>=d.draw_distance_m||d.min_step_m>=d.max_step_m||d.sample_interval_s>=d.max_gap_s||d.ski_width_m>.2f||d.pole_width_m>.2f||d.lifetime_s>120)
                throw new ArgumentException("Invalid snow track ranges.");
            for(int i=0;i<4;i++)if(!float.IsFinite(d.color[i])||d.color[i]<0||d.color[i]>1)throw new ArgumentException("Invalid mark color.");
            return d;
        }
    }
    public readonly struct SnowTrackMark
    {
        public readonly Vector3 A,B,Normal;
        public readonly double Time;
        public readonly float Width;
        public SnowTrackMark(Vector3 a,Vector3 b,Vector3 normal,double time,float width){A=a;B=b;Normal=normal;Time=time;Width=width;}
    }
    /// <summary>Bounded render history only. Does not represent physical deformation or save state.</summary>
    public sealed class SnowTrackBuffer
    {
        private readonly SnowTrackSettings settings;
        private readonly SnowTrackMark[] marks;
        private readonly Dictionary<(string,int),Emitter> emitters;
        private struct Emitter { public Vector3 Position; public double Time; public bool Contact; }
        private int first,count;
        private double clock=double.NegativeInfinity;
        public int Count=>count;
        public int EmitterCount=>emitters.Count;
        public int Overwritten {get;private set;}
        public SnowTrackBuffer(SnowTrackSettings definition)
        {
            settings=SnowTrackSettings.FromJson(JsonUtility.ToJson(definition));marks=new SnowTrackMark[settings.max_marks];
            emitters=new Dictionary<(string,int),Emitter>(settings.max_emitters);
        }
        public SnowTrackMark Get(int index){if(index<0||index>=count)throw new ArgumentOutOfRangeException();return marks[(first+index)%marks.Length];}
        public void Clear(){first=count=Overwritten=0;emitters.Clear();clock=double.NegativeInfinity;}
        public void Advance(double time)
        {
            if(!double.IsFinite(time)||time<0)throw new ArgumentOutOfRangeException(nameof(time));
            if(time<clock)Clear();clock=time;
            while(count>0&&time-marks[first].Time>=settings.lifetime_s){first=(first+1)%marks.Length;count--;}
        }
        public void Sample(string athlete,int channel,Vector3 position,Vector3 normal,double time,bool contact,bool pole=false)
        {
            if(string.IsNullOrWhiteSpace(athlete)||channel<0||channel>3||!Finite(position)||!Finite(normal)||normal.sqrMagnitude<.01f)throw new ArgumentException("Invalid mark source.");
            Advance(time);var key=(athlete,channel);
            bool existed=emitters.TryGetValue(key,out var previous);
            if(!existed&&emitters.Count>=settings.max_emitters)return;
            if(!contact){emitters[key]=new Emitter{Position=position,Time=time,Contact=false};return;}
            if(existed&&previous.Contact&&time-previous.Time+0.000001<settings.sample_interval_s)return;
            normal.Normalize();
            if(pole)
            {
                if(!existed||!previous.Contact)
                {
                    var direction=Vector3.Cross(normal,Vector3.right).normalized;
                    if(direction.sqrMagnitude<.1f)direction=Vector3.Cross(normal,Vector3.forward).normalized;
                    Add(new SnowTrackMark(position-direction*settings.pole_width_m*.5f,position+direction*settings.pole_width_m*.5f,normal,time,settings.pole_width_m));
                }
            }
            else if(existed&&previous.Contact&&time-previous.Time<=settings.max_gap_s)
            {
                float distance=Vector3.Distance(previous.Position,position);
                if(distance<settings.min_step_m)return;
                if(distance>=settings.min_step_m&&distance<=settings.max_step_m)
                    Add(new SnowTrackMark(previous.Position,position,normal,time,settings.ski_width_m));
            }
            emitters[key]=new Emitter{Position=position,Time=time,Contact=true};
        }
        private void Add(SnowTrackMark mark)
        {
            if(count==marks.Length){first=(first+1)%marks.Length;count--;Overwritten++;}
            marks[(first+count)%marks.Length]=mark;count++;
        }
        private static bool Finite(Vector3 v)=>float.IsFinite(v.x)&&float.IsFinite(v.y)&&float.IsFinite(v.z);
    }
}
