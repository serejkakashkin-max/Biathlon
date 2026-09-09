using System;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TacticalIntentTests
    {
        [Test] public void AllEightIntentsRoundTripWithExplicitTargetAndLifetime()
        {
            var athlete = new AthleteId("athlete"); var target = new AthleteId("rival");
            for (int i = 0; i < 8; i++)
            {
                var kind = (TacticalIntentKind)i; var decision = new TacticalIntentDecision("tactics_v1", athlete, kind,
                    new SimulationTick(10), new SimulationTick(15), TacticalIntentDecision.RequiresTarget(kind) ? target : default);
                Assert.That(TacticalIntentDecision.FromJson(decision.ToJson()), Is.EqualTo(decision));
                Assert.That(decision.IsCurrentAt(new SimulationTick(9)), Is.False);
                Assert.That(decision.IsCurrentAt(new SimulationTick(10)), Is.True);
                Assert.That(decision.IsCurrentAt(new SimulationTick(14)), Is.True);
                Assert.That(decision.IsCurrentAt(new SimulationTick(15)), Is.False);
                decision.ValidateFor(athlete, new SimulationTick(11));
                Assert.Catch(() => decision.ValidateFor(target, new SimulationTick(11)));
            }
        }
        [Test] public void InvalidTargetsLifetimesSchemasAndDefaultsAreRejected()
        {
            var athlete = new AthleteId("athlete"); var until = new SimulationTick(5);
            Assert.Catch(() => new TacticalIntentDecision("t", athlete, TacticalIntentKind.Draft, default, until));
            Assert.Catch(() => new TacticalIntentDecision("t", athlete, TacticalIntentKind.Draft, default, until, athlete));
            Assert.Catch(() => new TacticalIntentDecision("t", athlete, TacticalIntentKind.Hold, default, until, new AthleteId("other")));
            Assert.Catch(() => new TacticalIntentDecision("t", athlete, (TacticalIntentKind)8, default, until));
            Assert.Catch(() => new TacticalIntentDecision("t", athlete, TacticalIntentKind.Hold, until, until));
            var valid = new TacticalIntentDecision("t", athlete, TacticalIntentKind.Hold, default, until).ToJson();
            Assert.Catch(() => TacticalIntentDecision.FromJson(valid.Replace("\"schema_version\":1", "\"schema_version\":2")));
            Assert.Catch(() => TacticalIntentDecision.FromJson(valid.Replace(",\"target_id\":null", "")));
            Assert.That(default(TacticalIntentDecision).IsCurrentAt(default), Is.False);
            Assert.Catch(() => default(TacticalIntentDecision).ToJson());
        }
        [Test] public void DecisionCreationAndValidationDoNotAllocate()
        {
            var athlete = new AthleteId("athlete"); var until = new SimulationTick(1000);
            new TacticalIntentDecision("t", athlete, TacticalIntentKind.Hold, default, until).ValidateFor(athlete, default);
            long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 1000; i++)
                new TacticalIntentDecision("t", athlete, TacticalIntentKind.Hold, new SimulationTick(i), until).ValidateFor(athlete, new SimulationTick(i));
            Assert.That(GC.GetAllocatedBytesForCurrentThread() - before, Is.Zero);
        }
    }
}


