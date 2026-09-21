using System;
using Biathlon.Unity.Presentation;
using NUnit.Framework;
namespace Biathlon.PlayModeTests
{
    public class BenchmarkProfileProbeTests
    {
        [Test]
        public void DefaultRunIncludesCompleteQualificationMatrix()
        {
            var d=BenchmarkProfileDefinition.Load();var probe=BenchmarkProfileProbe.Parse(new[]{"-biathlonProfile"},d);
            Assert.That(probe.IsDiagnostic,Is.False);Assert.That(probe.Name,Is.EqualTo("baseline"));foreach(var c in d.cases)Assert.That(probe.Includes(c.id),Is.True);
        }
        [Test]
        public void SingleCaseAndEveryAblationAreExplicitDiagnostics()
        {
            var d=BenchmarkProfileDefinition.Load();
            foreach(string name in new[]{"baseline","forest","forest_shadows","surface_shadows","athlete_shadows","crowd_shadows"})
            {
                var probe=BenchmarkProfileProbe.Parse(new[]{"-biathlonProfileCase","high-athlete","-biathlonProfileProbe",name},d);
                Assert.That(probe.IsDiagnostic,Is.True);Assert.That(probe.Name,Is.EqualTo(name));Assert.That(probe.Includes("high-athlete"),Is.True);Assert.That(probe.Includes("low-athlete"),Is.False);
            }
            Assert.That(BenchmarkProfileProbe.Parse(new[]{"-biathlonProfileCase","high-athlete"},d).IsDiagnostic,Is.True);
        }
        [Test]
        public void AmbiguousOrMalformedDiagnosticArgumentsFailClosed()
        {
            var d=BenchmarkProfileDefinition.Load();
            foreach(var args in new[]{new[]{"-biathlonProfileProbe","forest"},new[]{"-biathlonProfileCase"},new[]{"-biathlonProfileCase","missing"},new[]{"-biathlonProfileCase","high-athlete","-biathlonProfileProbe","missing"},new[]{"-biathlonProfileCase","high-athlete","-biathlonProfileCase","low-athlete"}})
                Assert.Throws<ArgumentException>(()=>BenchmarkProfileProbe.Parse(args,d));
        }
    }
}
