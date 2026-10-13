using System;
using UnityEngine;
using UnityEngine.Audio;

namespace Biathlon.Unity.Presentation
{
    /// <summary>One bounded, prewarmed presentation mixer. Never feeds back into simulation.</summary>
    public sealed class BiathlonAudio:MonoBehaviour
    {
        public static BiathlonAudio Instance {get;private set;}
        public BiathlonAudioDefinition Definition {get;private set;}
        public BiathlonAudioPreferences Preferences {get;private set;}
        public int Played {get;private set;} public int Dropped {get;private set;} public int PeakVoices {get;private set;}
        public int SourceCount=>voices?.Length??0;
        public long PcmBytes {get;private set;}
        public int ActiveVoices {get;private set;}
        public RaceAudioPresentation Race {get;private set;}
        private sealed class Voice
        {
            public AudioSource source;public AudioCue cue;public float target,gain,pitch=1;public bool spatial;
            public double until;public int importance;public Vector3 worldPosition;
        }
        private Voice[] voices;
        private AudioClip[][] clips;
        private AudioMixerGroup[] groups;
        private AudioBus[] buses;
        private int[] variations;
        private int[] playedByCue;
        private AudioListener listener;
        private bool focused=true,menuVisible=true;
        private double nextUi;
        public static BiathlonAudio Ensure(bool persist=true)
        {
            if(Instance!=null)return Instance;
            var go=new GameObject("Biathlon audio");var audio=go.AddComponent<BiathlonAudio>();
            audio.Initialize(persist?BiathlonAudioPreferences.Load():new BiathlonAudioPreferences());
            DontDestroyOnLoad(go);return audio;
        }
        public void Initialize(BiathlonAudioPreferences preferences)
        {
            if(Instance!=null||voices!=null||preferences==null||!preferences.Valid())throw new InvalidOperationException("Fresh audio owner and valid preferences required.");
            Definition=BiathlonAudioDefinition.Load();Preferences=preferences;
            var mixer=Resources.Load<AudioMixer>("Biathlon/Audio/Biathlon");if(mixer==null)throw new InvalidOperationException("Missing Biathlon audio mixer.");
            groups=new AudioMixerGroup[5];for(int i=0;i<groups.Length;i++)
            {var found=mixer.FindMatchingGroups(((AudioBus)i).ToString());if(found.Length!=1)throw new InvalidOperationException("Missing audio bus.");groups[i]=found[0];}
            clips=new AudioClip[Definition.cues.Length][];variations=new int[clips.Length];playedByCue=new int[clips.Length];buses=new AudioBus[clips.Length];
            for(int i=0;i<clips.Length;i++)
            {
                buses[i]=(AudioBus)Enum.Parse(typeof(AudioBus),Definition.cues[i].bus);
                clips[i]=new AudioClip[Definition.cues[i].clips.Length];
                for(int j=0;j<clips[i].Length;j++)
                {
                    var clip=Resources.Load<AudioClip>("Biathlon/Audio/"+Definition.cues[i].clips[j]);
                    if(clip==null||!clip.LoadAudioData())throw new InvalidOperationException("Missing/unreadable audio clip: "+Definition.cues[i].clips[j]);
                    clips[i][j]=clip;PcmBytes+=(long)clip.samples*clip.channels*4;
                }
            }
            voices=new Voice[Definition.transient_voices+Definition.loop_voices];
            for(int i=0;i<voices.Length;i++)
            {
                var child=new GameObject("Audio voice "+i);child.transform.SetParent(transform,false);
                var source=child.AddComponent<AudioSource>();source.playOnAwake=false;source.volume=0;source.dopplerLevel=0;source.rolloffMode=AudioRolloffMode.Linear;
                voices[i]=new Voice{source=source};
            }
            listener=gameObject.AddComponent<AudioListener>();Instance=this;
        }
        public void Bind(RaceAudioPresentation race){if(Race!=race)StopRaceVoices();Race=race;menuVisible=false;}
        public void Unbind(RaceAudioPresentation race){if(Race!=race)return;StopRaceVoices();Race=null;menuVisible=true;}
        public void SetMenu(bool visible)=>menuVisible=visible;
        public void SetPreferences(BiathlonAudioPreferences preferences,bool persist)
        {if(preferences==null||!preferences.Valid())throw new ArgumentException("Invalid audio preferences.");Preferences=preferences;
            if(voices!=null)foreach(var v in voices)v.source.volume=Gain(v,Race!=null&&Race.ShootingFocus);
            if(persist)preferences.Save();}
        public bool Play(AudioCue cue,Vector3 position=default,bool spatial=false,float gain=1,float pitch=1,bool local=false,double delay=0)
        {
            var c=Definition.cues[(int)cue];if(c.loop)throw new ArgumentException("Loop cue requires a reserved loop voice.");
            if(!float.IsFinite(gain)||gain<0||!float.IsFinite(pitch)||pitch<=0||!double.IsFinite(delay)||delay<0)throw new ArgumentException("Invalid audio request.");
            int importance=local?Math.Max(0,c.priority-10):c.priority+30;
            int slot=-1,worst=-1;double now=AudioSettings.dspTime;
            for(int i=Definition.loop_voices;i<voices.Length;i++)
            {
                if(voices[i].until<=now){slot=i;break;}
                if(worst<0||voices[i].importance>voices[worst].importance)worst=i;
            }
            if(slot<0&&worst>=0&&voices[worst].importance>importance)slot=worst;
            if(slot<0){Dropped++;return false;}
            var voice=voices[slot];voice.source.Stop();Configure(voice,cue,position,spatial,pitch);
            voice.importance=importance;voice.target=voice.gain=Mathf.Clamp01(gain);voice.until=now+delay+voice.source.clip.length/voice.source.pitch;
            voice.source.volume=Gain(voice,false);
            if(delay>0)voice.source.PlayScheduled(now+delay);else voice.source.Play();Played++;playedByCue[(int)cue]++;return true;
        }
        public int PlayedCue(AudioCue cue)=>playedByCue[(int)cue];
        public void Ui(AudioCue cue)
        {
            double now=Time.realtimeSinceStartupAsDouble;if(cue==AudioCue.Focus&&now<nextUi)return;
            nextUi=now+Definition.ui_cooldown_seconds;Play(cue,local:true);
        }
        public void Loop(int slot,AudioCue cue,float gain,float pitch=1,Vector3 position=default,bool spatial=false)
        {
            if(slot<0||slot>=Definition.loop_voices||!Definition.cues[(int)cue].loop)throw new ArgumentException("Invalid loop voice.");
            var v=voices[slot];
            if(v.source.clip==null||v.cue!=cue){v.source.Stop();Configure(v,cue,position,spatial,pitch);v.source.loop=true;v.gain=0;}
            v.target=Mathf.Clamp01(gain);v.source.pitch=Mathf.Clamp(pitch,.5f,2);v.worldPosition=position;v.source.transform.position=position;
        }
        public void StopRaceVoices()
        {
            if(voices==null)return;
            for(int i=0;i<voices.Length;i++)
            {
                if(i==5)continue;var v=voices[i];v.target=v.gain=0;v.until=0;v.source.Stop();
            }
        }
        public void StopBreath()
        {
            for(int i=Definition.loop_voices;i<voices.Length;i++)if(voices[i].cue==AudioCue.Breath)
            {voices[i].source.Stop();voices[i].until=0;}
        }
        private void Configure(Voice voice,AudioCue cue,Vector3 position,bool spatial,float pitch)
        {
            int index=(int)cue;var c=Definition.cues[index];var list=clips[index];
            voice.cue=cue;voice.spatial=spatial;voice.source.clip=list[variations[index]++%list.Length];
            voice.source.outputAudioMixerGroup=groups[(int)buses[index]];voice.source.loop=false;voice.worldPosition=position;
            voice.source.spatialBlend=spatial?1:0;voice.source.minDistance=c.min_distance;voice.source.maxDistance=c.max_distance;
            voice.source.priority=c.priority;voice.source.pitch=Mathf.Clamp(pitch,.5f,2);voice.source.transform.position=position;
        }
        private float Gain(Voice voice,bool duck)
        {
            var c=Definition.cues[(int)voice.cue];int b=(int)buses[(int)voice.cue];
            float quiet=Preferences.quiet&&(voice.cue==AudioCue.Rifle||voice.cue==AudioCue.Cheer)? .55f:1;
            return voice.gain*c.gain*Preferences.master*Preferences.Gain((AudioBus)b)*Definition.mix_headroom*quiet*
                (duck&&b==(int)AudioBus.Ambience?Definition.focus_duck:1)*(!focused&&Preferences.mute_unfocused?0:1);
        }
        private void LateUpdate()
        {
            if(voices==null)return;
            if(Race!=null)Race.RenderAudio();
            Loop(5,AudioCue.Menu,menuVisible&&Race==null?1:0);
            var camera=Race!=null?Race.View:null;
            if(camera!=null){transform.SetPositionAndRotation(camera.transform.position,camera.transform.rotation);}
            ActiveVoices=0;float step=Time.unscaledDeltaTime/Definition.fade_seconds;
            for(int i=0;i<voices.Length;i++)
            {
                var v=voices[i];if(v.source.clip==null)continue;
                if(v.spatial)v.source.transform.position=v.worldPosition;
                if(i<Definition.loop_voices)
                {
                    v.gain=Mathf.MoveTowards(v.gain,v.target,step);
                    if(v.gain>Definition.silent_threshold&&!v.source.isPlaying)v.source.Play();
                    else if(v.gain<=Definition.silent_threshold&&v.source.isPlaying)v.source.Stop();
                }
                v.source.volume=Gain(v,Race!=null&&Race.ShootingFocus);
                if(v.source.isPlaying)ActiveVoices++;
            }
            PeakVoices=Math.Max(PeakVoices,ActiveVoices);
        }
        private void OnApplicationFocus(bool value)=>focused=value;
        private void OnDisable(){if(voices!=null)foreach(var v in voices)v.source.Stop();}
        private void OnDestroy(){if(Instance==this)Instance=null;}
    }
}
