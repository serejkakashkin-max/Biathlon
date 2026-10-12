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
        [DataMember(Name="maximum_age",EmitDefaultValue=false)] public int MaximumAge {get;private set;}
        [DataMember(Name="meeting_id",EmitDefaultValue=false)] public string MeetingId {get;private set;}
        public bool Eligible(CareerAthlete athlete)=>athlete!=null&&athlete.Tier>=MinimumTier&&(MaximumAge==0||athlete.Age<=MaximumAge);
    }
    [DataContract] public sealed class CareerCalendar
    {
        private CareerCalendar() { }
        [DataMember(Name="schema_version",IsRequired=true)] public int SchemaVersion { get; private set; }
        [DataMember(Name="id",IsRequired=true)] public string Id { get; private set; }
        [DataMember(Name="weeks_per_season",IsRequired=true)] public int WeeksPerSeason { get; private set; }
        [DataMember(Name="training_slots_per_week",IsRequired=true)] public int TrainingSlotsPerWeek { get; private set; }
        [DataMember(Name="race_week_training_slots",IsRequired=true)] public int RaceWeekTrainingSlots { get; private set; }
        [DataMember(Name="circuit",EmitDefaultValue=false)] public CareerCircuit Circuit {get;private set;}
        [DataMember(Name="leader_bib_roles",EmitDefaultValue=false)] private int? leaderBibRoles {get;set;}
        public CareerLeaderBib LeaderBibRoles=>leaderBibRoles.HasValue?(CareerLeaderBib)leaderBibRoles.Value:
            Circuit==CareerCircuit.WorldCup?CareerLeaderBib.Total|CareerLeaderBib.Discipline:
            Circuit==CareerCircuit.Development?CareerLeaderBib.Total:CareerLeaderBib.None;
        [DataMember(Name="mass_start_total_priority",EmitDefaultValue=false)] public int MassStartTotalPriority {get;private set;}
        [DataMember(Name="mass_start_number_from_right",EmitDefaultValue=false)] public bool MassStartNumberFromRight {get;private set;}
        [DataMember(Name="events",IsRequired=true)] private CareerCalendarEvent[] events { get; set; }
        public int Count=>events.Length;public CareerCalendarEvent Get(int index)=>events[index];
        public CareerCalendarEvent AtWeek(int week){CareerUnits.Range(week,1,WeeksPerSeason,"week");foreach(var e in events)if(e.Week==week)return e;return null;}
        public int TrainingSlots(int week)=>AtWeek(week)==null?TrainingSlotsPerWeek:RaceWeekTrainingSlots;
        public string ToJson()=>CareerJson.Write(this);
        public static CareerCalendar FromJson(string json)
        {
            var d=CareerJson.Read<CareerCalendar>(json);if(d==null||d.SchemaVersion!=1)throw new SerializationException("Invalid calendar schema.");CareerUnits.Id(d.Id);
            CareerUnits.Range(d.WeeksPerSeason,1,366,"season length");CareerUnits.Range(d.TrainingSlotsPerWeek,1,14,"training slots");CareerUnits.Range(d.RaceWeekTrainingSlots,0,d.TrainingSlotsPerWeek,"race training slots");
            CareerUnits.Range((int)d.Circuit,0,3,"career circuit");
            if(d.leaderBibRoles.HasValue)CareerUnits.Range(d.leaderBibRoles.Value,0,3,"leader bib roles");
            CareerUnits.Range(d.MassStartTotalPriority,0,60,"mass start score priority");
            if(d.events==null||d.events.Length<1||d.events.Length>d.WeeksPerSeason)throw new SerializationException("Invalid calendar events.");
            for(int i=0;i<d.Count;i++)
            {
                var e=d.events[i];if(e==null)throw new SerializationException("Null calendar event.");CareerUnits.Id(e.Id);CareerUnits.Id(e.VenueId);CareerUnits.Range(e.Week,1,d.WeeksPerSeason,"event week");CareerUnits.Range((int)e.MinimumTier,0,2,"tier");
                if(e.MaximumAge!=0)CareerUnits.Range(e.MaximumAge,16,60,"maximum event age");
                if(e.MeetingId!=null)CareerUnits.Id(e.MeetingId);
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
