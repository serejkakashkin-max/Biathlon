using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct CorneringOutput
    {
        public double LateralAccelerationLimit { get; }
        public double SafeSpeedMetersPerSecond { get; }
        public double ExcessSpeedMetersPerSecond { get; }
        public double BrakingNewtons { get; }
        public bool IsOverspeed => ExcessSpeedMetersPerSecond > 0;
        internal CorneringOutput(double lateral, double safe, double excess, double braking)
        { LateralAccelerationLimit = lateral; SafeSpeedMetersPerSecond = safe; ExcessSpeedMetersPerSecond = excess; BrakingNewtons = braking; }
    }
    public sealed class CorneringSystem
    {
        private readonly CorneringDefinition definition;
        private readonly SkiPhysicsDefinition physics;
        public CorneringSystem(CorneringDefinition definition, SkiPhysicsDefinition physics)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.physics = physics ?? throw new ArgumentNullException(nameof(physics));
            if (definition.MaximumBraking * physics.MassKilograms > PhysiologyNumbers.MaximumMagnitude)
                throw new ArgumentException("Braking force exceeds supported numerical range.");
        }
        public CorneringOutput Evaluate(SkiState state, TrackSample track, int corneringSkill, int downhillSkill,
            double lineQuality, double muscularFatigue, double gripMultiplier = 1)
            => EvaluateCore(state,track,corneringSkill,downhillSkill,lineQuality,muscularFatigue,gripMultiplier,null);

        /// <summary>Observed yaw has lateral demand planarSpeed * abs(yawRate).
        /// It cannot inherit braking from a centre-line turn the skier is not making.</summary>
        public CorneringOutput EvaluateWorldHeading(SkiState state,TrackSample surface,double yawRate,int corneringSkill,int downhillSkill,
            double lineQuality,double muscularFatigue,double gripMultiplier=1)
        {
            PhysiologyNumbers.RequireRange(yawRate,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(yawRate));
            return EvaluateCore(state,surface,corneringSkill,downhillSkill,lineQuality,muscularFatigue,gripMultiplier,yawRate);
        }
        private CorneringOutput EvaluateCore(SkiState state,TrackSample track,int corneringSkill,int downhillSkill,
            double lineQuality,double muscularFatigue,double gripMultiplier,double? actualYawRate)
        {
            if (corneringSkill < 0 || corneringSkill > 100 || downhillSkill < 0 || downhillSkill > 100)
                throw new ArgumentOutOfRangeException(nameof(corneringSkill));
            if (string.IsNullOrEmpty(track.SegmentId)) throw new ArgumentException("A valid track sample is required.", nameof(track));
            PhysiologyNumbers.RequireRange(lineQuality, 0, 1, nameof(lineQuality));
            PhysiologyNumbers.RequireRange(muscularFatigue, 0, 1, nameof(muscularFatigue));
            PhysiologyNumbers.RequireRange(gripMultiplier, PhysiologyNumbers.MinimumPositive, PhysiologyNumbers.MaximumMagnitude, nameof(gripMultiplier));
            PhysiologyNumbers.RequireRange(state.SpeedMetersPerSecond, 0, physics.MaximumSpeedMetersPerSecond, nameof(state));
            double lateral = definition.NoviceLateralAcceleration + (definition.ExpertLateralAcceleration - definition.NoviceLateralAcceleration) * corneringSkill / 100.0;
            lateral *= definition.PoorLineModifier + (1 - definition.PoorLineModifier) * lineQuality;
            lateral *= 1 - muscularFatigue * (1 - definition.ExhaustedModifier);
            lateral *= 1 - track.RiskLevel * (1 - definition.MaximumRiskModifier);
            double descentWeight = Math.Min(1, Math.Max(0, track.Grade / definition.FullDownhillGrade));
            lateral *= 1 - descentWeight * (1 - definition.NoviceDownhillModifier) * (1 - downhillSkill / 100.0);
            lateral *= gripMultiplier;
            PhysiologyNumbers.RequireRange(lateral, 0, PhysiologyNumbers.MaximumMagnitude, nameof(gripMultiplier));
            double curvature = Math.Abs(track.CurvaturePerMeter);
            double safe = curvature == 0 ? physics.MaximumSpeedMetersPerSecond :
                Math.Min(physics.MaximumSpeedMetersPerSecond, Math.Sqrt(lateral / curvature));
            if(actualYawRate.HasValue)
            {
                double rate=Math.Abs(actualYawRate.Value);
                double planarScale=1/Math.Sqrt(1+track.Grade*track.Grade);
                safe=rate==0?physics.MaximumSpeedMetersPerSecond:
                    Math.Min(physics.MaximumSpeedMetersPerSecond,lateral/(rate*planarScale));
            }
            double excess = Math.Max(0, state.SpeedMetersPerSecond - safe);
            double braking = Math.Min(definition.MaximumBraking, excess / definition.BrakingResponseSeconds) * physics.MassKilograms;
            return new CorneringOutput(lateral, safe, excess, braking);
        }
    }
}
