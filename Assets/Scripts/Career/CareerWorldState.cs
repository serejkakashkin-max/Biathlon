using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerWorldEntry
    {
        [DataMember(Name="athlete",IsRequired=true)] public CareerAthlete Athlete {get;private set;}
        [DataMember(Name="circuit",IsRequired=true)] public CareerCircuit Circuit {get;internal set;}
        internal CareerWorldEntry(CareerAthlete athlete,CareerCircuit circuit){Athlete=athlete;Circuit=circuit;}
    }
    [DataContract] public sealed class CareerRetiredAthlete
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="name",IsRequired=true)] public string Name {get;private set;}
        [DataMember(Name="nation",IsRequired=true)] public string Nation {get;private set;}
        [DataMember(Name="age",IsRequired=true)] public int Age {get;private set;}
        [DataMember(Name="retired_season",IsRequired=true)] public int RetiredSeason {get;private set;}
        [DataMember(Name="peak_ovr",IsRequired=true)] public int PeakOverall {get;private set;}
        [DataMember(Name="peak_age",IsRequired=true)] public int PeakAge {get;private set;}
        [DataMember(Name="races",IsRequired=true)] public int Races {get;private set;}
        [DataMember(Name="wins",IsRequired=true)] public int Wins {get;private set;}
        [DataMember(Name="reason",IsRequired=true)] public string Reason {get;private set;}
        internal CareerRetiredAthlete(CareerAthlete a,int season,string reason)
        {Id=a.Id;Name=a.Name;Nation=a.Nation;Age=a.Age;RetiredSeason=season;PeakOverall=a.PeakOverall;PeakAge=a.PeakAge;Races=a.Races;Wins=a.Wins;Reason=reason;}
        internal void Validate()
        {
            CareerUnits.Id(Id);CareerUnits.Id(Nation);CareerUnits.Text(Name,100,"retired name");CareerUnits.Range(Age,1,120,"retired age");CareerUnits.Range(RetiredSeason,1,10000,"retired season");
            CareerUnits.Range(PeakOverall,0,99,"retired peak");CareerUnits.Range(PeakAge,0,120,"retired peak age");if(Races<0||Wins<0||Wins>Races||(Reason!="retired"&&Reason!="world_capacity"))throw new SerializationException("Invalid compact athlete record.");
        }
    }
    public sealed partial class CareerAthlete
    {
        [DataMember(Name="background_weeks",EmitDefaultValue=false)] public int BackgroundWeeks {get;internal set;}
    }
    public sealed partial class CareerSave
    {
        [DataMember(Name="world_policy_id",EmitDefaultValue=false)] public string WorldPolicyId {get;internal set;}
        [DataMember(Name="off_field_pool",EmitDefaultValue=false)] private CareerWorldEntry[] offField {get;set;}
        [DataMember(Name="retired_athletes",EmitDefaultValue=false)] private CareerRetiredAthlete[] retired {get;set;}
        [DataMember(Name="last_world_transition_season",EmitDefaultValue=false)] public int LastWorldTransitionSeason {get;internal set;}
        public int PoolCount=>offField?.Length??0;
        public CareerWorldEntry Pool(int index)=>offField[index];
        public int RetiredCount=>retired?.Length??0;
        public CareerRetiredAthlete Retired(int index)=>retired[index];
        public int WorldAthleteCount=>AthleteCount+PoolCount;
        public CareerAthlete WorldAthlete(int index)=>index<AthleteCount?Athlete(index):Pool(index-AthleteCount).Athlete;
        public CareerAthlete FindWorldAthlete(string id)
        {for(int i=0;i<WorldAthleteCount;i++)if(WorldAthlete(i).Id==id)return WorldAthlete(i);return null;}
        internal void SetPool(string policy,IReadOnlyList<CareerWorldEntry> rows)
        {var copy=new CareerWorldEntry[rows.Count];for(int i=0;i<rows.Count;i++)copy[i]=rows[i];WorldPolicyId=policy;offField=copy;}
        internal void AddRetired(IReadOnlyList<CareerRetiredAthlete> additions,int limit)
        {
            int total=RetiredCount+additions.Count,keep=Math.Min(limit,total);var rows=new CareerRetiredAthlete[keep];int skip=total-keep;
            for(int i=skip;i<total;i++)rows[i-skip]=i<RetiredCount?retired[i]:additions[i-RetiredCount];retired=rows;
        }
        private void ValidateOrdinal(string id)
        {
            string prefix=Id+"_athlete_";
            if(!id.StartsWith(prefix,StringComparison.Ordinal)||!long.TryParse(id.Substring(prefix.Length),NumberStyles.None,CultureInfo.InvariantCulture,out long n)||n<0||n>=NextAthleteOrdinal)throw new SerializationException("World athlete ordinal is inconsistent.");
        }
        private void ValidateWorldPool(HashSet<string> activeIds)
        {
            if(WorldPolicyId==null){if(offField!=null||retired!=null||LastWorldTransitionSeason!=0)throw new SerializationException("World metadata without policy.");return;}
            CareerUnits.Id(WorldPolicyId);if(offField==null||WorldAthleteCount>240||RetiredCount>500||NextAthleteOrdinal<WorldAthleteCount)throw new SerializationException("World population budget exceeded.");
            CareerUnits.Range(LastWorldTransitionSeason,0,Season,"world transition season");
            // Keep the caller's active set separate: current race results must still reference the active field.
            var seen=new HashSet<string>(activeIds,StringComparer.Ordinal);
            foreach(var row in offField)
            {
                if(row?.Athlete==null)throw new SerializationException("Null off-field athlete.");CareerUnits.Range((int)row.Circuit,1,3,"off-field circuit");var a=row.Athlete;a.Validate();ValidateOrdinal(a.Id);
                if(!seen.Add(a.Id)||a.Id==PlayerId||a.PeakSeason>Season||a.LastProgressionSeason>Season)throw new SerializationException("Duplicate or future off-field identity.");
                for(int j=0;j<a.DevelopmentCount;j++)if(a.Development(j).Season>Season||(a.Development(j).Season==Season&&a.Development(j).Week>Week))throw new SerializationException("Future off-field development.");
            }
            int previousSeason=0;
            for(int i=0;i<RetiredCount;i++)
            {var r=retired[i];if(r==null)throw new SerializationException("Null compact athlete.");r.Validate();ValidateOrdinal(r.Id);if(!seen.Add(r.Id)||r.RetiredSeason<previousSeason||r.RetiredSeason>Season)throw new SerializationException("Invalid compact athlete identity/order.");previousSeason=r.RetiredSeason;}
        }
    }
}
