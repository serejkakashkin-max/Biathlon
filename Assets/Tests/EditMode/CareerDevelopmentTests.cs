using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerDevelopmentTests
    {
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        public static CareerGrowthDefinition Definition()=>CareerGrowthDefinition.FromJson(Read("career_growth_v1.json"));
        private static CareerSave Initialized(ulong seed=71000,int? level=null,int? age=null,CareerGrowthDefinition definition=null)
        {
            var save=CareerFoundationTests.Create(seed);string json=save.ToJson();
            if(level.HasValue){var stats=new int[13];for(int i=0;i<13;i++)stats[i]=level.Value;json=new Regex("\"stats\":\\[[^\\]]*\\]").Replace(json,"\"stats\":["+string.Join(",",stats)+"]",1);}
            if(age.HasValue)json=PhysiologyContractTests.ReplaceNumber(json,"age_years",age.Value.ToString(System.Globalization.CultureInfo.InvariantCulture));
            save=CareerSave.FromJson(json);CareerDevelopment.InitializeWorld(save,definition??Definition(),BiathlonRatingEngineTests.Definition(),s=>new SeededRandomSource(s));return save;
        }
        private static CareerSave Clone(CareerSave s)=>CareerSave.FromJson(s.ToJson());
        private static CareerSave AtLastWeek(CareerSave save)
        {string json=save.ToJson();int at=json.LastIndexOf("\"week\":",StringComparison.Ordinal);return CareerSave.FromJson(json.Substring(0,at)+Regex.Replace(json.Substring(at),"^(\"week\":)[0-9]+","${1}52"));}
        [Test] public void IndependentProfilesPreserveLegacyAbilitiesIdentityEconomyAndMainCheckpoint()
        {
            var source=CareerFoundationTests.Create();var save=Clone(source);var d=Definition();var ratings=BiathlonRatingEngineTests.Definition();
            Assert.That(save.Athlete(0).Growth,Is.Null);Assert.That(CareerSave.FromJson(source.ToJson()).ToJson(),Is.EqualTo(source.ToJson()));
            Assert.That(CareerDevelopment.InitializeWorld(save,d,ratings,s=>new SeededRandomSource(s)),Is.EqualTo(30));
            Assert.That(save.RandomState,Is.EqualTo(source.RandomState));Assert.That(save.NextAthleteOrdinal,Is.EqualTo(source.NextAthleteOrdinal));
            var identities=new HashSet<string>();
            for(int i=0;i<30;i++)
            {
                var a=save.Athlete(i);var old=source.Athlete(i);Assert.That(a.Name,Is.EqualTo(old.Name));Assert.That(a.Nation,Is.EqualTo(old.Nation));Assert.That(a.Age,Is.EqualTo(old.Age));Assert.That(a.Potential,Is.EqualTo(old.Potential));
                Assert.That(a.Money,Is.EqualTo(old.Money));Assert.That(a.SelectedSki,Is.EqualTo(old.SelectedSki));Assert.That(a.Appearance.ToJson(),Is.EqualTo(old.Appearance.ToJson()));
                for(int j=0;j<13;j++)Assert.That(a.Stat((CareerStat)j),Is.EqualTo(old.Stat((CareerStat)j)));
                Assert.That(a.DevelopmentCount,Is.EqualTo(1));Assert.That(a.Development(0).Season,Is.EqualTo(save.Season));Assert.That(a.PeakOverall,Is.EqualTo(BiathlonRatingEngine.Calculate(a,ratings).Overall));identities.Add(a.Growth.ToJson());
            }
            Assert.That(identities.Count,Is.EqualTo(30));string before=save.ToJson();Assert.That(CareerDevelopment.InitializeWorld(save,d,ratings,s=>throw new Exception("must not reroll")),Is.Zero);Assert.That(save.ToJson(),Is.EqualTo(before));
            var repeat=Clone(source);CareerDevelopment.InitializeWorld(repeat,d,ratings,s=>new SeededRandomSource(s));Assert.That(repeat.ToJson(),Is.EqualTo(before));Assert.That(Clone(save).ToJson(),Is.EqualTo(before));
        }
        [Test] public void FailedSubstreamInitializationIsAtomicAndDoesNotPartiallyMigrate()
        {
            var save=CareerFoundationTests.Create();string before=save.ToJson();int calls=0;
            Assert.Catch(()=>CareerDevelopment.InitializeWorld(save,Definition(),BiathlonRatingEngineTests.Definition(),s=>++calls==15?null:new SeededRandomSource(s)));
            Assert.That(save.ToJson(),Is.EqualTo(before));Assert.That(calls,Is.EqualTo(15));
        }
        [Test] public void CreationPresetsCosmeticsNationAgeAndRetryIdsCannotRerollHiddenDevelopment()
        {
            var draft=CareerCreationDraftTests.Draft();var d=CareerDefinitions.ForNewProductionCareer(Read,draft.StartProfileId);string profile=null;
            foreach(string preset in new[]{"balanced","skier","shooter","endurance","sprinter","technician"})
            {
                draft.SelectPreset(preset);
                if(preset!="balanced"){draft.Name="Other identity";draft.Nation="FRA";draft.SetAge(20);draft.SetAppearance(AppearanceProfile.Create("angular","dark_deep","athletic"));}
                var save=draft.Confirm("retry_"+preset,d.Calendar,s=>new SeededRandomSource(s));CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,s=>new SeededRandomSource(s),draft.Tier.BaselineStat);
                if(profile==null)profile=save.Athlete(0).Growth.ToJson();else Assert.That(save.Athlete(0).Growth.ToJson(),Is.EqualTo(profile));
            }
        }
        [Test] public void IndividualTrainingSharesRecoveryCostDiminishingReturnsAndGroupedCeilings()
        {
            var save=Initialized(level:45000,age:20);var d=Definition();var adaptation=CareerAdaptationTests.Definition();var session=CareerTrainingTests.Definition().Get(CareerTrainingKind.Endurance);
            int Gain(CareerSave s,int quality=10000,int prior=0)=>CareerAdaptation.Apply(s.Athlete(0),session,adaptation,quality,d,prior).TotalGain;
            int normal=Gain(Clone(save));Assert.That(normal,Is.GreaterThan(0));Assert.That(Gain(Clone(save),5000),Is.LessThan(normal));Assert.That(Gain(Clone(save),10000,4),Is.LessThan(normal));
            var tired=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(save.ToJson(),"fatigue","8000"));Assert.That(Gain(tired),Is.LessThan(normal));Assert.That(Gain(Clone(save),0),Is.Zero);
            var a=save.Athlete(0);var ceiling=a.Growth.Ceiling(CareerGrowthGroup.Aerobic);
            for(int i=0;i<6000;i++)CareerAdaptation.Apply(a,session,adaptation,10000,d);
            Assert.That(a.Stat(CareerStat.Endurance),Is.InRange(ceiling-1000,ceiling));Assert.That(Gain(save),Is.LessThan(normal));
            string before=save.ToJson();Assert.Catch(()=>CareerAdaptation.Apply(a,session,adaptation,12001,d));Assert.That(save.ToJson(),Is.EqualTo(before));
            Assert.Catch(()=>CareerAdaptation.Apply(a,session,adaptation,10000,d,-1));Assert.That(save.ToJson(),Is.EqualTo(before));
            // An already achieved ability above its future ceiling remains valid.
            var over=Initialized(level:100000);before=over.Athlete(0).ToJson();Assert.That(CareerAdaptation.Apply(over.Athlete(0),session,adaptation,10000,d).TotalGain,Is.Zero);Assert.That(over.Athlete(0).ToJson(),Is.EqualTo(before));
        }
        [Test] public void AgeingUsesDifferentPhysicalRecoveryTechnicalShootingAndMentalCurves()
        {
            var save=Initialized(level:80000,age:37);var a=save.Athlete(0);var before=new int[13];for(int i=0;i<13;i++)before[i]=a.Stat((CareerStat)i);
            var d=Definition();var expected=new int[13];for(int i=0;i<13;i++)expected[i]=d.AnnualDecline(a,(CareerStat)i,38);
            var frame=CareerAgeing.AdvanceYear(a,CareerAdaptationTests.Definition(),d);int sum=0;for(int i=0;i<13;i++){Assert.That(a.Stat((CareerStat)i),Is.EqualTo(before[i]-expected[i]));sum+=expected[i];}
            Assert.That(frame.TotalDecline,Is.EqualTo(sum));Assert.That(expected[(int)CareerStat.AnaerobicCapacity],Is.GreaterThan(expected[(int)CareerStat.Composure]));
            Assert.That(expected[(int)CareerStat.Recovery],Is.GreaterThan(expected[(int)CareerStat.ProneStability]));Assert.That(expected[(int)CareerStat.Cornering],Is.Not.EqualTo(expected[(int)CareerStat.StandingStability]));
        }
        [Test] public void GrowthStylesHaveIndividualResponsesStagnationAndRareExceptionalCeilings()
        {
            var d=Definition();var styles=new HashSet<string>();var bands=new Dictionary<string,int>();int stagnant=0,exceptional=0;
            // Independent worlds; all samples share current ability and age.
            for(ulong seed=1;seed<=12;seed++)
            {
                var save=Initialized(seed,45000,20,d);
                for(int i=0;i<save.AthleteCount;i++)
                {
                    var p=save.Athlete(i).Growth;styles.Add(p.StyleId);if(!bands.ContainsKey(p.TalentBandId))bands.Add(p.TalentBandId,0);bands[p.TalentBandId]++;
                    if(p.IsStagnating(25))stagnant++;if(p.TalentBandId=="exceptional")exceptional++;
                    Assert.That(p.RetirementAge,Is.InRange(33,44));Assert.That(p.Ceiling(CareerGrowthGroup.Aerobic),Is.InRange(1000,100000));
                }
            }
            Assert.That(styles.Count,Is.EqualTo(7));Assert.That(bands.Count,Is.GreaterThanOrEqualTo(4));Assert.That(bands["ordinary"],Is.InRange(100,220));Assert.That(exceptional,Is.LessThan(10));Assert.That(stagnant,Is.InRange(15,90));
            TestContext.Out.WriteLine("360 profiles; exceptional="+exceptional+" stagnant-at25="+stagnant);
        }
        [Test] public void LateBloomersImproveTheirLearningResponseWhileEarlyProdigiesSlowDown()
        {
            var d=Definition();string json=Initialized(level:49000,age:19).ToJson();
            CareerAthlete At(string style,int age,int shift)
            {
                string value=Regex.Replace(json,"\"style_id\":\"[^\"]+\"","\"style_id\":\""+style+"\"");
                value=PhysiologyContractTests.ReplaceNumber(value,"age_years",age.ToString());value=PhysiologyContractTests.ReplaceNumber(value,"age_shift",shift.ToString());
                value=PhysiologyContractTests.ReplaceNumber(value,"stagnation_16_79","0");return CareerSave.FromJson(value).Athlete(0);
            }
            Assert.That(d.AgeResponse(At("late_bloomer",30,-2),CareerStat.Endurance),Is.GreaterThan(d.AgeResponse(At("late_bloomer",19,-2),CareerStat.Endurance)));
            Assert.That(d.AgeResponse(At("early_prodigy",30,2),CareerStat.Endurance),Is.LessThan(d.AgeResponse(At("early_prodigy",19,2),CareerStat.Endurance)));
        }
        [Test] public void PlayerAndAiUseIdenticalSharedTrainingAndHistorySurvivesSeasonBoundary()
        {
            var save=Initialized(level:49000,age:19);var copy=Clone(save);var d=CareerSeasonTests.Definitions();var a=save.Athlete(0);var b=copy.Athlete(0);
            var plan=CareerAiTraining.Plan(a,d.Training,d.Calendar,d.Policy,1,d.Growth,d.Adaptation);
            int x=CareerTrainingWeek.Apply(a,plan,d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,d.Policy.Quality,false,d.Growth);
            int y=CareerTrainingWeek.Apply(b,plan,d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,d.Policy.Quality,false,d.Growth);
            Assert.That(x,Is.GreaterThan(0));Assert.That(y,Is.EqualTo(x));Assert.That(a.ToJson(),Is.EqualTo(b.ToJson()));Assert.That(a.TrainingSessions,Is.EqualTo(plan.Count));
            save=AtLastWeek(save);int baseline=a.SeasonBaselineStat(CareerStat.Endurance);
            var prepared=CareerSeasonEngine.PrepareWeek(save,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));Assert.That(prepared.RequiresRace,Is.False);
            var next=CareerSeasonEngine.CompleteWeek(prepared,d,null,s=>new SeededRandomSource(s));var player=next.Athlete(0);
            Assert.That(player.DevelopmentCount,Is.EqualTo(2));Assert.That(player.Development(1).Kind,Is.EqualTo(CareerDevelopmentObservation.SeasonEnd));Assert.That(player.Development(1).Season,Is.EqualTo(1));Assert.That(player.Development(1).Age,Is.EqualTo(19));Assert.That(player.Age,Is.EqualTo(20));
            Assert.That(player.SeasonBaselineStat(CareerStat.Endurance),Is.GreaterThanOrEqualTo(baseline));Assert.That(next.Season,Is.EqualTo(2));Assert.That(Clone(next).ToJson(),Is.EqualTo(next.ToJson()));
        }
        [Test] public void BoundedSeasonalObservationsKeepLifetimePeakAfterOldRowsAreTrimmed()
        {
            var d=CareerSeasonTests.Definitions();var growth=CareerGrowthDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.Growth.ToJson(),"history_limit","2"));
            var save=Initialized(level:80000,age:35,definition:growth);int peak=save.Athlete(0).PeakOverall;
            for(int season=1;season<=5;season++)
            {
                save=AtLastWeek(save);CareerDevelopment.SealSeason(save,growth,d.Ratings);save=CareerHistory.SealSeason(save,d.Calendar,d.Scoring,d.History);
                for(int i=0;i<save.AthleteCount;i++)CareerAgeing.AdvanceYear(save.Athlete(i),d.Adaptation,growth);
            }
            var a=save.Athlete(0);Assert.That(a.DevelopmentCount,Is.EqualTo(2));Assert.That(a.Development(0).Season,Is.EqualTo(4));Assert.That(a.Development(1).Season,Is.EqualTo(5));
            Assert.That(a.PeakOverall,Is.EqualTo(peak));Assert.That(a.PeakAge,Is.EqualTo(35));Assert.That(a.PeakSeason,Is.EqualTo(1));Assert.That(Clone(save).ToJson(),Is.EqualTo(save.ToJson()));
            Assert.That(BiathlonRatingEngine.Calculate(a,d.Ratings).Overall,Is.LessThan(peak));
        }
        [Test] public void LegacyWeekPreparationMigratesOnlyTheDetachedCandidateAtItsActualCurrentWeek()
        {
            var source=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),"week","11"));string before=source.ToJson();var d=CareerSeasonTests.Definitions();
            var a=CareerSeasonEngine.PrepareWeek(source,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));var b=CareerSeasonEngine.PrepareWeek(source,d,Array.Empty<CareerTrainingKind>(),s=>new SeededRandomSource(s));
            Assert.That(source.ToJson(),Is.EqualTo(before));Assert.That(a.SourceJson,Is.EqualTo(before));Assert.That(a.RaceSnapshotJson,Is.EqualTo(b.RaceSnapshotJson));
            var candidate=CareerSave.FromJson(a.RaceSnapshotJson);Assert.That(candidate.Athlete(0).Development(0).Week,Is.EqualTo(11));Assert.That(candidate.Athlete(0).Development(0).Kind,Is.EqualTo(CareerDevelopmentObservation.Baseline));Assert.That(candidate.RandomState,Is.EqualTo(source.RandomState));
            for(int i=0;i<13;i++)Assert.That(candidate.Athlete(0).Stat((CareerStat)i),Is.EqualTo(source.Athlete(0).Stat((CareerStat)i)));
        }
        [Test] public void GrowthDefinitionAndSavedMetadataRejectInconsistentOrUnboundedInputs()
        {
            string json=Definition().ToJson();
            Assert.Catch(()=>CareerGrowthDefinition.FromJson(json.Replace("\"curve\":\"aerobic\"","\"curve\":\"unknown\"")));
            Assert.Catch(()=>CareerGrowthDefinition.FromJson(json.Replace("10000,8500,7000","10000,11000,7000")));
            Assert.Catch(()=>CareerGrowthDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"substream_stride","0")));
            Assert.Catch(()=>CareerGrowthDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"history_limit","101")));
            json=Initialized().ToJson();Assert.Catch(()=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"retirement_age","25")));
            Assert.Catch(()=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(json,"stagnation_80_120",ulong.MaxValue.ToString())));
            Assert.Catch(()=>CareerSave.FromJson(json.Replace("\"growth_profile\":{","\"growth_profile\":null,\"unused\":{")));
        }
    }
}
