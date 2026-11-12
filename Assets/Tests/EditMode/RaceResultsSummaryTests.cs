using System;
using System.Linq;
using System.Runtime.Serialization;
using Biathlon.Career;
using Biathlon.Domain;
using Biathlon.Presentation;
using NUnit.Framework;

namespace Biathlon.Tests
{
    public sealed class RaceResultsSummaryTests
    {
        private static CareerResultDetails Details(string id)=>new CareerResultDetails(id,new[]{31,27},new[]{ShootingStance.Prone,ShootingStance.Standing},new[]{
            new CareerResultSplit("split_1","lap_1",60000,90000),new CareerResultSplit("split_2","lap_2",180000,210000)});
        private static CareerRaceRecord Record(CareerSave save,string eventId,CompetitionFormat format,bool reverse=false,bool details=true)
        {
            var rows=new CareerFinish[save.AthleteCount];
            for(int i=0;i<rows.Length;i++)
            {
                var a=save.Athlete(reverse?rows.Length-1-i:i);
                rows[i]=new CareerFinish(a.Id,300000+i*1000,format==CompetitionFormat.Pursuit?250000+i*500:300000+i*1000,1,details:details?Details(a.Id):null);
            }
            return new CareerRaceRecord(save.SeasonId+"_"+eventId,save.SeasonId,eventId,format,1,new string('a',64),rows);
        }
        [Test] public void FinalSeasonProjectionSeparatesOverallAndDisciplinePointsAndUsesTheRaceAward()
        {
            var save=CareerFoundationTests.Create();var scoring=CareerStandingsTests.Scoring();
            CareerStandings.Register(save,Record(save,"earlier_sprint",CompetitionFormat.Sprint));
            string original=save.ToJson();
            var race=Record(save,"last_mass_start",CompetitionFormat.MassStart,true);
            var model=new RaceResultsSummary(save,race,scoring);
            var overall=model.OverallStandings.Single(row=>row.IsPlayer);
            var discipline=model.DisciplineStandings.Single(row=>row.IsPlayer);
            Assert.That(overall.RacePoints,Is.EqualTo(model.Player.Points));
            Assert.That(discipline.RacePoints,Is.EqualTo(model.Player.Points));
            Assert.That(discipline.Points,Is.EqualTo(model.Player.Points));
            Assert.That(overall.Points,Is.EqualTo(scoring.Points(1,CompetitionFormat.Sprint)+model.Player.Points));
            Assert.That(discipline.Rank,Is.EqualTo(30));
            Assert.That(overall.Rank,Is.EqualTo(model.StandingAfter).And.LessThan(discipline.Rank));
            Assert.That(model.Season,Is.EqualTo(save.Season));
            Assert.That(save.ToJson(),Is.EqualTo(original));
        }
        [TestCase(CompetitionFormat.Sprint)] [TestCase(CompetitionFormat.Pursuit)] [TestCase(CompetitionFormat.MassStart)]
        public void ProtocolReadsImportedOrderingAndScoringWithoutChangingCareer(CompetitionFormat format)
        {
            var save=CareerFoundationTests.Create();var scoring=CareerStandingsTests.Scoring();
            CareerStandings.Register(save,Record(save,"prior",format));string source=save.ToJson();
            var result=Record(save,"current",format,true);var model=new RaceResultsSummary(save,result,scoring);
            Assert.That(save.ToJson(),Is.EqualTo(source));Assert.That(model.Count,Is.EqualTo(save.AthleteCount));
            Assert.That(model.Player.Rank,Is.EqualTo(30));Assert.That(model.Player.GapMilliseconds,Is.EqualTo(29000));
            Assert.That(model.Player.Points,Is.EqualTo(scoring.Points(30)));Assert.That(model.PointsBefore,Is.EqualTo(scoring.Points(1)));
            Assert.That(model.PointsAfter,Is.EqualTo(model.PointsBefore+model.Player.Points));Assert.That(model.StandingBefore,Is.EqualTo(1));
            var projection=CareerSave.FromJson(source);CareerStandings.Register(projection,result);var table=CareerStandings.Table(projection,scoring);
            for(int i=0;i<table.Length;i++)if(table[i].AthleteId==save.PlayerId)Assert.That(model.StandingAfter,Is.EqualTo(i+1));
            Assert.That(model.Player.Finish.ElapsedMilliseconds,Is.EqualTo(result.Get(29).ElapsedMilliseconds));
        }
        [Test] public void SavedDetailsRoundTripAndLegacyResultsRemainReadable()
        {
            var save=CareerFoundationTests.Create();var record=Record(save,"race",CompetitionFormat.Sprint);CareerStandings.Register(save,record);
            var restored=CareerSave.FromJson(save.ToJson());Assert.That(restored.ToJson(),Is.EqualTo(save.ToJson()));
            var details=restored.Race(0).Get(0).Details;Assert.That(details.TotalHits,Is.EqualTo(9));Assert.That(details.ClosedMask(1),Is.EqualTo(27));
            Assert.That(details.Stance(1),Is.EqualTo(ShootingStance.Standing));Assert.That(details.Split(1).CheckpointId,Is.EqualTo("split_2"));
            Assert.That(details.Split(1).ElapsedMilliseconds-details.Split(0).ElapsedMilliseconds,Is.EqualTo(120000));
            var old=CareerFoundationTests.Create();CareerStandings.Register(old,Record(old,"legacy",CompetitionFormat.Sprint,details:false));
            var legacy=CareerSave.FromJson(old.ToJson());Assert.That(legacy.ToJson(),Does.Not.Contain("\"details\""));
            var model=new RaceResultsSummary(CareerFoundationTests.Create(),legacy.Race(0),CareerStandingsTests.Scoring());
            Assert.That(model.Player.Finish.Details,Is.Null);Assert.That(model.StandingBefore,Is.Null);Assert.That(model.PointsAfter,Is.EqualTo(model.Player.Points));
        }
        [Test] public void ObservationsAreCopiedAndMalformedDetailsAreRejected()
        {
            var masks=new[]{31,27};var stances=new[]{ShootingStance.Prone,ShootingStance.Standing};var splits=new[]{new CareerResultSplit("split","lap",1000,1000)};
            var details=new CareerResultDetails("athlete",masks,stances,splits);masks[1]=0;stances[0]=ShootingStance.Standing;splits[0]=null;
            Assert.That(details.ClosedMask(1),Is.EqualTo(27));Assert.That(details.Stance(0),Is.EqualTo(ShootingStance.Prone));Assert.That(details.Split(0),Is.Not.Null);
            Assert.Throws<SerializationException>(()=>new CareerFinish("athlete",5000,5000,0,details:details));
            Assert.Throws<SerializationException>(()=>new CareerFinish("other",5000,5000,1,details:details));
            Assert.Throws<SerializationException>(()=>new CareerFinish("athlete",500,500,1,details:details));
            Assert.Throws<SerializationException>(()=>new CareerResultDetails("athlete",new[]{32},new[]{ShootingStance.Prone},splits));
            Assert.Throws<SerializationException>(()=>new CareerResultDetails("athlete",new[]{31},new[]{ShootingStance.Prone},new[]{new CareerResultSplit("b","lap",2000,2000),new CareerResultSplit("a","lap",1000,1000)}));
            Assert.Throws<SerializationException>(()=>new CareerResultSplit("a","lap",2000,1000));
        }
    }
}
