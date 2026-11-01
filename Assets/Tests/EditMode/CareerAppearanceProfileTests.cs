using System;
using System.Text.RegularExpressions;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Career;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerAppearanceProfileTests
    {
        private static string WithoutProfiles(string json)=>Regex.Replace(json,"\"appearance_profile\":\\{[^{}]*\\},","");
        [Test] public void V2MigrationIsIdempotentAndPreservesEveryExistingCareerFieldAndRng()
        {
            string current=CareerFoundationTests.Create().ToJson();
            string old=PhysiologyContractTests.ReplaceNumber(WithoutProfiles(current),"save_version","2");
            var migrated=CareerSave.FromJson(old);Assert.That(migrated.SaveVersion,Is.EqualTo(CareerSave.CurrentVersion));
            string oldAgain=PhysiologyContractTests.ReplaceNumber(WithoutProfiles(migrated.ToJson()),"save_version","2");
            Assert.That(oldAgain,Is.EqualTo(old),"Only the version and new visual profiles change.");
            for(int i=0;i<migrated.AthleteCount;i++)
            {var a=migrated.Athlete(i);Assert.That(a.Appearance.IsLegacy,Is.True);Assert.That(a.Appearance.LegacyIdentity,Is.EqualTo(a.Id));}
            Assert.That(CareerSave.FromJson(migrated.ToJson()).ToJson(),Is.EqualTo(migrated.ToJson()));
            Assert.Catch(()=>CareerSave.FromJson(WithoutProfiles(current)),"v3 requires each profile; missing fields cannot silently regenerate.");
        }
        [Test] public void ExplicitCosmeticsAndCreationProvenanceSurviveSaveLoadWithoutChangingAbilities()
        {
            var draft=CareerCreationDraftTests.Draft();draft.SelectOrigin("former_skier");
            var calendar=CareerDefinitions.ForNewProductionCareer(PhysiologyContractTests.DefinitionJson,draft.StartProfileId).Calendar;
            var before=draft.Confirm("career_personal",calendar,s=>new SeededRandomSource(s));
            var profile=AppearanceProfile.Create("angular","dark_deep","athletic");draft.SetAppearance(profile);
            var after=draft.Confirm("career_personal",calendar,s=>new SeededRandomSource(s));var restored=CareerSave.FromJson(after.ToJson());
            Assert.That(restored.Athlete(0).Appearance.ToJson(),Is.EqualTo(profile.ToJson()));
            Assert.That(restored.RandomState,Is.EqualTo(before.RandomState));Assert.That(restored.Creation.OriginStoryId,Is.EqualTo("former_skier"));Assert.That(restored.Creation.SpecializationId,Is.EqualTo("skier"));
            Assert.That(restored.Creation.Mode,Is.EqualTo(CareerCreationMode.CreateOwn));Assert.That(restored.Creation.PolicyId,Is.EqualTo(draft.PolicyId));
            for(int athlete=0;athlete<restored.AthleteCount;athlete++)for(int stat=0;stat<13;stat++)Assert.That(restored.Athlete(athlete).Stat((CareerStat)stat),Is.EqualTo(before.Athlete(athlete).Stat((CareerStat)stat)));
            for(int stat=0;stat<13;stat++)Assert.That(restored.Creation.InitialStat((CareerStat)stat),Is.EqualTo(restored.Athlete(0).Stat((CareerStat)stat)));
            draft.SelectPreset("shooter");Assert.That(restored.Creation.InitialStat(CareerStat.Endurance),Is.EqualTo(before.Athlete(0).Stat(CareerStat.Endurance)));
        }
        [Test] public void MigrationLeavesExistingRacePayloadAndFingerprintByteForByteIntact()
        {
            var save=CareerFoundationTests.Create();
            // Explicit persistence-boundary fixture, not a simulated sporting outcome.
            const string payload="{\"recorded_bytes\":[1,2,3],\"note\":\"keep exact whitespace\" }";
            string hash=BalanceRegression.Hash(payload);
            CareerStandings.Register(save,new CareerRaceRecord("migration_record",save.SeasonId,"opening_sprint",CompetitionFormat.Sprint,123,hash,new[]{new CareerFinish(save.PlayerId,300000,300000)},payload));
            string old=PhysiologyContractTests.ReplaceNumber(WithoutProfiles(save.ToJson()),"save_version","2");
            var restored=CareerSave.FromJson(old);Assert.That(restored.RaceCount,Is.EqualTo(1));
            Assert.That(restored.Race(0).AuthorityResultJson,Is.EqualTo(payload));Assert.That(restored.Race(0).AuthoritySha256,Is.EqualTo(hash));
            Assert.That(PhysiologyContractTests.ReplaceNumber(WithoutProfiles(restored.ToJson()),"save_version","2"),Is.EqualTo(old));
        }
        [Test] public void ProfileVersionsMixedModesAndUnsafeResourceIdsAreRejected()
        {
            var profile=AppearanceProfile.Create("oval","light_warm","natural");Assert.That(AppearanceProfile.FromJson(profile.ToJson()).ToJson(),Is.EqualTo(profile.ToJson()));
            var legacy=AppearanceProfile.Legacy("career_athlete_1");Assert.That(AppearanceProfile.FromJson(legacy.ToJson()).ToJson(),Is.EqualTo(legacy.ToJson()));
            Assert.Catch(()=>AppearanceProfile.Create("../bad","light_warm","natural"));
            Assert.Catch(()=>AppearanceProfile.FromJson(PhysiologyContractTests.ReplaceNumber(profile.ToJson(),"schema_version","2")));
            Assert.Catch(()=>AppearanceProfile.FromJson(profile.ToJson().Replace("g2_personal_v1","unknown")));
            Assert.Catch(()=>AppearanceProfile.FromJson(profile.ToJson().Replace("\"face_id\":","\"legacy_identity\":\"career_athlete_1\",\"face_id\":")));
            Assert.Catch(()=>AppearanceProfile.Legacy(""));Assert.Throws<ArgumentNullException>(()=>CareerCreationDraftTests.Draft().SetAppearance(null));
        }
    }
}
