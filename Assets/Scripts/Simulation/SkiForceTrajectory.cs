using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>The same frozen-force velocity ramp as SkiForceSystem, including a
    /// mid-step stop or speed cap. Distance is physical ski travel, not route progress.</summary>
    public readonly struct SkiForceTrajectory
    {
        public SkiState InitialState { get; }
        public SkiForceOutput Output { get; }
        public double DurationSeconds { get; }
        public double Acceleration { get; }
        public double MaximumSpeed { get; }
        public double SpeedBoundarySeconds { get; }
        public bool IsValid => DurationSeconds > 0;
        public double PeakSpeed => Math.Max(InitialState.SpeedMetersPerSecond, SpeedRaw(DurationSeconds));

        internal SkiForceTrajectory(SkiState initial, SkiForceOutput output, double duration,
            double mass, double maximumSpeed)
        {
            InitialState=initial;Output=output;DurationSeconds=duration;
            Acceleration=output.NetNewtons/mass;MaximumSpeed=maximumSpeed;
            double speed=initial.SpeedMetersPerSecond;
            SpeedBoundarySeconds=Acceleration<0 ? -speed/Acceleration :
                Acceleration>0 ? (maximumSpeed-speed)/Acceleration : double.PositiveInfinity;
        }

        public double SpeedAt(double seconds) { Validate(seconds);return SpeedRaw(seconds); }
        public double DistanceAt(double seconds)
        {
            Validate(seconds);
            double changing=Math.Min(seconds,SpeedBoundarySeconds);
            return (InitialState.SpeedMetersPerSecond+SpeedRaw(changing))*.5*changing
                +SpeedRaw(changing)*(seconds-changing);
        }
        public SkiState StateAt(double seconds)
        {
            Validate(seconds);
            if(seconds==0)return InitialState;
            if(seconds==DurationSeconds)return Output.State;
            long travel=checked((long)Math.Round(DistanceAt(seconds)*SkiState.NanometersPerMeter,MidpointRounding.ToEven));
            return new SkiState(checked(InitialState.DistanceNanometers+travel),SpeedRaw(seconds));
        }
        internal double SpeedRaw(double seconds) => Math.Min(MaximumSpeed,
            Math.Max(0,InitialState.SpeedMetersPerSecond+Acceleration*seconds));
        private void Validate(double seconds)
        {
            if(!IsValid)throw new InvalidOperationException("Initialized ski force trajectory required.");
            PhysiologyNumbers.RequireRange(seconds,0,DurationSeconds,nameof(seconds));
        }
    }
}
