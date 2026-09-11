using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;
namespace Biathlon.Tests
{
    public class ChampionshipHarnessTests
    {
        private static string Read(string name)=>PhysiologyContractTests.DefinitionJson(name);
        private static string Fixture(string name)=>PhysiologyContractTests.ProjectFile("tests/fixtures/tracks/"+name);
#if !UNITY_EDITOR
        [Test,Timeout(1200000)] public void ThirtyAthleteSprintPursuitChainAndIndependentMassStart()=>Run();
#else
        [UnityEngine.TestTools.UnityTest,Timeout(1200000)]
        public System.Collections.IEnumerator ThirtyAthleteSprintPursuitChainAndIndependentMassStart()
        {
            using(var cancellation=new System.Threading.CancellationTokenSource())
            {
                var work=System.Threading.Tasks.Task.Run(()=>Run(cancellation.Token),cancellation.Token);
                try{while(!work.IsCompleted)yield return null;work.GetAwaiter().GetResult();}finally{cancellation.Cancel();}
            }
        }
#endif
        private static void Run(System.Threading.CancellationToken cancellation=default)
        {
            var massEvents=new RaceEventLog(300000);
            var mass=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.MassStart,massEvents,new AiDecisionCounts());
            var sprintEvents=new RaceEventLog(300000);
            var sprint=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Sprint,sprintEvents,new AiDecisionCounts());
            Complete(sprint,sprintEvents,"sprint",cancellation);
            var source=SprintResultSnapshot.FromJson(sprint.Field.Result.ToJson());
            var profile=PursuitQualificationTests.Profile();var q=PursuitQualificationSnapshot.Create("championship_qualification_v1",source,profile);
            q=PursuitQualificationSnapshot.FromJson(q.ToJson(),source.Id,profile);
            var pursuitEvents=new RaceEventLog(300000);
            var pursuit=ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Pursuit,pursuitEvents,new AiDecisionCounts(),q);
            Complete(pursuit,pursuitEvents,"pursuit",cancellation);
            Assert.That(PursuitResultSnapshot.FromJson(pursuit.Field.PursuitResult.ToJson(),q,20,new SimulationTick(1)).Count,Is.EqualTo(30));
            for(int i=0;i<30;i++)
            {
                Assert.That(pursuit.Field.GetAthlete(i).AthleteId.Value,Is.EqualTo(q.Get(i).AthleteId));
                Assert.That((pursuit.Field.GetAthlete(i).ScheduledStart.Value-1)*q.GapUnitsPerSecond,Is.EqualTo(q.Get(i).StartGapUnits*20));
                Assert.That(mass.Field.GetAthlete(i).AthleteId.Value,Does.StartWith("mass_"));
            }
            Complete(mass,massEvents,"mass_start",cancellation);
            Assert.That(MassStartResultSnapshot.FromJson(mass.Field.MassStartResult.ToJson(),mass.Field.StartGrid,20,new SimulationTick(1)).Count,Is.EqualTo(30));
        }
        private static void Complete(PackAiRaceRunner runner,RaceEventLog events,string name,System.Threading.CancellationToken cancellation)
        {
            while(!runner.Field.IsComplete&&runner.Field.Tick.Value<120000){cancellation.ThrowIfCancellationRequested();runner.Step();}
            Assert.That(runner.Field.IsComplete,Is.True);int shots=0,starts=0;
            for(int i=0;i<events.Count;i++){if(events[i].EventType==RaceEventType.ShotAccepted)shots++;if(events[i].EventType==RaceEventType.ScheduledStartDue)starts++;}
            Assert.That(starts,Is.EqualTo(30));Assert.That(shots,Is.EqualTo(30*runner.Field.Profile.BoutCount*5));
            for(int i=0;i<30;i++)
            {
                var actor=runner.Field.GetAthlete(i);Assert.That(actor.Lifecycle.CompletedBouts,Is.EqualTo(runner.Field.Profile.BoutCount));Assert.That(actor.Lifecycle.CompletedLaps,Is.EqualTo(runner.Field.Profile.LapCount));
                Assert.That(actor.Motion.DistanceNanometers,Is.EqualTo(runner.Field.Route.Length.Millimeters*SkiState.NanometersPerMillimeter+150L*SkiState.NanometersPerMeter*actor.TotalPenaltyLoops+actor.InitialStartApproachNanometers));
            }
            string result=name=="sprint"?runner.Field.Result.ToJson():name=="pursuit"?runner.Field.PursuitResult.ToJson():runner.Field.MassStartResult.ToJson();
            using(var sha=System.Security.Cryptography.SHA256.Create())TestContext.Out.WriteLine("Championship "+name+" ticks="+runner.Field.Tick.Value+"; shots="+shots+"; result_sha256="+BitConverter.ToString(sha.ComputeHash(System.Text.Encoding.UTF8.GetBytes(result))).Replace("-","").ToLowerInvariant());
        }
        [Test] public void InvalidDependenciesAndRosterRejectBeforeEvents()
        {
            var events=new RaceEventLog(100);var sink=new AiDecisionCounts();
            Assert.Catch(()=>ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.Pursuit,events,sink));
            Assert.Catch(()=>ChampionshipRaceFactory.Create(Read,Fixture,CompetitionFormat.MassStart,events,sink,count:31));
            Assert.Catch(()=>ChampionshipRaceFactory.Create(Read,Fixture,(CompetitionFormat)99,events,sink));
            Assert.That(events.Count,Is.Zero);
        }
    }
}
