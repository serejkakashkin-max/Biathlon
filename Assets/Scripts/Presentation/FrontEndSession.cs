using System;
using Biathlon.Career;
using Biathlon.Domain;

namespace Biathlon.Presentation
{
    public enum FrontEndScreen { Home, NewCareer, Career, Settings }
    public interface ICareerProfileStore
    {
        bool HasProfile { get; }
        string ReadLatest();
        void WriteNew(string id, string json);
    }

    /// <summary>Menu transitions and career boundary; no Unity, scene references or simulated outcomes.</summary>
    public sealed class FrontEndSession
    {
        private readonly Func<string,string> read;
        private readonly ICareerProfileStore profiles;
        private readonly Func<ulong,IRandomSource> restoreRandom;
        private FrontEndScreen settingsReturn;
        public FrontEndScreen Screen { get; private set; }
        public CareerSave Career { get; private set; }
        public CareerDefinitions Definitions { get; private set; }
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
        { Screen=Screen==FrontEndScreen.Settings?settingsReturn:FrontEndScreen.Home; }
        public void CreateCareer(string id,string name,string nation,ulong seed)
        {
            Require(FrontEndScreen.NewCareer);
            var definitions=CareerDefinitions.ForNewProductionCareer(read);
            var candidate=CareerFactory.Create(definitions.Roster,definitions.Calendar,id,name?.Trim(),nation,"player_appearance",restoreRandom(seed));
            // Publish only after the durable write succeeds. New careers never overwrite another profile.
            string json=candidate.ToJson();CareerSave.FromJson(json);profiles.WriteNew(id,json);
            Career=candidate;Definitions=definitions;Screen=FrontEndScreen.Career;
        }
        public void ContinueCareer()
        {
            Require(FrontEndScreen.Home);
            var decoded=CareerSave.FromJson(profiles.ReadLatest());
            var definitions=CareerDefinitions.ForSave(read,decoded);
            var candidate=CareerFactory.Restore(decoded.ToJson(),definitions.Roster,definitions.Calendar,restoreRandom(decoded.RandomState));
            Career=candidate;Definitions=definitions;Screen=FrontEndScreen.Career;
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
