using System;
namespace Biathlon.Presentation
{
    // Presentation time only. Race/input clocks stay at tick zero until Ready.
    public sealed class RaceStartCountdown
    {
        public double Remaining {get;private set;}
        public bool Ready=>Remaining==0;
        public int Number=>(int)Math.Ceiling(Remaining);
        public RaceStartCountdown(double seconds)
        {if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<=0)throw new ArgumentOutOfRangeException(nameof(seconds));Remaining=seconds;}
        public void Advance(double seconds)
        {if(double.IsNaN(seconds)||double.IsInfinity(seconds)||seconds<0)throw new ArgumentOutOfRangeException(nameof(seconds));Remaining=Math.Max(0,Remaining-seconds);}
    }
}
