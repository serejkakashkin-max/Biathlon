using System;
using Biathlon.Domain;
namespace Biathlon.Career
{
    public readonly struct CareerEquipmentComparison
    {
        public string CurrentId { get; }
        public string CandidateId { get; }
        public double GlideDelta { get; }
        public double GripDelta { get; }
        public double StabilityDelta { get; }
        public double CorneringDelta { get; }
        public bool Owned { get; }
        public long PurchasePrice { get; }
        internal CareerEquipmentComparison(SkiEquipmentDefinition current,SkiEquipmentDefinition candidate,string snow,bool owned,long price)
        {
            CurrentId=current.Id;CandidateId=candidate.Id;Owned=owned;PurchasePrice=price;
            GlideDelta=candidate.GlideMultiplier*candidate.AffinityFor(snow)-current.GlideMultiplier*current.AffinityFor(snow);
            GripDelta=candidate.GripMultiplier-current.GripMultiplier;StabilityDelta=candidate.StabilityMultiplier-current.StabilityMultiplier;
            CorneringDelta=candidate.GripMultiplier*candidate.StabilityMultiplier-current.GripMultiplier*current.StabilityMultiplier;
        }
    }
    public static class CareerInventory
    {
        public static bool Owns(CareerAthlete athlete,string skiId)
        {if(athlete==null)throw new ArgumentNullException(nameof(athlete));for(int i=0;i<athlete.InventoryCount;i++)if(athlete.InventoryItem(i)==skiId)return true;return false;}
        public static void Select(CareerAthlete athlete,SkiEquipmentCatalog catalog,string skiId)
        {
            if(athlete==null||catalog==null)throw new ArgumentNullException();athlete.Validate();catalog.GetSki(skiId);
            if(!Owns(athlete,skiId))throw new InvalidOperationException("Selected skis must be owned.");athlete.SelectedSki=skiId;
        }
        public static CareerEquipmentComparison Compare(CareerAthlete athlete,SkiEquipmentCatalog catalog,CareerEconomyDefinition economy,string candidateId,string snowProfileId)
        {
            if(athlete==null||catalog==null||economy==null)throw new ArgumentNullException();athlete.Validate();CareerUnits.Id(snowProfileId);
            var current=catalog.GetSki(athlete.SelectedSki);var candidate=catalog.GetSki(candidateId);bool owned=Owns(athlete,candidateId);
            return new CareerEquipmentComparison(current,candidate,snowProfileId,owned,owned?0:economy.EquipmentPrice(candidateId));
        }
    }
}
