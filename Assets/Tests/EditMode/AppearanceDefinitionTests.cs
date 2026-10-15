using System;
using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class AppearanceDefinitionTests
    {
        private static AppearanceDefinition Policy()=>AppearanceDefinition.FromJson(PhysiologyContractTests.DefinitionJson("athlete_creator_appearance_v1.json"));
        [Test] public void DetailedAppearanceRoundTripsAllStableIdsAndRemainsDetachedFromItsSource()
        {
            var d=Policy();var original=AppearanceProfile.Create("angular","brown_deep","athletic");string before=original.ToJson();
            var p=d.Edit(original,heightMillimeters:1980,shoulderWidthBasis:10000,hairStyleId:"short_swept",hairColorId:"blond",eyeColorId:"green",headwearId:"headband",gogglesId:"raised",lensId:"amber",glovesId:"ivory",bootsId:"red",trainingAccentId:"sunrise");
            Assert.That(p.SchemaVersion,Is.EqualTo(2));Assert.That(p.Id,Is.EqualTo("g2_personal_v2"));Assert.That(original.ToJson(),Is.EqualTo(before));
            var restored=AppearanceProfile.FromJson(p.ToJson());d.ValidateProfile(restored);Assert.That(restored.ToJson(),Is.EqualTo(p.ToJson()));Assert.That(restored.HeightMillimeters,Is.EqualTo(1980));Assert.That(restored.ShoulderWidthBasis,Is.EqualTo(10000));
            Assert.That(restored.HairStyleId,Is.EqualTo("short_swept"));Assert.That(restored.HairColorId,Is.EqualTo("blond"));Assert.That(restored.EyeColorId,Is.EqualTo("green"));Assert.That(restored.HeadwearId,Is.EqualTo("headband"));Assert.That(restored.GogglesId,Is.EqualTo("raised"));Assert.That(restored.LensId,Is.EqualTo("amber"));Assert.That(restored.GlovesId,Is.EqualTo("ivory"));Assert.That(restored.BootsId,Is.EqualTo("red"));Assert.That(restored.TrainingAccentId,Is.EqualTo("sunrise"));
            var face=d.Edit(restored,faceId:"broad");Assert.That(face.HeightMillimeters,Is.EqualTo(p.HeightMillimeters));Assert.That(face.HairStyleId,Is.EqualTo(p.HairStyleId));Assert.That(face.TrainingAccentId,Is.EqualTo(p.TrainingAccentId));
        }
        [Test] public void LegacyAndV1ProfilesDoNotAcquireUnrequestedNewFields()
        {
            foreach(var p in new[]{AppearanceProfile.Legacy("old_athlete"),AppearanceProfile.Create("oval","light_warm","natural")})
            {string json=p.ToJson();Assert.That(json,Does.Not.Contain("height_mm"));Assert.That(json,Does.Not.Contain("hair_style_id"));Assert.That(AppearanceProfile.FromJson(json).ToJson(),Is.EqualTo(json));}
            var d=Policy();var upgraded=d.Edit(AppearanceProfile.Create("oval","light_warm","natural"));Assert.That(upgraded.HeightMillimeters,Is.EqualTo(d.BaseHeightMillimeters));Assert.That(upgraded.ShoulderWidthBasis,Is.EqualTo(d.DefaultShoulderWidthBasis));
        }
        [Test] public void EveryAuthoredCosmeticIsSelectableAndInvalidEditsAreAtomic()
        {
            var d=Policy();var p=d.Edit(AppearanceProfile.Create("oval","light_warm","natural"));string baseline=p.ToJson();
            foreach(string group in new[]{"hair_style","hair_color","eye_color","headwear","goggles","lens","gloves","boots","training_accent"})
            for(int i=0;i<d.OptionCount(group);i++)Assert.That(d.Index(group,d.Option(group,i).Id),Is.EqualTo(i));
            Assert.Catch(()=>d.Edit(p,heightMillimeters:d.MinimumHeightMillimeters-1));Assert.Catch(()=>d.Edit(p,heightMillimeters:d.MaximumHeightMillimeters+1));Assert.Catch(()=>d.Edit(p,shoulderWidthBasis:10001));
            Assert.Catch(()=>d.Edit(p,hairStyleId:"missing"));Assert.Catch(()=>d.Edit(p,eyeColorId:"../bad"));Assert.Catch(()=>d.Edit(p,bootsId:"world_cup_speed_bonus"));Assert.Catch(()=>d.Edit(AppearanceProfile.Legacy("old_athlete")));
            Assert.That(p.ToJson(),Is.EqualTo(baseline));Assert.Throws<ArgumentNullException>(()=>d.Edit(null));
        }
        [Test] public void DetailedCosmeticSaveNeverChangesStatsInventoryMoneyOrSimulationRandomState()
        {
            var draft=CareerCreationDraftTests.Draft();var calendar=CareerDefinitions.ForNewProductionCareer(PhysiologyContractTests.DefinitionJson,draft.StartProfileId).Calendar;
            var a=draft.Confirm("cosmetic_parity",calendar,s=>new Biathlon.Simulation.SeededRandomSource(s));
            draft.SetAppearance(Policy().Edit(draft.Appearance,heightMillimeters:1700,shoulderWidthBasis:0,hairColorId:"auburn",gogglesId:"raised",bootsId:"red"));
            var b=CareerSave.FromJson(draft.Confirm("cosmetic_parity",calendar,s=>new Biathlon.Simulation.SeededRandomSource(s)).ToJson());
            Assert.That(b.RandomState,Is.EqualTo(a.RandomState));Assert.That(b.PlayerId,Is.EqualTo(a.PlayerId));
            for(int athlete=0;athlete<a.AthleteCount;athlete++)for(int stat=0;stat<13;stat++)Assert.That(b.Athlete(athlete).Stat((CareerStat)stat),Is.EqualTo(a.Athlete(athlete).Stat((CareerStat)stat)));
            // Compare all serialized state with only the explicitly cosmetic profile normalized.
            string changed=b.Athlete(0).Appearance.ToJson();Assert.That(b.ToJson().Replace(changed,a.Athlete(0).Appearance.ToJson()),Is.EqualTo(a.ToJson()));
        }
        [Test] public void InvalidPolicyVersionsDuplicateIdsAndNonColorsFailBeforeCreatingAProfile()
        {
            string json=PhysiologyContractTests.DefinitionJson("athlete_creator_appearance_v1.json");
            Assert.Catch(()=>AppearanceDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"schema_version","2")));
            Assert.Catch(()=>AppearanceDefinition.FromJson(json.Replace("\"short_swept\"","\"crew\"")));
            Assert.Catch(()=>AppearanceDefinition.FromJson(json.Replace("#30201B","oops")));
        }
    }
}
