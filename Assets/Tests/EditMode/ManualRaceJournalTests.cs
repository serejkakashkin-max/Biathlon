using System;
using System.IO;
using Biathlon.Domain;
using Biathlon.Replay;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class ManualRaceJournalTests
    {
        [TestCase("sprint",1)][TestCase("pursuit",3)]
        public void StreamedAcceptedCommandsReplayWithoutAiAndPreserveResourcesAndPose(string format,int count)
        {
            var source=ManualReusableRangeTests.Initial(format,count);var replay=ManualReusableRangeTests.Initial(format,count);
            using(var stream=new MemoryStream())
            {
                using(var writer=new ManualRaceJournalWriter(stream,source.stepper,SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json"))))
                {
                    source.stepper.CommandObserver=writer;for(int i=0;i<1200;i++)source.runner.Step();writer.Complete();
                    Assert.That(writer.Failure,Is.Null);Assert.That(writer.TickCount,Is.EqualTo(1200));
                }
                stream.Position=0;using(var reader=new ManualRaceJournalReader(stream,replay.stepper,1200))while(reader.HasNext)reader.Step();
                Assert.That(replay.runner.TacticalDecisions,Is.Zero);
                for(int i=0;i<count;i++){Assert.That(replay.stepper.World.GetActor(i).Pose,Is.EqualTo(source.stepper.World.GetActor(i).Pose));Assert.That(replay.runner.Field.GetAthlete(i).LastFrame,Is.EqualTo(source.runner.Field.GetAthlete(i).LastFrame));}
            }
        }
        [Test] public void IncompatibleContextAndTruncatedJournalRejectClearly()
        {
            var source=ManualReusableRangeTests.Initial("sprint",1);using(var stream=new MemoryStream())
            {
                using(var writer=new ManualRaceJournalWriter(stream,source.stepper,SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json")))){source.stepper.CommandObserver=writer;source.runner.Step();writer.Complete();}
                stream.Position=0;var foreign=ManualReusableRangeTests.Initial("sprint",3);Assert.Throws<InvalidDataException>(()=>new ManualRaceJournalReader(stream,foreign.stepper,1));
                var bytes=stream.ToArray();Array.Resize(ref bytes,bytes.Length-4);using(var truncated=new MemoryStream(bytes))
                {var fresh=ManualReusableRangeTests.Initial("sprint",1);using(var reader=new ManualRaceJournalReader(truncated,fresh.stepper,1))Assert.Throws<EndOfStreamException>(()=>reader.Step());}
            }
        }
        [Test] public void FailedOptionalRecordingDoesNotRejectCommittedSimulationTick()
        {
            var source=ManualReusableRangeTests.Initial("sprint",1);var stream=new MemoryStream();var writer=new ManualRaceJournalWriter(stream,source.stepper,SkiTechniqueDefinition.FromJson(PhysiologyContractTests.DefinitionJson("ski_technique_provisional_v1.json")));
            source.stepper.CommandObserver=writer;stream.Dispose();Assert.DoesNotThrow(()=>source.runner.Step());Assert.That(source.runner.Field.Tick.Value,Is.EqualTo(1));Assert.That(writer.Failure,Is.Not.Null);
        }
    }
    public sealed class RaceHighlightDetectorTests
    {
        private static RaceEvent Event(long tick,RaceEventType type,int payload=0,string id="a")=>new RaceEvent(new SimulationTick(tick),20,new AthleteId(id),type,payload);
        [Test] public void HighlightsUseActualFinalRangeMissOvertakeLeaderAndCloseFinishEvents()
        {
            var d=new RaceHighlightDetector(2,20,1.5,8);d.Record(Event(10,RaceEventType.ShootingMatCaptured));d.Record(Event(20,RaceEventType.ShootingBoutCompleted));
            d.Record(Event(40,RaceEventType.ShootingMatCaptured));d.Record(Event(50,RaceEventType.ShotResolved,0));d.Record(Event(55,RaceEventType.ShotResolved,0));
            d.ObserveLeader(60,"a");d.ObserveLeader(80,"b");d.Record(Event(90,RaceEventType.OvertakeCompleted));d.Record(Event(100,RaceEventType.RaceFinished));d.Record(Event(120,RaceEventType.RaceFinished,id:"b"));
            var all=d.Highlights();Assert.That(Array.FindAll(all,x=>x.Kind==RaceHighlightKind.FinalRange).Length,Is.EqualTo(1));Assert.That(Array.FindAll(all,x=>x.Kind==RaceHighlightKind.KeyMiss).Length,Is.EqualTo(1));
            foreach(var kind in new[]{RaceHighlightKind.LeadChange,RaceHighlightKind.Overtake,RaceHighlightKind.CloseFinish})Assert.That(Array.Exists(all,x=>x.Kind==kind),Is.True);
        }
        [Test] public void IntervalStartsNeverInferCloseFinishFromGlobalArrivalTicks()
        {var d=new RaceHighlightDetector(2,20,1.5,8,true);d.Record(Event(100,RaceEventType.RaceFinished));d.Record(Event(110,RaceEventType.RaceFinished,id:"b"));Assert.That(Array.Exists(d.Highlights(),x=>x.Kind==RaceHighlightKind.CloseFinish),Is.False);d.ObserveRegisteredFinish("b",.7);Assert.That(Array.Exists(d.Highlights(),x=>x.Kind==RaceHighlightKind.CloseFinish),Is.True);}
    }
}
