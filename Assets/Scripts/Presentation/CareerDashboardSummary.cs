using System;
using Biathlon.Career;

namespace Biathlon.Presentation
{
    public sealed class CareerCalendarRow
    {
        public CareerCalendarEvent Event { get; }
        public bool Completed { get; }
        public bool Current { get; }
        public bool Eligible { get; }
        public bool Past { get; }
        public int? PlayerRank { get; }
        public int? PlayerPoints { get; }
        internal CareerCalendarRow(CareerCalendarEvent e,CareerSave save,CareerAthlete player,CareerScoringDefinition scoring)
        {
            Event=e;Current=e.Week==save.Week;Past=e.Week<save.Week;Eligible=e.Eligible(player);
            for(int r=0;r<save.RaceCount;r++)
            {
                var race=save.Race(r);if(race.CalendarEventId!=e.Id)continue;Completed=true;
                for(int rank=0;rank<race.Count;rank++)if(race.Get(rank).AthleteId==player.Id){PlayerRank=CareerStandings.ResultRank(race,rank,scoring);PlayerPoints=scoring.Points(PlayerRank.Value,race.Format);}
                break;
            }
        }
    }

    /// <summary>Read-only current-season calendar/readiness/ranking. Opening a page never simulates a week.</summary>
    public sealed class CareerDashboardSummary
    {
        private readonly CareerCalendarRow[] calendar;
        public CareerAthlete Player { get; }
        public int Season { get; }
        public int Week { get; }
        public int RaceCount { get; }
        public int Points { get; }
        public int Wins { get; }
        public int Podiums { get; }
        public int? Standing { get; }
        public CareerCalendarRow Next { get; }
        public int CalendarCount=>calendar.Length;
        public CareerCalendarRow Calendar(int index)=>calendar[index];
        public CareerDashboardSummary(CareerSave save,CareerDefinitions definitions)
        {
            if(save==null||definitions==null)throw new ArgumentNullException();
            if(save.CalendarId!=definitions.Calendar.Id)throw new ArgumentException("Saved calendar and dashboard definitions disagree.");
            Season=save.Season;Week=save.Week;RaceCount=save.RaceCount;
            for(int i=0;i<save.AthleteCount;i++)if(save.Athlete(i).Id==save.PlayerId)Player=save.Athlete(i);
            if(Player==null)throw new ArgumentException("Career player required.");
            var table=CareerStandings.Table(save,definitions.Scoring);
            for(int i=0;i<table.Length;i++)if(table[i].AthleteId==Player.Id)
            {Points=table[i].Points;Wins=table[i].Wins;Podiums=table[i].Podiums;if(RaceCount>0)Standing=i+1;break;}
            calendar=new CareerCalendarRow[definitions.Calendar.Count];
            for(int i=0;i<calendar.Length;i++)
            {
                var row=new CareerCalendarRow(definitions.Calendar.Get(i),save,Player,definitions.Scoring);calendar[i]=row;
                if(Next==null&&!row.Completed&&!row.Past&&row.Eligible)Next=row;
            }
        }
    }
}
