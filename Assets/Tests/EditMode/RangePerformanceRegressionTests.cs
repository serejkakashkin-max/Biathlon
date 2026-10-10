using System;
using System.Diagnostics;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RangePerformanceRegressionTests
    {
        [Test] public void ThirtyAthletesClearTheFirstBoutWithoutAnAisleDeadlock()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);int complete=0;
            while(f.runner.Field.Tick.Value<12000&&complete<30)
            {
                f.runner.Step();complete=0;
                for(int i=0;i<30;i++)if(f.runner.Field.GetAthlete(i).Lifecycle.CompletedBouts>=1)complete++;
            }
            TestContext.WriteLine($"first_bouts={complete}/30 tick={f.runner.Field.Tick.Value}");
            Assert.That(complete,Is.EqualTo(30),"Approach changes must not trap competitors at their first mat.");
        }
        [Test] public void FirstRangeAndDepartureHaveBoundedSnapshotAllocation()
        {
            var f=ManualProductionFieldTests.Initial("mass_start",30);
            TestContext.WriteLine($"context_chars runner={f.runner.ContextJson.Length} world={f.stepper.World.ContextJson.Length}");
            TestContext.WriteLine($"field={f.runner.Field.ContextJson.Length} catalog={f.stepper.World.GetCatalog(0).ContextJson.Length} projection={f.stepper.World.GetCatalog(0).ProjectionGeometry.ContextJson.Length}");
            var timer=Stopwatch.StartNew();bool reached=false;
            for(int i=0;i<10000;i++)
            {
                f.runner.Step();
                if(i%1000==0)TestContext.WriteLine($"tick={i+1} ms={timer.Elapsed.TotalMilliseconds:F1} pieces={f.stepper.LastStep.Intervals}");
                if(f.runner.Field.GetAthlete(0).Lifecycle.CompletedBouts>0){reached=true;break;}
            }
            Assert.That(reached,Is.True,"Must exercise the first bout and departure, not just the start.");
            RacePresentationSnapshot.Capture(f.runner.Field);
            long before=GC.GetAllocatedBytesForCurrentThread();timer.Restart();
            for(int i=0;i<100;i++)RacePresentationSnapshot.Capture(f.runner.Field);
            long bytes=(GC.GetAllocatedBytesForCurrentThread()-before)/100;
            TestContext.WriteLine($"departure_snapshot_bytes={bytes} snapshot_ms={timer.Elapsed.TotalMilliseconds/100:F3}");
            Assert.That(bytes,Is.LessThan(100000),"A detached 30-athlete snapshot must not allocate growing track/context copies.");
            Assert.That(f.runner.ContextJson.Length,Is.LessThan(20000000),"Context identities must not duplicate hundreds of MB of nested geometry.");
        }
    }
}
