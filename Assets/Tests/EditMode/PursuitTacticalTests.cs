using System;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PursuitTacticalTests
    {
        private static PursuitTacticsDefinition Definition()=>PursuitTacticsDefinition.FromJson(PhysiologyContractTests.DefinitionJson("pursuit_tactics_provisional_v1.json"));
        [Test] public void ChaseDefenseCooperationAndLastRangeHaveDistinctBoundedPlans()
        {
            var planner=new PursuitTacticalPlanner(Definition());
            var leader=planner.Evaluate(0,1,5,0,false,0.5);
            var chase=planner.Evaluate(40,3,null,0,false,0.5);
            var group=planner.Evaluate(4,2,null,0.8,false,0.5);
            Assert.That(leader.Reason,Is.EqualTo(PursuitTacticalReason.Defend));Assert.That(leader.DefensePressure,Is.EqualTo(0.75));
            Assert.That(chase.Reason,Is.EqualTo(PursuitTacticalReason.Chase));Assert.That(chase.GapPressure,Is.EqualTo(1));
            Assert.That(group.Reason.HasFlag(PursuitTacticalReason.Cooperate),Is.True);
            var lastChase=planner.Evaluate(40,3,null,0,true,0.5);var lastLeader=planner.Evaluate(0,1,5,0,true,0.5);
            Assert.That(lastChase.ShootingUrgency,Is.GreaterThan(chase.ShootingUrgency));
            Assert.That(lastLeader.ShootingUrgency,Is.LessThan(leader.ShootingUrgency));
            var preferences=new[]{0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5};
            planner.ApplyPreferences(group,preferences);Assert.That(preferences[2],Is.GreaterThan(0.5));Assert.That(preferences[3],Is.GreaterThan(0.5));
            planner.ApplyPreferences(chase,preferences);Assert.That(preferences[5],Is.GreaterThan(0.5));
            for(int i=0;i<100;i++)planner.ApplyPreferences(chase,preferences);
            foreach(double value in preferences)Assert.That(value,Is.InRange(0,1));
            Assert.That(planner.Evaluate(null,null,null,0,false,0.5).Reason,Is.EqualTo(PursuitTacticalReason.None));
        }
        [Test] public void PreferenceChangesCannotEnableAnIneligibleAttackAndWarmPlanningDoesNotAllocate()
        {
            var planner=new PursuitTacticalPlanner(Definition());var candidates=TacticalUtilityTests.Candidates();
            candidates[5]=new TacticalCandidate(TacticalIntentKind.Attack,false);
            var utility=new TacticalUtilityEvaluator(TacticalUtilityTests.Definition());var features=new double[8];
            var preferences=new[]{0.5,0.5,0.5,0.5,0.5,0.5,0.5,0.5};var scores=new TacticalUtilityScore[8];
            var plan=planner.Evaluate(40,2,5,1,true,0.5);planner.ApplyPreferences(plan,preferences);
            var chosen=utility.Evaluate(new AthleteId("racer"),new SimulationTick(1),new SimulationTick(2),features,preferences,candidates,scores);
            Assert.That(chosen.Kind,Is.Not.EqualTo(TacticalIntentKind.Attack));Assert.That(scores[5].Eligible,Is.False);
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<10000;i++){plan=planner.Evaluate(40,2,5,1,true,0.5);planner.ApplyPreferences(plan,preferences);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
        }
        [Test] public void MalformedDefinitionsAndObservationsRejectWithoutPreferenceWrites()
        {
            var d=Definition();Assert.That(PursuitTacticsDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
            foreach(var pair in new[]{("schema_version","2"),("full_chase_gap_s","0"),("group_preference","2")})
                Assert.Catch(()=>PursuitTacticsDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(d.ToJson(),pair.Item1,pair.Item2)));
            var p=new PursuitTacticalPlanner(d);
            Assert.Catch(()=>p.Evaluate(double.NaN,1,null,0,false,0.5));Assert.Catch(()=>p.Evaluate(0,0,null,0,false,0.5));
            Assert.Catch(()=>p.Evaluate(0,1,-1,0,false,0.5));Assert.Catch(()=>p.Evaluate(0,1,null,2,false,0.5));
            var preferences=new[]{0.5,0.5,0.5,0.5,0.5,0.5,0.5,double.NaN};
            Assert.Catch(()=>p.ApplyPreferences(p.Evaluate(40,2,null,1,true,0.5),preferences));Assert.That(preferences[5],Is.EqualTo(0.5));
        }
    }
}
