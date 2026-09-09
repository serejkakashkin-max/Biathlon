using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct SteeringOutput
    {
        public SkiPlacement ProposedPlacement { get; }
        public double TargetLateralMeters { get; }
        public double LineQuality { get; }
        public double PropulsionMultiplier { get; }
        public bool Blocked { get; }
        public AthleteId BlockerId { get; }
        internal SteeringOutput(SkiPlacement proposed, double target, double quality, double propulsion, bool blocked, AthleteId blocker)
        { ProposedPlacement = proposed; TargetLateralMeters = target; LineQuality = quality; PropulsionMultiplier = propulsion; Blocked = blocked; BlockerId = blocker; }
    }
    public sealed class SteeringSystem
    {
        private readonly SteeringDefinition definition;
        private readonly OccupancySystem occupancy;
        private readonly double baseStep;
        public SteeringSystem(SteeringDefinition definition, OccupancySystem occupancy, int frequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.occupancy = occupancy ?? throw new ArgumentNullException(nameof(occupancy));
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            baseStep = definition.LateralSpeed / frequency;
            if (baseStep * definition.HighSpeedResponseMultiplier < PhysiologyNumbers.MinimumPositive) throw new ArgumentException("Steering rate below coordinate precision.");
        }
        public SteeringOutput Evaluate(PlayerCommand command, SkiPlacement current, TrackSample track,
            ReadOnlySpan<SkiPlacement> roster, double postureResponse = 1, bool assistedCenter = false, double? corridorMinimumWidth = null)
        {
            if (!command.IsValid || !current.IsValid || !command.AthleteId.Equals(current.AthleteId)) throw new ArgumentException("Invalid steering athlete/command.");
            double width = corridorMinimumWidth.HasValue ? Math.Min(track.WidthMeters, corridorMinimumWidth.Value) : track.WidthMeters;
            if (string.IsNullOrEmpty(track.SegmentId) || !occupancy.FitsWidth(current.LateralMeters, width)) throw new ArgumentException("Current placement must fit the track.");
            PhysiologyNumbers.RequireRange(postureResponse, 0, 1, nameof(postureResponse));
            double extent = Math.Floor((width * 0.5 - occupancy.Definition.EdgeMargin) * SkiState.NanometersPerMeter) / SkiState.NanometersPerMeter;
            int deadzone = assistedCenter ? definition.AssistedCenterDeadzone : 0;
            double intent = Math.Sign(command.LineIntent) * Math.Max(0, Math.Abs(command.LineIntent) - deadzone) / (double)(PlayerCommand.LineIntentUnits - deadzone);
            double target = extent * intent;
            double speedFactor = 1 - Math.Min(1, current.Motion.SpeedMetersPerSecond / definition.HighSpeedReference) * (1 - definition.HighSpeedResponseMultiplier);
            double step = baseStep * speedFactor * postureResponse;
            double lateral = PhysiologyNumbers.Quantize(current.LateralMeters + Math.Max(-step, Math.Min(step, target - current.LateralMeters)));
            lateral = Math.Max(-extent, Math.Min(extent, lateral));
            var proposed = new SkiPlacement(current.AthleteId, current.CorridorId, current.Motion, lateral);
            var query = occupancy.QueryMove(current, proposed, width, roster);
            if (query.Blocked) { proposed = current; lateral = current.LateralMeters; }
            double movement = Math.Min(1, Math.Abs(lateral - current.LateralMeters) / baseStep);
            // Line quality measures following the requested line, not an invented ideal racing line.
            double error = extent > 0 ? Math.Min(1, Math.Abs(target - lateral) / (2 * extent)) : 0;
            double quality = 1 - definition.LineErrorLoss * error;
            return new SteeringOutput(proposed, target, quality, 1 - definition.LateralPropulsionLoss * movement, query.Blocked, query.BlockerId);
        }
    }
}
