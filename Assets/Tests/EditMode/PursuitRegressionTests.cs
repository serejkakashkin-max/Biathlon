using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PursuitRegressionTests
    {
#if !UNITY_EDITOR
        [Test, Timeout(300000)] public void RepeatedPursuitMatrixKeepsStartsBodiesPenaltiesAndResultsConsistent() => Matrix();
#else
        [UnityEngine.TestTools.UnityTest, Timeout(300000)]
        public System.Collections.IEnumerator RepeatedPursuitMatrixKeepsStartsBodiesPenaltiesAndResultsConsistent()
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var token=cancellation.Token;var work=System.Threading.Tasks.Task.Run(()=>Matrix(token),token);
                try {while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}
                finally {cancellation.Cancel();}
            }
        }
#endif
        private static PursuitQualificationSnapshot Qualification(int count,int gapSeconds)
        {
            var roster=new AthleteId[count];for(int i=0;i<count;i++)roster[i]=new AthleteId("racer_"+i);
            var book=new SprintResultBook(CompetitionProfileTests.TestProfile(),SprintResultsTests.Timing(),roster,20,new RaceEventLog(100));
            for(int i=0;i<count;i++)book.Record(SprintResultsTests.Finisher(roster[i].Value,1+600L*i,200+20L*gapSeconds*i));
            return PursuitQualificationSnapshot.Create("matrix_"+count+"_"+gapSeconds,book.Complete("source_"+count+"_"+gapSeconds),PursuitQualificationTests.Profile());
        }
        private static void Matrix(System.Threading.CancellationToken cancellation=default)
        {
            Run(3,0,123,"expert",cancellation);
            Run(6,2,456,"developing",cancellation);
            Run(6,30,789,"expert",cancellation);
        }
        private static void Run(int count,int gap,ulong seed,string difficulty,System.Threading.CancellationToken cancellation)
        {
            var q=Qualification(count,gap);var la=new RaceEventLog(150000);var lb=new RaceEventLog(150000);
            var a=PursuitFieldTests.Create(q,la,new AiDecisionCounts(),seed:seed,difficulty:difficulty);
            var b=PursuitFieldTests.Create(q,lb,new AiDecisionCounts(),seed:seed,difficulty:difficulty);
            var body=OccupancyTests.Definition();var previous=new long[count];var stalled=new int[count];var wasSkiing=new bool[count];
            long maximumStall=0;int penalties=0,starts=0,shots=0,passes=0;
            while(!a.Field.IsComplete && a.Field.Tick.Value<60000)
            {
                cancellation.ThrowIfCancellationRequested();
                for(int i=0;i<count;i++) {var actor=a.Field.GetAthlete(i);previous[i]=actor.Motion.DistanceNanometers;wasSkiing[i]=a.Field.IsOnCourse(i)&&actor.ActiveShooting==null;}
                a.Step();b.Step();
                for(int i=0;i<count;i++)
                {
                    var actor=a.Field.GetAthlete(i);Assert.That(actor.LastFrame,Is.EqualTo(b.Field.GetAthlete(i).LastFrame));
                    if(wasSkiing[i] && previous[i]==actor.Motion.DistanceNanometers)stalled[i]++;else stalled[i]=0;
                    maximumStall=Math.Max(maximumStall,stalled[i]);Assert.That(stalled[i],Is.LessThanOrEqualTo(2400));
                    if(!a.Field.IsOnCourse(i))continue;
                    var point=a.Field.GetPhysicalPoint(i);
                    for(int j=0;j<i;j++)if(a.Field.IsOnCourse(j))
                    {
                        var other=a.Field.GetPhysicalPoint(j);if(point.LoopId!=other.LoopId)continue;
                        long distance=Math.Abs(point.LocalNanometers-other.LocalNanometers);
                        if(point.Closed)distance=Math.Min(distance,point.LoopLengthNanometers-distance);
                        Assert.That(distance/1e9>=body.LongitudinalSeparation || Math.Abs(a.Field.GetLateral(i)-a.Field.GetLateral(j))>=body.LateralSeparation,Is.True,"Physical bodies overlap.");
                    }
                }
            }
            Assert.That(a.Field.IsComplete && b.Field.IsComplete,Is.True);Assert.That(a.Field.PrecisionFallbackTicks,Is.Zero);
            Assert.That(a.ContextJson,Is.EqualTo(b.ContextJson));Assert.That(la.Count,Is.EqualTo(lb.Count));
            for(int i=0;i<la.Count;i++)
            {
                var e=la[i];Assert.That(e,Is.EqualTo(lb[i]));
                if(e.EventType==RaceEventType.ScheduledStartDue) {starts++;int slot=e.Payload-1;Assert.That(e.Tick.Value,Is.EqualTo(1+q.Get(slot).StartGapUnits*20/q.GapUnitsPerSecond));}
                if(e.EventType==RaceEventType.PenaltyLoopCompleted)penalties++;
                if(e.EventType==RaceEventType.ShotAccepted)shots++;
                if(e.EventType==RaceEventType.OvertakeCompleted)passes++;
            }
            Assert.That(starts,Is.EqualTo(count));Assert.That(shots,Is.EqualTo(count*20));int expectedPenalties=0;
            for(int i=0;i<count;i++)
            {
                var actor=a.Field.GetAthlete(i);expectedPenalties+=actor.TotalPenaltyLoops;
                Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(4));Assert.That(actor.Lifecycle.CompletedLaps,Is.EqualTo(5));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo((5000L+actor.TotalPenaltyLoops*150L)*SkiState.NanometersPerMeter));
                Assert.That(actor.Lifecycle.StartTick.Value,Is.EqualTo(1+20L*gap*i));
            }
            Assert.That(penalties,Is.EqualTo(expectedPenalties));Assert.That(penalties,Is.GreaterThan(0));
            if(count==6 && gap==2) Assert.That(passes,Is.GreaterThan(0),"Dense regression must exercise an actual completed overtake.");
            var result=a.Field.PursuitResult;Assert.That(result.ToJson(),Is.EqualTo(b.Field.PursuitResult.ToJson()));
            Assert.That(PursuitResultSnapshot.FromJson(result.ToJson(),q,20,new SimulationTick(1)).ToJson(),Is.EqualTo(result.ToJson()));
            for(int i=1;i<count;i++)Assert.That(result.Get(i-1).GlobalFinish(20).CompareTo(result.Get(i).GlobalFinish(20)),Is.LessThanOrEqualTo(0));
            using(var sha=System.Security.Cryptography.SHA256.Create())
                TestContext.Out.WriteLine($"pursuit count={count}; gap={gap}; seed={seed}; difficulty={difficulty}; ticks={a.Field.Tick.Value}; max_stall={maximumStall}; penalties={penalties}; passes={passes}; result="+
                    BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(result.ToJson()))).Replace("-","").ToLowerInvariant());
        }
    }
}
