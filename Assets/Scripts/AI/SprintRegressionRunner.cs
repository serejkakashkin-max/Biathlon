using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.AI
{
    /// <summary>Precise regression controller, not a difficulty preset. All outcomes come from the shared shooting session.</summary>
    public sealed class SprintRegressionRunner
    {
        private readonly SprintRegressionSuite suite;
        private readonly SprintRegressionScenario scenario;
        private readonly ShootingModelDefinitions shootingModel;
        private readonly ShootingAiDefinition aimDefinition;
        private ShootingSession previousShooting;
        private ShootingAiController controller;
        public SprintFieldSession Field { get; }
        public SprintRegressionRunner(SprintRegressionSuite suite, SprintRegressionScenario scenario, Func<string, string> readJson,
            TrackDefinition track, IRaceEventSink events)
        {
            if (suite == null || scenario == null || readJson == null || track == null) throw new ArgumentNullException();
            this.suite = suite; this.scenario = scenario;
            var profile = CompetitionProfile.FromJson(readJson("sprint_test_v1.json"));
            var ski = TimeTrialAthlete.FromJson(readJson("time_trial_athlete_v1.json"));
            var shooting = ShootingAthleteDefinition.FromJson(readJson("shooting_athlete_v1.json"));
            shooting = ShootingAthleteDefinition.FromJson(shooting.ToJson().Replace(shooting.Id, ski.Id));
            var route = new RaceRoute(track, RaceRoutePlan.TimeTrial("sprint_regression_route", track.Id, "main_long", profile.ToRouteRequirements()), profile.ToRouteRequirements());
            shootingModel = new ShootingModelDefinitions(readJson); aimDefinition = ShootingAiDefinition.FromJson(readJson("shooting_regression_controller_v1.json"));
            if (suite.IntentionalMissYaw > shootingModel.Aim.MaxYawRadians || suite.IntentionalMissYaw <=
                Math.Atan((shootingModel.Targets.CenterX(4) + shootingModel.Targets.StandingDiameterMeters) / shootingModel.Targets.DistanceMeters))
                throw new ArgumentException("Intentional miss aim must be reachable and outside all target apertures.");
            Field = new SprintFieldSession(new SimulationConfig(1, suite.Id, suite.Frequency, suite.Seed), profile,
                new SkiModelDefinitions(readJson), shootingModel, track, route,
                RaceSnowConditions.FromJson(readJson("race_snow_neutral_v1.json").Replace("packed_neutral", scenario.SnowProfile)),
                ResultTimingDefinition.FromJson(readJson("result_timing_v1.json")), new[] { new SprintEntrant(ski, shooting, scenario.SkiId) },
                new SimulationTick(1), scenario.Id + "_result", events);
        }
        public SprintAthleteInput NextInput()
        {
            if (Field.IsComplete || Field.Tick.Value >= suite.MaximumTicks) throw new InvalidOperationException("Sprint scenario completed or tick budget exhausted.");
            var s = Field.GetAthlete(0); var tick = Field.Tick.Next(); var shooting = s.ActiveShooting;
            WeaponAimCommand aim = new WeaponAimCommand(tick, s.AthleteId, 0, 0); bool trigger = false, hold = false;
            if (shooting != null)
            {
                if (shooting != previousShooting)
                {
                    controller = new ShootingAiController(aimDefinition, shootingModel.Targets, shootingModel.Aim,
                        shootingModel.Stances.Get(s.ActiveBout == 0 ? ShootingStance.Prone : ShootingStance.Standing), s.AthleteId, suite.Frequency,
                        new SeededRandomSource(unchecked(suite.Seed + (ulong)s.ActiveBout)), s.Tick);
                    previousShooting = shooting;
                }
                var sight = shooting.Sight;
                var decision = controller.Step(tick, new ShootingAiObservation(shooting.Phase, shooting.ClosedMask, shooting.ShotsFired, sight.YawRadians, sight.PitchRadians));
                aim = decision.Aim; trigger = decision.Trigger; hold = decision.HoldBreath;
                if (scenario.IntentionalFirstMiss && s.ActiveBout == 0 && shooting.ShotsFired == 0)
                {
                    bool ready = sight.YawRadians >= suite.IntentionalMissYaw;
                    aim = new WeaponAimCommand(tick, s.AthleteId, ready ? 0 : 1000, 0);
                    trigger = ready && shooting.Phase == ShootingPhase.Aiming;
                }
            }
            return new SprintAthleteInput(new TimeTrialInput(new SimulationInput(tick, s.AthleteId, scenario.EffortControl), 1, 1, 0), aim, trigger, hold);
        }
    }
}
