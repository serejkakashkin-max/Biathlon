using System;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class CareerCreationDraftTests
    {
        public static CareerCreationDefinition Definition()=>CareerCreationDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_creation_v1.json"));
        public static CareerCreationDraft Draft(ulong seed=71000)=>new CareerCreationDraft(Definition(),CareerStartDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_start_v1.json")),BiathlonRatingEngineTests.Definition(),CareerFoundationTests.Rules(),seed){Name="Alex North",Nation="NOR"};
        private static CareerCalendar Calendar(CareerCreationDraft draft)=>CareerDefinitions.ForNewProductionCareer(PhysiologyContractTests.DefinitionJson,draft.StartProfileId).Calendar;
        [Test] public void EveryTierAndPresetProducesAValidBoundedCandidate()
        {
            var policy=Definition();var draft=Draft();
            for(int tier=0;tier<policy.TierCount;tier++)for(int preset=0;preset<policy.PresetCount;preset++)
            {
                draft.SelectTier(policy.Tier(tier).Id);draft.SelectPreset(policy.Preset(preset).Id);draft.Validate();
                Assert.That(draft.Overall,Is.InRange(draft.Tier.MinimumOvr,draft.Tier.MaximumOvr));Assert.That(draft.BudgetRemaining,Is.Zero,"Every authored preset must be a complete, equal-budget allocation.");Assert.That(draft.BudgetSpent,Is.EqualTo(draft.Tier.Budget));
                var save=draft.Confirm("career_created",Calendar(draft),seed=>new SeededRandomSource(seed));
                Assert.That(save.StartProfileId,Is.EqualTo(draft.StartProfileId));Assert.That(save.Athlete(0).Age,Is.EqualTo(draft.Age));
                for(int i=0;i<13;i++)Assert.That(save.Athlete(0).Stat((CareerStat)i),Is.EqualTo(draft.Stat((CareerStat)i)));
                Assert.That(BiathlonRatingEngine.Calculate(save.Athlete(0),BiathlonRatingEngineTests.Definition()).Overall,Is.EqualTo(draft.Overall));
            }
        }
        [Test] public void PresetAndTierSwitchingNeverAccumulatesPointsOrConsumesDraftSeed()
        {
            var draft=Draft();draft.SelectPreset("skier");int[] before=new int[13];for(int i=0;i<13;i++)before[i]=draft.Stat((CareerStat)i);
            int cost=draft.BudgetSpent;
            for(int repeat=0;repeat<30;repeat++)
            {
                draft.SelectTier("world_cup_debut");draft.SelectPreset("shooter");draft.SelectTier("junior_novice");draft.SelectPreset("skier");
                Assert.That(draft.BudgetSpent,Is.EqualTo(cost));for(int i=0;i<13;i++)Assert.That(draft.Stat((CareerStat)i),Is.EqualTo(before[i]));
                Assert.That(draft.DraftSeed,Is.EqualTo(71000));
            }
        }
        [Test] public void InvalidAllocationIsAtomicAndRepeatedRefundsCannotCreateBudget()
        {
            var draft=Draft();draft.ResetAllocation();draft.SetAllocation(CareerStat.Endurance,4);int cost=draft.BudgetSpent;
            Assert.Catch(()=>draft.SetAllocation(CareerStat.Endurance,10));Assert.That(draft.Allocation(CareerStat.Endurance),Is.EqualTo(4));Assert.That(draft.BudgetSpent,Is.EqualTo(cost));
            Assert.Catch(()=>draft.SetAllocation(CareerStat.Endurance,-9));Assert.Catch(()=>draft.SetAllocation((CareerStat)13,0));
            for(int i=0;i<20;i++){draft.SetAllocation(CareerStat.Strength,-4);draft.SetAllocation(CareerStat.Strength,0);}
            Assert.That(draft.BudgetSpent,Is.EqualTo(cost));draft.ResetAllocation();Assert.That(draft.BudgetSpent,Is.Zero);
            for(int stat=0;stat<13;stat++)Assert.That(draft.Allocation((CareerStat)stat),Is.Zero);
            Assert.Catch(()=>draft.SetAllocation(CareerStat.Strength,int.MaxValue));
        }
        [Test] public void OriginUsesSameBudgetAndRespectsAgeWithoutPromisingPotential()
        {
            var draft=Draft();Assert.Catch(()=>draft.SelectOrigin("late_debut"));
            draft.SelectOrigin("sport_school");draft.Validate();int cost=draft.BudgetSpent;draft.SelectPreset("technician");Assert.That(draft.BudgetSpent,Is.EqualTo(cost));
            draft.SelectTier("senior_debut");draft.SetAge(27);Assert.That(draft.OriginStoryId,Is.EqualTo("newcomer"));draft.SelectOrigin("late_debut");
            Assert.Catch(()=>draft.SelectOrigin("young_shooter"));Assert.That(draft.OriginStoryId,Is.EqualTo("late_debut"));
            draft.SetAge(21);Assert.That(draft.OriginStoryId,Is.EqualTo("newcomer"));Assert.Catch(()=>draft.SetAge(20));
            draft.SelectTier("junior_novice");Assert.That(draft.Age,Is.EqualTo(18),"An incompatible age resets to the selected tier's authored default.");
        }
        [Test] public void ConfirmationValidatesIdentityAndIsDeterministicAndDetached()
        {
            var draft=Draft();draft.Name=" ";Assert.Catch(()=>draft.Confirm("career_created",Calendar(draft),s=>new SeededRandomSource(s)));
            draft.Name=" Alex North ";draft.Nation="INVALID";Assert.Catch(()=>draft.Validate());draft.Nation="FRA";
            int calls=0;Biathlon.Domain.IRandomSource Restore(ulong s){calls++;return new SeededRandomSource(s);}
            for(int i=0;i<20;i++){_=draft.Overall;draft.SelectPreset("balanced");}Assert.That(calls,Is.Zero);
            var a=draft.Confirm("career_created",Calendar(draft),Restore);var b=draft.Confirm("career_created",Calendar(draft),Restore);
            Assert.That(calls,Is.EqualTo(2));Assert.That(a.ToJson(),Is.EqualTo(b.ToJson()));Assert.That(a.Athlete(0).Name,Is.EqualTo("Alex North"));Assert.That(a.Athlete(0).Nation,Is.EqualTo("FRA"));
            int ability=a.Athlete(0).Stat(CareerStat.Endurance);draft.SetAllocation(CareerStat.Endurance,0);Assert.That(a.Athlete(0).Stat(CareerStat.Endurance),Is.EqualTo(ability));Assert.That(draft.Stat(CareerStat.Endurance),Is.Not.EqualTo(ability));
            Assert.Catch(()=>draft.Confirm("career_created",CareerFoundationTests.Calendar(),Restore));
        }
        [Test] public void StartingAllocationAndAppearanceCannotIncreaseHiddenPotential()
        {
            var draft=Draft(71000);draft.SelectTier("world_cup_debut");var calendar=Calendar(draft);
            var baseline=draft.Confirm("career_potential",calendar,s=>new SeededRandomSource(s));int potential=baseline.Athlete(0).Potential;
            Assert.That(potential,Is.LessThan(draft.Stat(CareerStat.Endurance)),"This fixed seed reproduces a starting ability above the independent legacy ceiling.");
            var policy=Definition();
            for(int i=0;i<policy.PresetCount;i++)
            {
                draft.SelectPreset(policy.Preset(i).Id);draft.SetAppearance(AppearanceProfile.Create("round","brown_warm","athletic"));
                var candidate=draft.Confirm("career_potential",calendar,s=>new SeededRandomSource(s));
                Assert.That(candidate.Athlete(0).Potential,Is.EqualTo(potential));Assert.That(candidate.RandomState,Is.EqualTo(baseline.RandomState));
            }
        }
        [Test] public void CreationDefinitionRejectsDuplicateUnboundedAndMissingChoices()
        {
            string json=Definition().ToJson();Assert.That(CareerCreationDefinition.FromJson(json).ToJson(),Is.EqualTo(json));
            Assert.Catch(()=>CareerCreationDefinition.FromJson(json.Replace("\"stat_step\":1000","\"stat_step\":1001")));
            Assert.Catch(()=>CareerCreationDefinition.FromJson(json.Replace("\"id\":\"skier\"","\"id\":\"balanced\"")));
            Assert.Catch(()=>CareerCreationDefinition.FromJson(json.Replace("\"refund_per_step\":1","\"refund_per_step\":2")));
            Assert.Catch(()=>CareerCreationDefinition.FromJson(json.Replace("\"id\":\"senior_debut\"","\"id\":\"junior_novice\"")));
            Assert.Catch(()=>Draft().SelectTier("missing"));
        }
        [Test] public void ResetKeepsAZeroManualAllocationWhileEveryRepeatedPresetRestoresItsCompleteBudget()
        {
            var draft=Draft();var policy=Definition();
            for(int tier=0;tier<policy.TierCount;tier++)for(int preset=0;preset<policy.PresetCount;preset++)
            {
                draft.SelectTier(policy.Tier(tier).Id);draft.ResetAllocation();Assert.That(draft.BudgetRemaining,Is.EqualTo(24));
                for(int stat=0;stat<13;stat++)Assert.That(draft.Allocation((CareerStat)stat),Is.Zero);
                for(int repeat=0;repeat<10;repeat++)
                {
                    draft.SelectPreset(policy.Preset(preset).Id);Assert.That(draft.BudgetSpent,Is.EqualTo(24));
                    for(int stat=0;stat<13;stat++)Assert.That(draft.Allocation((CareerStat)stat),Is.EqualTo(policy.Preset(preset).Allocation((CareerStat)stat)));
                    Assert.That(draft.DraftSeed,Is.EqualTo(71000));
                }
            }
        }
        [Test] public void PresetHandlingCanChangeCadenceButCannotRejectAGeometricallyValidPlayerRay()
        {
            var draft=Draft();var policy=Definition();var targets=TargetGeometryTests.Definition();var stance=ShootingStanceTests.Definition().Prone;
            for(int tier=0;tier<policy.TierCount;tier++)for(int preset=0;preset<policy.PresetCount;preset++)
            {
                draft.SelectTier(policy.Tier(tier).Id);draft.SelectPreset(policy.Preset(preset).Id);
                int skill=(int)Math.Round(draft.Stat(CareerStat.ShootingSpeed)/(decimal)CareerUnits.StatScale,0,MidpointRounding.ToEven);
                var handling=new ShootingHandlingProfile(ShootingHandlingTests.Definition(),ShootingHandlingTests.Equipment(),skill);
                var shots=new ShotResolutionSystem(ShootingStateTests.Definition(),targets,ShootingWindTests.Definition(),stance,new AthleteId("created_geometric_player"),"range",20,new RaceEventLog(100),handling:handling);
                shots.Step(new SimulationTick(1),ShootingAction.EnterRange);shots.Step(new SimulationTick(2),ShootingAction.AssignLane,new ShootingLaneAssignment("range","lane",1));
                for(int tick=3;tick<=500&&shots.Phase!=ShootingPhase.Complete;tick++)
                {int target=Math.Min(4,shots.ShotsFired);shots.Step(new SimulationTick(tick),ShootingAction.Trigger,ray:new ShotRay(0,0,0,targets.CenterX(target),0,targets.DistanceMeters));}
                Assert.That(shots.ShotsFired,Is.EqualTo(5));Assert.That(shots.ClosedMask,Is.EqualTo(31),policy.Preset(preset).Id+" introduced a non-geometric miss.");
            }
        }
    }
}
