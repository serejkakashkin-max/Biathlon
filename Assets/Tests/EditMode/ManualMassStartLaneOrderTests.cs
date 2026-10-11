using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public sealed class ManualMassStartLaneOrderTests
    {
        [Test] public void SecondBoutUsesPaidArrivalOrderInsteadOfKeepingBibLane()
        {
            const int count=6;
            var f=ManualProductionFieldTests.Initial("mass_start",count);bool checkedFirst=false;
            while(f.runner.Field.Tick.Value<14000)
            {
                f.runner.Step();var player=f.runner.Field.GetAthlete(0);
                if(!checkedFirst&&player.ActiveShooting!=null)
                {
                    Assert.That(f.runner.Field.GetRangeArrivalCount(0),Is.GreaterThan(0),"Signed movement must record its accepted range entry.");
                    Assert.That(player.AssignedShootingLane.Ordinal,Is.EqualTo(1));checkedFirst=true;
                }
                if(f.runner.Field.GetRangeArrivalCount(1)<count)continue;
                int changed=0;
                for(int i=0;i<count;i++)
                {
                    var a=f.runner.Field.GetAcceptedRangeArrival(i,1);int rank=1;
                    for(int j=0;j<count;j++)
                    {
                        var b=f.runner.Field.GetAcceptedRangeArrival(j,1);
                        if(b.Tick.Value<a.Tick.Value||b.Tick.Equals(a.Tick)&&b.Fraction<a.Fraction)rank++;
                    }
                    var athlete=f.runner.Field.GetAthlete(i);
                    Assert.That(athlete.AssignedShootingLane.Ordinal,Is.EqualTo(rank),"Actual paid arrival determines the later-bout mat.");
                    if(rank!=i+1)changed++;
                    TestContext.WriteLine($"bib={i+1} second_arrival={a.Tick.Value}:{a.Fraction} lane={rank}");
                }
                Assert.That(f.stepper.World.Tick,Is.EqualTo(f.runner.Field.Tick));
                Assert.That(changed,Is.GreaterThan(0),"Exercise arrival order that actually differs from initial bib order.");return;
            }
            Assert.Fail("Both real bouts must be reached through the owned movement path.");
        }
    }
}
