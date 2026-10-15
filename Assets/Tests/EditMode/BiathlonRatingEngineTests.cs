using System;
using System.Text.RegularExpressions;
using System.Runtime.CompilerServices;
using Biathlon.Career;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class BiathlonRatingEngineTests
    {
        public static BiathlonRatingDefinition Definition()=>BiathlonRatingDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_ratings_v1.json"));
        public static CareerAthlete WithStats(int[] stats)
        {
            string json=Regex.Replace(CareerFoundationTests.Create().ToJson(),"\"stats\":\\[[^\\]]*\\]","\"stats\":["+string.Join(",",stats)+"]");
            return CareerSave.FromJson(json).Athlete(0);
        }
        private static CareerAthlete Uniform(int value)
        {var stats=new int[CareerUnits.StatCount];for(int i=0;i<stats.Length;i++)stats[i]=value;return WithStats(stats);}

        [TestCase(0,1)] [TestCase(100000,99)] [TestCase(50000,50)] [TestCase(25000,26)] [TestCase(75000,74)]
        public void NormalizedGroupsShareBoundsAndHalfEvenRounding(int stat,int expected)
        {
            var a=Uniform(stat);var d=Definition();
            foreach(BiathlonRating group in Enum.GetValues(typeof(BiathlonRating)))
                Assert.That(BiathlonRatingEngine.Calculate(a,d,group),Is.EqualTo(expected),group.ToString());
            Assert.That(BiathlonRatingEngine.StatRating(stat),Is.EqualTo(expected));
        }
        [Test] public void EveryPermanentStatContributesWithAuthoredNormalizedWeights()
        {
            var d=Definition();int[] weights={14,7,6,4,8,8,3,5,7,12,12,4,10};
            for(int i=0;i<CareerUnits.StatCount;i++)
            {
                var stats=new int[CareerUnits.StatCount];stats[i]=100000;
                int expected=(int)Math.Round(1+98m*weights[i]/100,MidpointRounding.ToEven);
                Assert.That(BiathlonRatingEngine.Calculate(WithStats(stats),d).Overall,Is.EqualTo(expected),((CareerStat)i).ToString());
            }
        }
        [Test] public void RatingsDoNotMutateReadinessIdentityHistoryOrRandomCheckpoint()
        {
            var save=CareerFoundationTests.Create();string before=save.ToJson();var d=Definition();
            var first=BiathlonRatingEngine.Calculate(save.Athlete(0),d);
            var stats=new int[13];for(int i=0;i<13;i++)stats[i]=save.Athlete(0).Stat((CareerStat)i);
            foreach(BiathlonRating group in Enum.GetValues(typeof(BiathlonRating)))Assert.That(BiathlonRatingEngine.CalculateStats(stats,d,group),Is.EqualTo(BiathlonRatingEngine.Calculate(save.Athlete(0),d,group)));
            for(int i=0;i<1000;i++)Assert.That(BiathlonRatingEngine.Calculate(save.Athlete(0),d).Overall,Is.EqualTo(first.Overall));
            Assert.That(save.ToJson(),Is.EqualTo(before));
            var fatigued=CareerSave.FromJson(PhysiologyContractTests.ReplaceNumber(before,"fatigue","10000"));
            Assert.That(BiathlonRatingEngine.Calculate(fatigued.Athlete(0),d).Overall,Is.EqualTo(first.Overall));
            Assert.That(BiathlonRatingDefinition.FromJson(d.ToJson()).ToJson(),Is.EqualTo(d.ToJson()));
        }
        [Test] public void SpecialtyGroupsRespondOnlyToTheirSourceAttributes()
        {
            var d=Definition();var stats=new int[13];for(int i=0;i<8;i++)stats[i]=100000;
            var r=BiathlonRatingEngine.Calculate(WithStats(stats),d);
            Assert.That(r.Ski,Is.EqualTo(99));Assert.That(r.Shooting,Is.EqualTo(1));Assert.That(r.Mental,Is.EqualTo(1));
            stats[12]=100000;r=BiathlonRatingEngine.Calculate(WithStats(stats),d);
            Assert.That(r.Mental,Is.EqualTo(99));Assert.That(r.Shooting,Is.EqualTo(1));
        }
#if !UNITY_EDITOR
        // The engine-free CoreCLR test remains here; Unity's native GC.Alloc guard
        // lives in AthleteRatingAllocationTests in the engine-capable test assembly.
        [Test] public void WarmRatingProjectionAllocatesNoManagedMemory()
        {
            var a=CareerFoundationTests.Create().Athlete(0);var d=Definition();
            MeasureAllocations(a,d,10,false,out _);MeasureAllocations(a,d,10,true,out _);
            long allocated=MeasureAllocations(a,d,1000,false,out int sum);
            Assert.That(sum,Is.GreaterThan(0));Assert.That(allocated,Is.Zero);
            Assert.That(MeasureAllocations(a,d,1000,true,out _),Is.GreaterThanOrEqualTo(16000),"Positive control must detect real CoreCLR allocation.");
        }
        private static object allocationSink;
        // Keep assertion argument boxing outside the measured call on optimizing runtimes.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static long MeasureAllocations(CareerAthlete athlete,BiathlonRatingDefinition definition,int count,bool positiveControl,out int sum)
        {
            sum=0;long before=GC.GetAllocatedBytesForCurrentThread();
            for(int i=0;i<count;i++){sum+=BiathlonRatingEngine.Calculate(athlete,definition).Overall;if(positiveControl)allocationSink=new byte[16];}
            long delta=GC.GetAllocatedBytesForCurrentThread()-before;GC.KeepAlive(allocationSink);allocationSink=null;return delta;
        }
#endif
        [Test] public void InvalidWeightsAndGroupsAreRejected()
        {
            string json=Definition().ToJson();
            Assert.Catch(()=>BiathlonRatingDefinition.FromJson(json.Replace("\"stat\":1,","\"stat\":0,")));
            Assert.Catch(()=>BiathlonRatingDefinition.FromJson(json.Replace("\"overall\":14","\"overall\":0")));
            Assert.Catch(()=>BiathlonRatingDefinition.FromJson(json.Replace("\"ski\":14","\"ski\":-1")));
            Assert.Catch(()=>BiathlonRatingDefinition.FromJson(json.Replace("\"mental\":10","\"mental\":0")));
            Assert.Catch(()=>BiathlonRatingDefinition.FromJson(json.Replace("\"schema_version\":1","\"schema_version\":2")));
            Assert.Catch(()=>BiathlonRatingEngine.Calculate(Uniform(50000),Definition(),(BiathlonRating)6));
            Assert.Catch(()=>BiathlonRatingEngine.StatRating(100001));
            Assert.Catch(()=>BiathlonRatingEngine.CalculateStats(new int[12],Definition()));
            var invalid=new int[13];invalid[0]=-1;Assert.Catch(()=>BiathlonRatingEngine.CalculateStats(invalid,Definition()));
        }
    }
}
