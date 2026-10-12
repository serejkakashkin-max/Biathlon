using System;
using Biathlon.Domain;
namespace Biathlon.Presentation
{
    /// <summary>Continuous HR contribution from the same breathing formula as shooting; zones are guidance, not hit bonuses.</summary>
    public readonly struct ShootingReadiness
    {
        public double Load {get;}
        public double BreathingAmplitudeRatio {get;}
        public int Zone {get;}
        public ShootingReadiness(double heartRate,double fatigue,PhysiologyDefinition physiology,BreathingDefinition breathing,double calmLoad,double highLoad)
        {
            if(physiology==null||breathing==null||calmLoad<0||highLoad<=calmLoad||highLoad>1)throw new ArgumentException("Valid shooting guidance thresholds required.");
            PhysiologyNumbers.RequireRange(heartRate,physiology.RestingHrBpm,physiology.HrSafetyCeilingBpm,nameof(heartRate));
            PhysiologyNumbers.RequireRange(fatigue,0,1,nameof(fatigue));
            Load=Math.Min(1,(heartRate-physiology.RestingHrBpm)/(physiology.MaxHrBpm-physiology.RestingHrBpm));
            BreathingAmplitudeRatio=(1+(breathing.HighHrMultiplier-1)*Load)*(1+breathing.FatigueGain*fatigue);
            Zone=Load<=calmLoad?0:Load<highLoad?1:2;
        }
    }
}
