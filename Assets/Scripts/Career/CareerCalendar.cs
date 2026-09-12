using System;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Career
{
    [DataContract] public sealed class CareerCalendarEvent
    {
        private CareerCalendarEvent() { }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="week",IsRequired=true)] public int Week { get; private set; }
        [DataMember(Name="venue_id",IsRequired=true)] public string VenueId { get; private set; }
        [DataMember(Name="format",IsRequired=true)] public CompetitionFormat Format { get; private set; }
        [DataMember(Name="source_event_id")] public string SourceEventId { get; private set; }
        [DataMember(Name="minimum_tier",IsRequired=true)] public CareerTier MinimumTier { get; private set; }
        public bool Eligible(CareerAthlete athlete)=>athlete!=null&&athlete.Tier>=MinimumTier;
    }
    [DataContract] public sealed class CareerCalendar
    {
        private CareerCalendar() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="weeks_per_season",IsRequired=true)] public int WeeksPerSeason { get; private set; }
        [DataMember(Name="training_slots_per_week",IsRequired=true)] public int TrainingSlotsPerWeek { get; private set; }
        [DataMember(Name="race_week_training_slots",IsRequired=true)] public int RaceWeekTrainingSlots { get; private set; }
        [DataMember(Name="events",IsRequired=true)] private CareerCalendarEvent[] events { get; set; }
        public int Count=>events.Length;public CareerCalendarEvent Get(int index)=>events[index];
        public CareerCalendarEvent AtWeek(int week){CareerUnits.Range(week,1,WeeksPerSeason,"week");foreach(var e in events)if(e.Week==week)return e;return null;}
        public int TrainingSlots(int week)=>AtWeek(week)==null?TrainingSlotsPerWeek:RaceWeekTrainingSlots;
        public string ToJson()=>CareerJson.Write(this);
        public static CareerCalendar FromJson(string json)
        {
            var d=CareerJson.Read<CareerCalendar>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid calendar schema.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.WeeksPerSeason,1,366,"season length");CareerUnits.Range(d.TrainingSlotsPerWeek,1,14,"training slots");CareerUnits.Range(d.RaceWeekTrainingSlots,0,d.TrainingSlotsPerWeek,"race training slots");
            if(d.events==null||d.events.Length<1||d.events.Length>d.WeeksPerSeason)throw new SerializationException("Invalid calendar events.");
            for(int i=0;i<d.Count;i++)
            {
                var e=d.events[i];if(e==null)throw new SerializationException("Null calendar event.");CareerUnits.Id(e.Id);CareerUnits.Id(e.VenueId);CareerUnits.Range(e.Week,1,d.WeeksPerSeason,"event week");CareerUnits.Range((int)e.MinimumTier,0,2,"tier");
                if(e.Format!=CompetitionFormat.Sprint&&e.Format!=CompetitionFormat.Pursuit&&e.Format!=CompetitionFormat.MassStart)throw new SerializationException("Unsupported career race format.");
                for(int j=0;j<i;j++)if(d.events[j].Id==e.Id||d.events[j].Week>=e.Week)throw new SerializationException("Calendar must be uniquely identified and ordered by week.");
                if(e.Format==CompetitionFormat.Pursuit)
                {bool found=false;for(int j=0;j<i;j++)if(d.events[j].Id==e.SourceEventId&&d.events[j].Format==CompetitionFormat.Sprint&&d.events[j].VenueId==e.VenueId&&d.events[j].MinimumTier==e.MinimumTier)found=true;if(!found)throw new SerializationException("Pursuit needs an earlier matching Sprint.");}
                else if(e.SourceEventId!=null)throw new SerializationException("Only Pursuit consumes a source event.");
            }
            return d;
        }
    }
}
