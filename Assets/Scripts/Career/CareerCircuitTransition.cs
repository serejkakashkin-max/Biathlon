using System;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>League changes happen after season archival. Opponents are the new
    /// circuit's actual permanent cohort; the player's earned attributes stay intact.</summary>
    public static class CareerCircuitTransition
    {
        internal static CareerSave Advance(CareerSave source,CareerDefinitions d,Func<ulong,IRandomSource> restoreRandom)
        {
            CareerAthlete player=null;for(int i=0;i<source.AthleteCount;i++)if(source.Athlete(i).Id==source.PlayerId)player=source.Athlete(i);
            string resource=player.Tier==CareerTier.International?"woodland_world_cup_calendar_v1.json":
                d.Calendar.Circuit==CareerCircuit.Junior&&!d.Calendar.Get(0).Eligible(player)?"woodland_development_calendar_v1.json":null;
            if(resource==null)return source;var calendar=CareerCalendar.FromJson(d.ReadDefinition(resource));if(calendar.Id==source.CalendarId)return source;
            var save=CareerSave.FromJson(source.ToJson());var random=restoreRandom(save.RandomState);
            var cohort=CareerCircuitCohorts.FromJson(d.ReadDefinition("career_circuit_cohorts_v1.json")).Get(calendar.Circuit);
            for(int i=0;i<save.AthleteCount;i++)
            {
                if(save.Athlete(i).Id==save.PlayerId)continue;
                var peer=CareerFactory.Generate(d.Roster,save.Id,save.NextAthleteOrdinal++,random,age:CareerFactory.Draw(random,cohort.MinimumAge,cohort.MaximumAge));
                int level=CareerFactory.Draw(random,cohort.MinimumSkill,cohort.MaximumSkill);peer.Tier=cohort.Tier;
                for(int stat=0;stat<CareerUnits.StatCount;stat++)peer.SetStat((CareerStat)stat,Math.Min(peer.Potential,Math.Max(0,level+CareerFactory.Draw(random,-cohort.StatSpread,cohort.StatSpread))));
                save.Athletes[i]=peer;
            }
            save.CalendarId=calendar.Id;save.RandomState=random.State;save.Validate();return save;
        }
    }
    [System.Runtime.Serialization.DataContract] public sealed class CareerCircuitCohort
    {
        [System.Runtime.Serialization.DataMember(Name="circuit",IsRequired=true)] public CareerCircuit Circuit {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="minimum_skill",IsRequired=true)] public int MinimumSkill {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="maximum_skill",IsRequired=true)] public int MaximumSkill {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="stat_spread",IsRequired=true)] public int StatSpread {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="minimum_age",IsRequired=true)] public int MinimumAge {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="maximum_age",IsRequired=true)] public int MaximumAge {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="tier",IsRequired=true)] public CareerTier Tier {get;private set;}
        internal void Validate(){CareerUnits.Range((int)Circuit,1,3,"cohort circuit");CareerUnits.Range(MinimumSkill,0,100000,"cohort skill");CareerUnits.Range(MaximumSkill,MinimumSkill,100000,"cohort skill");CareerUnits.Range(StatSpread,0,20000,"cohort spread");CareerUnits.Range(MinimumAge,16,60,"cohort age");CareerUnits.Range(MaximumAge,MinimumAge,60,"cohort age");CareerUnits.Range((int)Tier,0,2,"cohort tier");}
    }
    [System.Runtime.Serialization.DataContract] public sealed class CareerCircuitCohorts
    {
        [System.Runtime.Serialization.DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [System.Runtime.Serialization.DataMember(Name="cohorts",IsRequired=true)] private CareerCircuitCohort[] cohorts {get;set;}
        public CareerCircuitCohort Get(CareerCircuit circuit){foreach(var c in cohorts)if(c.Circuit==circuit)return c;throw new ArgumentException("Missing circuit cohort.");}
        public static CareerCircuitCohorts FromJson(string json){var d=CareerJson.Read<CareerCircuitCohorts>(json);if(d==null||d.SchemaVersion!=1||d.cohorts==null||d.cohorts.Length<1)throw new System.Runtime.Serialization.SerializationException("Invalid circuit cohorts.");CareerUnits.Id(d.Id);for(int i=0;i<d.cohorts.Length;i++){d.cohorts[i].Validate();for(int j=0;j<i;j++)if(d.cohorts[i].Circuit==d.cohorts[j].Circuit)throw new System.Runtime.Serialization.SerializationException("Duplicate circuit cohort.");}return d;}
    }
}
