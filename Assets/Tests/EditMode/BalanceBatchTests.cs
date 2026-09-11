using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using Biathlon.AI;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class BalanceBatchTests
    {
        public static BalanceBatchDefinition Suite()=>BalanceBatchDefinition.FromJson(PhysiologyContractTests.DefinitionJson("balance_pacing_smoke_v1.json"));
        [Test] public void RejectsInvalidSchemaBoundsPathsDuplicatesAndSeedOverflow()
        {
            var s=Suite();Assert.That(BalanceBatchDefinition.FromJson(s.ToJson()).RunCount,Is.EqualTo(12));
            foreach(var p in new[]{("schema_version","2"),("iterations","0"),("tick_frequency","0"),("effort","1001"),("maximum_ticks","0"),("seed","18446744073709551615")})
                Assert.Catch(()=>BalanceBatchDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(s.ToJson(),p.Item1,p.Item2)));
            Assert.Catch(()=>BalanceBatchDefinition.FromJson(s.ToJson().Replace("synthetic_flat_v1.json","../escape.json")));
            Assert.Catch(()=>BalanceBatchDefinition.FromJson(s.ToJson().Replace("candidate","aggressive")));
            Assert.Catch(()=>s.SeedAt(-1));Assert.Catch(()=>s.SeedAt(s.Iterations));
        }
        [Test] public void StatisticsUsePopulationVarianceInterpolatedQuantilesAndPairedDirection()
        {
            var d=new BalanceDistribution(new double[]{4,1,3,2});Assert.That(d.Mean,Is.EqualTo(2.5));Assert.That(d.Median,Is.EqualTo(2.5));
            Assert.That(d.StandardDeviation,Is.EqualTo(Math.Sqrt(1.25)));Assert.That(d.P05,Is.EqualTo(1.15).Within(1e-12));Assert.That(d.P95,Is.EqualTo(3.85).Within(1e-12));
            Assert.That(BalanceDistribution.Correlation(new double[]{1,2,3},new double[]{3,2,1}),Is.EqualTo(-1));
            Assert.That(BalanceDistribution.Correlation(new double[]{1,1},new double[]{2,3}),Is.Null);
            Assert.That(BalanceDistribution.PairedBenefit(110,100,10),Is.EqualTo(1));
            Assert.Catch(()=>new BalanceDistribution(new double[0]));Assert.Catch(()=>new BalanceDistribution(new[]{double.NaN}));
            Assert.Catch(()=>BalanceDistribution.PairedBenefit(1,2,0));
        }
        [Test] public void CompletePairedRaceMetricsRepeatAndAccountingCloses()
        {
            var suite=Suite();var track=SyntheticTrackTests.Generated("synthetic_flat_v1");
            string first=null;
            for(int repeat=0;repeat<2;repeat++)
            {
                var events=new BalanceEventAudit(5);var runner=new BalanceRaceRunner(suite,1,0,PhysiologyContractTests.DefinitionJson,track,events);
                var m=BalanceMeasurement.Run(runner,suite,1,0,events);
                Assert.That(m.Number("ski_seconds")+m.Number("range_seconds")+m.Number("penalty_seconds"),Is.EqualTo(m.Number("finish_seconds")).Within(1e-8));
                Assert.That(events.Shots,Is.EqualTo(10));Assert.That(events.HitsProne+events.HitsStanding+events.Penalties,Is.EqualTo(10));
                Assert.That(m.Number("anaerobic_min"),Is.InRange(0,1));Assert.That(m.Number("fatigue_finish"),Is.InRange(0,1));
                Assert.That(m.Values["draft_benefit_seconds"],Is.Null);Assert.That(runner.Field.GetAthlete(0).Lifecycle.CompletedLaps,Is.EqualTo(3));
                string json=m.ToJson();if(first!=null)Assert.That(json,Is.EqualTo(first));first=json;
            }
            using(var sha=SHA256.Create())TestContext.Out.WriteLine("Balance candidate metrics SHA256="+BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(first))).Replace("-","").ToLowerInvariant());
            var culture=CultureInfo.CurrentCulture;try{CultureInfo.CurrentCulture=new CultureInfo("fr-FR");var m=new BalanceMetrics();m.Values["x"]=1.5;Assert.That(m.ToJson(),Is.EqualTo("{\"x\":1.500000000}"));m.Values["x"]=double.NaN;Assert.Catch(()=>m.ToJson());}finally{CultureInfo.CurrentCulture=culture;}
        }
        [Test] public void RecoveryOnlyStartsInsideConfiguredDistanceAndTickBudgetFails()
        {
            var suite=Suite();var runner=new BalanceRaceRunner(suite,1,0,PhysiologyContractTests.DefinitionJson,SyntheticTrackTests.Generated("synthetic_flat_v1"),new BalanceEventAudit(5));
            runner.Step();Assert.That(runner.LastEffort,Is.EqualTo(suite.Get(1).Effort));
            long mat=runner.Field.Track.Range.FiringLine.Distance.Millimeters*1000000;
            while(runner.Field.GetAthlete(0).ActiveShooting==null)
            {
                long before=runner.Field.GetAthlete(0).RouteNanometers;runner.Step();
                Assert.That(runner.LastEffort,Is.EqualTo(mat-before<=suite.Get(1).RecoveryDistance*1e9?suite.Get(1).RecoveryEffort:suite.Get(1).Effort));
            }
            var bounded=BalanceBatchDefinition.FromJson(PhysiologyContractTests.ReplaceNumber(suite.ToJson(),"maximum_ticks","1"));
            var b=new BalanceRaceRunner(bounded,0,0,PhysiologyContractTests.DefinitionJson,SyntheticTrackTests.Generated("synthetic_flat_v1"),new BalanceEventAudit(5));b.Step();Assert.Throws<InvalidOperationException>(()=>b.Step());
        }
    }
}
