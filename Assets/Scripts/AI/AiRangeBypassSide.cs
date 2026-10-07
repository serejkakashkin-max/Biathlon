using System;
using Biathlon.Domain;

namespace Biathlon.AI
{
    /// <summary>Choose the side away from the nearest observed forward body.
    /// This is only a goal ordering; physical admission still checks both sides.</summary>
    public static class AiRangeBypassSide
    {
        public static bool PreferRight(ReadOnlySpan<SkiPlacement> roster,
            double lookaheadMeters,double conflictLateralMeters)
        {
            if(roster.Length==0||!roster[0].IsValid||
                double.IsNaN(lookaheadMeters)||double.IsInfinity(lookaheadMeters)||lookaheadMeters<=0||
                double.IsNaN(conflictLateralMeters)||double.IsInfinity(conflictLateralMeters)||conflictLateralMeters<=0)
                throw new ArgumentException("Valid local roster and positive lookahead/separation required.");
            var self=roster[0];
            long lookahead=checked((long)Math.Round(lookaheadMeters*SkiState.NanometersPerMeter));
            long nearest=long.MaxValue;string nearestId=null;double obstacle=0;
            for(int i=1;i<roster.Length;i++)
            {
                var other=roster[i];if(!other.IsValid||other.AthleteId.Equals(self.AthleteId))
                    throw new ArgumentException("Local roster must contain distinct valid athletes.");
                long gap=other.Motion.DistanceNanometers-self.Motion.DistanceNanometers;
                if(gap<=0||gap>lookahead||
                    Math.Abs(other.LateralMeters-self.LateralMeters)>conflictLateralMeters)continue;
                if(gap<nearest||gap==nearest&&string.CompareOrdinal(other.AthleteId.Value,nearestId)<0)
                {nearest=gap;nearestId=other.AthleteId.Value;obstacle=other.LateralMeters;}
            }
            if(nearest==long.MaxValue)return false;
            return obstacle<self.LateralMeters||
                obstacle==self.LateralMeters&&self.LateralMeters<=0;
        }
    }
}
