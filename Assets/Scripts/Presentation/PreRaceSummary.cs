using System;
using Biathlon.Career;
using Biathlon.Domain;

namespace Biathlon.Presentation
{
    /// <summary>Actual event/rules/readiness/equipment for a detached prepared week.</summary>
    public sealed class PreRaceSummary
    {
        public CareerCalendarEvent Event { get; }
        public CareerAthlete Player { get; }
        public CompetitionProfile Profile { get; }
        public SkiEquipmentDefinition Skis { get; }
        public string SnowProfileId { get; }
        public int EntrantCount { get; }
        public PreRaceSummary(CareerWeekPreparation week,CompetitionProfile profile,
            RaceSnowConditions conditions,SkiEquipmentCatalog equipment)
        {
            if(week==null||profile==null||conditions==null||equipment==null)throw new ArgumentNullException();
            if(!week.RequiresRace||week.Event.Format!=profile.Format)throw new ArgumentException("A matching prepared event is required.");
            var save=CareerSave.FromJson(week.RaceSnapshotJson);Event=week.Event;Profile=profile;
            for(int i=0;i<save.AthleteCount;i++)
            {
                var a=save.Athlete(i);if(Event.Eligible(a))EntrantCount++;
                if(a.Id==save.PlayerId)Player=a;
            }
            if(Player==null||!Event.Eligible(Player)||EntrantCount>profile.FieldCapacity)throw new ArgumentException("Playable eligible field required.");
            Skis=equipment.GetSki(Player.SelectedSki);SnowProfileId=conditions.GetSurface(0).ProfileId;
        }
    }
}
