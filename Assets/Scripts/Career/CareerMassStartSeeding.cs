using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>Detached pre-start list; its order supplies physical grid slots and bibs.</summary>
    public static class CareerMassStartSeeding
    {
        public static string[] Capture(CareerSave save,CareerCalendar calendar,CareerScoringDefinition scoring,CareerCalendarEvent next)
        {
            if(save==null||calendar==null||scoring==null||next==null)throw new ArgumentNullException();
            if(save.CalendarId!=calendar.Id||next.Format!=CompetitionFormat.MassStart)throw new ArgumentException("Matching career Mass Start required.");
            bool found=false;for(int i=0;i<calendar.Count;i++)if(calendar.Get(i).Id==next.Id){next=calendar.Get(i);found=true;break;}
            if(!found)throw new ArgumentException("Start event must be in the saved calendar.");
            var eligible=new HashSet<string>(StringComparer.Ordinal);for(int i=0;i<save.AthleteCount;i++)if(next.Eligible(save.Athlete(i)))eligible.Add(save.Athlete(i).Id);
            var total=CareerStandings.BeforeStart(save,scoring,calendar,next);
            var totalOrder=new Dictionary<string,int>(StringComparer.Ordinal);var result=new List<string>();var remaining=new List<CareerStanding>();
            int priority=calendar.MassStartTotalPriority;
            // A calendar without meeting results is the game's season-ranked format.
            bool meeting=priority>0&&next.MeetingId!=null;
            for(int i=0;i<total.Length;i++)
            {
                totalOrder.Add(total[i].AthleteId,i);if(!eligible.Contains(total[i].AthleteId))continue;
                if(!meeting||result.Count<priority)result.Add(total[i].AthleteId);
            }
            if(meeting)
            {
                var seeded=new HashSet<string>(result,StringComparer.Ordinal);
                foreach(var row in CareerStandings.BeforeStart(save,scoring,calendar,next,meetingOnly:true))
                    if(eligible.Contains(row.AthleteId)&&!seeded.Contains(row.AthleteId))remaining.Add(row);
                remaining.Sort((a,b)=>
                {
                    int c=b.Points.CompareTo(a.Points);if(c==0)c=BestPlace(a,save.AthleteCount).CompareTo(BestPlace(b,save.AthleteCount));
                    return c!=0?c:totalOrder[a.AthleteId].CompareTo(totalOrder[b.AthleteId]);
                });
                foreach(var row in remaining)result.Add(row.AthleteId);
            }
            return result.ToArray();
        }
        private static int BestPlace(CareerStanding row,int count)
        {for(int i=1;i<=count;i++)if(row.FinishesAt(i)>0)return i;return int.MaxValue;}
    }
}
