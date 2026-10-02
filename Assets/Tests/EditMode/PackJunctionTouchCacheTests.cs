using System;
using System.Reflection;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class PackJunctionTouchCacheTests
    {
        private const BindingFlags Private=BindingFlags.Instance|BindingFlags.NonPublic;
        private static FieldInfo Member(Type type,string name)
        {
            var member=type.GetField(name,Private|BindingFlags.Public);
            Assert.That(member,Is.Not.Null,name+" must remain available to the private differential reference.");return member;
        }
        private static void DirectQueries(PackSprintFieldSession field)
            =>Member(typeof(PackSprintFieldSession),"bypassJunctionTouchCache").SetValue(field,true);
        private static byte[][] Caches(PackSprintFieldSession field)
        {
            var result=new byte[field.Count*2][];int next=0;
            foreach(string name in new[]{"traces","stationary"})
            {
                var traces=(Array)Member(typeof(PackSprintFieldSession),name).GetValue(field);
                foreach(var trace in traces)result[next++]=(byte[])Member(trace.GetType(),"JunctionTouches").GetValue(trace);
            }
            return result;
        }
        private static void Poison(byte[][] caches)
        {foreach(var cache in caches)for(int i=0;i<cache.Length;i++)cache[i]=2;}
        private static void AssertSame(PackSprintFieldSession actual,PackSprintFieldSession direct)
        {
            Assert.That(actual.Tick,Is.EqualTo(direct.Tick));Assert.That(actual.PrecisionFallbackTicks,Is.EqualTo(direct.PrecisionFallbackTicks));
            for(int i=0;i<actual.Count;i++)
            {
                Assert.That(actual.GetAthlete(i).LastFrame,Is.EqualTo(direct.GetAthlete(i).LastFrame));
                var point=actual.GetPhysicalPoint(i);var reference=direct.GetPhysicalPoint(i);
                Assert.That(point.LoopId,Is.EqualTo(reference.LoopId));Assert.That(point.LocalNanometers,Is.EqualTo(reference.LocalNanometers));
                Assert.That(point.LoopLengthNanometers,Is.EqualTo(reference.LoopLengthNanometers));Assert.That(point.Closed,Is.EqualTo(reference.Closed));
                Assert.That(actual.GetLateral(i),Is.EqualTo(direct.GetLateral(i)));Assert.That(actual.GetDraft(i),Is.EqualTo(direct.GetDraft(i)));
                Assert.That(actual.IsOnCourse(i),Is.EqualTo(direct.IsOnCourse(i)));Assert.That(actual.GetJunctionWaitSince(i),Is.EqualTo(direct.GetJunctionWaitSince(i)));
                Assert.That(actual.GetAssignedRangeLane(i),Is.EqualTo(direct.GetAssignedRangeLane(i)));
                Assert.That(actual.GetAthlete(i).RangePath?.LaneOrdinal,Is.EqualTo(direct.GetAthlete(i).RangePath?.LaneOrdinal));
                Assert.That(actual.IsWaitingForRangeCapacity(i),Is.EqualTo(direct.IsWaitingForRangeCapacity(i)));
                if(actual.UsesReusableRangeMats)for(int bout=0;bout<actual.Profile.BoutCount;bout++)
                    Assert.That(actual.RangeReservations.GetRecord(i,bout),Is.EqualTo(direct.RangeReservations.GetRecord(i,bout)));
            }
        }
        private static void AssertNewEvents(RaceEventLog actual,RaceEventLog direct,ref int compared)
        {
            Assert.That(actual.Count,Is.EqualTo(direct.Count));
            for(;compared<actual.Count;compared++)Assert.That(actual[compared],Is.EqualTo(direct[compared]));
        }
        private static PackSprintFieldSession Reusable(bool pursuit,RaceEventLog events)
        {
            var f=PhysicalRangeReservationsTests.Fixture(pursuit:pursuit);PursuitQualificationSnapshot qualification=null;
            if(pursuit)
            {
                var ids=PhysicalRangeReservationsTests.Roster(f);
                var source=new SprintResultBook(CompetitionProfileTests.TestProfile(),SprintResultsTests.Timing(),ids,20,new RaceEventLog(100));
                for(int i=0;i<ids.Length;i++)source.Record(SprintResultsTests.Finisher(ids[i].Value,1,200+2*i));
                qualification=PursuitQualificationSnapshot.Create("cache_qualification",source.Complete("cache_source"),f.Profile);
            }
            return new PackSprintFieldSession(new SimulationConfig(1,"cache_pack",20,123),f.Profile,
                new SkiModelDefinitions(PhysiologyContractTests.DefinitionJson),ShootingSessionTests.Model(),f.Track,f.Route,
                RaceSnowConditions.FromJson(SnowConditionTests.Conditions().ToJson().Replace("snow_test_packed","packed")),
                SprintResultsTests.Timing(),f.Entrants,new SimulationTick(1),"cache_result",OccupancyTests.Definition(),DraftingTests.Definition(),
                OvertakeTests.Definition(),f.Junctions,f.Connections,events,qualification:qualification,rangeLayout:f.Layout,
                reusableRangeMats:true,routeBoundJunctions:true);
        }
        private static void FillReusable(PackSprintFieldSession field,SprintAthleteInput[] commands,bool? trigger=null)
        {
            var tick=field.Tick.Next();
            for(int i=0;i<commands.Length;i++)
            {
                var id=field.GetAthlete(i).AthleteId;
                commands[i]=new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick,id,800),1,1,0),new WeaponAimCommand(tick,id,0,0),trigger??field.Tick.Value>=600);
            }
        }

        [Test] public void CacheStoresBothMembershipOutcomesAndMatchesEveryDirectTraceQuery()
        {
            var field=PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(),new RaceEventLog(5000));
            var commands=new SprintAthleteInput[field.Count];var lines=new double[field.Count];
            var cachedMethod=typeof(PackSprintFieldSession).GetMethod("Touches",Private);
            var directMethod=typeof(PackSprintFieldSession).GetMethod("TouchesCore",Private);
            Assert.That(cachedMethod,Is.Not.Null);Assert.That(directMethod,Is.Not.Null);
            int count=(int)Member(typeof(PackSprintFieldSession),"junctionCount").GetValue(field);bool positive=false,negative=false,tail=false;
            for(int tick=0;tick<400;tick++)
            {
                PackSprintFieldTests.Fill(field,commands);field.Step(commands,lines);
                foreach(string name in new[]{"traces","stationary"})foreach(var trace in (Array)Member(typeof(PackSprintFieldSession),name).GetValue(field))
                {
                    var cache=(byte[])Member(trace.GetType(),"JunctionTouches").GetValue(trace);Assert.That(cache.Length,Is.EqualTo(count));
                    foreach(string tailName in new[]{"OldTail","NewTail"})
                    {var value=Member(trace.GetType(),tailName).GetValue(trace);tail|=(bool)Member(value.GetType(),"Active").GetValue(value);}
                    for(int j=0;j<count;j++)
                    {
                        bool expected=(bool)directMethod.Invoke(field,new[]{trace,(object)j});
                        Assert.That((bool)cachedMethod.Invoke(field,new[]{trace,(object)j}),Is.EqualTo(expected));
                        Assert.That(cache[j],Is.EqualTo(expected?2:1));
                        Assert.That((bool)cachedMethod.Invoke(field,new[]{trace,(object)j}),Is.EqualTo(expected));positive|=expected;negative|=!expected;
                    }
                }
            }
            Assert.That(positive&&negative&&tail,Is.True,"The query proof must include both cache values and an incoming body footprint.");
        }

        [TestCase(false)][TestCase(true)]
        public void ReusableCompleteRacesMatchUncachedFramesReceiptsReservationsEventsAndPersistedReplay(bool pursuit)
        {
            var events=new RaceEventLog(50000);var directEvents=new RaceEventLog(50000);
            var cached=Reusable(pursuit,events);var direct=Reusable(pursuit,directEvents);DirectQueries(direct);
            Assert.That(cached.ContextJson,Is.EqualTo(direct.ContextJson));var caches=Caches(cached);
            var tape=new PackSprintRaceCommandTape("cached_membership_race",cached,100);
            var commands=new SprintAthleteInput[cached.Count];var lines=new double[cached.Count];int compared=0;bool denied=false,privateTail=false,released=false,penalty=false;
            while(!cached.IsComplete&&cached.Tick.Value<80000)
            {
                // A retained positive from an older trace must never influence a new preparation.
                Poison(caches);FillReusable(cached,commands);tape.StepAndRecord(cached,commands,lines);direct.Step(commands,lines);
                AssertSame(cached,direct);AssertNewEvents(events,directEvents,ref compared);
                for(int i=0;i<cached.Count;i++)
                {
                    denied|=cached.IsWaitingForRangeCapacity(i);penalty|=cached.GetPhysicalPoint(i).LoopId=="penalty";
                    if(cached.GetPhysicalPoint(i).LoopId=="exit_aisle")
                    {privateTail|=cached.GetAssignedRangeLane(i)>0;released|=cached.GetAssignedRangeLane(i)==0;}
                }
            }
            Assert.That(cached.IsComplete&&direct.IsComplete,Is.True);Assert.That(denied&&privateTail&&released&&penalty,Is.True);
            Assert.That(pursuit?cached.PursuitResult.ToJson():cached.Result.ToJson(),Is.EqualTo(pursuit?direct.PursuitResult.ToJson():direct.Result.ToJson()));
            var replayEvents=new RaceEventLog(50000);var replayField=Reusable(pursuit,replayEvents);DirectQueries(replayField);
            var replay=PackSprintRaceCommandTape.FromJson(tape.ToJson()).CreateReplay(replayField);
            while(replay.HasNext)replay.Step();AssertSame(cached,replayField);compared=0;AssertNewEvents(events,replayEvents,ref compared);
            Assert.That(pursuit?replayField.PursuitResult.ToJson():replayField.Result.ToJson(),Is.EqualTo(pursuit?cached.PursuitResult.ToJson():cached.Result.ToJson()));
        }

        [Test] public void OrderedPrivatePathsAndLateralNarrowingMatchDirectQueriesThroughEverySeamAndPenalty()
        {
            var events=new RaceEventLog(40000);var directEvents=new RaceEventLog(40000);
            var policy=MassStartRangePolicyDefinition.FromJson(PhysiologyContractTests.DefinitionJson("mass_start_range_policy_v1.json"));
            var cached=PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(gridColumns:2),events,policy);
            var direct=PhysicalRangePackTests.CreatePack(PhysicalRangePackTests.CreateFixture(gridColumns:2),directEvents,policy);DirectQueries(direct);
            var caches=Caches(cached);var commands=new SprintAthleteInput[cached.Count];var lines=new double[cached.Count];int compared=0;bool movedLaterally=false,penalty=false;
            while(!cached.IsComplete&&cached.Tick.Value<80000)
            {
                PackSprintFieldTests.Fill(cached,commands);
                for(int i=0;i<cached.Count;i++)
                {
                    var actor=cached.GetAthlete(i);long at=actor.RouteNanometers%(300*SkiState.NanometersPerMeter);
                    long center=(actor.RangePath.LaneOrdinal==1?5:25)*SkiState.NanometersPerMeter;
                    lines[i]=cached.GetPhysicalPoint(i).LoopId=="main"&&(at<center||at>=100*SkiState.NanometersPerMeter)?(i==0?-1:1):0;
                }
                Poison(caches);cached.Step(commands,lines);direct.Step(commands,lines);AssertSame(cached,direct);AssertNewEvents(events,directEvents,ref compared);
                for(int i=0;i<cached.Count;i++){movedLaterally|=Math.Abs(cached.GetLateral(i))>.6;penalty|=cached.GetPhysicalPoint(i).LoopId=="penalty";}
            }
            Assert.That(cached.IsComplete&&direct.IsComplete&&movedLaterally&&penalty,Is.True);
            Assert.That(cached.ContextJson,Is.EqualTo(direct.ContextJson));Assert.That(cached.MassStartResult.ToJson(),Is.EqualTo(direct.MassStartResult.ToJson()));
            for(int bout=0;bout<cached.Profile.BoutCount;bout++)for(int i=0;i<cached.Count;i++)
                Assert.That(cached.GetAcceptedRangeArrival(i,bout),Is.EqualTo(direct.GetAcceptedRangeArrival(i,bout)));
        }

        [Test] public void AuthoredCurvedStartReplacesStationaryCacheBeforeHandoffAndLateralSweeps()
        {
            var fixture=PhysicalStartContinuationTests.Fixture();var other=PhysicalStartContinuationTests.Fixture();
            var events=new RaceEventLog(10000);var directEvents=new RaceEventLog(10000);
            var cached=PhysicalStartContinuationTests.Create(fixture,events,PhysicalStartContinuationTests.Definition(fixture));
            var direct=PhysicalStartContinuationTests.Create(other,directEvents,PhysicalStartContinuationTests.Definition(other));DirectQueries(direct);
            var caches=Caches(cached);var commands=new SprintAthleteInput[cached.Count];var lines=new double[cached.Count];int compared=0;bool main=false;
            for(int tick=0;tick<500;tick++)
            {
                PackSprintFieldTests.Fill(cached,commands);
                for(int i=0;i<lines.Length;i++)lines[i]=cached.GetPhysicalPoint(i).LoopId=="authored_start"?fixture.Grid.Get(i).LateralNanometers/1e9:0;
                Poison(caches);cached.Step(commands,lines);direct.Step(commands,lines);AssertSame(cached,direct);AssertNewEvents(events,directEvents,ref compared);
                for(int i=0;i<cached.Count;i++)main|=cached.GetPhysicalPoint(i).LoopId=="main";
            }
            Assert.That(main,Is.True);Assert.That(cached.ContextJson,Is.EqualTo(direct.ContextJson));
        }

        [Test] public void WarmMemoUsesColdStorageAndRejectedBatchPreservesCommittedState()
        {
            var events=new RaceEventLog(30000);var field=Reusable(false,events);var caches=Caches(field);
            var commands=new SprintAthleteInput[field.Count];var lines=new double[field.Count];
            // Warm both shooting sessions and the capacity replan before measuring steady work;
            // first admission initializes shooting state and is outside the memo's warm scope.
            bool held=false;
            for(int tick=0;tick<3000&&!held;tick++)
            {
                FillReusable(field,commands,false);field.Step(commands,lines);
                held=field.IsWaitingForRangeCapacity(2)&&field.GetAthlete(0).CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting&&
                    field.GetAthlete(1).CurrentRangeMotion?.Phase==RangeTraversalPhase.Shooting;
            }
            Assert.That(held,Is.True);
            for(int tick=0;tick<20;tick++){FillReusable(field,commands,false);field.Step(commands,lines);}
            long before=GC.GetAllocatedBytesForCurrentThread();
            for(int tick=0;tick<400;tick++){FillReusable(field,commands,false);field.Step(commands,lines);}
            Assert.That(GC.GetAllocatedBytesForCurrentThread()-before,Is.Zero);
            var retained=Caches(field);for(int i=0;i<caches.Length;i++)Assert.That(retained[i],Is.SameAs(caches[i]));
            var snapshot=new SprintAthleteFrame[field.Count];for(int i=0;i<snapshot.Length;i++)snapshot[i]=field.GetAthlete(i).LastFrame;
            int count=events.Count;var oldTick=field.Tick;FillReusable(field,commands);lines[lines.Length-1]=double.NaN;
            Assert.Throws<ArgumentOutOfRangeException>(()=>field.Step(commands,lines));Assert.That(field.Tick,Is.EqualTo(oldTick));Assert.That(events.Count,Is.EqualTo(count));
            for(int i=0;i<snapshot.Length;i++)Assert.That(field.GetAthlete(i).LastFrame,Is.EqualTo(snapshot[i]));
        }
    }
}
