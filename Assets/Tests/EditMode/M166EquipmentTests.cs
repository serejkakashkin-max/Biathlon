using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class M166EquipmentTests
    {
        public static CareerEquipmentCatalog Gear()=>new CareerEquipmentCatalog(PhysiologyContractTests.DefinitionJson);
        public static string WithoutRifles(string json)
        {return Regex.Replace(json,"\"selected_rifle\":\"[^\"]*\",","").Replace(",\"rifle_neutral_v1\"","");}
        public static CareerSave Funded(long money=2000000)=>CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(CareerFoundationTests.Create().ToJson(),"money_minor",money.ToString(CultureInfo.InvariantCulture)));
        [Test] public void CatalogHasEightSkisFourRiflesAndRealSnowSpecializationsWithinOriginalEnvelope()
        {
            var gear=Gear();Assert.That(gear.Skis.Count,Is.EqualTo(8));Assert.That(gear.RifleCount,Is.EqualTo(4));
            Assert.That(gear.Skis.MaximumModifierDeviation,Is.EqualTo(SkiEquipmentTests.Catalog().MaximumModifierDeviation));
            var winners=new HashSet<string>();var snow=SnowConditionTests.Catalog();
            foreach(string condition in new[]{"packed_neutral","fresh_soft","icy_fast","wet_slow"})
            {
                string winner=null;double best=double.MaxValue;
                for(int i=0;i<gear.Skis.Count;i++){var ski=gear.Skis.Get(i);var output=new SkiEquipmentSystem(ski,snow).Evaluate(condition);if(output.SnowResistanceMultiplier<best){best=output.SnowResistanceMultiplier;winner=ski.Id;}}
                winners.Add(winner);
            }
            Assert.That(winners.Count,Is.GreaterThanOrEqualTo(4),"No universal fastest model.");
            Assert.That(CareerEquipmentCatalog.FromJson(gear.ToJson()).ToJson(),Is.EqualTo(gear.ToJson()));
            for(int i=0;i<gear.RifleCount;i++)new ShootingHandlingProfile(gear.Snapshot.Handling,gear.Rifle(i),50);
            Assert.That(gear.Rifle(CareerEquipmentCatalog.NeutralRifleId).ToJson(),Is.EqualTo(ShootingHandlingTests.Equipment().ToJson()));
        }
        [Test] public void PurchasesArePaidAtomicAndSlotsRemainIndependentAcrossSaveLoad()
        {
            var save=Funded();var a=save.Athlete(0);var gear=Gear();var economy=CareerEconomyTests.Definition();long before=a.Money;
            foreach(string id in new[]{"ski_wet_channel","rifle_precision"}){CareerEconomy.BuyEquipment(a,economy,gear,id);CareerInventory.Select(a,gear,id);before-=economy.EquipmentPrice(id);}
            Assert.That(a.Money,Is.EqualTo(before));Assert.That(a.SelectedSki,Is.EqualTo("ski_wet_channel"));Assert.That(a.SelectedRifle,Is.EqualTo("rifle_precision"));Assert.That(a.InventoryCount,Is.EqualTo(4));
            string stable=save.ToJson();Assert.Catch(()=>CareerInventory.Select(a,gear,"rifle_agile"));Assert.Catch(()=>CareerInventory.Select(a,gear,"unknown"));Assert.Catch(()=>CareerEconomy.BuyEquipment(a,economy,gear,"rifle_precision"));Assert.That(save.ToJson(),Is.EqualTo(stable));
            Assert.That(CareerSave.FromJson(stable).ToJson(),Is.EqualTo(stable));
            a=Funded(0).Athlete(0);stable=a.ToJson();Assert.Catch(()=>CareerEconomy.BuyEquipment(a,economy,gear,"rifle_agile"));Assert.That(a.ToJson(),Is.EqualTo(stable));
        }
        [Test] public void V3MigrationOnlyAddsNeutralRifleAndDoesNotTouchAppearanceMoneyOrRandom()
        {
            var save=Funded();new CareerRivalAppearance(PhysiologyContractTests.DefinitionJson).InitializeNewWorld(save);
            string old=PhysiologyContractTests.ReplaceNumber(WithoutRifles(save.ToJson()),"save_version","3");var result=CareerSaveMigrations.Load(old);
            Assert.That(result.StepCount,Is.EqualTo(1));Assert.That(result.StepId(0),Is.EqualTo("career_v3_to_v4_equipment"));
            Assert.That(PhysiologyContractTests.ReplaceNumber(WithoutRifles(result.Save.ToJson()),"save_version","3"),Is.EqualTo(old));
            Assert.That(result.Save.ToJson(),Is.EqualTo(save.ToJson()));Assert.Catch(()=>CareerSave.FromJson(WithoutRifles(save.ToJson())));
        }
        [Test] public void MigrationIncludesBackgroundWorldAndPreservesAFullLegacyInventory()
        {
            var catalog=new CareerAthleteCatalog(PhysiologyContractTests.DefinitionJson,"m166_migration",123,s=>new SeededRandomSource(s));var save=catalog.Confirm(catalog.Get(7).Id);
            Assert.That(save.WorldAthleteCount,Is.GreaterThan(save.AthleteCount));
            var items=new StringBuilder("\"ski_neutral\"");for(int i=1;i<100;i++)items.Append(",\"legacy_item_").Append(i).Append("\"");
            string old=PhysiologyContractTests.ReplaceNumber(WithoutRifles(save.ToJson()),"save_version","3").Replace("\"inventory\":[\"ski_neutral\"]","\"inventory\":["+items+"]");
            var migrated=CareerSave.FromJson(old);for(int i=0;i<migrated.WorldAthleteCount;i++){Assert.That(migrated.WorldAthlete(i).InventoryCount,Is.EqualTo(101));Assert.That(migrated.WorldAthlete(i).SelectedRifle,Is.EqualTo(CareerEquipmentCatalog.NeutralRifleId));}
            Assert.That(PhysiologyContractTests.ReplaceNumber(WithoutRifles(migrated.ToJson()),"save_version","3"),Is.EqualTo(old));
        }
        [Test] public void ReplayEquipmentSnapshotIsSelfContainedAndRejectsUnknownVersionsOrMissingBindings()
        {
            var gear=Gear();string frozen=gear.ToJson();var restored=RaceEquipmentSnapshot.FromJson(frozen);
            Assert.That(restored.Skis.ToJson(),Is.EqualTo(gear.Skis.ToJson()));Assert.That(restored.Rifle("rifle_agile").ToJson(),Is.EqualTo(gear.Rifle("rifle_agile").ToJson()));
            Assert.Catch(()=>RaceEquipmentSnapshot.FromJson(PhysiologyContractTests.ReplaceNumber(frozen,"schema_version","99")));
            Assert.Catch(()=>RaceEquipmentSnapshot.FromJson(frozen.Replace("rifle_agile","rifle_precision")));
        }
        [Test] public void RifleHandlingRecoilAndErgonomicsHaveMeasuredTradeoffsWithoutChangingRayGeometry()
        {
            var gear=Gear();var baseline=new ShootingHandlingProfile(gear.Snapshot.Handling,gear.Rifle(0),50);
            var precision=new ShootingHandlingProfile(gear.Snapshot.Handling,gear.Rifle("rifle_precision"),50);
            var agile=new ShootingHandlingProfile(gear.Snapshot.Handling,gear.Rifle("rifle_agile"),50);
            var endurance=new ShootingHandlingProfile(gear.Snapshot.Handling,gear.Rifle("rifle_endurance"),50);
            Assert.That(precision.RecoilY,Is.LessThan(baseline.RecoilY));Assert.That(precision.TimeMultiplier,Is.GreaterThan(baseline.TimeMultiplier));
            Assert.That(agile.TimeMultiplier,Is.LessThan(baseline.TimeMultiplier));Assert.That(agile.RecoilY,Is.GreaterThan(baseline.RecoilY));Assert.That(endurance.ExitTimeMultiplier,Is.LessThan(baseline.ExitTimeMultiplier));
            var ticks=new List<int>();
            for(int rifle=0;rifle<gear.RifleCount;rifle++)
            {
                var profile=new ShootingHandlingProfile(gear.Snapshot.Handling,gear.Rifle(rifle),50);
                var s=new ShotResolutionSystem(ShootingStateTests.Definition(),TargetGeometryTests.Definition(),ShootingWindTests.Definition(),ShootingStanceTests.Definition().Standing,new AthleteId("equipment"),"range",20,new RaceEventLog(100),handling:profile);
                s.Step(new SimulationTick(1),ShootingAction.EnterRange);s.Step(new SimulationTick(2),ShootingAction.AssignLane,new ShootingLaneAssignment("range","lane",1));
                for(int i=3;i<=500;i++){var f=s.Step(new SimulationTick(i),ShootingAction.Trigger,ray:new ShotRay(0,0,0,0,0,1));if(f.Lifecycle.ShotAccepted&&s.ShotsFired==1){Assert.That(f.Outcome,Is.EqualTo(ShotOutcome.TargetClosed));ticks.Add(i);break;}}
            }
            Assert.That(new HashSet<int>(ticks).Count,Is.GreaterThan(1));
        }
        [Test] public void AiPaysThePlayerPriceKeepsReserveAndDoesNotConsumeRandom()
        {
            var save=Funded();var a=save.Athlete(1);long start=a.Money;ulong seed=save.RandomState;var gear=Gear();var economy=CareerEconomyTests.Definition();var policy=CareerEquipmentPolicy.FromJson(PhysiologyContractTests.DefinitionJson("career_equipment_policy_v1.json"));
            CareerEquipmentPlanner.Prepare(a,gear,economy,policy,SnowConditionTests.Catalog(),"wet_slow");long paid=0;
            for(int i=2;i<a.InventoryCount;i++)paid+=economy.EquipmentPrice(a.InventoryItem(i));
            Assert.That(paid,Is.GreaterThan(0));Assert.That(a.Money,Is.EqualTo(start-paid));Assert.That(a.Money,Is.GreaterThanOrEqualTo(policy.Reserve));Assert.That(save.RandomState,Is.EqualTo(seed));
            string stable=a.ToJson();CareerEquipmentPlanner.Prepare(a,gear,economy,policy,SnowConditionTests.Catalog(),"wet_slow");Assert.That(a.ToJson(),Is.EqualTo(stable));
        }
        [Test] public void AllCareerSkisHaveMeasuredRaceEffectsBelowBroadAthleteProgression()
        {
            var gear=Gear();var report=new StringBuilder("snow,ski,neutral_seconds,equipped_seconds,expert_seconds\n");
            foreach(string snow in new[]{"packed_neutral","fresh_soft","icy_fast","wet_slow"})
            {
                double neutral=Trial("ski_neutral",snow,50),expert=Trial("ski_neutral",snow,80);
                for(int i=0;i<gear.Skis.Count;i++){string id=gear.Skis.Get(i).Id;double result=Trial(id,snow,50);report.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2:F3},{3:F3},{4:F3}\n",snow,id,neutral,result,expert);Assert.That(Math.Abs(result-neutral)/neutral,Is.LessThan(.04));Assert.That(result,Is.GreaterThan(expert));}
            }
            DraftCalibrationTests.Artifact("m166-ski-matrix.csv",report.ToString());
            double Trial(string id,string snow,int skill)
            {
                var track=SyntheticTrackTests.Generated("synthetic_rolling_v1");var requirements=CompetitionRouteRequirements.TimeTrial("trial_profile",track.GetLoop("main_long").Length,3);var plan=RaceRoutePlan.TimeTrial("trial_route",track.Id,"main_long",requirements);
                string json=TimeTrialTests.Athlete().ToJson();foreach(string stat in new[]{"endurance","anaerobic_capacity","recovery","strength","ski_technique","climbing","downhill","cornering"})json=PhysiologyContractTests.ReplaceNumber(json,stat,skill.ToString(CultureInfo.InvariantCulture));
                string Read(string name)=>name=="ski_equipment_provisional_v1.json"?gear.Skis.ToJson():PhysiologyContractTests.DefinitionJson(name);
                var s=new TimeTrialSession(new SimulationConfig(1,"trial_sim",20,12345),new SkiModelDefinitions(Read),TimeTrialAthlete.FromJson(json),track,new RaceRoute(track,plan,requirements),SnowConditionTests.Conditions(snow),id,new RaceEventLog(1000));
                while(!s.HasFinished&&s.Clock.Tick.Value<100000)s.Step(TimeTrialTests.Input(s,s.Clock.Tick.Value%1600<1200?850:550,tuck:true,sprint:true));Assert.That(s.HasFinished,Is.True);return s.FinishTime.Seconds;
            }
        }
        [Test] public void TwoSeasonFinancialScenariosShowSavingsAndServiceTradeoffsAcrossAllStarts()
        {
            var economy=CareerEconomyTests.Definition();var report=new StringBuilder("start,rank,two_season_races,income_minor,services_minor,ending_minor,top_ski_affordable\n");
            var profiles=CareerStartDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_start_v1.json"));
            foreach(string start in new[]{"junior_novice","senior_debut","world_cup_debut"})foreach(int level in new[]{0,1,2})
            {
                var p=profiles.Get(start);var calendar=CareerCalendar.FromJson(PhysiologyContractTests.DefinitionJson(p.CalendarResource));int races=calendar.Count*2;
                int[] ranks=level==0?new[]{27,23,18,29,21,26}:level==1?new[]{9,7,8,3,10,6}:new[]{3,1,2,1,4,2};
                long income=0,services=0,money=CareerFoundationTests.Rules().StartingMoney;
                for(int race=0;race<races;race++){long prize=economy.Prize(ranks[race%ranks.Length],p.PlayerTier);income+=prize;money+=prize;if(race%3==2&&money>=economy.CampPrice+economy.RecoveryPrice){money-=economy.CampPrice+economy.RecoveryPrice;services+=economy.CampPrice+economy.RecoveryPrice;}}
                Assert.That(money,Is.GreaterThanOrEqualTo(0));Assert.That(money,Is.LessThan(2000000),"No two-season unlimited equipment budget.");
                report.AppendFormat(CultureInfo.InvariantCulture,"{0},{1},{2},{3},{4},{5},{6}\n",start,level,races,income,services,money,money>=economy.EquipmentPrice("ski_cold_race"));
            }
            Assert.That(CareerFoundationTests.Rules().StartingMoney,Is.LessThan(economy.EquipmentPrice("ski_glide")));DraftCalibrationTests.Artifact("m166-economy-two-seasons.csv",report.ToString());
        }
    }
}
