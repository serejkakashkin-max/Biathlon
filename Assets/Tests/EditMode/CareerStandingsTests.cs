using System;
using Biathlon.Career;
using Biathlon.Domain;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public class CareerStandingsTests
    {
        public static CareerScoringDefinition Scoring()=>CareerScoringDefinition.FromJson(PhysiologyContractTests.DefinitionJson("career_scoring_v1.json"));
        public static CareerRaceRecord Record(CareerSave save,string eventId,CompetitionFormat format,bool reverse=false)
        {
            var rows=new CareerFinish[save.AthleteCount];for(int i=0;i<rows.Length;i++)rows[i]=new CareerFinish(save.Athlete(reverse?rows.Length-1-i:i).Id,300000+i*1000,300000+i*1000);
            return new CareerRaceRecord(save.SeasonId+"_"+eventId,save.SeasonId,eventId,format,1,new string('a',64),rows);
        }
        [Test] public void SupportedFormatsAccumulateDeterministicStandingsAndSaveHistory()
        {
            var save=CareerFoundationTests.Create();var scoring=Scoring();
            CareerStandings.Register(save,Record(save,"sprint",CompetitionFormat.Sprint));CareerStandings.Register(save,Record(save,"pursuit",CompetitionFormat.Pursuit,true));CareerStandings.Register(save,Record(save,"mass",CompetitionFormat.MassStart));
            var table=CareerStandings.Table(save,scoring);Assert.That(table[0].AthleteId,Is.EqualTo(save.PlayerId));Assert.That(table[0].Wins,Is.EqualTo(2));Assert.That(table[0].Races,Is.EqualTo(3));
            Assert.That(table[0].Points,Is.EqualTo(61));Assert.That(CareerStandings.Table(save,scoring,CompetitionFormat.Pursuit)[0].AthleteId,Is.EqualTo(save.Athlete(29).Id));
            var copy=CareerSave.FromJson(save.ToJson());Assert.That(copy.ToJson(),Is.EqualTo(save.ToJson()));Assert.That(copy.RaceCount,Is.EqualTo(3));
        }
        [Test] public void InvalidAndRepeatedResultsLeaveTheSaveUnchanged()
        {
            var save=CareerFoundationTests.Create();var result=Record(save,"sprint",CompetitionFormat.Sprint);CareerStandings.Register(save,result);string before=save.ToJson();
            Assert.Catch(()=>CareerStandings.Register(save,result));Assert.That(save.ToJson(),Is.EqualTo(before));
            var unknown=new CareerRaceRecord("other",save.SeasonId,"other",CompetitionFormat.Sprint,1,new string('b',64),new[]{new CareerFinish("unknown",1000,1000)});
            Assert.Catch(()=>CareerStandings.Register(save,unknown));Assert.That(save.ToJson(),Is.EqualTo(before));
            Assert.Catch(()=>new CareerRaceRecord("bad",save.SeasonId,"bad",CompetitionFormat.Sprint,1,new string('a',64),new[]{result.Get(1),result.Get(0)}));
            Assert.Catch(()=>new CareerRaceRecord("bad",save.SeasonId,"bad",CompetitionFormat.Sprint,1,new string('a',64),new[]{result.Get(0),result.Get(0)}));
            Assert.Catch(()=>CareerScoringDefinition.FromJson("{\"schema_version\":1,\"id\":\"bad\",\"rank_points\":[1,2]}"));
        }
    }
}
