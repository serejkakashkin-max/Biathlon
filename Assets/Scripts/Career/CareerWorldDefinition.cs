using System;
using System.Runtime.Serialization;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerWorldCircuitDefinition
    {
        [DataMember(Name="circuit",IsRequired=true)] public CareerCircuit Circuit {get;private set;}
        [DataMember(Name="start_profile_id",IsRequired=true)] public string StartProfileId {get;private set;}
        [DataMember(Name="calendar_resource",IsRequired=true)] public string CalendarResource {get;private set;}
    }
    [DataContract] public sealed class CareerWorldDefinition
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="minimum_population",IsRequired=true)] public int MinimumPopulation {get;private set;}
        [DataMember(Name="maximum_population",IsRequired=true)] public int MaximumPopulation {get;private set;}
        [DataMember(Name="retired_history_limit",IsRequired=true)] public int RetiredHistoryLimit {get;private set;}
        [DataMember(Name="annual_prospects",IsRequired=true)] public int AnnualProspects {get;private set;}
        [DataMember(Name="junior_maximum_age",IsRequired=true)] public int JuniorMaximumAge {get;private set;}
        [DataMember(Name="world_cup_admission_ovr",IsRequired=true)] public int WorldCupAdmissionOverall {get;private set;}
        [DataMember(Name="world_cup_retention_ovr",IsRequired=true)] public int WorldCupRetentionOverall {get;private set;}
        [DataMember(Name="incumbent_selection_bonus",IsRequired=true)] public int IncumbentSelectionBonus {get;private set;}
        [DataMember(Name="aggregate_race_minutes",IsRequired=true)] public int AggregateRaceMinutes {get;private set;}
        [DataMember(Name="substream_salt",IsRequired=true)] public ulong SubstreamSalt {get;private set;}
        [DataMember(Name="substream_stride",IsRequired=true)] public ulong SubstreamStride {get;private set;}
        [DataMember(Name="circuits",IsRequired=true)] private CareerWorldCircuitDefinition[] circuits {get;set;}
        public CareerWorldCircuitDefinition Circuit(CareerCircuit circuit){foreach(var c in circuits)if(c.Circuit==circuit)return c;throw new ArgumentException("Unknown world circuit.");}
        public string ToJson()=>CareerJson.Write(this);
        public static CareerWorldDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerWorldDefinition>(json);if(d==null||d.SchemaVersion!=1||d.circuits==null||d.circuits.Length!=3)throw new SerializationException("Three world circuits required.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.MinimumPopulation,90,180,"minimum world population");CareerUnits.Range(d.MaximumPopulation,d.MinimumPopulation,240,"maximum world population");CareerUnits.Range(d.RetiredHistoryLimit,30,500,"compact athlete history");
            CareerUnits.Range(d.AnnualProspects,1,30,"annual prospects");CareerUnits.Range(d.JuniorMaximumAge,18,23,"junior maximum age");
            CareerUnits.Range(d.WorldCupAdmissionOverall,1,99,"World Cup ability admission");CareerUnits.Range(d.WorldCupRetentionOverall,1,d.WorldCupAdmissionOverall,"World Cup retention");
            CareerUnits.Range(d.IncumbentSelectionBonus,0,10,"incumbent selection");CareerUnits.Range(d.AggregateRaceMinutes,1,180,"aggregate race workload");
            if(d.SubstreamSalt==0||d.SubstreamStride==0)throw new SerializationException("Independent world substream required.");
            var seen=new bool[4];foreach(var c in d.circuits)
            {if(c==null)throw new SerializationException("Null world circuit.");CareerUnits.Range((int)c.Circuit,1,3,"world circuit");if(seen[(int)c.Circuit])throw new SerializationException("Duplicate world circuit.");seen[(int)c.Circuit]=true;CareerUnits.Id(c.StartProfileId);if(string.IsNullOrWhiteSpace(c.CalendarResource)||!c.CalendarResource.EndsWith(".json",StringComparison.Ordinal)||c.CalendarResource.IndexOfAny(new[]{'/','\\'})>=0)throw new SerializationException("Unsafe world calendar.");}
            return d;
        }
    }
}
