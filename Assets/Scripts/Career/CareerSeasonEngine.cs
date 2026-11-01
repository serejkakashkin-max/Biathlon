using System;
using System.Collections.Generic;
using Biathlon.Domain;
namespace Biathlon.Career
{
    public sealed class CareerDefinitions
    {
        private const string LegacyCalendarResource="career_calendar_v1.json";
        private const string LegacyCalendarId="career_calendar_v1";
        private const string WoodlandCalendarResource="woodland_career_calendar_v1.json";
        private const string WoodlandCalendarId="woodland_career_calendar_v1";
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
        public CareerGrowthDefinition Growth {get;}
        public BiathlonRatingDefinition Ratings {get;}
        public CareerWorldDefinition World {get;}
        private CareerRivalAppearance rivalAppearance;
        public CareerRivalAppearance RivalAppearance=>rivalAppearance??(rivalAppearance=new CareerRivalAppearance(ReadDefinition));
        public CareerDefinitions(Func<string,string> read) : this(read,LegacyCalendarResource) { }
        /// <summary>New production careers use the approved one-venue season.
        /// Existing saves are selected from their persisted calendar ID instead.</summary>
        public static CareerDefinitions ForNewProductionCareer(Func<string,string> read)
        {
            var selected=new CareerDefinitions(read,WoodlandCalendarResource);
            if(selected.Calendar.Id!=WoodlandCalendarId)
                throw new InvalidOperationException("Production calendar resource has an unexpected ID.");
            return selected;
        }
        public static CareerDefinitions ForNewProductionCareer(Func<string,string> read,string startProfileId)
        {
            var p=CareerStartDefinition.FromJson(read("career_start_v1.json")).Get(startProfileId);
            return new CareerDefinitions(read,p.CalendarResource);
        }
        public static CareerDefinitions ForSave(Func<string,string> read,CareerSave save)
        {
            if(save==null)throw new ArgumentNullException(nameof(save));
            string resource=save.CalendarId==LegacyCalendarId?LegacyCalendarResource:
                save.CalendarId==WoodlandCalendarId?WoodlandCalendarResource:
                save.CalendarId=="woodland_junior_calendar_v1"?"woodland_junior_calendar_v1.json":
                save.CalendarId=="woodland_development_calendar_v1"?"woodland_development_calendar_v1.json":
                save.CalendarId=="woodland_world_cup_calendar_v1"?"woodland_world_cup_calendar_v1.json":null;
            if(resource==null)throw new ArgumentException("Unknown saved career calendar.",nameof(save));
            var selected=new CareerDefinitions(read,resource);
            if(selected.Calendar.Id!=save.CalendarId)
                throw new InvalidOperationException("Saved calendar ID does not match its loaded definition.");
            return selected;
        }
        public CareerDefinitions(Func<string,string> read,string calendarResource)
        {
            if(read==null)throw new ArgumentNullException(nameof(read));
            if(string.IsNullOrWhiteSpace(calendarResource))throw new ArgumentException("Explicit calendar resource required.",nameof(calendarResource));
            Roster=CareerRosterDefinition.FromJson(read("career_roster_v1.json"));Calendar=CareerCalendar.FromJson(read(calendarResource));
            Training=CareerTrainingDefinition.FromJson(read("career_training_v1.json"));Policy=CareerTrainingPolicy.FromJson(read("career_training_policy_v1.json"));Adaptation=CareerAdaptationDefinition.FromJson(read("career_adaptation_v1.json"));
            Fatigue=CareerFatigueDefinition.FromJson(read("career_fatigue_v1.json"));Form=CareerFormDefinition.FromJson(read("career_form_v1.json"));Mental=CareerMentalDefinition.FromJson(read("career_mental_v1.json"));Health=CareerHealthDefinition.FromJson(read("career_health_v1.json"));
            Generation=CareerGenerationDefinition.FromJson(read("career_generation_v1.json"));Economy=CareerEconomyDefinition.FromJson(read("career_economy_v1.json"));Scoring=CareerScoringDefinition.FromJson(read(Calendar.Circuit==CareerCircuit.WorldCup?"career_scoring_ibu_2026_v1.json":"career_scoring_v1.json"));History=CareerHistoryDefinition.FromJson(read("career_history_v1.json"));Progression=CareerProgressionDefinition.FromJson(read("career_progression_v1.json"));
            ReadDefinition=read;
            Growth=CareerGrowthDefinition.FromJson(read("career_growth_v1.json"));Ratings=BiathlonRatingDefinition.FromJson(read("career_ratings_v1.json"));
            World=CareerWorldDefinition.FromJson(read("career_world_v1.json"));
        }
        internal Func<string,string> ReadDefinition {get;}
    }
    public interface ICareerRaceProvider
    {
        CareerRaceRecord Run(CareerSave snapshot,CareerCalendarEvent calendarEvent,ulong seed);
    }
    /// <summary>A detached trained week that can wait for a player-controlled race.</summary>
    public sealed class CareerWeekPreparation
    {
        public string SourceJson { get; }
        public string RaceSnapshotJson { get; }
        public CareerCalendarEvent Event { get; }
        public ulong RaceSeed { get; }
        public bool RequiresRace { get; }
        internal ulong CompletionRandomState { get; }
        private readonly int[] usedSlots;
        internal int UsedSlots(int index)=>usedSlots[index];
        internal CareerWeekPreparation(string source,string snapshot,CareerCalendarEvent e,
            ulong seed,bool requiresRace,ulong randomState,int[] usedSlots)
        {SourceJson=source;RaceSnapshotJson=snapshot;Event=e;RaceSeed=seed;RequiresRace=requiresRace;CompletionRandomState=randomState;this.usedSlots=usedSlots;}
    }
    public static class CareerSeasonEngine
    {
        public static CareerSave AdvanceWeek(CareerSave source,CareerDefinitions d,IReadOnlyList<CareerTrainingKind> playerPlan,ICareerRaceProvider races,Func<ulong,IRandomSource> restoreRandom)
        {
            if(source==null||d==null||playerPlan==null||races==null||restoreRandom==null)throw new ArgumentNullException();
            var prepared=PrepareWeek(source,d,playerPlan,restoreRandom);
            var result=prepared.RequiresRace?races.Run(CareerSave.FromJson(prepared.RaceSnapshotJson),prepared.Event,prepared.RaceSeed):null;
            return CompleteWeek(prepared,d,result,restoreRandom);
        }
        public static CareerWeekPreparation PrepareWeek(CareerSave source,CareerDefinitions d,IReadOnlyList<CareerTrainingKind> playerPlan,Func<ulong,IRandomSource> restoreRandom)
        {
            if(source==null||d==null||playerPlan==null||restoreRandom==null)throw new ArgumentNullException();
            string sourceJson=source.ToJson();
            var random=restoreRandom(source.RandomState);var save=CareerFactory.Restore(source.ToJson(),d.Roster,d.Calendar,random);
            save=CareerWorld.Initialize(save,d,restoreRandom);
            int? ownBaseline=save.Creation?.Mode==CareerCreationMode.CreateOwn?
                CareerCreationDefinition.FromJson(d.ReadDefinition("career_creation_v1.json")).Tier(save.StartProfileId).BaselineStat:(int?)null;
            CareerDevelopment.InitializeWorld(save,d.Growth,d.Ratings,restoreRandom,ownBaseline);
            var plans=new CareerWeekPlan[save.AthleteCount];
            var used=new int[plans.Length];
            for(int i=0;i<save.AthleteCount;i++)
            {
                var a=save.Athlete(i);plans[i]=a.Id==save.PlayerId?new CareerWeekPlan(d.Training,d.Calendar,save.Week,playerPlan):CareerAiTraining.Plan(a,d.Training,d.Calendar,d.Policy,save.Week,d.Growth,d.Adaptation);
                int quality=a.PendingCampQuality>0?a.PendingCampQuality:d.Policy.Quality;
                bool taper=save.Week<d.Calendar.WeeksPerSeason&&d.Calendar.AtWeek(save.Week+1)!=null&&plans[i].UsedSlots<d.Calendar.TrainingSlots(save.Week);
                CareerTrainingWeek.Apply(a,plans[i],d.Training,d.Adaptation,d.Fatigue,d.Form,d.Mental,d.Health,quality,taper,d.Growth);
                a.ObservePeak(save.Season,d.Ratings);
                if(plans[i].Count>0)a.PendingCampQuality=0;
                used[i]=plans[i].UsedSlots;
            }
            var e=d.Calendar.AtWeek(save.Week);
            CareerWorld.TrainBackgroundWeek(save,d);
            bool requiresRace=false;
            if(e!=null)for(int i=0;i<save.AthleteCount;i++)requiresRace|=e.Eligible(save.Athlete(i));
            ulong seed=requiresRace?random.NextUInt64():0;
            return new CareerWeekPreparation(sourceJson,save.ToJson(),e,seed,requiresRace,random.State,used);
        }
        public static CareerSave CompleteWeek(CareerWeekPreparation prepared,CareerDefinitions d,CareerRaceRecord record,Func<ulong,IRandomSource> restoreRandom)
        {
            if(prepared==null||d==null||restoreRandom==null)throw new ArgumentNullException();
            var decoded=CareerSave.FromJson(prepared.RaceSnapshotJson);
            var save=CareerFactory.Restore(decoded.ToJson(),d.Roster,d.Calendar,restoreRandom(decoded.RandomState));
            var e=d.Calendar.AtWeek(save.Week);
            if(e?.Id!=prepared.Event?.Id)throw new ArgumentException("Prepared calendar changed.");
            if(!prepared.RequiresRace&&record!=null)throw new ArgumentException("This week has no race to register.");
            if(prepared.RequiresRace)
            {
                var expected=new HashSet<string>(StringComparer.Ordinal);for(int i=0;i<save.AthleteCount;i++)if(e.Eligible(save.Athlete(i)))expected.Add(save.Athlete(i).Id);
                if(expected.Count>0)
                {
                    ulong seed=prepared.RaceSeed;
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
            for(int i=0;i<save.AthleteCount;i++){CareerFatigue.Rest(save.Athlete(i),d.Fatigue,d.Calendar.TrainingSlots(save.Week)-prepared.UsedSlots(i));CareerMental.Rest(save.Athlete(i),d.Mental);}
            save.RandomState=prepared.CompletionRandomState;
            if(save.Week==d.Calendar.WeeksPerSeason)
            {
                CareerDevelopment.SealSeason(save,d.Growth,d.Ratings);
                save=CareerProgression.ApplySeason(save,d.Calendar,d.Scoring,d.Progression);save=CareerHistory.SealSeason(save,d.Calendar,d.Scoring,d.History);
                for(int i=0;i<save.WorldAthleteCount;i++)CareerAgeing.AdvanceYear(save.WorldAthlete(i),d.Adaptation,d.Growth);
                if(save.WorldPolicyId!=null)save=CareerWorld.AdvanceSeason(save,d,restoreRandom);
                else
                {
                    save=CareerGeneration.ReplaceRetirees(save,d.Roster,d.Generation,restoreRandom,d.Growth,d.Ratings,d.RivalAppearance).Save;
                    if(save.StartProfileId!=null)save=CareerCircuitTransition.Advance(save,d,restoreRandom);
                }
            }
            else save.Week++;
            save.Validate();return save;
        }
    }
}
