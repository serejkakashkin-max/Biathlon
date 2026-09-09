using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum WindStrength { Calm = 0, Light = 1, Moderate = 2, Strong = 3 }
    public readonly struct WindIndicator
    {
        public WindStrength Strength { get; } public int CrosswindDirection { get; }
        public WindIndicator(WindStrength strength, int direction) { Strength = strength; CrosswindDirection = direction; }
    }
    public readonly struct ShootingWindFrame
    {
        public double HeadwindMetersPerSecond { get; } public double CrosswindMetersPerSecond { get; }
        public double FlightTimeSeconds { get; } public double HorizontalDriftMeters { get; }
        public WindIndicator Indicator { get; }
        public ShootingWindFrame(double headwind, double crosswind, double flightTime, double drift, WindIndicator indicator)
        { HeadwindMetersPerSecond = headwind; CrosswindMetersPerSecond = crosswind; FlightTimeSeconds = flightTime; HorizontalDriftMeters = drift; Indicator = indicator; }
    }
    public sealed class ShootingWindSystem
    {
        private readonly ShootingWindDefinition definition;
        public ShootingWindSystem(ShootingWindDefinition definition) => this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        /// <param name="headingRadians">Clockwise from world north toward the target. Wind vector points where air travels.</param>
        public ShootingWindFrame Evaluate(WindVector wind, double headingRadians, double exposure, double flightDistanceMeters)
        {
            PhysiologyNumbers.RequireRange(headingRadians, -Math.PI * 2, Math.PI * 2, nameof(headingRadians));
            PhysiologyNumbers.RequireRange(exposure, 0, 1, nameof(exposure));
            PhysiologyNumbers.RequireRange(flightDistanceMeters, 0, PhysiologyNumbers.MaximumMagnitude, nameof(flightDistanceMeters));
            double speed = Math.Sqrt(wind.EastMetersPerSecond * wind.EastMetersPerSecond + wind.NorthMetersPerSecond * wind.NorthMetersPerSecond);
            if (speed > definition.MaximumSpeed) throw new ArgumentOutOfRangeException(nameof(wind));
            double sin = Math.Sin(headingRadians), cos = Math.Cos(headingRadians);
            double head = -(wind.EastMetersPerSecond * sin + wind.NorthMetersPerSecond * cos) * exposure;
            double cross = (wind.EastMetersPerSecond * cos - wind.NorthMetersPerSecond * sin) * exposure;
            double flight = flightDistanceMeters / definition.ProjectileSpeed;
            double strength = speed * exposure;
            var band = strength < definition.CalmThreshold ? WindStrength.Calm : strength < definition.ModerateThreshold ? WindStrength.Light :
                strength < definition.StrongThreshold ? WindStrength.Moderate : WindStrength.Strong;
            int direction = Math.Abs(cross) < definition.CalmThreshold ? 0 : Math.Sign(cross);
            return new ShootingWindFrame(PhysiologyNumbers.Quantize(head), PhysiologyNumbers.Quantize(cross), PhysiologyNumbers.Quantize(flight),
                PhysiologyNumbers.Quantize(cross * definition.BallisticScale * flight), new WindIndicator(band, direction));
        }
    }
}
