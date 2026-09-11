using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    public readonly struct ShootingBalanceResult
    {
        public ulong Seed { get; } public long Ticks { get; } public int Shots { get; } public int ClosedMask { get; }
        public int Hits { get { int count = 0; for (int i = 0; i < TargetGeometryDefinition.TargetCount; i++) if ((ClosedMask & (1 << i)) != 0) count++; return count; } }
        public ShootingBalanceResult(ulong seed, long ticks, int shots, int mask) { Seed = seed; Ticks = ticks; Shots = shots; ClosedMask = mask; }
    }
    public static class ShootingBalanceRunner
    {
        // Separate deterministic streams for body sway and AI observation error; no random outcome roll.
        private const ulong ControllerStream = 0x9E3779B97F4A7C15UL;
        private sealed class NullEvents : IRaceEventSink { public static readonly NullEvents Instance = new NullEvents(); public void Record(RaceEvent value) { } }
        public static ShootingBalanceResult Run(ShootingBalanceSuite suite, ShootingBalanceScenario scenario, ShootingModelDefinitions model,
            ShootingAthleteDefinition athlete, ShootingAiDefinition aiDefinition, ulong seed)
        {
            if (suite == null || scenario == null || model == null || athlete == null || aiDefinition == null) throw new ArgumentNullException(nameof(suite));
            var session = new ShootingSession(new SimulationConfig(1, suite.Id, suite.Frequency, seed), model, athlete, scenario.Stance, "balance_range", NullEvents.Instance);
            var ai = new ShootingAiController(aiDefinition, model.Targets, model.Aim, model.Stances.Get(scenario.Stance), session.AthleteId, suite.Frequency, new SeededRandomSource(seed ^ ControllerStream));
            // An authored, imperfect wind reading, never the true impact or a hidden hit probability.
            double estimatedDrift = new ShootingWindSystem(model.Wind).Evaluate(new WindVector(scenario.EstimatedCrosswind, 0), 0, 1, model.Targets.DistanceMeters).HorizontalDriftMeters;
            while (session.Phase != ShootingPhase.Complete)
            {
                if (session.Tick.Value >= suite.MaximumTicks) throw new InvalidOperationException("Shooting balance bout exceeded tick budget: " + scenario.Id);
                var tick = session.Tick.Next(); var sight = session.Sight;
                var command = ai.Step(tick, new ShootingAiObservation(session.Phase, session.ClosedMask, session.ShotsFired, sight.YawRadians, sight.PitchRadians,
                    session.Tick.Value == 0 ? 1 : session.LastFrame.Pressure.DecisionDelayMultiplier, estimatedDrift));
                var action = tick.Value == 1 ? ShootingAction.EnterRange : tick.Value == 2 ? ShootingAction.AssignLane : command.Trigger ? ShootingAction.Trigger : ShootingAction.None;
                session.Step(new ShootingSessionInput(command.Aim, action,
                    tick.Value == 2 ? new ShootingLaneAssignment("balance_range", "balance_lane", 1) : default, command.HoldBreath,
                    scenario.HeartRate, scenario.Fatigue, scenario.ArmFatigue, new WindVector(scenario.Crosswind, 0)));
            }
            return new ShootingBalanceResult(seed, session.Tick.Value, session.ShotsFired, session.ClosedMask);
        }
    }
}
