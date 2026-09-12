using System;
using System.Collections.Generic;
using Biathlon.Domain;
namespace Biathlon.Career
{
    public sealed class CareerDefinitions
    {
        public CareerRosterDefinition Roster { get; }
        public CareerCalendar Calendar { get; }
        public CareerTrainingDefinition Training { get; }
        public CareerTrainingPolicy Policy { get; }
        public CareerAdaptationDefinition Adaptation { get; }
        public CareerFatigueDefinition Fatigue { get; }
        public CareerFormDefinition Form { get; }
        public CareerMentalDefinition Mental { get; }
        public CareerHealthDefinition Health { get; }
        public CareerGenerationDefinition Generation { get; }
        public CareerEconomyDefinition Economy { get; }
        public CareerScoringDefinition Scoring { get; }
        public CareerHistoryDefinition History { get; }
        public CareerProgressionDefinition Progression { get; }
        public CareerDefinitions(Func<string,string> read)
        {
            if(read==null)throw new ArgumentNullException(nameof(read));Roster=CareerRosterDefinition.FromJson(read("career_roster_v1.json"));Calendar=CareerCalendar.FromJson(read("career_calendar_v1.json"));
            Training=CareerTrainingDefinition.FromJson(read("career_training_v1.json"));Policy=CareerTrainingPolicy.FromJson(read("career_training_policy_v1.json"));Adaptation=CareerAdaptationDefinition.FromJson(read("career_adaptation_v1.json"));
            Fatigue=CareerFatigueDefinition.FromJson(read("career_fatigue_v1.json"));Form=CareerFormDefinition.FromJson(read("career_form_v1.json"));Mental=CareerMentalDefinition.FromJson(read("career_mental_v1.json"));Health=CareerHealthDefinition.FromJson(read("career_health_v1.json"));
            Generation=CareerGenerationDefinition.FromJson(read("career_generation_v1.json"));Economy=CareerEconomyDefinition.FromJson(read("career_economy_v1.json"));Scoring=CareerScoringDefinition.FromJson(read("career_scoring_v1.json"));History=CareerHistoryDefinition.FromJson(read("career_history_v1.json"));Progression=CareerProgressionDefinition.FromJson(read("career_progression_v1.json"));
        }
    }
    public interface ICareerRaceProvider
    {
        CareerRaceRecord Run(CareerSave snapshot,CareerCalendarEvent calendarEvent,ulong seed);
    }
    public static class CareerSeasonEngine
    {
        public static CareerSave AdvanceWeek(CareerSave source,CareerDefinitions d,IReadOnlyList<CareerTrainingKind> playerPlan,ICareerRaceProvider races,Func<ulong,IRandomSource> restoreRandom)
        {
            if(source==null||d==null||playerPlan==null||races==null||restoreRandom==null)throw new ArgumentNullException();
            var random=restoreRandom(source.RandomState);var save=CareerFactory.Restore(source.ToJson(),d.Roster,d.Calendar,random);
            var plans=new CareerWeekPlan[save.AthleteCount];
            for(int i=0;i<save.AthleteCount;i++)
            {
                var a=save.Athlete(i);plans[i]=a.Id==save.PlayerId?new CareerWeekPlan(d.Training,d.Calendar,save.Week,playerPlan):CareerAiTraining.Plan(a,d.Training,d.Calendar,d.Policy,save.Week);
                int quality=a.PendingCampQuality>0?a.PendingCampQuality:d.Policy.Quality;
                bool taper=save.Week<d.Calendar.WeeksPerSeason&&d.Calendar.AtWeek(save.Week+1)!=null&&plans[i].UsedSlots<d.Calendar.TrainingSlots(save.Week);
                CareerTrainingWeek.Apply(a,plans[i],d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,quality,taper);
                if(plans[i].Count>0)a.PendingCampQuality=0;
            }
            var e=d.Calendar.AtWeek(save.Week);
            if(e!=null)
            {
                var expected=new HashSet<string>(StringComparer.Ordinal);for(int i=0;i<save.AthleteCount;i++)if(e.Eligible(save.Athlete(i)))expected.Add(save.Athlete(i).Id);
                if(expected.Count>0)
                {
                    ulong seed=random.NextUInt64();var record=races.Run(CareerSave.FromJson(save.ToJson()),e,seed);
                    if(record==null||record.CalendarEventId!=e.Id||record.SeasonId!=save.SeasonId||record.Format!=e.Format||record.Seed!=seed||record.Count!=expected.Count)throw new InvalidOperationException("Race boundary returned a mismatched event.");
                    for(int i=0;i<record.Count;i++)if(!expected.Remove(record.Get(i).AthleteId))throw new InvalidOperationException("Race boundary returned a mismatched field.");
                    var standings=CareerStandings.Table(save,d.Scoring);CareerStandings.Register(save,record);
                    for(int rank=0;rank<record.Count;rank++)
                    {
                        var finish=record.Get(rank);CareerAthlete a=null;for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==finish.AthleteId)a=save.Athlete(i);
                        int expectedRank=rank+1;for(int i=0;i<standings.Length;i++)if(standings[i].AthleteId==a.Id)expectedRank=Math.Min(record.Count,i+1);
                        CareerEconomy.Award(a,d.Economy,rank+1);a.Races=checked(a.Races+1);if(rank==0)a.Wins=checked(a.Wins+1);
                        CareerFatigue.Race(a,d.Fatigue,finish.ElapsedMilliseconds);CareerMental.Race(a,d.Mental,rank+1,expectedRank,record.Count);
                    }
                }
            }
            for(int i=0;i<save.AthleteCount;i++){CareerFatigue.Rest(save.Athlete(i),d.Fatigue,d.Calendar.TrainingSlots(save.Week)-plans[i].UsedSlots);CareerMental.Rest(save.Athlete(i),d.Mental);}
            save.RandomState=random.State;
            if(save.Week==d.Calendar.WeeksPerSeason)
            {
                save=CareerProgression.ApplySeason(save,d.Calendar,d.Scoring,d.Progression);save=CareerHistory.SealSeason(save,d.Calendar,d.Scoring,d.History);
                for(int i=0;i<save.AthleteCount;i++)CareerAgeing.AdvanceYear(save.Athlete(i),d.Adaptation);
                save=CareerGeneration.ReplaceRetirees(save,d.Roster,d.Generation,restoreRandom).Save;
            }
            else save.Week++;
            save.Validate();return save;
        }
    }
}
