using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    [DataContract] public sealed class RangeBroadcastDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)]public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)]public string Id {get;private set;}
        [DataMember(Name="before_m",IsRequired=true)]public double BeforeMeters {get;private set;}
        [DataMember(Name="after_m",IsRequired=true)]public double AfterMeters {get;private set;}
        [DataMember(Name="miss_flash_seconds",IsRequired=true)]public double MissFlashSeconds {get;private set;}
        [DataMember(Name="hit_flash_seconds",IsRequired=true)]public double HitFlashSeconds {get;private set;}
        [DataMember(Name="completed_hold_seconds",IsRequired=true)]public double CompletedHoldSeconds {get;private set;}
        [DataMember(Name="completion_flash_seconds",IsRequired=true)]public double CompletionFlashSeconds {get;private set;}
        [DataMember(Name="fade_seconds",IsRequired=true)]public double FadeSeconds {get;private set;}
        [DataMember(Name="enter_seconds",IsRequired=true)]public double EnterSeconds {get;private set;}
        [DataMember(Name="maximum_rows",IsRequired=true)]public int MaximumRows {get;private set;}
        [DataMember(Name="maximum_athletes",IsRequired=true)]public int MaximumAthletes {get;private set;}
        [DataMember(Name="panel_width",IsRequired=true)]public int PanelWidth {get;private set;}
        [DataMember(Name="row_height",IsRequired=true)]public int RowHeight {get;private set;}
        [DataMember(Name="active_row_style",IsRequired=true)]public string ActiveRowStyle {get;private set;}
        [DataMember(Name="player_marker_enabled",IsRequired=true)]public bool PlayerMarkerEnabled {get;private set;}
        public static RangeBroadcastDefinition FromJson(string json)
        {
            RangeBroadcastDefinition d;
            using(var s=new MemoryStream(Encoding.UTF8.GetBytes(json)))d=(RangeBroadcastDefinition)new DataContractJsonSerializer(typeof(RangeBroadcastDefinition)).ReadObject(s);
            if(d==null||d.SchemaVersion!=2||string.IsNullOrWhiteSpace(d.Id)||
                !In(d.BeforeMeters,0,500)||!In(d.AfterMeters,0,500)||!In(d.MissFlashSeconds,.2,2)||!In(d.HitFlashSeconds,.1,1)||
                !In(d.CompletedHoldSeconds,1,10)||!In(d.CompletionFlashSeconds,.2,2)||!In(d.FadeSeconds,.3,.45)||!In(d.EnterSeconds,.1,.6)||
                d.MaximumAthletes<1||d.MaximumAthletes>60||d.MaximumRows<1||d.MaximumRows>d.MaximumAthletes||d.MaximumRows>30||
                d.PanelWidth<310||d.PanelWidth>450||d.RowHeight<24||d.RowHeight>36||d.ActiveRowStyle!="frostline_rail")
                throw new SerializationException("Invalid live shooting broadcast definition.");
            return d;
        }
        private static bool In(double n,double low,double high)=>double.IsFinite(n)&&n>=low&&n<=high;
    }
    public enum RangeShotMark { Pending=0, Hit=1, Miss=2, Unobserved=3 }
    public enum RangeRowPhase { Empty=0, Shooting=1, Retained=2, Fading=3, Departed=4 }
    /// <summary>Accepted shooting evidence. ClosedMask is physical target identity, not shot order.</summary>
    public readonly struct RangeBroadcastRow
    {
        public string AthleteId {get;} public int Bout {get;} public int Lane {get;}
        public int Shots {get;} public int ClosedMask {get;} public int Misses=>Shots-Hits(ClosedMask);
        public bool Active {get;} public long ArrivalTick {get;} public long CompletedTick {get;}
        public long AdmissionTick {get;} public long LastMissTick {get;} public long LastHitTick {get;}
        public int LastShot {get;} public int ShotMarks {get;} public bool Eligible {get;} public bool CompletionObserved {get;}
        public RangeBroadcastRow(string id,int bout,int lane,int shots,int mask,bool active,long lastMissTick=-1,
            long arrivalTick=-1,long completedTick=-1,long admissionTick=-1,long lastHitTick=-1,int lastShot=-1,int shotMarks=0,bool eligible=true,bool completionObserved=true)
        {
            if(string.IsNullOrWhiteSpace(id)||bout<0||lane<0||shots<0||shots>5||mask<0||mask>31||Hits(mask)>shots)throw new ArgumentException("Invalid accepted shooting row.");
            AthleteId=id;Bout=bout;Lane=lane;Shots=shots;ClosedMask=mask;Active=active;LastMissTick=lastMissTick;ArrivalTick=arrivalTick;
            CompletedTick=completedTick;AdmissionTick=admissionTick;LastHitTick=lastHitTick;LastShot=lastShot;ShotMarks=shotMarks;Eligible=eligible;CompletionObserved=completionObserved;
        }
        public RangeShotMark Shot(int index){if(index<0||index>=5)throw new ArgumentOutOfRangeException(nameof(index));return (RangeShotMark)((ShotMarks>>(index*2))&3);}
        public static int Hits(int mask){int hits=0;for(int i=0;i<5;i++)if((mask&(1<<i))!=0)hits++;return hits;}
        public bool Flashing(long tick,int frequency,double seconds)=>LastMissTick>=0&&tick>=LastMissTick&&tick-LastMissTick<frequency*seconds;
        public long ResultStartTick=>Math.Max(CompletedTick,AdmissionTick);
        public RangeRowPhase Phase(long tick,int frequency,RangeBroadcastDefinition d)
        {
            if(!Eligible||Bout==0)return RangeRowPhase.Empty;
            if(CompletedTick<0)return RangeRowPhase.Shooting;
            if(AdmissionTick<0)return RangeRowPhase.Retained;
            double age=(tick-ResultStartTick)/(double)frequency;
            return age<d.CompletedHoldSeconds?RangeRowPhase.Retained:age<d.CompletedHoldSeconds+d.FadeSeconds?RangeRowPhase.Fading:RangeRowPhase.Departed;
        }
        public double Opacity(double tick,int frequency,RangeBroadcastDefinition d)
        {
            if(AdmissionTick<0)return 0;
            double entering=Math.Max(0,Math.Min(1,(tick-AdmissionTick)/(frequency*d.EnterSeconds)));
            double leaving=CompletedTick<0?1:Math.Max(0,Math.Min(1,1-((tick-ResultStartTick)/frequency-d.CompletedHoldSeconds)/d.FadeSeconds));
            return Math.Min(entering,leaving);
        }
        internal RangeBroadcastRow Admit(long tick)=>new RangeBroadcastRow(AthleteId,Bout,Lane,Shots,ClosedMask,Active,LastMissTick,ArrivalTick,CompletedTick,tick,LastHitTick,LastShot,ShotMarks,Eligible,CompletionObserved);
    }
    /// <summary>Detached copy of accepted snapshot fields; also used by strict observer fixtures.</summary>
    public readonly struct RangeBroadcastObservation
    {
        public readonly string AthleteId;public readonly int CompletedBouts,Lane,Shots,Mask,LastCompletedMask;public readonly bool Active;
        public RangeBroadcastObservation(string id,int completed,int lane,int shots,int mask,int lastMask,bool active)
        {AthleteId=id;CompletedBouts=completed;Lane=lane;Shots=shots;Mask=mask;LastCompletedMask=lastMask;Active=active;}
    }
    /// <summary>Fixed-capacity accepted-tick observer. Stable per-visit queue, no player pin or pages.</summary>
    public sealed class RangeBroadcastModel
    {
        public RangeBroadcastDefinition Definition {get;}
        private readonly double[] entries,exits;
        private readonly RangeBroadcastRow[] rows;
        private readonly RangeBroadcastObservation[] observations;
        private readonly int[] latest,order,displayed,capacity;
        private long previousTick=-1;private string profileId;private int frequency;
        private readonly string expectedProfile,expectedTrack;
        public int Count=>observations.Length;
        public long Tick=>previousTick;
        public int Frequency=>frequency;
        public int VisibleCount {get;private set;}
        public int OverflowCount {get;private set;}
        public int ActiveCount {get;private set;}
        public RangeBroadcastRow Get(int i)=>latest[i]<0?default:rows[latest[i]];
        public RangeBroadcastRow Get(int athlete,int boutIndex)=>rows[boutIndex*Count+athlete];
        public int Displayed(int slot)=>displayed[slot];
        public RangeBroadcastModel(RangeBroadcastDefinition definition,TrackDefinition track,RaceRoute route,CompetitionProfile profile,int count)
        {
            Definition=definition??throw new ArgumentNullException(nameof(definition));
            if(track==null||route==null||profile==null||count<1||count>definition.MaximumAthletes||route.TrackId!=track.Id||route.CompetitionProfileId!=profile.Id)throw new ArgumentException("Matching bounded route, profile and roster required.");
            expectedTrack=track.Id;expectedProfile=profile.Id;
            entries=new double[profile.BoutCount];exits=new double[profile.BoutCount];rows=new RangeBroadcastRow[count*profile.BoutCount];
            observations=new RangeBroadcastObservation[count];latest=new int[count];order=new int[count];displayed=new int[count];capacity=new int[profile.BoutCount];Reset();
            for(int bout=0;bout<entries.Length;bout++)
            {
                bool found=false;int lap=profile.GetBout(bout).AfterLap-1;
                for(int i=0;i<route.SectionCount;i++)
                {
                    var s=route.GetSection(i);if(s.LapIndex!=lap||!s.HasRange||s.LoopId!=track.Range.Entry.LoopId)continue;
                    entries[bout]=s.RouteDistanceOf(track.Range.Entry.Distance).Meters;exits[bout]=s.RouteDistanceOf(track.Range.Exit.Distance).Meters;found=true;break;
                }
                if(!found)throw new ArgumentException("Shooting occurrence is absent from route.");
            }
        }
        public void Reset()
        {Array.Clear(rows,0,rows.Length);for(int i=0;i<latest.Length;i++)latest[i]=-1;Array.Clear(capacity,0,capacity.Length);previousTick=-1;frequency=0;profileId=null;VisibleCount=OverflowCount=ActiveCount=0;}
        public int Window(double routeMeters,bool onCourse,CompetitionPhase phase)
        {
            if(!onCourse||phase==CompetitionPhase.Prestart||phase>=CompetitionPhase.Finished||!double.IsFinite(routeMeters))return -1;
            for(int bout=0;bout<entries.Length;bout++)if(routeMeters>=Math.Max(0,entries[bout]-Definition.BeforeMeters)&&routeMeters<=exits[bout]+Definition.AfterMeters)return bout;
            return -1;
        }
        public void Observe(RacePresentationSnapshot snapshot)
        {
            if(snapshot==null||snapshot.Count!=Count||snapshot.TrackId!=expectedTrack||snapshot.ProfileId!=expectedProfile)throw new ArgumentException("Stable bound race/roster required.");
            for(int i=0;i<Count;i++)
            {
                var a=snapshot.Get(i);bool active=a.Phase==CompetitionPhase.Range&&a.ShootingActive&&a.ShootingPhase!=ShootingPhase.Outside&&a.ShootingPhase!=ShootingPhase.Complete;
                observations[i]=new RangeBroadcastObservation(a.AthleteId,a.CompletedBouts,a.RangeLane,a.ShotsFired,a.ClosedTargetsMask,a.LastCompletedBoutClosedMask,active);
            }
            ObserveAccepted(snapshot.Tick,snapshot.TickFrequency,snapshot.ProfileId,observations);
        }
        public void ObserveAccepted(long tick,int tickFrequency,string raceProfile,ReadOnlySpan<RangeBroadcastObservation> accepted)
        {
            if(tick<0||tickFrequency<1||accepted.Length!=Count||raceProfile!=expectedProfile)throw new ArgumentException("Accepted bound tick/roster required.");
            if(tick==previousTick&&raceProfile==profileId&&tickFrequency==frequency)return;
            bool continuous=previousTick>=0&&tick==previousTick+1&&raceProfile==profileId&&tickFrequency==frequency;
            if(!continuous)Reset();
            for(int i=0;i<Count;i++)
            {
                var a=accepted[i];int bout=a.Active?a.CompletedBouts+1:a.CompletedBouts;
                if(bout<0||bout>entries.Length||string.IsNullOrWhiteSpace(a.AthleteId))throw new ArgumentException("Accepted visit/athlete required.");
                if(bout==0)continue;
                int at=(bout-1)*Count+i;var old=rows[at];bool same=old.AthleteId==a.AthleteId&&old.Bout==bout;
                int shots=a.Active?a.Shots:5,mask=a.Active?a.Mask:a.LastCompletedMask;
                bool complete=shots==5||!a.Active;
                long arrival=same?old.ArrivalTick:tick,done=same?old.CompletedTick:-1,admit=same?old.AdmissionTick:-1;
                long miss=continuous&&same?old.LastMissTick:-1,hit=continuous&&same?old.LastHitTick:-1;int lastShot=same?old.LastShot:-1,marks=same?old.ShotMarks:0;
                int prior=same?old.Shots:0,priorHits=same?RangeBroadcastRow.Hits(old.ClosedMask):0;
                int delta=shots-prior,hitDelta=RangeBroadcastRow.Hits(mask)-priorHits;
                if(delta<0||hitDelta<0||hitDelta>delta)throw new ArgumentException("Accepted series regressed without a tick reset.");
                for(int shot=prior;shot<shots;shot++)
                {
                    var mark=hitDelta==delta?RangeShotMark.Hit:hitDelta==0?RangeShotMark.Miss:RangeShotMark.Unobserved;
                    marks|=(int)mark<<(shot*2);
                }
                if(continuous&&same&&delta>0){lastShot=shots-1;if(hitDelta>0)hit=tick;if(delta>hitDelta)miss=tick;}
                bool completionObserved=same?old.CompletionObserved:false;
                if(complete&&done<0){done=tick;completionObserved=continuous&&same;}
                bool eligible=same?old.Eligible:a.Active||continuous;
                int lane=a.Lane>0?a.Lane:same?old.Lane:0;
                rows[at]=new RangeBroadcastRow(a.AthleteId,bout,lane,shots,mask,a.Active&&!complete,miss,arrival,done,admit,hit,lastShot,marks,eligible,completionObserved);latest[i]=at;
            }
            previousTick=tick;frequency=tickFrequency;profileId=raceProfile;
            for(int bout=0;bout<capacity.Length;bout++)if(capacity[bout]>0)Compose(bout,capacity[bout]);
        }
        public int Compose(int boutIndex,int requestedCapacity)
        {
            if(boutIndex<0||boutIndex>=entries.Length||requestedCapacity<1||requestedCapacity>Definition.MaximumRows)throw new ArgumentOutOfRangeException();
            capacity[boutIndex]=requestedCapacity;int total=0;ActiveCount=0;
            for(int i=0;i<Count;i++)
            {
                var r=Get(i,boutIndex);if(r.AthleteId==null||!r.Eligible)continue;
                if(r.Active)ActiveCount++;
                if(r.Phase(previousTick,frequency,Definition)==RangeRowPhase.Departed)continue;
                int at=total;while(at>0&&Before(i,order[at-1])){order[at]=order[at-1];at--;}order[at]=i;total++;
            }
            VisibleCount=Math.Min(requestedCapacity,total);OverflowCount=total-VisibleCount;
            for(int i=0;i<VisibleCount;i++)
            {int athlete=order[i];displayed[i]=athlete;int at=boutIndex*Count+athlete;if(rows[at].AdmissionTick<0)rows[at]=rows[at].Admit(previousTick);}
            return VisibleCount;
            bool Before(int a,int b)
            {
                var x=Get(a,boutIndex);var y=Get(b,boutIndex);
                if((x.AdmissionTick>=0)!=(y.AdmissionTick>=0))return x.AdmissionTick>=0;
                if(x.ArrivalTick!=y.ArrivalTick)return x.ArrivalTick<y.ArrivalTick;
                if(x.Lane!=y.Lane)return x.Lane<y.Lane;
                return string.CompareOrdinal(x.AthleteId,y.AthleteId)<0;
            }
        }
    }
}
