using System;
using System.IO;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    [DataContract]
    public sealed class MassStartResultRow
    {
        private MassStartResultRow() { }
        [DataMember(Name="athlete_id", IsRequired=true)] public string AthleteId { get; private set; }
        [DataMember(Name="rank", IsRequired=true)] public int Rank { get; private set; }
        [DataMember(Name="start_tick", IsRequired=true)] public long StartTick { get; private set; }
        [DataMember(Name="finish_whole_ticks", IsRequired=true)] public long FinishWholeTicks { get; private set; }
        [DataMember(Name="finish_fraction_parts", IsRequired=true)] public int FinishFractionParts { get; private set; }
        public FinishTime GlobalFinish(int frequency) => new FinishTime(FinishWholeTicks, FinishFractionParts, frequency);
        internal static MassStartResultRow Create(CompetitionLifecycle race) => new MassStartResultRow {
            AthleteId=race.AthleteId.Value, StartTick=race.StartTick.Value,
            FinishWholeTicks=race.GlobalFinishTime.WholeTicks, FinishFractionParts=race.GlobalFinishTime.FractionParts };
        internal MassStartResultRow Ranked(int rank) => new MassStartResultRow { AthleteId=AthleteId, Rank=rank,
            StartTick=StartTick, FinishWholeTicks=FinishWholeTicks, FinishFractionParts=FinishFractionParts };
    }

    /// <summary>Complete finisher field, ranked by exact global crossing time. Registered Sprint
    /// elapsed times and display precision never determine Mass Start arrival order.</summary>
    [DataContract]
    public sealed class MassStartResultSnapshot
    {
        private MassStartResultSnapshot() { }
        [DataMember(Name="schema_version", IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id", IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="grid_context_json", IsRequired=true)] public string GridContextJson { get; private set; }
        [DataMember(Name="tick_frequency", IsRequired=true)] public int TickFrequency { get; private set; }
        [DataMember(Name="first_start_tick", IsRequired=true)] public long FirstStartTick { get; private set; }
        [DataMember(Name="rows", IsRequired=true)] private MassStartResultRow[] rows;
        public int Count => rows.Length;
        public MassStartResultRow Get(int index) => rows[index];

        public static MassStartResultSnapshot Create(string id, MassStartGrid qualification,
            int frequency, SimulationTick firstStart, ReadOnlySpan<CompetitionLifecycle> races)
        {
            if (qualification==null || races.Length!=qualification.Count) throw new ArgumentException("Complete qualifying field required.");
            var rows=new MassStartResultRow[races.Length];
            for(int i=0;i<races.Length;i++)
            {
                var race=races[i];
                if(race==null || race.CompetitionProfileJson!=qualification.ProfileJson || race.TickFrequency!=frequency ||
                    (race.Phase!=CompetitionPhase.Finished && race.Phase!=CompetitionPhase.Classified))
                    throw new ArgumentException("Complete compatible Mass Start finisher required.");
                rows[i]=MassStartResultRow.Create(race);
            }
            return Canonical(id,qualification,frequency,firstStart,rows);
        }
        private sealed class SilentEvents : IRaceEventSink { public void Record(RaceEvent value) { } }
        private static MassStartResultSnapshot Canonical(string id, MassStartGrid q, int frequency,
            SimulationTick firstStart, MassStartResultRow[] input)
        {
            if(string.IsNullOrWhiteSpace(id) || input==null || input.Length!=q.Count) throw new ArgumentException("Valid result identity and complete rows required.");
            var schedule=new SimultaneousStartScheduler(q,frequency,firstStart,new SilentEvents());
            var seen=new bool[q.Count]; var copy=(MassStartResultRow[])input.Clone();
            for(int i=0;i<copy.Length;i++)
            {
                var row=copy[i]; int slot=-1;
                if(row==null) throw new SerializationException("Missing result row.");
                for(int j=0;j<q.Count;j++) if(q.Get(j).AthleteId.Value==row.AthleteId) {slot=j;break;}
                if(slot<0 || seen[slot] || row.StartTick!=schedule.Get(slot).Tick.Value ||
                    row.GlobalFinish(frequency).CompareTo(new FinishTime(row.StartTick,0,frequency))<=0)
                    throw new SerializationException("Wrong, duplicate or inconsistent finisher.");
                seen[slot]=true;
            }
            Array.Sort(copy,(a,b)=> {int c=a.GlobalFinish(frequency).CompareTo(b.GlobalFinish(frequency));return c!=0?c:StringComparer.Ordinal.Compare(a.AthleteId,b.AthleteId);});
            int rank=0;
            for(int i=0;i<copy.Length;i++)
            { if(i==0 || copy[i].GlobalFinish(frequency).CompareTo(copy[i-1].GlobalFinish(frequency))!=0) rank=i+1; copy[i]=copy[i].Ranked(rank); }
            return new MassStartResultSnapshot {SchemaVersion=1,Id=id,GridContextJson=q.ContextJson,TickFrequency=frequency,FirstStartTick=firstStart.Value,rows=copy};
        }
        public string ToJson()
        {
            using(var stream=new MemoryStream()) {new DataContractJsonSerializer(typeof(MassStartResultSnapshot)).WriteObject(stream,this);return Encoding.UTF8.GetString(stream.ToArray());}
        }
        public static MassStartResultSnapshot FromJson(string json, MassStartGrid expectedQualification,
            int expectedFrequency, SimulationTick expectedFirstStart)
        {
            if(expectedQualification==null) throw new ArgumentNullException(nameof(expectedQualification));
            MassStartResultSnapshot value;
            using(var stream=new MemoryStream(Encoding.UTF8.GetBytes(json??throw new ArgumentNullException(nameof(json)))))
                value=(MassStartResultSnapshot)new DataContractJsonSerializer(typeof(MassStartResultSnapshot)).ReadObject(stream);
            if(value==null || value.SchemaVersion!=1 || value.GridContextJson!=expectedQualification.ContextJson ||
                value.TickFrequency!=expectedFrequency || value.FirstStartTick!=expectedFirstStart.Value)
                throw new SerializationException("Wrong result schema or caller-owned competition context.");
            var canonical=Canonical(value.Id,expectedQualification,expectedFrequency,expectedFirstStart,value.rows);
            for(int i=0;i<canonical.Count;i++)
                if(value.rows[i].AthleteId!=canonical.rows[i].AthleteId || value.rows[i].Rank!=canonical.rows[i].Rank)
                    throw new SerializationException("Altered result order or rank.");
            return canonical;
        }
    }
}
