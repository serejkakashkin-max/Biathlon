using Biathlon.Career;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class CareerInventoryTests
    {
        [Test] public void ComparisonsExposeConditionTradeoffsAndSelectionPersistsWithoutStacking()
        {
            var save=CareerFoundationTests.Create();var a=save.Athlete(0);var catalog=CareerEconomyTests.Equipment();var economy=CareerEconomyTests.Definition();
            var glide=CareerInventory.Compare(a,catalog,economy,"ski_glide","fresh_soft");Assert.That(glide.GlideDelta,Is.GreaterThan(0));Assert.That(glide.CorneringDelta,Is.LessThan(0));Assert.That(glide.Owned,Is.False);
            var soft=CareerInventory.Compare(a,catalog,economy,"ski_soft_specialist","fresh_soft");var ice=CareerInventory.Compare(a,catalog,economy,"ski_soft_specialist","icy_fast");Assert.That(soft.GlideDelta,Is.GreaterThan(ice.GlideDelta));
            string before=a.ToJson();Assert.Catch(()=>CareerInventory.Select(a,catalog,"ski_glide"));Assert.That(a.ToJson(),Is.EqualTo(before));
            CareerEconomy.BuyEquipment(a,economy,catalog,"ski_glide");CareerInventory.Select(a,catalog,"ski_glide");CareerInventory.Select(a,catalog,"ski_glide");
            Assert.That(a.InventoryCount,Is.EqualTo(2));Assert.That(CareerInventory.Compare(a,catalog,economy,"ski_glide","fresh_soft").GlideDelta,Is.Zero);
            Assert.That(CareerSave.FromJson(save.ToJson()).Athlete(0).SelectedSki,Is.EqualTo("ski_glide"));CareerInventory.Select(a,catalog,"ski_neutral");Assert.That(a.SelectedSki,Is.EqualTo("ski_neutral"));
        }
    }
}
