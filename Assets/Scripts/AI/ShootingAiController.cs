using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct ShootingAiObservation
    {
        public ShootingPhase Phase { get; } public int ClosedMask { get; } public int ShotsFired { get; }
        public double SightYaw { get; } public double SightPitch { get; } public double DecisionDelayMultiplier { get; }
        public double EstimatedHorizontalDriftMeters { get; }
        public ShootingAiObservation(ShootingPhase phase, int closedMask, int shotsFired, double sightYaw, double sightPitch, double decisionDelayMultiplier = 1,
            double estimatedHorizontalDriftMeters = 0)
        {
            if (phase < ShootingPhase.Outside || phase > ShootingPhase.Complete || closedMask < 0 || closedMask > 31 || shotsFired < 0 || shotsFired > 100)
                throw new ArgumentOutOfRangeException(nameof(phase));
            SightYaw = PhysiologyNumbers.RequireRange(sightYaw, -0.5, 0.5, nameof(sightYaw));
            SightPitch = PhysiologyNumbers.RequireRange(sightPitch, -0.5, 0.5, nameof(sightPitch));
            DecisionDelayMultiplier = PhysiologyNumbers.RequireRange(decisionDelayMultiplier, 1, 3, nameof(decisionDelayMultiplier));
            EstimatedHorizontalDriftMeters = PhysiologyNumbers.RequireRange(estimatedHorizontalDriftMeters, -10, 10, nameof(estimatedHorizontalDriftMeters));
            Phase = phase; ClosedMask = closedMask; ShotsFired = shotsFired;
        }
    }
    public readonly struct ShootingAiFrame
    {
        public WeaponAimCommand Aim { get; } public bool Trigger { get; } public bool HoldBreath { get; }
        public int TargetIndex { get; } public bool DecisionMade { get; }
        public ShootingAiFrame(WeaponAimCommand aim, bool trigger, bool hold, int target, bool decision)
        { Aim = aim; Trigger = trigger; HoldBreath = hold; TargetIndex = target; DecisionMade = decision; }
    }
    public sealed class ShootingAiController
    {
        private readonly ShootingAiDefinition definition;
        private readonly TargetGeometryDefinition targets;
        private readonly AthleteId athlete;
        private readonly int frequency, stableTicks, maximumAimTicks;
        private readonly double aimRate;
        private double phaseX, phaseY;
        private double previousTolerance;
        private long tick, nextDecision, stableSince = -1;
        private int horizontal, vertical, targetIndex = -1, aimingTicks, observedShots;
        public ShootingAiFrame LastFrame { get; private set; }
        public ShootingAiController(ShootingAiDefinition definition, TargetGeometryDefinition targets, WeaponAimDefinition aim,
            ShootingStanceProfile stance, AthleteId athlete, int frequency, IRandomSource random, SimulationTick initialTick = default)
        {
            this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
            this.targets = targets ?? throw new ArgumentNullException(nameof(targets));
            if (aim == null || stance == null || random == null) throw new ArgumentNullException(nameof(aim));
            if (!athlete.IsValid || frequency <= 2 * definition.TrackingFrequency || frequency > 10000) throw new ArgumentException("Invalid AI clock/identity.");
            this.athlete = athlete; this.frequency = frequency; aimRate = aim.RateRadiansPerSecond * stance.AimRateMultiplier;
            tick = initialTick.Value; nextDecision = initialTick.Value;
            stableTicks = (int)Math.Ceiling(definition.StableSeconds * frequency); maximumAimTicks = (int)Math.Ceiling(definition.MaximumAimSeconds * frequency);
            previousTolerance = definition.FireTolerance;
            phaseX = (random.NextUInt64() >> 11) / 9007199254740992.0; phaseY = (random.NextUInt64() >> 11) / 9007199254740992.0;
        }
        public ShootingAiFrame Step(SimulationTick targetTick, ShootingAiObservation observation)
            => Step(targetTick, observation, null);
        public ShootingAiFrame Step(SimulationTick targetTick, ShootingAiObservation observation, ShootingStrategyPlan? strategy)
        {
            if (targetTick.Value != checked(tick + 1)) throw new ArgumentException("AI ticks must be consecutive.");
            if (observation.DecisionDelayMultiplier < 1) throw new ArgumentException("Invalid default AI observation.");
            if (strategy.HasValue) strategy.Value.Validate();
            int requiredStable = strategy.HasValue ? (int)Math.Ceiling(strategy.Value.StableSeconds * frequency) : stableTicks;
            int aimLimit = strategy.HasValue ? (int)Math.Ceiling(strategy.Value.MaximumAimSeconds * frequency) : maximumAimTicks;
            double tolerance = strategy.HasValue ? strategy.Value.FireTolerance : definition.FireTolerance;
            long decisionDeadline = checked(targetTick.Value + (long)Math.Ceiling(definition.ReactionSeconds * frequency * observation.DecisionDelayMultiplier));
            if (tolerance != previousTolerance) stableSince = -1;
            previousTolerance = tolerance;
            phaseX = PhysiologyNumbers.Quantize((phaseX + definition.TrackingFrequency / frequency) % 1) % 1;
            phaseY = PhysiologyNumbers.Quantize((phaseY + definition.TrackingFrequency / frequency) % 1) % 1;
            if (observation.ShotsFired != observedShots) { aimingTicks = 0; stableSince = -1; observedShots = observation.ShotsFired; }
            if (observation.Phase == ShootingPhase.Aiming) aimingTicks = Math.Min(aimLimit, aimingTicks + 1);
            bool decision = targetTick.Value >= nextDecision, trigger = false;
            if (decision)
            {
                int selected = -1;
                for (int i = 0; i < TargetGeometryDefinition.TargetCount; i++) if ((observation.ClosedMask & (1 << i)) == 0) { selected = i; break; }
                if (selected != targetIndex) stableSince = -1;
                targetIndex = selected; nextDecision = decisionDeadline;
                if (selected >= 0 && (observation.Phase == ShootingPhase.Aiming || observation.Phase == ShootingPhase.Setup || observation.Phase == ShootingPhase.BoltCycle))
                {
                    double desiredYaw = Math.Atan((targets.CenterX(selected) - observation.EstimatedHorizontalDriftMeters) / targets.DistanceMeters) + definition.TrackingError * Math.Sin(phaseX * 2 * Math.PI);
                    double desiredPitch = definition.TrackingError * Math.Sin(phaseY * 2 * Math.PI);
                    double dx = desiredYaw - observation.SightYaw, dy = desiredPitch - observation.SightPitch;
                    horizontal = Encode(dx * definition.AimGain / aimRate); vertical = Encode(dy * definition.AimGain / aimRate);
                    if (observation.Phase == ShootingPhase.Aiming && Math.Sqrt(dx * dx + dy * dy) <= tolerance)
                    { if (stableSince < 0) stableSince = targetTick.Value; }
                    else stableSince = -1;
                    trigger = observation.Phase == ShootingPhase.Aiming && ((stableSince >= 0 && targetTick.Value - stableSince >= requiredStable) || aimingTicks >= aimLimit);
                    if (trigger) { horizontal = vertical = 0; stableSince = -1; }
                }
                else { horizontal = vertical = 0; stableSince = -1; }
            }
            tick = targetTick.Value;
            LastFrame = new ShootingAiFrame(new WeaponAimCommand(targetTick, athlete, horizontal, vertical), trigger,
                observation.Phase == ShootingPhase.Aiming, targetIndex, decision);
            return LastFrame;
        }
        private static int Encode(double value) => (int)Math.Round(Math.Max(-1, Math.Min(1, value)) * 1000, MidpointRounding.ToEven);
    }
}
