using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using Biathlon.Domain;
namespace Biathlon.Career
{
    [DataContract] public sealed class CareerEquipmentPrice
    {
        [DataMember(Name="ski_id",IsRequired=true)] public string SkiId { get; private set; }
        [DataMember(Name="price_minor",IsRequired=true)] public long Price { get; private set; }
    }
    [DataContract] public sealed class CareerEconomyDefinition
    {
        private CareerEconomyDefinition() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="prizes_minor",IsRequired=true)] private long[] prizes { get; set; }
        [DataMember(Name="prize_tier_basis",EmitDefaultValue=false)] private int[] prizeTiers {get;set;}
        [DataMember(Name="equipment",IsRequired=true)] private CareerEquipmentPrice[] equipment { get; set; }
        [DataMember(Name="camp_price_minor",IsRequired=true)] public long CampPrice { get; private set; }
        [DataMember(Name="camp_quality",IsRequired=true)] public int CampQuality { get; private set; }
        [DataMember(Name="recovery_price_minor",IsRequired=true)] public long RecoveryPrice { get; private set; }
        [DataMember(Name="recovery_gain",IsRequired=true)] public int RecoveryGain { get; private set; }
        public long Prize(int rank){if(rank<1)throw new ArgumentOutOfRangeException(nameof(rank));return rank<=prizes.Length?prizes[rank-1]:0;}
        public long Prize(int rank,CareerTier tier)
        {CareerUnits.Range((int)tier,0,2,"prize tier");return prizeTiers==null?Prize(rank):checked((long)decimal.Round(Prize(rank)*(decimal)prizeTiers[(int)tier]/CareerUnits.Basis,0,MidpointRounding.ToEven));}
        public long EquipmentPrice(string id){foreach(var item in equipment)if(item.SkiId==id)return item.Price;throw new ArgumentException("Equipment is not offered.");}
        public static CareerEconomyDefinition FromJson(string json)
        {
            var d=CareerJson.Read<CareerEconomyDefinition>(json);if(d==null||d.SchemaVersion!=1&&d.SchemaVersion!=2||d.prizes==null||d.prizes.Length==0||d.prizes.Length>1000||d.equipment==null)throw new SerializationException("Invalid economy.");
            if(d.SchemaVersion==2&&(d.prizeTiers==null||d.prizeTiers.Length!=3))throw new SerializationException("Explicit prize tiers required.");
            if(d.prizeTiers!=null)foreach(int tier in d.prizeTiers)CareerUnits.Range(tier,1000,20000,"prize multiplier");
            CareerUnits.Id(d.Id);if(d.CampPrice<=0||d.RecoveryPrice<=0)throw new SerializationException("Services need positive prices.");
            CareerUnits.Range(d.CampQuality,1,12000,"camp quality");CareerUnits.Range(d.RecoveryGain,1,CareerUnits.Basis,"recovery gain");
            long prior=long.MaxValue;foreach(long prize in d.prizes){if(prize<0||prize>prior)throw new SerializationException("Invalid prize order.");prior=prize;}
            var ids=new HashSet<string>(StringComparer.Ordinal);foreach(var item in d.equipment){if(item==null||item.Price<=0)throw new SerializationException("Invalid equipment price.");CareerUnits.Id(item.SkiId);if(!ids.Add(item.SkiId))throw new SerializationException("Duplicate price.");}return d;
        }
    }
    public static class CareerEconomy
    {
        public static void BuyEquipment(CareerAthlete athlete,CareerEconomyDefinition definition,CareerEquipmentCatalog catalog,string id)
        {
            if(athlete==null||definition==null||catalog==null)throw new ArgumentNullException();catalog.Slot(id);long price=definition.EquipmentPrice(id);CanPay(athlete,price);
            if(CareerInventory.Owns(athlete,id))throw new InvalidOperationException("Equipment already owned.");
            if(athlete.InventoryCount>=CareerAthlete.InventoryCapacity)throw new InvalidOperationException("Inventory full.");athlete.AddEquipment(id);athlete.Money-=price;
        }
        public static long Award(CareerAthlete athlete,CareerEconomyDefinition definition,int rank)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();athlete.Validate();long prize=definition.Prize(rank,athlete.Tier);
            long money=checked(athlete.Money+prize),earnings=checked(athlete.LifetimeEarnings+prize);athlete.Money=money;athlete.LifetimeEarnings=earnings;return prize;
        }
        private static void CanPay(CareerAthlete athlete,long price){athlete.Validate();if(athlete.Money<price)throw new InvalidOperationException("Insufficient funds.");}
        public static void BuyCamp(CareerAthlete athlete,CareerEconomyDefinition definition,CareerAdaptationDefinition adaptation)
        {
            if(athlete==null||definition==null||adaptation==null)throw new ArgumentNullException();CanPay(athlete,definition.CampPrice);
            if(athlete.PendingCampQuality!=0||definition.CampQuality>adaptation.MaximumQuality)throw new InvalidOperationException("Camp already pending or unsupported quality.");
            athlete.Money-=definition.CampPrice;athlete.PendingCampQuality=definition.CampQuality;
        }
        public static void BuyRecovery(CareerAthlete athlete,CareerEconomyDefinition definition)
        {
            if(athlete==null||definition==null)throw new ArgumentNullException();CanPay(athlete,definition.RecoveryPrice);
            if(athlete.Fatigue==0)throw new InvalidOperationException("No recovery needed.");
            athlete.Money-=definition.RecoveryPrice;athlete.Fatigue=Math.Max(0,athlete.Fatigue-definition.RecoveryGain);
        }
        public static void BuyEquipment(CareerAthlete athlete,CareerEconomyDefinition definition,SkiEquipmentCatalog catalog,string skiId)
        {
            if(athlete==null||definition==null||catalog==null)throw new ArgumentNullException();catalog.GetSki(skiId);long price=definition.EquipmentPrice(skiId);CanPay(athlete,price);
            for(int i=0;i<athlete.InventoryCount;i++)if(athlete.InventoryItem(i)==skiId)throw new InvalidOperationException("Equipment already owned.");
            if(athlete.InventoryCount>=CareerAthlete.InventoryCapacity)throw new InvalidOperationException("Inventory full.");athlete.AddEquipment(skiId);athlete.Money-=price;
        }
    }
}
