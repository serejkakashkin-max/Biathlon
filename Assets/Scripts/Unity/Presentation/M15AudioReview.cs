using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Opt-in native mixer audition; never created during normal gameplay.</summary>
    public sealed class M15AudioReview:MonoBehaviour
    {
        [Serializable]private sealed class Receipt
        {
            public int schema_version=1;public string id="m15_native_audio_review",unity;
            public bool success;public int source_count,peak_voices,played,dropped,captured_samples,sample_rate,channels;
            public long pcm_bytes;public float peak,rms;public string[] errors;
        }
        private readonly List<string> errors=new List<string>();
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]private static void Boot()
        {
            if(Application.isEditor||!Debug.isDebugBuild||Array.IndexOf(Environment.GetCommandLineArgs(),"-biathlonAudioReview")<0)return;
            new GameObject("Explicit M15 audio review").AddComponent<M15AudioReview>();
        }
        private void Log(string text,string trace,LogType type){if(type==LogType.Error||type==LogType.Exception||type==LogType.Assert)errors.Add(text);}
        private IEnumerator Start()
        {
            Application.runInBackground=true;Application.logMessageReceived+=Log;yield return null;
            var a=BiathlonAudio.Ensure(false);var old=a.Preferences;
            a.SetPreferences(new BiathlonAudioPreferences{mute_unfocused=false},false);a.SetMenu(false);
            var capture=a.gameObject.AddComponent<M15AudioCapture>();capture.Begin(32);
            a.SetMenu(true);yield return new WaitForSecondsRealtime(8);a.SetMenu(false);
            a.Loop(1,AudioCue.Wind,.5f);a.Loop(3,AudioCue.Crowd,.5f);a.Loop(0,AudioCue.SkiPacked,.9f);
            a.Play(AudioCue.Pole);yield return new WaitForSecondsRealtime(1);
            a.Play(AudioCue.Pole);yield return new WaitForSecondsRealtime(1);
            a.Loop(0,AudioCue.SkiSoft,.9f);a.Loop(2,AudioCue.Snowfall,.6f);yield return new WaitForSecondsRealtime(2);
            a.Loop(0,AudioCue.SkiIce,.7f);yield return new WaitForSecondsRealtime(2);
            a.Loop(0,AudioCue.SkiIce,0);a.Play(AudioCue.Breath);a.Play(AudioCue.Heart);yield return new WaitForSecondsRealtime(1);
            a.Play(AudioCue.Bolt);yield return new WaitForSecondsRealtime(.6f);
            a.Play(AudioCue.Rifle,local:true);a.Play(AudioCue.TargetHit,delay:.14);yield return new WaitForSecondsRealtime(2);
            a.Play(AudioCue.Bolt);yield return new WaitForSecondsRealtime(.6f);
            a.Play(AudioCue.Rifle,local:true);a.Play(AudioCue.TargetMiss,delay:.14);yield return new WaitForSecondsRealtime(2);
            a.Play(AudioCue.Cheer);yield return new WaitForSecondsRealtime(3);
            capture.End();yield return null;
            string[] args=Environment.GetCommandLineArgs();int output=Array.IndexOf(args,"-biathlonAudioReviewOutput");
            string folder=Path.GetFullPath(output>=0&&output+1<args.Length?args[output+1]:Path.Combine(Application.dataPath,"../AudioReview"));Directory.CreateDirectory(folder);
            var receipt=new Receipt{unity=Application.unityVersion,source_count=a.SourceCount,peak_voices=a.PeakVoices,played=a.Played,dropped=a.Dropped,pcm_bytes=a.PcmBytes,
                captured_samples=capture.Count,sample_rate=AudioSettings.outputSampleRate,channels=capture.Channels,peak=capture.Peak(),rms=capture.Rms(),errors=errors.ToArray()};
            receipt.success=errors.Count==0&&receipt.source_count==22&&receipt.peak_voices<=22&&receipt.captured_samples>10000&&receipt.peak>.01f&&receipt.peak<.98f&&receipt.rms>.001f;
            capture.Save(Path.Combine(folder,"native-mix.wav"));File.WriteAllText(Path.Combine(folder,"audio-review.json"),JsonUtility.ToJson(receipt,true));
            a.StopRaceVoices();a.SetPreferences(old,false);Application.logMessageReceived-=Log;Application.Quit(receipt.success?0:1);
        }
    }
    public sealed class M15AudioCapture:MonoBehaviour
    {
        private float[] buffer;private int count,channels;private volatile bool recording;
        public int Count=>Volatile.Read(ref count);public int Channels=>channels;
        public void Begin(int seconds){buffer=new float[AudioSettings.outputSampleRate*seconds*8];count=0;recording=true;}
        public void End()=>recording=false;
        private void OnAudioFilterRead(float[] data,int channelCount)
        {
            if(!recording||buffer==null)return;channels=channelCount;
            int offset=count,n=Math.Min(data.Length,buffer.Length-offset);if(n<=0)return;
            Array.Copy(data,0,buffer,offset,n);Volatile.Write(ref count,offset+n);
        }
        public float Peak(){float peak=0;for(int i=0;i<Count;i++)peak=Math.Max(peak,Math.Abs(buffer[i]));return peak;}
        public float Rms(){double sum=0;for(int i=0;i<Count;i++)sum+=buffer[i]*buffer[i];return Count>0?(float)Math.Sqrt(sum/Count):0;}
        public void Save(string file)
        {
            int length=Count;using(var w=new BinaryWriter(File.Create(file)))
            {
                w.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));w.Write(36+length*2);w.Write(System.Text.Encoding.ASCII.GetBytes("WAVEfmt "));
                w.Write(16);w.Write((short)1);w.Write((short)Math.Max(1,channels));w.Write(AudioSettings.outputSampleRate);w.Write(AudioSettings.outputSampleRate*Math.Max(1,channels)*2);
                w.Write((short)(Math.Max(1,channels)*2));w.Write((short)16);w.Write(System.Text.Encoding.ASCII.GetBytes("data"));w.Write(length*2);
                for(int i=0;i<length;i++)w.Write((short)(Mathf.Clamp(buffer[i],-1,1)*32767));
            }
        }
    }
}
