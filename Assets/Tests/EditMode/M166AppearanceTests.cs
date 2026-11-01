using Biathlon.Career;
using System.Collections.Generic;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class M166AppearanceTests
    {
        [Test] public void AuthoredGogglesPolicyAllowsCompleteAbsenceWithoutChangingOtherChoices()
        {
            var policy=AppearanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("athlete_creator_appearance_v1.json"));
            var source=AppearanceProfile.Create("oval","light_neutral","natural");
            foreach(string headwear in new[]{"cap","headband","none"})
            {
                var p=policy.Edit(source,headwearId:headwear,gogglesId:"absent");
                Assert.That(p.GogglesId,Is.EqualTo("absent"));Assert.That(p.HeadwearId,Is.EqualTo(headwear));
                Assert.That(AppearanceProfile.FromJson(p.ToJson()).ToJson(),Is.EqualTo(p.ToJson()));
            }
        }
        [Test] public void NewRivalProfilesAreDiversePersistedAndCannotConsumeCareerRandomOrChangeAbilities()
        {
            var save=CareerFoundationTests.Create();var before=CareerSave.FromJson(save.ToJson());
            var generator=new CareerRivalAppearance(PhysiologyContractTests.DefinitionJson);
            string player=save.Athlete(0).Appearance.ToJson();generator.InitializeNewWorld(save);
            Assert.That(save.RandomState,Is.EqualTo(before.RandomState));Assert.That(save.Athlete(0).Appearance.ToJson(),Is.EqualTo(player));
            var looks=new HashSet<string>();var goggles=new HashSet<string>();var headwear=new HashSet<string>();
            for(int i=1;i<save.AthleteCount;i++)
            {
                var a=save.Athlete(i);Assert.That(a.Appearance.HasDetails,Is.True);looks.Add(a.Appearance.ToJson());goggles.Add(a.Appearance.GogglesId);headwear.Add(a.Appearance.HeadwearId);
                Assert.That(a.Appearance.ToJson(),Is.EqualTo(generator.Generate(a.Id).ToJson()));
                Assert.That(a.Nation,Is.EqualTo(before.Athlete(i).Nation));Assert.That(a.Money,Is.EqualTo(before.Athlete(i).Money));
                for(int stat=0;stat<13;stat++)Assert.That(a.Stat((CareerStat)stat),Is.EqualTo(before.Athlete(i).Stat((CareerStat)stat)));
            }
            Assert.That(looks.Count,Is.EqualTo(save.AthleteCount-1));Assert.That(goggles.Count,Is.EqualTo(3));Assert.That(headwear.Count,Is.EqualTo(3));
            string json=save.ToJson();Assert.That(CareerSave.FromJson(json).ToJson(),Is.EqualTo(json));generator.InitializeNewWorld(save);Assert.That(save.ToJson(),Is.EqualTo(json));
        }
        [Test] public void LoadingAnOldCareerNeverGeneratesDetailedProfilesAndCatalogSelectionRetainsItsSavedLook()
        {
            string legacy=CareerFoundationTests.Create().ToJson();var restored=CareerSave.FromJson(legacy);
            Assert.That(restored.ToJson(),Is.EqualTo(legacy));for(int i=0;i<restored.AthleteCount;i++)Assert.That(restored.Athlete(i).Appearance.IsLegacy,Is.True);
            var catalog=new CareerAthleteCatalog(PhysiologyContractTests.DefinitionJson,"m166_catalog",123,s=>new Biathlon.Simulation.SeededRandomSource(s));
            var entry=catalog.Get(7);Assert.That(entry.Appearance.HasDetails,Is.True);
            Assert.That(catalog.Confirm(entry.Id).FindWorldAthlete(entry.Id).Appearance.ToJson(),Is.EqualTo(entry.Appearance.ToJson()));
        }
    }
}
