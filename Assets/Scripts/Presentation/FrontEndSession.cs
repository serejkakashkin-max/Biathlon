using System;
using System.Collections.Generic;
using Biathlon.Career;
using Biathlon.Domain;

namespace Biathlon.Presentation
{
    public enum FrontEndScreen { Home, NewCareer, Career, Settings, PreRace, Race, Results, Training, Equipment, Standings, History, Rivalries, Replays }
    public interface ICareerProfileStore
    {
        bool HasProfile { get; }
        string ReadLatest();
        void WriteNew(string id, string json);
        void Replace(string id,string expectedJson,string json);
    }

    /// <summary>Menu transitions and career boundary; no Unity, scene references or simulated outcomes.</summary>
    public sealed class FrontEndSession
    {
        private readonly Func<string,string> read;
        private readonly ICareerProfileStore profiles;
        private readonly Func<ulong,IRandomSource> restoreRandom;
        private FrontEndScreen settingsReturn;
        private readonly List<CareerTrainingKind> training=new List<CareerTrainingKind>();
        public int TrainingCount=>training.Count;
        public CareerTrainingKind Training(int index)=>training[index];
        public SkiEquipmentCatalog EquipmentCatalog=>SkiEquipmentCatalog.FromJson(read("ski_equipment_provisional_v1.json"));
        public CareerStartDefinition StartProfiles=>CareerStartDefinition.FromJson(read("career_start_v1.json"));
        public FrontEndScreen Screen { get; private set; }
        public CareerSave Career { get; private set; }
        public CareerDefinitions Definitions { get; private set; }
        public CareerWeekPreparation PreparedWeek { get; private set; }
        public CareerRaceRecord Result { get; private set; }
        public RaceResultsSummary ResultsSummary { get; private set; }
        public bool CanContinue => profiles.HasProfile;
        public CareerAthlete Player
        {
            get { if(Career==null)return null;for(int i=0;i<Career.AthleteCount;i++)if(Career.Athlete(i).Id==Career.PlayerId)return Career.Athlete(i);throw new InvalidOperationException("Missing player."); }
        }
        public FrontEndSession(Func<string,string> read, ICareerProfileStore profiles,Func<ulong,IRandomSource> restoreRandom)
        { this.read=read??throw new ArgumentNullException(nameof(read));this.profiles=profiles??throw new ArgumentNullException(nameof(profiles));this.restoreRandom=restoreRandom??throw new ArgumentNullException(nameof(restoreRandom)); }
        public void OpenNewCareer()
        { Require(FrontEndScreen.Home);Screen=FrontEndScreen.NewCareer; }
        public void OpenSettings()
        {
            if(Screen!=FrontEndScreen.Home&&Screen!=FrontEndScreen.Career)throw new InvalidOperationException("Settings require a menu or career page.");
            settingsReturn=Screen;Screen=FrontEndScreen.Settings;
        }
        public void Back()
        {
            if(Screen==FrontEndScreen.Race)throw new InvalidOperationException("Finish or explicitly abandon the active race.");
            if(Screen==FrontEndScreen.PreRace){PreparedWeek=null;Screen=FrontEndScreen.Career;}
            else if(Screen>=FrontEndScreen.Training)Screen=FrontEndScreen.Career;
            else Screen=Screen==FrontEndScreen.Settings?settingsReturn:FrontEndScreen.Home;
        }
        public void OpenCareerPage(FrontEndScreen page)
        {Require(FrontEndScreen.Career);if(page<FrontEndScreen.Training||page>FrontEndScreen.Replays)throw new ArgumentException("Career page required.");Screen=page;}
        public CareerWeekPlan TrainingPlan()=>new CareerWeekPlan(Definitions.Training,Definitions.Calendar,Career.Week,training);
        public void AddTraining(CareerTrainingKind kind)
        {
            Require(FrontEndScreen.Training);var candidate=new List<CareerTrainingKind>(training){kind};
            _=new CareerWeekPlan(Definitions.Training,Definitions.Calendar,Career.Week,candidate);training.Add(kind);
        }
        public void RemoveTraining(int index){Require(FrontEndScreen.Training);training.RemoveAt(index);}
        public void ClearTraining(){Require(FrontEndScreen.Training);training.Clear();}
        public CareerAthlete TrainingPreview()
        {
            Require(FrontEndScreen.Training);var prepared=CareerSeasonEngine.PrepareWeek(Career,Definitions,training,restoreRandom);
            var projected=CareerSave.FromJson(prepared.RaceSnapshotJson);
            for(int i=0;i<projected.AthleteCount;i++)if(projected.Athlete(i).Id==projected.PlayerId)return projected.Athlete(i);
            throw new InvalidOperationException("Missing projected player.");
        }
        public void CompleteTrainingWeek()
        {
            Require(FrontEndScreen.Career);if(Definitions.Calendar.AtWeek(Career.Week)!=null)throw new InvalidOperationException("A race week requires its event.");
            var prepared=CareerSeasonEngine.PrepareWeek(Career,Definitions,training,restoreRandom);
            var candidate=CareerSeasonEngine.CompleteWeek(prepared,Definitions,null,restoreRandom);
            PublishSaved(candidate,prepared.SourceJson);training.Clear();
        }
        public void Equip(string skiId)
        {
            Require(FrontEndScreen.Equipment);string expected=Career.ToJson();var candidate=CareerSave.FromJson(expected);CareerAthlete player=null;
            for(int i=0;i<candidate.AthleteCount;i++)if(candidate.Athlete(i).Id==candidate.PlayerId)player=candidate.Athlete(i);
            var catalog=EquipmentCatalog;if(!CareerInventory.Owns(player,skiId))CareerEconomy.BuyEquipment(player,Definitions.Economy,catalog,skiId);
            CareerInventory.Select(player,catalog,skiId);PublishSaved(candidate,expected);
        }
        public void RestWeek()
        {
            Require(FrontEndScreen.Career);
            if(Definitions.Calendar.AtWeek(Career.Week)!=null)throw new InvalidOperationException("A race week requires its event.");
            var prepared=CareerSeasonEngine.PrepareWeek(Career,Definitions,Array.Empty<CareerTrainingKind>(),restoreRandom);
            var candidate=CareerSeasonEngine.CompleteWeek(prepared,Definitions,null,restoreRandom);
            PublishSaved(candidate,prepared.SourceJson);
            training.Clear();
        }
        public void OpenPreRace()
        {
            Require(FrontEndScreen.Career);
            var e=Definitions.Calendar.AtWeek(Career.Week);
            if(e==null||!e.Eligible(Player))throw new InvalidOperationException("An eligible current-week event is required.");
            var prepared=CareerSeasonEngine.PrepareWeek(Career,Definitions,training,restoreRandom);
            PreparedWeek=prepared;Result=null;ResultsSummary=null;Screen=FrontEndScreen.PreRace;
        }
        public void BeginRace() {Require(FrontEndScreen.PreRace);Screen=FrontEndScreen.Race;}
        public void AbandonRace() {Require(FrontEndScreen.Race);Screen=FrontEndScreen.PreRace;}
        public void FinishRace(CareerRaceRecord result)
        {
            Require(FrontEndScreen.Race);
            var candidate=CareerSeasonEngine.CompleteWeek(PreparedWeek,Definitions,result,restoreRandom);
            var summary=new RaceResultsSummary(CareerSave.FromJson(PreparedWeek.SourceJson),result,Definitions.Scoring);
            PublishSaved(candidate,PreparedWeek.SourceJson);
            Result=result;ResultsSummary=summary;PreparedWeek=null;training.Clear();Screen=FrontEndScreen.Results;
        }
        public void ReturnFromResults() {Require(FrontEndScreen.Results);Screen=FrontEndScreen.Career;}
        private void PublishSaved(CareerSave candidate,string expected)
        {profiles.Replace(candidate.Id,expected,candidate.ToJson());Career=candidate;Definitions=CareerDefinitions.ForSave(read,candidate);}
        public void CreateCareer(string id,string name,string nation,ulong seed,string startProfileId="junior_novice",int? age=null)
        {
            Require(FrontEndScreen.NewCareer);
            var definitions=CareerDefinitions.ForNewProductionCareer(read,startProfileId);
            var candidate=CareerFactory.Create(definitions.Roster,definitions.Calendar,id,name?.Trim(),nation,"player_appearance",restoreRandom(seed),StartProfiles,startProfileId,age);
            candidate=CareerWorld.Initialize(candidate,definitions,restoreRandom);
            CareerDevelopment.InitializeWorld(candidate,definitions.Growth,definitions.Ratings,restoreRandom);
            // Publish only after the durable write succeeds. New careers never overwrite another profile.
            string json=candidate.ToJson();CareerSave.FromJson(json);profiles.WriteNew(id,json);
            Career=candidate;Definitions=definitions;training.Clear();Screen=FrontEndScreen.Career;
        }
        public void CreateCareer(string id,CareerCreationDraft draft)
        {
            Require(FrontEndScreen.NewCareer);if(draft==null)throw new ArgumentNullException(nameof(draft));
            var definitions=CareerDefinitions.ForNewProductionCareer(read,draft.StartProfileId);
            var candidate=draft.Confirm(id,definitions.Calendar,restoreRandom);
            candidate=CareerWorld.Initialize(candidate,definitions,restoreRandom);
            CareerDevelopment.InitializeWorld(candidate,definitions.Growth,definitions.Ratings,restoreRandom,draft.Tier.BaselineStat);
            string json=candidate.ToJson();CareerSave.FromJson(json);profiles.WriteNew(id,json);
            Career=candidate;Definitions=definitions;training.Clear();Screen=FrontEndScreen.Career;
        }
        public void CreateCareer(CareerAthleteCatalog catalog,string athleteId)
        {
            Require(FrontEndScreen.NewCareer);if(catalog==null)throw new ArgumentNullException(nameof(catalog));
            var candidate=catalog.Confirm(athleteId);var definitions=CareerDefinitions.ForSave(read,candidate);
            candidate=CareerFactory.Restore(candidate.ToJson(),definitions.Roster,definitions.Calendar,restoreRandom(candidate.RandomState));
            string json=candidate.ToJson();profiles.WriteNew(candidate.Id,json);
            Career=candidate;Definitions=definitions;training.Clear();Screen=FrontEndScreen.Career;
        }
        public void ContinueCareer()
        {
            Require(FrontEndScreen.Home);
            var decoded=CareerSave.FromJson(profiles.ReadLatest());
            var definitions=CareerDefinitions.ForSave(read,decoded);
            var candidate=CareerFactory.Restore(decoded.ToJson(),definitions.Roster,definitions.Calendar,restoreRandom(decoded.RandomState));
            Career=candidate;Definitions=definitions;training.Clear();Screen=FrontEndScreen.Career;
        }
        public CareerCalendarEvent NextEvent()
        {
            if(Career==null)return null;
            for(int i=0;i<Definitions.Calendar.Count;i++)
            {
                var e=Definitions.Calendar.Get(i);bool completed=false;
                for(int r=0;r<Career.RaceCount;r++)completed|=Career.Race(r).CalendarEventId==e.Id;
                if(e.Week>=Career.Week&&!completed&&e.Eligible(Player))return e;
            }
            return null;
        }
        private void Require(FrontEndScreen screen)
        { if(Screen!=screen)throw new InvalidOperationException("Invalid front-end transition."); }
    }
}
