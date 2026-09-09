using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class ShootingScenarioRunner
    {
        private readonly ShootingTrainingScenario scenario;
        public ShootingSession Session { get; }
        public bool IsComplete => Session.Phase == ShootingPhase.Complete;
        public ShootingScenarioRunner(ShootingTrainingScenario scenario, ShootingModelDefinitions model, ShootingAthleteDefinition athlete, IRaceEventSink events)
        {
            this.scenario = scenario ?? throw new ArgumentNullException(nameof(scenario));
            if (model == null) throw new ArgumentNullException(nameof(model));
            PhysiologyNumbers.RequireRange(scenario.HeartRate, model.Physiology.RestingHrBpm, model.Physiology.HrSafetyCeilingBpm, nameof(scenario.HeartRate));
            new ShootingWindSystem(model.Wind).Evaluate(new WindVector(scenario.Crosswind, 0), 0, scenario.Exposure, model.Targets.DistanceMeters);
            Session = new ShootingSession(new SimulationConfig(1, scenario.Id, scenario.Frequency, scenario.Seed), model, athlete, scenario.Stance, "training_range", events);
        }
        public ShootingSessionInput NextInput()
        {
            if (IsComplete || Session.Tick.Value >= scenario.MaximumTicks) throw new InvalidOperationException("Training complete or tick budget exhausted.");
            var tick = Session.Tick.Next();
            var action = tick.Value == 1 ? ShootingAction.EnterRange : tick.Value == 2 ? ShootingAction.AssignLane : ShootingAction.Trigger;
            return new ShootingSessionInput(new WeaponAimCommand(tick, Session.AthleteId,
                tick.Value % scenario.AimPeriod < scenario.AimPeriod / 2 ? -scenario.AimHorizontal : scenario.AimHorizontal, 0, true), action,
                tick.Value == 2 ? new ShootingLaneAssignment("training_range", "training_lane", 1) : default,
                tick.Value % scenario.HoldPeriod < scenario.HoldTicks, scenario.HeartRate, scenario.Fatigue, scenario.ArmFatigue,
                new WindVector(scenario.Crosswind, 0), 0, scenario.Exposure);
        }
    }
}
