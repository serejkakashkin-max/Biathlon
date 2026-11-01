using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    public enum CareerEquipmentSlot { Skis, Rifle }
    /// <summary>Resolved, recordable equipment data. Original base definitions remain authoritative.</summary>
    public sealed class CareerEquipmentCatalog
    {
        public const string NeutralRifleId="rifle_neutral_v1";
        [DataContract] private sealed class RifleExtension
        {
            [DataMember(Name="schema_version",IsRequired=true)] public int Version {get;set;}
            [DataMember(Name="id",IsRequired=true)] public string Id {get;set;}
            [DataMember(Name="rifles",IsRequired=true)] public ShootingEquipmentDefinition[] Rifles {get;set;}
        }
        public RaceEquipmentSnapshot Snapshot {get;}
        public SkiEquipmentCatalog Skis=>Snapshot.Skis;
        public int RifleCount=>Snapshot.RifleCount;
        public ShootingEquipmentDefinition Rifle(int index)=>Snapshot.Rifle(index);
        public ShootingEquipmentDefinition Rifle(string id)=>Snapshot.Rifle(id);
        public CareerEquipmentSlot Slot(string id)
        {if(Snapshot.IsRifle(id))return CareerEquipmentSlot.Rifle;Skis.GetSki(id);return CareerEquipmentSlot.Skis;}
        private CareerEquipmentCatalog(RaceEquipmentSnapshot snapshot){Snapshot=snapshot;Rifle(NeutralRifleId);}
        public CareerEquipmentCatalog(Func<string,string> read,bool legacy=false)
        {
            if(read==null)throw new ArgumentNullException(nameof(read));
            var skis=SkiEquipmentCatalog.FromJson(read("ski_equipment_provisional_v1.json"));
            var neutral=ShootingEquipmentDefinition.FromJson(read("rifle_neutral_v1.json"));var rifles=new[]{neutral};
            if(!legacy)
            {
                skis=skis.WithAdditional(read("career_ski_models_v2.json"));var extra=CareerJson.Read<RifleExtension>(read("career_rifles_v1.json"));
                if(extra==null||extra.Version!=1||extra.Rifles==null||extra.Rifles.Length<1||extra.Rifles.Length>12)throw new SerializationException("Invalid rifle catalog.");CareerUnits.Id(extra.Id);
                rifles=new ShootingEquipmentDefinition[1+extra.Rifles.Length];rifles[0]=neutral;Array.Copy(extra.Rifles,0,rifles,1,extra.Rifles.Length);
            }
            Snapshot=new RaceEquipmentSnapshot(skis,rifles,ShootingHandlingDefinition.FromJson(read("shooting_handling_v1.json")));Rifle(NeutralRifleId);
        }
        public string ToJson()=>Snapshot.ToJson();
        public static CareerEquipmentCatalog FromJson(string json)=>new CareerEquipmentCatalog(RaceEquipmentSnapshot.FromJson(json));
    }
}
