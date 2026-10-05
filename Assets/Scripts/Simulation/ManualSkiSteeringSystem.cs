using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Event-free trajectory preview. No course tangent or ideal line can turn the athlete.</summary>
    public readonly struct ManualSkiHeadingFrame
    {
        private readonly ManualSkiHeadingState initial;
        private readonly double acceleration, reachSeconds;
        private readonly int substeps;
        public ManualSkiHeadingState State { get; }
        public double DurationSeconds { get; }
        /// <summary>Horizontal world-plane metres, not the legacy slope-path odometer.</summary>
        public double PlanarTravelDistanceMeters { get; }
        public double RequestedYawRate { get; }
        public double TargetYawRate { get; }
        public double YawRateLimit { get; }
        public double HeadingDeltaRadians { get; }
        public double PeakYawRate { get; }
        public double LateralAccelerationDemand { get; }
        public double GripAccelerationLimit { get; }
        public bool RequestLimited => Math.Abs(RequestedYawRate) > YawRateLimit;
        public bool ExceedsGrip => LateralAccelerationDemand > GripAccelerationLimit;
        public bool IsValid => DurationSeconds > 0 && State.IsValid;

        internal ManualSkiHeadingFrame(ManualSkiHeadingState initial, double duration, double distance,
            double requestedRate, double targetRate, double rateLimit, double acceleration, double reach,
            int substeps, double speed, double gripLimit)
        {
            this.initial = initial; this.acceleration = acceleration; reachSeconds = reach; this.substeps = substeps;
            DurationSeconds = duration; PlanarTravelDistanceMeters = distance; RequestedYawRate = requestedRate;
            TargetYawRate = targetRate; YawRateLimit = rateLimit; GripAccelerationLimit = gripLimit;
            // Unity's C# compiler requires every struct field assigned before instance helpers.
            State = default; HeadingDeltaRadians = 0; PeakYawRate = 0; LateralAccelerationDemand = 0;
            HeadingDeltaRadians = AngleAt(duration);
            double endRate = RateAt(duration);
            PeakYawRate = Math.Max(Math.Abs(initial.YawRateRadiansPerSecond), Math.Abs(endRate));
            LateralAccelerationDemand = PhysiologyNumbers.Quantize(PhysiologyNumbers.RequireRange(speed * PeakYawRate,
                0, PhysiologyNumbers.MaximumMagnitude, "manual turning acceleration"));
            State = Integrate(duration);
        }

        /// <summary>Approximate trajectory sample; alone it grants no collision or edge admission.</summary>
        public ManualSkiHeadingState Sample(double fraction)
        {
            if (!IsValid) throw new InvalidOperationException("Cannot sample a default manual heading preview.");
            PhysiologyNumbers.RequireRange(fraction, 0, 1, nameof(fraction));
            if (fraction == 0) return initial;
            if (fraction == 1) return State;
            return Integrate(DurationSeconds * fraction);
        }

        private double AngleAt(double time)
        {
            double changing = Math.Min(time, reachSeconds);
            return initial.YawRateRadiansPerSecond * changing + acceleration * changing * changing * 0.5
                + TargetYawRate * (time - changing);
        }

        private double RateAt(double time) => time >= reachSeconds ? TargetYawRate : initial.YawRateRadiansPerSecond + acceleration * time;

        private ManualSkiHeadingState Integrate(double time)
        {
            double x = initial.X, z = initial.Z;
            if (PlanarTravelDistanceMeters != 0)
            {
                // Fixed composite Simpson quadrature of continuous yaw. Distance is supplied
                // by the speed solver; this first kernel uses uniform travel density in time.
                double step = time / substeps;
                double weight = PlanarTravelDistanceMeters / DurationSeconds * step / 6;
                for (int i = 0; i < substeps; i++)
                {
                    double from = i * step, middle = from + step * 0.5, to = from + step;
                    double a = initial.HeadingRadians + AngleAt(from);
                    double b = initial.HeadingRadians + AngleAt(middle);
                    double c = initial.HeadingRadians + AngleAt(to);
                    x += weight * (Math.Sin(a) + 4 * Math.Sin(b) + Math.Sin(c));
                    z += weight * (Math.Cos(a) + 4 * Math.Cos(b) + Math.Cos(c));
                }
            }
            return new ManualSkiHeadingState(x, z, initial.HeadingRadians + AngleAt(time), RateAt(time));
        }
    }

    /// <summary>Pure fixed-tick manual heading. Callers retain route, forces, bodies and joint commit ownership.</summary>
    public sealed class ManualSkiSteeringSystem
    {
        private readonly ManualSkiSteeringDefinition definition;
        private readonly double duration;

        public ManualSkiSteeringSystem(ManualSkiSteeringDefinition definition, int frequency)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            if (frequency <= 0) throw new ArgumentOutOfRangeException(nameof(frequency));
            duration = 1.0 / frequency;
            if (definition.YawAcceleration * duration < PhysiologyNumbers.MinimumPositive ||
                definition.NeutralYawDeceleration * duration < PhysiologyNumbers.MinimumPositive)
                throw new ArgumentException("Manual angular response is below state encoding precision.");
        }

        public ManualSkiHeadingFrame Preview(ManualSkiHeadingState state, ManualSkiCommand command,
            double planarTravelDistanceMeters, double speedMetersPerSecond, double gripAccelerationLimit, double postureResponse = 1)
        {
            if (!command.IsValid) throw new ArgumentException("A valid fixed-tick manual command is required.", nameof(command));
            return Preview(state, command.SteeringInput, planarTravelDistanceMeters, speedMetersPerSecond, gripAccelerationLimit, postureResponse);
        }

        public ManualSkiHeadingFrame Preview(ManualSkiHeadingState state, int steeringInput,
            double planarTravelDistanceMeters, double speedMetersPerSecond, double gripAccelerationLimit, double postureResponse = 1)
        {
            if (!state.IsValid) throw new ArgumentException("An explicitly initialized world pose is required.", nameof(state));
            if (steeringInput < -ManualSkiCommand.SteeringUnits || steeringInput > ManualSkiCommand.SteeringUnits)
                throw new ArgumentOutOfRangeException(nameof(steeringInput));
            PhysiologyNumbers.RequireRange(planarTravelDistanceMeters, 0, PhysiologyNumbers.MaximumMagnitude, nameof(planarTravelDistanceMeters));
            PhysiologyNumbers.RequireRange(speedMetersPerSecond, 0, PhysiologyNumbers.MaximumMagnitude, nameof(speedMetersPerSecond));
            PhysiologyNumbers.RequireRange(gripAccelerationLimit, 0, PhysiologyNumbers.MaximumMagnitude, nameof(gripAccelerationLimit));
            PhysiologyNumbers.RequireRange(postureResponse, 0, 1, nameof(postureResponse));
            PhysiologyNumbers.RequireRange(state.YawRateRadiansPerSecond, -definition.MaximumYawRate, definition.MaximumYawRate, nameof(state));
            double speedResponse = 1 - Math.Min(1, speedMetersPerSecond / definition.HighSpeedReference)
                * (1 - definition.HighSpeedResponseMultiplier);
            double response = speedResponse * postureResponse;
            double requestedRate = definition.MaximumYawRate * steeringInput / ManualSkiCommand.SteeringUnits * response;
            double limit = Math.Min(definition.MaximumYawRate * response,
                gripAccelerationLimit / Math.Max(speedMetersPerSecond, definition.MinimumSpeedForGripLimit));
            double boundedRate = Math.Max(-limit, Math.Min(limit, requestedRate));
            // A physical ceiling is rounded toward zero, so encoding cannot exceed grip.
            double targetRate = Math.Sign(boundedRate) * Math.Floor(Math.Abs(boundedRate) * ManualSkiHeadingState.NanoradiansPerRadian)
                / ManualSkiHeadingState.NanoradiansPerRadian;
            double change = targetRate - state.YawRateRadiansPerSecond;
            double magnitude = steeringInput == 0 ? definition.NeutralYawDeceleration : definition.YawAcceleration * response;
            double acceleration = Math.Sign(change) * magnitude;
            double reach = change == 0 ? 0 : magnitude == 0 ? double.PositiveInfinity : Math.Abs(change) / magnitude;
            return new ManualSkiHeadingFrame(state, duration, planarTravelDistanceMeters, requestedRate,
                targetRate, limit, acceleration, reach, definition.PathIntegrationSubsteps, speedMetersPerSecond, gripAccelerationLimit);
        }
    }
}
