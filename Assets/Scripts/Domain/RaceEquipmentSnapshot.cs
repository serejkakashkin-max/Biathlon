using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    /// <summary>Immutable resolved equipment and handling definitions recorded with a race.</summary>
    [DataContract] public sealed class RaceEquipmentSnapshot
    {
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion {get;private set;}
        [DataMember(Name="id",IsRequired=true)] public string Id {get;private set;}
        [DataMember(Name="skis_json",IsRequired=true)] private string skisJson {get;set;}
        [DataMember(Name="handling_json",IsRequired=true)] private string handlingJson {get;set;}
        [DataMember(Name="rifles",IsRequired=true)] private ShootingEquipmentDefinition[] rifles {get;set;}
        private Dictionary<string,ShootingEquipmentDefinition> byRifle;
        public SkiEquipmentCatalog Skis {get;private set;}
        public ShootingHandlingDefinition Handling {get;private set;}
        public int RifleCount=>rifles.Length;
        public ShootingEquipmentDefinition Rifle(int i)=>rifles[i];
        public ShootingEquipmentDefinition Rifle(string id)=>byRifle.TryGetValue(id,out var r)?r:throw new ArgumentException("Unknown rifle.");
        public bool IsRifle(string id)=>byRifle.ContainsKey(id);
        private RaceEquipmentSnapshot(){}
        public RaceEquipmentSnapshot(SkiEquipmentCatalog skis,ShootingEquipmentDefinition[] rifles,ShootingHandlingDefinition handling)
        {
            if(skis==null||rifles==null||handling==null)throw new ArgumentNullException();SchemaVersion=1;Id="race_equipment_snapshot_v1";
            skisJson=skis.ToJson();handlingJson=handling.ToJson();this.rifles=(ShootingEquipmentDefinition[])rifles.Clone();Validate();
        }
        private void Validate()
        {
            if(SchemaVersion!=1||Id!="race_equipment_snapshot_v1"||rifles==null||rifles.Length<1||rifles.Length>13)throw new SerializationException("Invalid recorded equipment.");
            Skis=SkiEquipmentCatalog.FromJson(skisJson);Handling=ShootingHandlingDefinition.FromJson(handlingJson);byRifle=new Dictionary<string,ShootingEquipmentDefinition>(StringComparer.Ordinal);
            for(int i=0;i<rifles.Length;i++)
            {if(rifles[i]==null)throw new SerializationException("Missing rifle.");var r=ShootingEquipmentDefinition.FromJson(rifles[i].ToJson());_=new ShootingHandlingProfile(Handling,r,50);byRifle.Add(r.Id,r);rifles[i]=r;}
            for(int i=0;i<Skis.Count;i++)if(byRifle.ContainsKey(Skis.Get(i).Id))throw new SerializationException("Equipment IDs must be unique across slots.");
        }
        public string ToJson()=>RouteJson.Write(this);
        public static RaceEquipmentSnapshot FromJson(string json)
        {var c=RouteJson.Read<RaceEquipmentSnapshot>(json)??throw new SerializationException("Missing recorded equipment.");c.Validate();return c;}
    }
}
