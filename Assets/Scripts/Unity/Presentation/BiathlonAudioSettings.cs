using System;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    public enum AudioBus { Effects, Body, Ambience, Interface, Music }
    public enum AudioCue { SkiPacked, SkiSoft, SkiIce, Pole, Breath, Heart, Rifle, Bolt, TargetHit, TargetMiss, Wind, Snowfall, Crowd, Cheer, Focus, Confirm, Back, Countdown, Start, Menu }
    [Serializable] public sealed class AudioCueDefinition
    {
        public string id, bus;
        public string[] clips;
        public float gain, min_distance=2, max_distance=100;
        public int priority;
        public bool loop;
    }
    [Serializable] public sealed class BiathlonAudioDefinition
    {
        public int schema_version, transient_voices, loop_voices, max_events_per_frame;
        public string id;
        public float fade_seconds, focus_duck, speed_reference, pole_distance, breath_min_seconds, breath_max_seconds,
            heart_threshold, heart_ceiling, event_gap_seconds, ui_cooldown_seconds, silent_threshold, mix_headroom;
        public AudioCueDefinition[] cues;
        public static BiathlonAudioDefinition Load()
        {
            var value=JsonUtility.FromJson<BiathlonAudioDefinition>(ProductionVenueHost.ReadResource("Biathlon/Audio/audio_v1"));
            value.Validate();return value;
        }
        public void Validate()
        {
            if(schema_version!=1||string.IsNullOrEmpty(id)||transient_voices<4||transient_voices>32||loop_voices!=6||max_events_per_frame<1||max_events_per_frame>32||
                !(fade_seconds>0&&fade_seconds<10)||!(focus_duck>=0&&focus_duck<=1)||!(speed_reference>0)||!(pole_distance>0)||
                !(breath_min_seconds>0&&breath_max_seconds>=breath_min_seconds)||!(heart_ceiling>heart_threshold&&heart_threshold>0)||
                !(event_gap_seconds>0&&event_gap_seconds<=2)||!(ui_cooldown_seconds>0)||!(silent_threshold>0&&silent_threshold<.01f)||!(mix_headroom>0&&mix_headroom<=1)||
                cues==null||cues.Length!=Enum.GetValues(typeof(AudioCue)).Length)throw new ArgumentException("Invalid audio definition.");
            for(int i=0;i<cues.Length;i++)
            {
                var c=cues[i];
                if(c==null||c.id!=((AudioCue)i).ToString()||!Enum.TryParse<AudioBus>(c.bus,out _)||c.clips==null||c.clips.Length<1||
                    !(c.gain>=0&&c.gain<=1)||c.priority<0||c.priority>255||!(c.min_distance>0&&c.max_distance>c.min_distance))throw new ArgumentException("Invalid audio cue.");
                foreach(var clip in c.clips)if(string.IsNullOrEmpty(clip)||clip.Contains("/")||clip.Contains(".."))throw new ArgumentException("Invalid clip ID.");
            }
        }
    }
    [Serializable] public sealed class BiathlonAudioPreferences
    {
        public int schema_version=1;
        public string id="player_audio_v1";
        public float master=.8f, effects=.85f, body=.65f, ambience=.7f, ui=.65f, music=.45f;
        public bool quiet, mute_unfocused=true;
        private const string Key="biathlon.audio.v1";
        public float Gain(AudioBus bus)=>bus==AudioBus.Effects?effects:bus==AudioBus.Body?body:bus==AudioBus.Ambience?ambience:bus==AudioBus.Interface?ui:music;
        public bool Valid()=>schema_version==1&&id=="player_audio_v1"&&Unit(master)&&Unit(effects)&&Unit(body)&&Unit(ambience)&&Unit(ui)&&Unit(music);
        private static bool Unit(float v)=>float.IsFinite(v)&&v>=0&&v<=1;
        public static BiathlonAudioPreferences Load()
        {
            try { var p=JsonUtility.FromJson<BiathlonAudioPreferences>(PlayerPrefs.GetString(Key,""));return p!=null&&p.Valid()?p:new BiathlonAudioPreferences(); }
            catch(ArgumentException){return new BiathlonAudioPreferences();}
        }
        public void Save(){if(!Valid())throw new ArgumentException("Invalid audio preferences.");PlayerPrefs.SetString(Key,JsonUtility.ToJson(this));PlayerPrefs.Save();}
    }
}
