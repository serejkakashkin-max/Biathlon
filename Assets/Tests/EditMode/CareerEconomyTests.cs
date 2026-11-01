using Biathlon.Career;
using Biathlon.Domain;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerEconomyTests
    {
        public static CareerEconomyDefinition Definition()=>CareerEconomyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_economy_v1.json"));
        public static SkiEquipmentCatalog Equipment()=>SkiEquipmentCatalog.FromJson(PhysiologyContractTests.DefinitionJson("ski_equipment_provisional_v1.json"));
        [Test] public void PrizesAndPurchasesUseExactMoneyAndPersistCampEntitlement()
        {
            var save=M166EquipmentTests.Funded();var a=save.Athlete(0);var d=Definition();long start=a.Money;
            CareerEconomy.Award(a,d,1);CareerEconomy.BuyEquipment(a,d,Equipment(),"ski_glide");CareerEconomy.BuyCamp(a,d,CareerAdaptationTests.Definition());
            CareerFatigue.Training(a,CareerTrainingTests.Definition().Get(CareerTrainingKind.Intensity));CareerEconomy.BuyRecovery(a,d);
            Assert.That(a.Money,Is.EqualTo(start+d.Prize(1,a.Tier)-d.EquipmentPrice("ski_glide")-d.CampPrice-d.RecoveryPrice));Assert.That(a.LifetimeEarnings,Is.EqualTo(d.Prize(1,a.Tier)));
            Assert.That(a.PendingCampQuality,Is.EqualTo(d.CampQuality));Assert.That(CareerSave.FromJson(save.ToJson()).ToJson(),Is.EqualTo(save.ToJson()));
            string before=a.ToJson();Assert.Catch(()=>CareerEconomy.BuyCamp(a,d,CareerAdaptationTests.Definition()));Assert.Catch(()=>CareerEconomy.BuyEquipment(a,d,Equipment(),"ski_glide"));Assert.That(a.ToJson(),Is.EqualTo(before));
        }
        [Test] public void InsufficientFundsUnknownGoodsAndOverflowAreAtomic()
        {
            var a=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),"money_minor","0")).Athlete(0);string before=a.ToJson();
            Assert.Catch(()=>CareerEconomy.BuyEquipment(a,Definition(),Equipment(),"ski_glide"));Assert.Catch(()=>CareerEconomy.BuyRecovery(a,Definition()));Assert.Catch(()=>CareerEconomy.BuyEquipment(a,Definition(),Equipment(),"missing"));Assert.That(a.ToJson(),Is.EqualTo(before));
            a=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),"money_minor",long.MaxValue.ToString())).Athlete(0);before=a.ToJson();Assert.Catch(()=>CareerEconomy.Award(a,Definition(),1));Assert.That(a.ToJson(),Is.EqualTo(before));
        }
    }
}
