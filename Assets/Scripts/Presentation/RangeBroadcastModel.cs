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
        [DataMember(Name="page_seconds",IsRequired=true)]public double PageSeconds {get;private set;}
        [DataMember(Name="maximum_rows",IsRequired=true)]public int MaximumRows {get;private set;}
        [DataMember(Name="panel_width",IsRequired=true)]public int PanelWidth {get;private set;}
        [DataMember(Name="row_height",IsRequired=true)]public int RowHeight {get;private set;}
        public static RangeBroadcastDefinition FromJson(string json)
        {
            RangeBroadcastDefinition d;
            using(var s=new MemoryStream(Encoding.UTF8.GetBytes(json)))d=(RangeBroadcastDefinition)new DataContractJsonSerializer(typeof(RangeBroadcastDefinition)).ReadObject(s);
            if(d==null||d.SchemaVersion!=1||string.IsNullOrWhiteSpace(d.Id)||!double.IsFinite(d.BeforeMeters)||!double.IsFinite(d.AfterMeters)||
                d.BeforeMeters<0||d.BeforeMeters>500||d.AfterMeters<0||d.AfterMeters>500||!double.IsFinite(d.MissFlashSeconds)||d.MissFlashSeconds<.2||d.MissFlashSeconds>2||
                !double.IsFinite(d.PageSeconds)||d.PageSeconds<3||d.PageSeconds>15||d.MaximumRows<1||d.MaximumRows>12||d.PanelWidth<250||d.PanelWidth>350||d.RowHeight<24||d.RowHeight>40)throw new SerializationException("Invalid range broadcast definition.");
            return d;
        }
    }
    public readonly struct RangeBroadcastRow
    {
        public string AthleteId {get;} public int Bout {get;} public int Lane {get;}
        public int Shots {get;} public int ClosedMask {get;} public int Misses {get;}
        public bool Active {get;} public long LastMissTick {get;}
        public RangeBroadcastRow(string id,int bout,int lane,int shots,int mask,bool active,long lastMissTick=-1)
        {
            if(string.IsNullOrWhiteSpace(id)||bout<0||shots<0||shots>5||mask<0||mask>31||Hits(mask)>shots)throw new ArgumentException("Invalid accepted shooting row.");
            AthleteId=id;Bout=bout;Lane=lane;Shots=shots;ClosedMask=mask;Misses=shots-Hits(mask);Active=active;LastMissTick=lastMissTick;
        }
        public static int Hits(int mask){int hits=0;for(int i=0;i<5;i++)if((mask&(1<<i))!=0)hits++;return hits;}
        public bool Flashing(long tick,int frequency,double seconds)=>LastMissTick>=0&&tick>=LastMissTick&&tick-LastMissTick<frequency*seconds;
    }
    /// <summary>Bounded accepted-state observer. Canonical route windows cross lap seams and keep penalties visible.</summary>
    public sealed class RangeBroadcastModel
    {
        public RangeBroadcastDefinition Definition {get;}
        private readonly double[] entries,exits;
        private readonly RangeBroadcastRow[] rows;
        private readonly int[] order;
        private readonly int[] visibleOrder;
        public int Page {get;private set;} public int Pages {get;private set;}
        private long previousTick=-1;private string profileId;
        public int Count=>rows.Length;
        public RangeBroadcastRow Get(int i)=>rows[i];
        public int Ordered(int i)=>order[i];
        public int Displayed(int i)=>visibleOrder[i];
        public RangeBroadcastModel(RangeBroadcastDefinition definition,TrackDefinition track,RaceRoute route,CompetitionProfile profile,int count)
        {
            Definition=definition??throw new ArgumentNullException(nameof(definition));
            if(track==null||route==null||profile==null||count<1||route.TrackId!=track.Id||route.CompetitionProfileId!=profile.Id)throw new ArgumentException("Matching route and profile required.");
            entries=new double[profile.BoutCount];exits=new double[profile.BoutCount];rows=new RangeBroadcastRow[count];order=new int[count];visibleOrder=new int[count];
            for(int bout=0;bout<entries.Length;bout++)
            {
                bool found=false;int lap=profile.GetBout(bout).AfterLap-1;
                for(int i=0;i<route.SectionCount;i++)
                {
                    var s=route.GetSection(i);if(s.LapIndex!=lap||!s.HasRange||s.LoopId!=track.Range.Entry.LoopId)continue;
                    entries[bout]=s.RouteDistanceOf(track.Range.Entry.Distance).Meters;
                    exits[bout]=s.RouteDistanceOf(track.Range.Exit.Distance).Meters;found=true;break;
                }
                if(!found)throw new ArgumentException("Shooting occurrence is absent from route.");
            }
        }
        /// <returns>Zero-based shooting visit, or -1 outside the contextual window.</returns>
        public int Window(double routeMeters,bool onCourse,CompetitionPhase phase)
        {
            if(!onCourse||phase==CompetitionPhase.Prestart||phase>=CompetitionPhase.Finished||!double.IsFinite(routeMeters))return -1;
            for(int bout=0;bout<entries.Length;bout++)if(routeMeters>=Math.Max(0,entries[bout]-Definition.BeforeMeters)&&routeMeters<=exits[bout]+Definition.AfterMeters)return bout;
            return -1;
        }
        public void Observe(RacePresentationSnapshot snapshot)
        {
            if(snapshot==null||snapshot.Count!=rows.Length)throw new ArgumentException("Stable roster required.");
            bool adjacent=previousTick>=0&&snapshot.Tick>previousTick&&snapshot.Tick-previousTick<=snapshot.TickFrequency*.5&&snapshot.ProfileId==profileId;
            if(snapshot.Tick==previousTick&&snapshot.ProfileId==profileId)return;
            for(int i=0;i<rows.Length;i++)
            {
                var a=snapshot.Get(i);var old=rows[i];
                bool active=a.ShootingActive&&a.ShootingPhase!=ShootingPhase.Outside&&a.ShootingPhase!=ShootingPhase.Complete;
                int bout=active?a.CompletedBouts+1:a.CompletedBouts;
                int shots=active?a.ShotsFired:bout>0?5:0;
                int mask=active?a.ClosedTargetsMask:bout>0?a.LastCompletedBoutClosedMask:0;
                int misses=shots-RangeBroadcastRow.Hits(mask);
                bool same=old.AthleteId==a.AthleteId&&old.Bout==bout;
                long flash=adjacent&&same?old.LastMissTick:-1;
                if(adjacent&&same&&shots>old.Shots&&misses>old.Misses)flash=snapshot.Tick;
                // Final-shot completion may release ActiveShooting in the same accepted tick.
                int lane=a.RangeLane>0?a.RangeLane:same?old.Lane:0;
                rows[i]=new RangeBroadcastRow(a.AthleteId,bout,lane,shots,mask,active,flash);
            }
            previousTick=snapshot.Tick;profileId=snapshot.ProfileId;
        }
        public int Select(int boutIndex,int player)
        {
            int count=0;
            for(int i=0;i<rows.Length;i++)if(rows[i].Bout==boutIndex+1)
            {
                int at=count;
                while(at>0&&ComesBefore(i,order[at-1],player)){order[at]=order[at-1];at--;}
                order[at]=i;count++;
            }
            return count;
        }
        public int SelectVisible(int total,int player,long tick,int frequency,int capacity)
        {
            int selected=0;
            bool Chosen(int index){for(int i=0;i<selected;i++)if(visibleOrder[i]==index)return true;return false;}
            for(int i=0;i<total;i++)if(order[i]==player){visibleOrder[selected++]=player;break;}
            for(int i=0;i<total&&selected<capacity;i++)if(!Chosen(order[i])&&rows[order[i]].Flashing(tick,frequency,Definition.MissFlashSeconds))visibleOrder[selected++]=order[i];
            int slots=capacity-selected,remaining=total-selected;
            Pages=slots>0?Math.Max(1,(remaining+slots-1)/slots):1;Page=(int)(tick/(frequency*Definition.PageSeconds))%Pages;
            int start=Page*slots,seen=0;
            for(int i=0;i<total&&selected<capacity;i++)
            {
                if(Chosen(order[i]))continue;
                if(seen++<start)continue;visibleOrder[selected++]=order[i];
            }
            return selected;
        }
        private bool ComesBefore(int a,int b,int player)
        {
            var x=rows[a];var y=rows[b];
            if(x.Active!=y.Active)return x.Active;
            if(a==player||b==player)return a==player;
            if(x.Active&&x.Lane!=y.Lane)return x.Lane<y.Lane;
            if(x.Shots!=y.Shots)return x.Shots>y.Shots;
            return string.CompareOrdinal(x.AthleteId,y.AthleteId)<0;
        }
    }
}
