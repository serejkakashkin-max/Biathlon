#if !UNITY_EDITOR
using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    /// <summary>Persisted, AI-free replay of a real production-size race.
    /// Kept headless because this is several full 30-athlete simulations.</summary>
    public sealed class WoodlandFullFieldCommandReplayTests
    {
        private const string Root="Biathlon/Venues/woodland_basin_v1/";
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static PackAiRaceRunner Create(IRaceEventSink events)
        {
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Read(Root+"manifest")),Read);
            return ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read(Root+"mass_start_profile")),
                RaceRoutePlan.FromJson(Read(Root+"mass_start_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),
                PhysiologyContractTests.DefinitionJson,events,new AiDecisionCounts(),count:30,seed:123);
        }

        [Test,Timeout(900000),NonParallelizable]
        public void ThirtyAthleteWoodlandMassStartPersistsAndReplaysExactFieldState()
        {
            GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
            var source=Create(new BalanceEventAudit(5));
            var recordedEvents=new BalanceEventAudit(5);
            var recorded=Create(recordedEvents).Field;
            var tape=new PackSprintRaceCommandTape("woodland_mass30_full_command_replay_v1",recorded,30*50000);
            var commands=new SprintAthleteInput[30];var lines=new double[30];
            while(!source.Field.IsComplete&&source.Field.Tick.Value<50000)
            {
                source.Step();
                for(int i=0;i<30;i++)
                {commands[i]=source.GetLastInput(i);lines[i]=source.GetLastLateral(i);}
                tape.StepAndRecord(recorded,commands,lines);
            }
            Assert.That(source.Field.IsComplete,Is.True);
            Assert.That(recorded.IsComplete,Is.True);
            string result=source.Field.MassStartResult.ToJson();
            Assert.That(recorded.MassStartResult.ToJson(),Is.EqualTo(result));
            var frames=new SprintAthleteFrame[30];var loops=new string[30];var local=new long[30];
            for(int i=0;i<30;i++)
            {
                frames[i]=recorded.GetAthlete(i).LastFrame;
                var point=recorded.GetPhysicalPoint(i);loops[i]=point.LoopId;local[i]=point.LocalNanometers;
            }
            long ticks=recorded.Tick.Value;int runs=tape.RunCount;ulong fieldHash=recordedEvents.Hash;
            string json=tape.ToJson();int jsonChars=json.Length;
            var restored=PackSprintRaceCommandTape.FromJson(json);
            Assert.That(restored.TickCount,Is.EqualTo(ticks));
            Assert.That(restored.AthleteCount,Is.EqualTo(30));
            source=null;recorded=null;tape=null;json=null;
            GC.Collect(GC.MaxGeneration,GCCollectionMode.Forced,true,true);
            var replayEvents=new BalanceEventAudit(5);var replay=Create(replayEvents).Field;
            var cursor=restored.CreateReplay(replay);
            while(cursor.HasNext)cursor.Step();
            Assert.That(replay.IsComplete,Is.True);
            Assert.That(replay.Tick.Value,Is.EqualTo(ticks));
            Assert.That(replay.MassStartResult.ToJson(),Is.EqualTo(result));
            Assert.That(replayEvents.Hash,Is.EqualTo(fieldHash),
                "The persisted command stream must reproduce all field race events without AI.");
            for(int i=0;i<30;i++)
            {
                Assert.That(replay.GetAthlete(i).LastFrame,Is.EqualTo(frames[i]));
                var point=replay.GetPhysicalPoint(i);
                Assert.That(point.LoopId,Is.EqualTo(loops[i]));
                Assert.That(point.LocalNanometers,Is.EqualTo(local[i]));
            }
            TestContext.WriteLine("wp175_full_field_replay athletes=30 ticks="+ticks+
                " command_runs="+runs+" json_chars="+jsonChars+
                " field_event_hash="+fieldHash);
        }
    }
}
#endif
