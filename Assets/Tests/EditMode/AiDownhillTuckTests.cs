using System;
using Biathlon.AI;
using Biathlon.Domain;
using Biathlon.Replay;
using Biathlon.Simulation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class AiDownhillTuckTests
    {
        private static string Read(string id)=>PhysiologyContractTests.ProjectFile("Assets/Resources/"+id+".json");
        private static PackAiRaceRunner Create(bool enabled,RaceEventLog log)
        {
            string Resource(string id)
            {
                string json=Read(id);
                if(id=="Biathlon/Venues/woodland_basin_v1/ai_ski_policy")
                    json=json.Replace("true",enabled?"true":"false");
                return json;
            }
            var venue=new VenueAuthoringPlan(VenueManifest.FromJson(Resource("Biathlon/Venues/woodland_basin_v1/manifest")),Resource);
            string Core(string name)
            {
                string json=PhysiologyContractTests.DefinitionJson(name);
                if(name=="ai_race_policy_provisional_v1.json")
                {
                    json=System.Text.RegularExpressions.Regex.Replace(json,",?\\s*\"downhill_tuck_enabled\"\\s*:\\s*(true|false)","");
                    json=json.Insert(json.IndexOf('{')+1,"\"downhill_tuck_enabled\":"+(enabled?"true":"false")+",");
                }
                return json;
            }
            return ProductionVenueRaceFactory.Create(venue,
                CompetitionProfile.FromJson(Read("Biathlon/Venues/woodland_basin_v1/mass_start_profile")),
                RaceRoutePlan.FromJson(Read("Biathlon/Venues/woodland_basin_v1/mass_start_route")),
                RaceSnowConditions.FromJson(Read("Biathlon/race_snow_neutral_v1")),Core,log,new AiDecisionCounts(),count:3);
        }
        [Test] public void AiUsesTheSameEligibleDownhillTuckAndRecordedCommandsReplayExactly()
        {
            var log=new RaceEventLog(10000);var replayLog=new RaceEventLog(10000);
            var runner=Create(true,log);var target=Create(true,replayLog).Field;
            var tape=new PackSprintRaceCommandTape("ai_shared_downhill_tuck",target,3*1800);
            var inputs=new SprintAthleteInput[3];var lines=new double[3];
            var tuck=new TuckSystem(TuckTests.Definition());int requests=0,active=0;
            for(int t=0;t<1800;t++)
            {
                var eligible=new bool[3];
                for(int i=0;i<3;i++)
                {
                    var actor=runner.Field.GetAthlete(i);
                    var sample=actor.RangePath.SampleRoute(actor.RouteNanometers);
                    eligible[i]=tuck.Evaluate(true,0,actor.DownhillSkill,actor.Motion,sample).Active;
                }
                runner.Step();
                for(int i=0;i<3;i++)
                {
                    inputs[i]=runner.GetLastInput(i);lines[i]=runner.GetLastLateral(i);
                    if(inputs[i].Ski.TuckRequested){requests++;Assert.That(eligible[i],Is.True);}
                    if(runner.Field.GetAthlete(i).TuckActive)active++;
                }
                tape.StepAndRecord(target,inputs,lines);
                for(int i=0;i<3;i++)Assert.That(target.GetAthlete(i).LastFrame,Is.EqualTo(runner.Field.GetAthlete(i).LastFrame));
            }
            Assert.That(requests,Is.GreaterThan(0),"AI must request the actual shared tuck on the early descent.");
            Assert.That(active,Is.GreaterThan(0));
            int replayEvent=0,tuckEvents=0;
            for(int i=0;i<log.Count;i++)if(log[i].EventType==RaceEventType.TuckStarted||log[i].EventType==RaceEventType.TuckEnded)
            {
                while(replayEvent<replayLog.Count&&replayLog[replayEvent].EventType!=RaceEventType.TuckStarted&&replayLog[replayEvent].EventType!=RaceEventType.TuckEnded)replayEvent++;
                Assert.That(replayEvent,Is.LessThan(replayLog.Count));Assert.That(replayLog[replayEvent++],Is.EqualTo(log[i]));tuckEvents++;
            }
            Assert.That(tuckEvents,Is.GreaterThan(0));
            while(replayEvent<replayLog.Count)
            {Assert.That(replayLog[replayEvent].EventType,Is.Not.EqualTo(RaceEventType.TuckStarted));Assert.That(replayLog[replayEvent++].EventType,Is.Not.EqualTo(RaceEventType.TuckEnded));}
            var decoded=PackSprintRaceCommandTape.FromJson(tape.ToJson());var replay=Create(true,new RaceEventLog(10000)).Field;
            var cursor=decoded.CreateReplay(replay);while(cursor.HasNext)cursor.Step();
            for(int i=0;i<3;i++)Assert.That(replay.GetAthlete(i).LastFrame,Is.EqualTo(runner.Field.GetAthlete(i).LastFrame));
        }
        [Test] public void DisabledAiNeverRequestsTuckAndPolicyIsCaptured()
        {
            var runner=Create(false,new RaceEventLog(10000));
            for(int t=0;t<800;t++){runner.Step();for(int i=0;i<3;i++)Assert.That(runner.GetLastInput(i).Ski.TuckRequested,Is.False);}
            Assert.That(runner.ContextJson,Is.Not.EqualTo(Create(true,new RaceEventLog(10000)).ContextJson));
        }
    }
}
