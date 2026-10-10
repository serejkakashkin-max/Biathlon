using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    // Produces bounded signed intent only. Shared ski forces own all actual yaw
    // and travel; held manual steering always takes immediate precedence.
    public sealed class PlayerCourseReturnSystem
    {
        private readonly PlayerCourseAssistanceDefinition definition;
        private readonly double dt;
        private double releasedSeconds,returnInput;
        private bool armed,captured;
        private double releaseHeading;
        public double? ReleaseHeadingRadians=>captured?releaseHeading:(double?)null;
        public void Reset(){armed=captured=false;releasedSeconds=returnInput=0;}
        public PlayerCourseReturnSystem(PlayerCourseAssistanceDefinition definition,int frequency)
        {this.definition=definition??throw new ArgumentNullException(nameof(definition));if(frequency<=0)throw new ArgumentOutOfRangeException(nameof(frequency));dt=1.0/frequency;}
        public int Evaluate(int manualInput,int routeIntent,bool travelling)
        {
            if(Math.Abs(manualInput)>ManualSkiCommand.SteeringUnits||Math.Abs(routeIntent)>ManualSkiCommand.SteeringUnits)throw new ArgumentOutOfRangeException();
            if(manualInput!=0||!travelling){releasedSeconds=returnInput=0;return manualInput;}
            releasedSeconds+=dt;
            if(releasedSeconds<=definition.ReleaseDelay)return 0;
            double wanted=Math.Max(-definition.MaximumReturnInput,Math.Min(definition.MaximumReturnInput,routeIntent*definition.ReturnGain));
            double change=definition.ReturnInputPerSecond*dt;
            returnInput+=Math.Max(-change,Math.Min(change,wanted-returnInput));
            return (int)Math.Round(returnInput,MidpointRounding.ToEven);
        }
        // Only straighten toward the tangent captured where steering ended.
        // No target position, centreline or future course tangent is followed.
        public int Evaluate(int manualInput,ManualSkiHeadingState pose,double localHeading,bool travelling)
        {
            if(!pose.IsValid||!double.IsFinite(localHeading))throw new ArgumentException("Actual heading required.");
            if(manualInput!=0){armed=true;captured=false;return Evaluate(manualInput,0,travelling);}
            if(!travelling){armed=captured=false;return Evaluate(0,0,false);}
            if(!armed)return 0;
            if(!captured){releaseHeading=localHeading;captured=true;}
            double delta=releaseHeading-pose.HeadingRadians;
            int intent=(int)Math.Round(Math.Max(-1,Math.Min(1,Math.Atan2(Math.Sin(delta),Math.Cos(delta))))*ManualSkiCommand.SteeringUnits);
            return Evaluate(0,intent,true);
        }
    }
}
