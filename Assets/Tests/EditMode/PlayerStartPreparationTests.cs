using System;
using Biathlon.Domain;
using Biathlon.Presentation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class PlayerStartPreparationTests
    {
        private static RaceStartDefinition Definition()=>RaceStartDefinition.FromJson(PhysiologyContractTests.DefinitionJson("race_start_v2.json"));
        [Test]public void ImmediateStartHasThreeSecondsOfLocalPreparationAndNoRaceClockAdvance()
        {
            var p=new PlayerStartPreparation(Definition(),20,1);
            Assert.That(p.HoldsRace,Is.True);Assert.That(p.CountdownRemaining(0),Is.EqualTo(3));
            p.AdvanceHold(1);Assert.That(p.CountdownRemaining(0),Is.EqualTo(2));
            p.AdvanceHold(1);Assert.That(p.CountdownRemaining(0),Is.EqualTo(1));
            p.AdvanceHold(1);Assert.That(p.HoldsRace,Is.False);Assert.That(p.Remaining(0),Is.Zero);Assert.That(p.CanSkip(0),Is.False);
        }
        [TestCase(601,541)][TestCase(6001,5941)][TestCase(66001,65941)]
        public void DelayedPersonalCountdownUsesScheduledTickAndSkipStopsExactlyThreeSecondsBeforeIt(long start,long target)
        {
            var p=new PlayerStartPreparation(Definition(),20,start);
            Assert.That(p.HoldsRace,Is.False);Assert.That(p.CountdownRemaining(0),Is.Zero);Assert.That(p.CanSkip(0),Is.True);
            Assert.That(p.SkipTargetTick,Is.EqualTo(target));Assert.That(p.CanSkip(target-1),Is.True);
            Assert.That(p.CanSkip(target),Is.False);Assert.That(p.CountdownRemaining(target),Is.EqualTo(3));
            Assert.That(p.CountdownRemaining(target+20),Is.EqualTo(2));Assert.That(p.CountdownRemaining(target+40),Is.EqualTo(1));
            Assert.That(p.CountdownRemaining(start),Is.Zero);
        }
        [TestCase(2)][TestCase(21)][TestCase(60)]
        public void EarlyPursuitDepartureStillAllowsFullThreeSecondsWithoutChangingItsRegisteredGap(long start)
        {
            var p=new PlayerStartPreparation(Definition(),20,start);
            Assert.That(p.CountdownRemaining(0),Is.EqualTo(3).Within(1e-12));Assert.That(p.StartTick,Is.EqualTo(start));
            Assert.That(p.CanSkip(0),Is.False);p.AdvanceHold(3);
            Assert.That(p.HoldsRace,Is.False);Assert.That(p.Remaining(start),Is.Zero);
        }
        [Test]public void DefinitionAndClockRejectInvalidBudgetsAndNonRepresentableCountdowns()
        {
            string json=PhysiologyContractTests.DefinitionJson("race_start_v2.json");
            foreach(var pair in new[]{("skip_maximum_steps_per_frame","0"),("skip_work_budget_ms","0"),("countdown_seconds","0"),("schema_version","3")})
                Assert.Catch(()=>RaceStartDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,pair.Item1,pair.Item2)));
            var d=RaceStartDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(json,"countdown_seconds","3.01"));
            Assert.Throws<ArgumentException>(()=>new PlayerStartPreparation(d,20,601));
            Assert.Throws<ArgumentOutOfRangeException>(()=>new PlayerStartPreparation(Definition(),0,1));
        }
        [Test]public void RepeatedObservationsAllocateNoManagedObjects()
        {
            var p=new PlayerStartPreparation(Definition(),20,601);
            ObserveAll(p);ObserveAll(p);
            long bytes=MeasureObservations(p);
            Assert.That(bytes,Is.Zero);Assert.That(observed,Is.GreaterThan(0));
        }
        private static double observed;
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static void ObserveAll(PlayerStartPreparation p)
        {double sum=0;for(int i=0;i<10000;i++){sum+=p.Remaining(i%601)+p.CountdownRemaining(i%601);if(p.CanSkip(i%601))sum++;}observed=sum;}
        // Keep NUnit's boxed assertion values outside the measured method on CoreCLR.
        [System.Runtime.CompilerServices.MethodImpl(System.Runtime.CompilerServices.MethodImplOptions.NoInlining)]
        private static long MeasureObservations(PlayerStartPreparation p)
        {long before=GC.GetAllocatedBytesForCurrentThread();ObserveAll(p);return GC.GetAllocatedBytesForCurrentThread()-before;}
    }
}
