using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum TuckRejection { NotRequested, None, Grade, Speed, Curvature, Risk }
    public readonly struct TuckOutput
    {
        public bool Active => Rejection == TuckRejection.None;
        public TuckRejection Rejection { get; }
        public int EffectiveEffortControl { get; }
        public double DragAreaMultiplier { get; }
        public double SteeringResponseMultiplier { get; }
        internal TuckOutput(TuckRejection rejection, int effort, double drag, double steering)
        { Rejection = rejection; EffectiveEffortControl = effort; DragAreaMultiplier = drag; SteeringResponseMultiplier = steering; }
    }
    public sealed class TuckSystem
    {
        private readonly TuckDefinition definition;
        public TuckSystem(TuckDefinition definition)
        { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public TuckOutput Evaluate(bool requested, int effortControl, int downhillSkill, SkiState state, TrackSample track)
        {
            if (effortControl < 0 || effortControl > 1000 || downhillSkill < 0 || downhillSkill > 100)
                throw new ArgumentOutOfRangeException(nameof(effortControl));
            if (string.IsNullOrEmpty(track.SegmentId)) throw new ArgumentException("A valid track sample is required.", nameof(track));
            double skill = downhillSkill / 100.0;
            double curvatureLimit = definition.NoviceMaximumCurvature + skill * (definition.ExpertMaximumCurvature - definition.NoviceMaximumCurvature);
            double riskLimit = definition.NoviceMaximumRisk + skill * (definition.ExpertMaximumRisk - definition.NoviceMaximumRisk);
            var rejection = !requested ? TuckRejection.NotRequested :
                track.Grade > definition.MaximumGrade ? TuckRejection.Grade :
                state.SpeedMetersPerSecond < definition.MinimumSpeed ? TuckRejection.Speed :
                Math.Abs(track.CurvaturePerMeter) > curvatureLimit ? TuckRejection.Curvature :
                track.RiskLevel > riskLimit ? TuckRejection.Risk : TuckRejection.None;
            bool active = rejection == TuckRejection.None;
            return new TuckOutput(rejection, active ? Math.Min(effortControl, definition.MaximumEffortControl) : effortControl,
                active ? definition.DragAreaMultiplier : 1, active ? definition.SteeringResponseMultiplier : 1);
        }
    }
}
