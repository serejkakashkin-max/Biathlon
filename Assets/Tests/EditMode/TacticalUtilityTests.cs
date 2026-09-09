using System;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TacticalUtilityTests
    {
        public static TacticalUtilityDefinition Definition() => TacticalUtilityDefinition.FromJson(PhysiologyContractTests.DefinitionJson("tactical_utility_provisional_v1.json"));
        public static TacticalCandidate[] Candidates()
        {
            var result = new TacticalCandidate[8];
            for (int i = 0; i < 8; i++) { var kind = (TacticalIntentKind)i;
                result[i] = new TacticalCandidate(kind, true, TacticalIntentDecision.RequiresTarget(kind) ? new AthleteId("rival") : default); }
            return result;
        }
        private static readonly double[] Neutral = { 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5, 0.5 };
        [Test] public void ContributionsPersonalityAndEligibilityDetermineInspectableWinner()
        {
            var evaluator = new TacticalUtilityEvaluator(Definition()); var features = new double[8];
            var candidates = Candidates(); var scores = new TacticalUtilityScore[8]; var self = new AthleteId("self");
            TacticalIntentDecision Evaluate(double[] personality) => evaluator.Evaluate(self, default, new SimulationTick(5), features, personality, candidates, scores);
            Assert.That(Evaluate(Neutral).Kind, Is.EqualTo(TacticalIntentKind.Hold));
            features[(int)TacticalUtilityFeature.Reserve] = 1; features[(int)TacticalUtilityFeature.AttackOpportunity] = 0.25;
            Assert.That(Evaluate(Neutral).Kind, Is.EqualTo(TacticalIntentKind.Attack));
            Assert.That(scores[5].Features, Is.EqualTo(3)); Assert.That(scores[5].Personality, Is.Zero); Assert.That(scores[5].Total, Is.EqualTo(3));
            var cautious = (double[])Neutral.Clone(); cautious[5] = 0; cautious[0] = 1;
            Assert.That(Evaluate(cautious).Kind, Is.EqualTo(TacticalIntentKind.Hold)); // canonical tie with attack=1
            features[7] = 1; Assert.That(Evaluate(Neutral).Kind, Is.EqualTo(TacticalIntentKind.FinalSprint));
            candidates[6] = new TacticalCandidate(TacticalIntentKind.FinalSprint, false);
            Assert.That(Evaluate(Neutral).Kind, Is.EqualTo(TacticalIntentKind.Attack)); Assert.That(scores[6].Eligible, Is.False);
            Assert.That(scores[6].Total, Is.GreaterThan(scores[5].Total));
            features[0] = 1; features[3] = 1; features[7] = 0;
            Assert.That(Evaluate(Neutral).Kind, Is.EqualTo(TacticalIntentKind.PreRangeRecovery));
        }
        [Test] public void RequiredRowsAndMalformedInputFailBeforeScoresArePublished()
        {
            var definition = Definition(); string json = definition.ToJson();
            Assert.That(TacticalUtilityDefinition.FromJson(json).ToJson(), Is.EqualTo(json));
            Assert.Catch(() => TacticalUtilityDefinition.FromJson(json.Replace("\"schema_version\":1", "\"schema_version\":2")));
            Assert.Catch(() => TacticalUtilityDefinition.FromJson(json.Replace("\"intent\":7", "\"intent\":6")));
            Assert.Catch(() => TacticalUtilityDefinition.FromJson(json.Replace("\"personality_weight\":2", "\"personality_weight\":101")));
            var evaluator = new TacticalUtilityEvaluator(definition); var features = new double[8]; var candidates = Candidates(); var scores = new TacticalUtilityScore[8];
            void Evaluate() => evaluator.Evaluate(new AthleteId("self"), default, new SimulationTick(1), features, Neutral, candidates, scores);
            features[7] = double.NaN; Assert.Catch(Evaluate); Assert.That(scores[0].Eligible, Is.False);
            features[7] = 0; candidates[1] = new TacticalCandidate(TacticalIntentKind.Hold, false); Assert.Catch(Evaluate);
            candidates[1] = new TacticalCandidate(TacticalIntentKind.Hold, true);
            candidates[3] = new TacticalCandidate(TacticalIntentKind.Draft, true, new AthleteId("self")); Assert.Catch(Evaluate);
        }
        [Test] public void WarmEvaluationIsAllocationFreeAndStable()
        {
            var evaluator = new TacticalUtilityEvaluator(Definition()); var features = new double[8]; var candidates = Candidates();
            var scores = new TacticalUtilityScore[8]; var self = new AthleteId("self"); var until = new SimulationTick(1001);
            evaluator.Evaluate(self, default, until, features, Neutral, candidates, scores);
            long before = GC.GetAllocatedBytesForCurrentThread(); TacticalIntentDecision last = default;
            for (int i = 0; i < 1000; i++) last = evaluator.Evaluate(self, new SimulationTick(i), until, features, Neutral, candidates, scores);
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero); Assert.That(last.Kind, Is.EqualTo(TacticalIntentKind.Hold));
        }
    }
}
