using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Career
{
    /// <summary>One detached, unbiased draw for every eligible Sprint entrant, including the player.</summary>
    public static class CareerSprintStartDraw
    {
        public static string[] Capture(CareerSave save,CareerCalendarEvent next,IRandomSource random)
        {
            if(save==null||next==null||random==null)throw new ArgumentNullException();
            if(next.Format!=CompetitionFormat.Sprint)throw new ArgumentException("Sprint start draw requires a Sprint event.");
            var ids=new List<string>();
            for(int i=0;i<save.AthleteCount;i++)if(next.Eligible(save.Athlete(i)))ids.Add(save.Athlete(i).Id);
            if(ids.Count==0)throw new ArgumentException("Sprint draw requires eligible entrants.");
            for(int i=ids.Count-1;i>0;i--)
            {
                // Rejection sampling avoids modulo bias; this is a dedicated draw stream,
                // never the live career or athlete shooting/decision RNG.
                ulong size=(ulong)(i+1),threshold=unchecked(0UL-size)%size,value;
                do{value=random.NextUInt64();}while(value<threshold);
                int other=(int)(value%size);var id=ids[i];ids[i]=ids[other];ids[other]=id;
            }
            return ids.ToArray();
        }
    }
}
