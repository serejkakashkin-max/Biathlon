using System;
using System.Runtime.Serialization;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerEquipmentPreference
    {
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="stability_weight_basis",IsRequired=true)] public int Stability {get;private set;}
        [DataMember(Name="handling_weight_basis",IsRequired=true)] public int Handling {get;private set;}
        [DataMember(Name="ergonomics_weight_basis",IsRequired=true)] public int Ergonomics {get;private set;}
    }
    [DataContract] public sealed class CareerEquipmentPolicy
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int Version {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="reserve_minor",IsRequired=true)] public long Reserve {get;private set;}
        [DataMember(Name="glide_weight_basis",IsRequired=true)] public int GlideWeight {get;private set;}
        [DataMember(Name="minimum_score_gain",IsRequired=true)] public double MinimumGain {get;private set;}
        [DataMember(Name="preferences",IsRequired=true)] private CareerEquipmentPreference[] preferences {get;set;}
        public CareerEquipmentPreference Preference(string id)
        {foreach(var p in preferences)if(p.Id==id)return p;return preferences[0];}
        public static CareerEquipmentPolicy FromJson(string json)
        {
            var p=CareerJson.Read<CareerEquipmentPolicy>(json);if(p==null||p.Version!=1||p.Reserve<0||p.preferences==null||p.preferences.Length==0||p.preferences[0].Id!="default")throw new SerializationException("Invalid equipment policy.");
            CareerUnits.Id(p.Id);CareerUnits.Range(p.GlideWeight,0,10000,"glide weight");if(!double.IsFinite(p.MinimumGain)||p.MinimumGain<=0||p.MinimumGain>.1)throw new SerializationException("Invalid upgrade threshold.");
            foreach(var r in p.preferences){CareerUnits.Id(r.Id);CareerUnits.Range(r.Stability,0,10000,"rifle weight");CareerUnits.Range(r.Handling,0,10000,"rifle weight");CareerUnits.Range(r.Ergonomics,0,10000,"rifle weight");if(r.Stability+r.Handling+r.Ergonomics!=10000)throw new SerializationException("Equipment weights must sum to one.");}return p;
        }
    }
    public static class CareerEquipmentPlanner
    {
        public static void Prepare(CareerAthlete athlete,CareerEquipmentCatalog gear,CareerEconomyDefinition economy,CareerEquipmentPolicy policy,SnowCatalog snow,string snowId)
        {
            if(athlete==null||gear==null||economy==null||policy==null||snow==null)throw new ArgumentNullException();
            string best=athlete.SelectedSki;double score=SkiScore(best);
            for(int i=0;i<gear.Skis.Count;i++)
            {
                string id=gear.Skis.Get(i).Id;if(!Available(id))continue;double candidate=SkiScore(id);
                if(candidate>score+policy.MinimumGain){score=candidate;best=id;}
            }
            Acquire(best);var taste=policy.Preference(athlete.PersonalityId);best=athlete.SelectedRifle;score=RifleScore(gear.Rifle(best));
            for(int i=0;i<gear.RifleCount;i++){var r=gear.Rifle(i);if(!Available(r.Id))continue;double candidate=RifleScore(r);if(candidate>score+policy.MinimumGain){score=candidate;best=r.Id;}}
            Acquire(best);
            double SkiScore(string id)
            {var output=new SkiEquipmentSystem(gear.Skis.GetSki(id),snow).Evaluate(snowId);return (policy.GlideWeight/output.SnowResistanceMultiplier+(10000-policy.GlideWeight)*output.CorneringGripMultiplier)/10000;}
            double RifleScore(ShootingEquipmentDefinition r)=>(taste.Stability/r.RecoilMultiplier+taste.Handling/r.HandlingMultiplier+taste.Ergonomics/r.ErgonomicsMultiplier)/10000;
            bool Available(string id)=>CareerInventory.Owns(athlete,id)||economy.EquipmentPrice(id)<=Math.Max(0,athlete.Money-policy.Reserve);
            void Acquire(string id){if(!CareerInventory.Owns(athlete,id))CareerEconomy.BuyEquipment(athlete,economy,gear,id);CareerInventory.Select(athlete,gear,id);}
        }
    }
}
