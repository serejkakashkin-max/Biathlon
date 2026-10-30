using System;
using Biathlon.Presentation;
using Biathlon.Simulation;
using UnityEngine;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Consumes accepted ticks. No key polling, speculative hits or simulation mutation.</summary>
    public sealed class RaceAudioPresentation:MonoBehaviour
    {
        public ProductionVenueHost Host {get;private set;}
        public Camera View=>viewOverride!=null?viewOverride:range!=null&&range.AimCamera.enabled?range.AimCamera:Host?.ChaseCamera;
        public bool ShootingFocus {get;private set;}
        public int AcceptedShots {get;private set;} public int TargetClosures {get;private set;} public int BoltCycles {get;private set;}
        public int SuppressedObservations {get;private set;}
        public int PlayerHitReactions {get;private set;} public int PlayerMissReactions {get;private set;}
        private BiathlonAudio audio;private RangePresentation range;
        private RacePresentationSnapshot previous;
        private Camera viewOverride;
        private int selected,frame=-1,events,countdown=-1;
        private bool replay,replaySuppressed,wasSuspended=true;
        private double nextBreath,nextHeart,nextPole;
        private Vector3 crowdPoint;private float crowdRadius;
        public bool Suspended=>Host==null||Host.HasSimulationFault||Host.FastFinish||Host.StartWaitFastForwarding||
            (replay?replaySuppressed:!Host.enabled);
        public void Initialize(ProductionVenueHost host)
        {
            if(Host!=null||host?.Feed==null)throw new ArgumentException("Fresh race audio and accepted feed required.");
            Host=host;selected=host.PlayerIndex;audio=BiathlonAudio.Ensure();range=host.GetComponent<RangePresentation>();
            for(int i=0;i<host.Venue.Manifest.AmbienceCount;i++)
            {
                var hook=host.Venue.Manifest.Ambience(i);if(hook.EventId!="ambience_stadium_crowd")continue;
                var p=host.Venue.Visual.Get(hook.Anchor.LoopId).Sample(hook.Anchor.Distance.Meters);
                crowdPoint=new Vector3((float)p.X,(float)p.Y,(float)p.Z);crowdRadius=(float)hook.Radius;break;
            }
            previous=host.Feed.Current;host.Feed.Published+=Observe;audio.Bind(this);
        }
        public void ReplayContext(Camera camera,int athlete,bool suppressed)
        {replay=true;viewOverride=camera;selected=athlete;replaySuppressed=suppressed;}
        public void SpectatorContext(Camera camera,int athlete){viewOverride=camera;selected=athlete;}
        public void Observe(RacePresentationSnapshot current)
        {
            if(current==null)return;
            var prior=previous;previous=current;
            if(Suspended||prior==null||current.Tick<=prior.Tick||current.Count!=prior.Count||current.ProfileId!=prior.ProfileId||current.TrackId!=prior.TrackId||current.TickFrequency!=prior.TickFrequency||
                current.Tick-prior.Tick>current.TickFrequency*audio.Definition.event_gap_seconds)
            {SuppressedObservations++;return;}
            if(frame!=Time.frameCount){frame=Time.frameCount;events=0;}
            // The actual player owns crowd feedback even when spectating/replaying a bot.
            // Only adjacent accepted ticks identify one fresh shot; seeks never replay a reaction.
            if(current.Tick==prior.Tick+1&&Host.PlayerIndex>=0&&Host.PlayerIndex<current.Count)
            {
                var a=prior.Get(Host.PlayerIndex);var b=current.Get(Host.PlayerIndex);
                var oldShot=new RangeBroadcastObservation(a.AthleteId,a.CompletedBouts,a.RangeLane,a.ShotsFired,a.ClosedTargetsMask,a.LastCompletedBoutClosedMask,a.ShootingActive);
                var newShot=new RangeBroadcastObservation(b.AthleteId,b.CompletedBouts,b.RangeLane,b.ShotsFired,b.ClosedTargetsMask,b.LastCompletedBoutClosedMask,b.ShootingActive);
                var result=AcceptedShotTransition.Result(oldShot,newShot);
                if(result==RangeShotMark.Hit||result==RangeShotMark.Miss)
                {
                    if(result==RangeShotMark.Hit)PlayerHitReactions++;else PlayerMissReactions++;
                    Emit(result==RangeShotMark.Hit?AudioCue.CrowdHit:AudioCue.CrowdMiss,crowdPoint,false,1,true,audio.Definition.crowd_reaction_delay_seconds);
                }
            }
            // Player/observed athlete first, then nearby reports. Dense fields cannot starve player feedback.
            Handle(current,prior,selected);
            for(int i=0;i<current.Count&&events<audio.Definition.max_events_per_frame;i++)if(i!=selected)Handle(current,prior,i);
        }
        private void Handle(RacePresentationSnapshot current,RacePresentationSnapshot prior,int i)
        {
            if(i<0||i>=current.Count)return;
            var a=prior.Get(i);var b=current.Get(i);if(a.AthleteId!=b.AthleteId)return;
            bool local=i==selected;var proxy=Host.Proxies.GetProxy(b.AthleteId);if(proxy==null)return;
            var position=proxy.position+Vector3.up;
            if(!local&&View!=null&&(position-View.transform.position).sqrMagnitude>22500)return;
            if(local&&b.ShootingActive&&a.ShootingActive&&b.CompletedBouts==a.CompletedBouts&&b.CompletedHeartbeats>a.CompletedHeartbeats)
            {
                float strain=Mathf.InverseLerp(audio.Definition.heart_threshold,audio.Definition.heart_ceiling,(float)b.Physiology.HeartRateBpm);
                Emit(AudioCue.Heart,position,false,Mathf.Lerp(.3f,.8f,strain),true);
            }
            if(b.ShootingActive&&a.ShootingActive&&b.CompletedBouts==a.CompletedBouts&&b.ShotsFired>a.ShotsFired)
            {
                AcceptedShots++;bool hit=(b.ClosedTargetsMask&~a.ClosedTargetsMask)!=0;
                if(hit)TargetClosures++;
                Emit(AudioCue.Rifle,position,!local,1,local);
                var forward=Host.Venue.Manifest.Range.Forward;
                var target=position+new Vector3((float)forward.X,0,(float)forward.Z)*50;
                Emit(hit?AudioCue.TargetHit:AudioCue.TargetMiss,target,true,1,local,.14);
            }
            if(b.ShootingPhase==ShootingPhase.BoltCycle&&a.ShootingPhase!=ShootingPhase.BoltCycle)
            {BoltCycles++;Emit(AudioCue.Bolt,position,!local,local?1:.35f,local);}
            if(i==Host.PlayerIndex&&b.Phase==CompetitionPhase.Finished&&a.Phase!=CompetitionPhase.Finished)
                Emit(AudioCue.Cheer,crowdPoint,true,1,true);
        }
        private void Emit(AudioCue cue,Vector3 position,bool spatial,float gain,bool local,double delay=0)
        {if(events>=audio.Definition.max_events_per_frame){return;}events++;audio.Play(cue,position,spatial,gain,local:local,delay:delay);}
        public void RenderAudio()
        {
            var snapshot=Host?.Feed.Current;if(snapshot==null||selected>=snapshot.Count)return;
            bool paused=Suspended;
            if(paused){if(!wasSuspended)audio.StopRaceVoices();wasSuspended=true;return;}
            if(wasSuspended){previous=snapshot;nextBreath=nextHeart=nextPole=Time.unscaledTimeAsDouble+.1;wasSuspended=false;}
            var state=snapshot.Get(selected);var d=audio.Definition;double now=Time.unscaledTimeAsDouble;
            bool finished=state.Phase==CompetitionPhase.Finished;
            ShootingFocus=state.ShootingActive&&(state.ShootingPhase==ShootingPhase.Setup||state.ShootingPhase==ShootingPhase.Aiming||state.ShootingPhase==ShootingPhase.BoltCycle);
            var subject=Host.Proxies.GetProxy(state.AthleteId);var pos=subject!=null?subject.position:Vector3.zero;
            string weather=Host.Environment?.WeatherPresetId;
            float speed=(float)state.SpeedMetersPerSecond,load=Mathf.Clamp01((float)state.Effort.DeliveredRelativeEffort);
            float move=state.OnCourse&&!finished?Mathf.Clamp01(speed/d.speed_reference):0;
            string snowProfile=Host.Environment?.AudioSnowProfile;
            AudioCue snow=weather=="snowfall"?AudioCue.SkiSoft:snowProfile!=null&&(snowProfile.Contains("ice")||snowProfile.Contains("icy"))?AudioCue.SkiIce:AudioCue.SkiPacked;
            audio.Loop(0,snow,move*(.65f+.35f*load),Mathf.Lerp(.7f,1.35f,move));
            float wind=Host.Environment!=null?Host.Environment.AudioWind.magnitude:0;
            float storm=Mathf.Clamp01(.15f+wind/10+(weather=="snowfall"?.2f:0));
            audio.Loop(1,AudioCue.Wind,storm);
            audio.Loop(2,AudioCue.Snowfall,weather=="snowfall"?1:0);
            float stadium=crowdRadius>0?Mathf.Clamp01(1-Vector3.Distance(pos,crowdPoint)/crowdRadius):0;
            audio.Loop(3,AudioCue.Crowd,stadium);
            audio.Loop(4,AudioCue.Wind,move*.3f,1.3f);
            if(!replay&&Host.CountdownNumber!=countdown)
            {
                int prior=countdown;countdown=Host.CountdownNumber;
                if(countdown>0&&countdown<=3)audio.Play(AudioCue.Countdown,local:true);
                else if(countdown==0&&prior==1)audio.Play(AudioCue.Start,local:true);
            }
            bool waiting=state.StartCountdownSeconds>0||!replay&&Host.WaitingForPlayerStart;
            float hr=(float)state.Physiology.HeartRateBpm;
            float strain=Mathf.InverseLerp(d.heart_threshold,d.heart_ceiling,hr);
            if(finished||waiting)return;
            if(state.BreathHolding)audio.StopBreath();
            if(!state.BreathHolding&&now>=nextBreath)
            {
                float interval=Mathf.Lerp(d.breath_max_seconds,d.breath_min_seconds,strain);
                audio.Play(AudioCue.Breath,gain:Mathf.Lerp(.25f,1,strain),pitch:Mathf.Clamp(2.8f/interval,.7f,1.5f),local:true);nextBreath=now+interval;
            }
            if(!state.ShootingActive&&strain>0&&now>=nextHeart)
            {audio.Play(AudioCue.Heart,gain:strain*.4f,local:true);nextHeart=now+60/Math.Max(40,hr);}
            if(move>0&&!state.TuckActive&&load>.1f&&!ShootingFocus&&now>=nextPole)
            {audio.Play(AudioCue.Pole,gain:Mathf.Lerp(.35f,1,load),pitch:Mathf.Lerp(.85f,1.15f,load),local:true);nextPole=now+d.pole_distance/Math.Max(2,speed);}
        }
        private void OnDisable(){if(audio!=null)audio.Unbind(this);}
        private void OnEnable(){if(Host!=null&&audio!=null){previous=Host.Feed.Current;audio.Bind(this);}}
        private void OnDestroy(){if(Host?.Feed!=null)Host.Feed.Published-=Observe;if(audio!=null)audio.Unbind(this);}
    }
}
