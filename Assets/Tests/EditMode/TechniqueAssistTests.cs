using System;
using Biathlon.Domain;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class TechniqueAssistTests
    {
        private static PlayerCommand Command(string technique = null) => new PlayerCommand(new SimulationTick(1), new AthleteId("a"), 800, 0, techniqueId: technique);
        [Test]
        public void AutomaticUsesSharedBestAndManualKeepsExplicitCost()
        {
            var definition = SkiTechniqueTests.Definition(); var system = new TechniqueAssistSystem(definition);
            var sample = DraftingTests.Flat(); var stats = new SkiTechniqueStats(70, 70);
            var automatic = system.Evaluate(TechniqueAssistMode.Automatic, Command("skate_climb"), stats, sample, 0.2, 350);
            var manual = system.Evaluate(TechniqueAssistMode.Manual, Command("skate_climb"), stats, sample, 0.2, 350);
            Assert.That(automatic.RequestOverridden, Is.True); Assert.That(automatic.EfficiencyLoss, Is.Zero);
            Assert.That(manual.Applied.TechniqueId, Is.EqualTo("skate_climb")); Assert.That(manual.RequestOverridden, Is.False);
            Assert.That(manual.EfficiencyLoss, Is.GreaterThan(0)); Assert.That(manual.Applied.PropulsivePowerWatts, Is.LessThan(automatic.Applied.PropulsivePowerWatts));
            Assert.That(automatic.Applied, Is.EqualTo(new SkiTechniqueSystem(definition).Evaluate(stats, sample, 0.2, 350)));
        }
        [Test]
        public void InvalidModesMissingManualAndUnknownIdsReject()
        {
            var system = new TechniqueAssistSystem(SkiTechniqueTests.Definition()); var sample = DraftingTests.Flat();
            Assert.Catch(() => system.Evaluate(TechniqueAssistMode.Manual, Command(), default, sample, 0, 350));
            Assert.Catch(() => system.Evaluate((TechniqueAssistMode)99, Command(), default, sample, 0, 350));
            Assert.Catch(() => system.Evaluate(TechniqueAssistMode.Automatic, Command("unknown"), default, sample, 0, 350));
            Assert.Catch(() => system.Evaluate(TechniqueAssistMode.Automatic, default, default, sample, 0, 350));
        }
        [Test]
        public void SelectionHasNoPerTickAllocations()
        {
            var system = new TechniqueAssistSystem(SkiTechniqueTests.Definition()); var sample = DraftingTests.Flat(); var command = Command();
            system.Evaluate(TechniqueAssistMode.Automatic, command, default, sample, 0, 350);
            double sum = 0; long before = GC.GetAllocatedBytesForCurrentThread();
            for (int i = 0; i < 10000; i++) sum += system.Evaluate(TechniqueAssistMode.Automatic, command, default, sample, 0, 350).Applied.PropulsivePowerWatts;
            long allocated = GC.GetAllocatedBytesForCurrentThread() - before;
            Assert.That(allocated, Is.Zero); Assert.That(sum, Is.GreaterThan(0));
        }
    }
}
