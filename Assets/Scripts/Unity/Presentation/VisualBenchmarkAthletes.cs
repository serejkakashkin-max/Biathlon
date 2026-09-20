using System;
using System.IO;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Ten detached real-race poses on a repeatable visual approach. This is not a live race.
    /// Only this fixture moves its presentation roots; no authoritative state is advanced after setup.</summary>
    public sealed class VisualBenchmarkAthletes : MonoBehaviour
    {
        public const int AthleteCount=10;
        private RacePresentationSnapshot snapshot;
        private readonly AthleteAnimationPresentation[] athletes=new AthleteAnimationPresentation[AthleteCount];
        private SnowTrackPresentation tracks;private SnowSprayPresentation spray;private BreathVaporPresentation vapor;
        private double lastTime=-1;private Camera view;
        public int Count=>snapshot==null?0:AthleteCount;
        public long SourceTick=>snapshot?.Tick??-1;
        public Transform Subject=>athletes[0].transform;
        public float MaximumFootResidual {get;private set;}
        public void Initialize(Camera camera)
        {
            if(snapshot!=null||camera==null)throw new ArgumentException("Fresh fixture and camera required.");view=camera;
            string Read(string n)=>Resources.Load<TextAsset>("Biathlon/"+Path.GetFileNameWithoutExtension(n)).text;
            var runner=ChampionshipRaceFactory.Create(Read,n=>Resources.Load<TextAsset>("Biathlon/Tracks/"+Path.GetFileNameWithoutExtension(n)).text,CompetitionFormat.MassStart,new BalanceEventAudit(5),new AiDecisionCounts());
            for(int i=0;i<400;i++)runner.Step();snapshot=RacePresentationSnapshot.Capture(runner.Field);
            for(int i=0;i<AthleteCount;i++)
            {
                var pivot=new GameObject("Observed benchmark athlete");pivot.transform.SetParent(transform,false);var rig=AthleteRigFactory.Create(pivot.transform);
                athletes[i]=pivot.AddComponent<AthleteAnimationPresentation>();athletes[i].Initialize(rig);
            }
            tracks=gameObject.AddComponent<SnowTrackPresentation>();tracks.Initialize();spray=gameObject.AddComponent<SnowSprayPresentation>();spray.Initialize();vapor=gameObject.AddComponent<BreathVaporPresentation>();vapor.Initialize();
        }
        public static float Height(Vector3 p)=>p.z<-30?Mathf.Min(4,(-30-p.z)*.04f):0;
        public static float Grade(Vector3 p)=>p.z<-30&&p.z>-130?-.04f:0;
        public void SetEnvironment(BreathEnvironment environment)=>vapor.SetEnvironment(environment);
        public void Seek(double time)
        {
            ValidateTime(time);tracks.Clear();spray.Clear();vapor.Clear();lastTime=-1;
            // Rewind each pose driver before rebuilding a fixed two-second VFX history.
            RenderAt(0);tracks.Clear();spray.Clear();vapor.Clear();
            double start=Math.Max(0,time-2);for(int i=0;i<120;i++){double t=start+i/60.0;if(t>=time)break;RenderAt(t);}RenderAt(time);
        }
        private void ValidateTime(double time){if(snapshot==null||!double.IsFinite(time)||time<0||time>3600)throw new ArgumentOutOfRangeException(nameof(time));}
        public void RenderAt(double time)
        {
            ValidateTime(time);if(lastTime>time){tracks.Clear();spray.Clear();vapor.Clear();}lastTime=time;MaximumFootResidual=0;
            long tick=(long)Math.Floor(time*60)+1;double alpha=time*60-Math.Floor(time*60);float lodScale=ProjectionAwareLod.Scale(view);
            for(int i=0;i<AthleteCount;i++)
            {
                var a=athletes[i];var observed=snapshot.Get(i);float speed=(float)observed.SpeedMetersPerSecond;
                float z=-65+(float)((time*speed+(i/2)*7)%57);var p=new Vector3((i%2==0?-1:1)*1.25f,0,z);p.y=Height(p);a.transform.localPosition=p;a.transform.localRotation=Quaternion.identity;
                var frame=new AthleteAnimationFrame(observed,tick,60,alpha,0,Grade(p));float distance=Vector3.Distance(view.transform.position,a.transform.position);a.Render(frame,distance*lodScale);
                MaximumFootResidual=Mathf.Max(MaximumFootResidual,a.Rig.GetComponent<AthleteGroundContact>().MaxFootResidualMeters);
                tracks.Observe(observed.AthleteId,a.Rig,frame,distance);spray.Observe(observed.AthleteId,a.Rig,frame,distance);vapor.Observe(observed.AthleteId,a.Rig,frame,distance);
            }
            tracks.Render(time,view.transform.position);spray.Render(time,view);vapor.Render(time,view);
        }
    }
}
