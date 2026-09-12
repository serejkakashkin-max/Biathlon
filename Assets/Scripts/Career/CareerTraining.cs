using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    public enum CareerTrainingKind { Endurance, Intensity, Technique, Prone, Standing, Recovery }
    [DataContract] public sealed class CareerTrainingSession
    {
        private CareerTrainingSession() { }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="kind",IsRequired=true)] public CareerTrainingKind Kind { get; private set; }
        [DataMember(Name="time_slots",IsRequired=true)] public int TimeSlots { get; private set; }
        [DataMember(Name="fatigue_cost",IsRequired=true)] public int FatigueCost { get; private set; }
        [DataMember(Name="recovery_need",IsRequired=true)] public int RecoveryNeed { get; private set; }
        [DataMember(Name="recovery_gain",IsRequired=true)] public int RecoveryGain { get; private set; }
        [DataMember(Name="health_risk",IsRequired=true)] public int HealthRisk { get; private set; }
        [DataMember(Name="adaptation",IsRequired=true)] private int[] adaptation { get; set; }
        public int Gain(CareerStat stat){CareerUnits.Range((int)stat,0,CareerUnits.StatCount-1,"stat");return adaptation[(int)stat];}
        internal void Validate()
        {
            CareerUnits.Id(Id);CareerUnits.Range((int)Kind,0,5,"training kind");CareerUnits.Range(TimeSlots,1,14,"training time");
            foreach(int value in new[]{FatigueCost,RecoveryNeed,RecoveryGain,HealthRisk})CareerUnits.Range(value,0,CareerUnits.Basis,"training cost");
            if(adaptation==null||adaptation.Length!=CareerUnits.StatCount)throw new SerializationException("All training adaptations must be explicit.");
            foreach(int gain in adaptation)CareerUnits.Range(gain,0,CareerUnits.StatScale,"adaptation gain");
            if(Kind!=CareerTrainingKind.Recovery&&FatigueCost==0)throw new SerializationException("Training must have a fatigue cost.");
        }
    }
    [DataContract] public sealed class CareerTrainingDefinition
    {
        private CareerTrainingDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="sessions",IsRequired=true)] private CareerTrainingSession[] sessions { get; set; }
        public CareerTrainingSession Get(CareerTrainingKind kind){CareerUnits.Range((int)kind,0,5,"training kind");foreach(var s in sessions)if(s.Kind==kind)return s;throw new InvalidOperationException("Missing training kind.");}
        public string ToJson()=>CareerJson.Write(this);
        public static CareerTrainingDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerTrainingDefinition>(json);if(d==null||d.SchemaVersion!=1||d.sessions==null||d.sessions.Length!=6)throw new SerializationException("Six training kinds required.");CareerUnits.Id(d.Id);
            var ids=new HashSet<string>(StringComparer.Ordinal);var kinds=new HashSet<CareerTrainingKind>();
            foreach(var s in d.sessions){if(s==null)throw new SerializationException("Null session.");s.Validate();if(!ids.Add(s.Id)||!kinds.Add(s.Kind))throw new SerializationException("Duplicate training session.");}return d;
        }
    }
    public sealed class CareerWeekPlan
    {
        private readonly CareerTrainingKind[] sessions;
        public int Count=>sessions.Length;public CareerTrainingKind Get(int index)=>sessions[index];public int UsedSlots { get; }
        public CareerWeekPlan(CareerTrainingDefinition definition,CareerCalendar calendar,int week,IReadOnlyList<CareerTrainingKind> requested)
        {
            if(definition==null||calendar==null||requested==null)throw new ArgumentNullException();
            int available=calendar.TrainingSlots(week);if(requested.Count>available)throw new ArgumentException("Weekly time budget exceeded.");
            sessions=new CareerTrainingKind[requested.Count];int used=0;
            for(int i=0;i<requested.Count;i++){sessions[i]=requested[i];used=checked(used+definition.Get(sessions[i]).TimeSlots);}
            if(used>available)throw new ArgumentException("Weekly time budget exceeded.");UsedSlots=used;
        }
    }
}
