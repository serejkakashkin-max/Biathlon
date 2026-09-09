using System;
using System.Runtime.Serialization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PlayerTimeTrialFrame
    {
        public PlayerCommand Command { get; }
        public TimeTrialFrame Race { get; }
        public EffortControlOutput Effort { get; }
        public SteeringOutput Steering { get; }
        public TechniqueAssistOutput Technique { get; }
        public bool OvertakeHasNoTarget => Command.OvertakeRequested;
        internal PlayerTimeTrialFrame(PlayerCommand command, TimeTrialFrame race, EffortControlOutput effort, SteeringOutput steering, TechniqueAssistOutput technique)
        { Command = command; Race = race; Effort = effort; Steering = steering; Technique = technique; }
    }
    public sealed class PlayerTimeTrialSession
    {
        private readonly TimeTrialSession race;
        private readonly EffortControlSystem effort;
        private readonly SteeringSystem steering;
        private readonly TechniqueAssistSystem technique;
        private readonly TuckSystem tuck;
        private readonly FinalSprintSystem sprint;
        private readonly PlayerControlSettings settings;
        private readonly TimeTrialAthlete athlete;
        private readonly double corridorMinimumWidth;
        private readonly SkiPlacement[] noOtherAthletes = Array.Empty<SkiPlacement>();
        public string ContextJson { get; }
        public SkiPlacement Placement { get; private set; }
        public SimulationClock Clock => race.Clock;
        public AthleteId AthleteId => race.AthleteId;
        public PhysiologyState PhysiologyState => race.PhysiologyState;
        public bool HasFinished => race.HasFinished;
        public FinishTime FinishTime => race.FinishTime;
        public PlayerTimeTrialFrame LastFrame { get; private set; }
        public PlayerTimeTrialSession(SimulationConfig config, SkiModelDefinitions model, TimeTrialAthlete athlete,
            TrackDefinition track, RaceRoute route, RaceSnowConditions snow, string skiId, PlayerControlSettings settings,
            EffortControlDefinition effortDefinition, SteeringDefinition steeringDefinition, OccupancyDefinition occupancyDefinition, IRaceEventSink events)
        {
            this.settings = settings ?? throw new ArgumentNullException(nameof(settings));
            this.athlete = athlete ?? throw new ArgumentNullException(nameof(athlete));
            race = new TimeTrialSession(config, model, athlete, track, route, snow, skiId, events);
            PhysiologyNumbers.RequireRange(settings.Headwind, -model.Physics.MaximumHeadwindMetersPerSecond, model.Physics.MaximumHeadwindMetersPerSecond, nameof(settings));
            effort = new EffortControlSystem(effortDefinition, race.AthleteId, config.TickFrequency);
            var occupancy = new OccupancySystem(occupancyDefinition);
            steering = new SteeringSystem(steeringDefinition, occupancy, config.TickFrequency);
            technique = new TechniqueAssistSystem(model.Technique); tuck = new TuckSystem(model.Tuck); sprint = new FinalSprintSystem(model.Sprint, route.Length);
            corridorMinimumWidth = double.MaxValue;
            for (int i = 0; i < route.SectionCount; i++)
            {
                var loop = track.GetLoop(route.GetSection(i).LoopId);
                for (int j = 0; j < loop.SegmentCount; j++) corridorMinimumWidth = Math.Min(corridorMinimumWidth, loop.GetSegment(j).WidthMeters);
            }
            if (!occupancy.FitsWidth(0, corridorMinimumWidth)) throw new ArgumentException("Track too narrow for player body.");
            Placement = new SkiPlacement(race.AthleteId, route.Id, default, 0);
            ContextJson = SerializeContext(new Context {
                SchemaVersion = 1, Id = "player_time_trial_context_v1", Frequency = config.TickFrequency, Seed = config.Seed, ConfigId = config.Id,
                SkiId = skiId, Definitions = new[] { model.Physiology.ToJson(), model.StatCurves.ToJson(), model.Physics.ToJson(), model.Technique.ToJson(),
                    model.Tuck.ToJson(), model.Cornering.ToJson(), model.Snow.ToJson(), model.Equipment.ToJson(), model.Sprint.ToJson(),
                    athlete.ToJson(), track.ToJson(), route.PlanSnapshotJson, route.RequirementsSnapshotJson, snow.ToJson(), settings.ToJson(),
                    effortDefinition.ToJson(), steeringDefinition.ToJson(), occupancyDefinition.ToJson() }
            });
        }
        private static string SerializeContext(Context context)
        {
            using (var stream = new System.IO.MemoryStream())
            {
                new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(Context)).WriteObject(stream, context);
                return System.Text.Encoding.UTF8.GetString(stream.ToArray());
            }
        }
        public PlayerTimeTrialFrame Step(PlayerCommand command)
        {
            if (race.HasFinished) throw new InvalidOperationException("Player trial has finished.");
            var input = effort.Preview(command, settings.AssistedEffort);
            var track = race.Route.Sample(Placement.Motion.Distance).Track;
            var finalEffort = sprint.Evaluate(input.FinalSprintRequested, input.AppliedControl, Placement.Motion);
            var posture = tuck.Evaluate(command.TuckHeld, finalEffort.EffectiveEffortControl, athlete.Downhill, Placement.Motion, track);
            var line = steering.Evaluate(command, Placement, track, noOtherAthletes, posture.SteeringResponseMultiplier, settings.AssistedCenter, corridorMinimumWidth);
            double startingFatigue = race.PhysiologyState.MuscularFatigue;
            var selected = technique.Evaluate(settings.ManualTechnique ? TechniqueAssistMode.Manual : TechniqueAssistMode.Automatic,
                command, athlete.TechniqueStats, track, race.PhysiologyState.MuscularFatigue, 0);
            // Solo trial has no overtake target; raw pulse remains recorded and feedback states this explicitly.
            var work = new TimeTrialInput(new SimulationInput(command.Tick, command.AthleteId, input.AppliedControl), settings.Form,
                line.LineQuality, settings.Headwind, command.TuckHeld, input.FinalSprintRequested, selected.Applied.TechniqueId, line.PropulsionMultiplier);
            var frame = race.Step(work);
            effort.Commit(command, input);
            Placement = new SkiPlacement(Placement.AthleteId, Placement.CorridorId, frame.Forces.State, line.ProposedPlacement.LateralMeters);
            // Selection was validated before the race commit; attach actual delivered-power telemetry.
            selected = technique.Evaluate(settings.ManualTechnique ? TechniqueAssistMode.Manual : TechniqueAssistMode.Automatic,
                command, athlete.TechniqueStats, track, startingFatigue, frame.Physiology.Effort.DeliveredPowerWatts);
            LastFrame = new PlayerTimeTrialFrame(command, frame, input, line, selected);
            return LastFrame;
        }
        [DataContract]
        private sealed class Context
        {
            [DataMember(Name = "schema_version")] public int SchemaVersion;
            [DataMember(Name = "id")] public string Id;
            [DataMember(Name = "tick_frequency")] public int Frequency;
            [DataMember(Name = "seed")] public ulong Seed;
            [DataMember(Name = "config_id")] public string ConfigId;
            [DataMember(Name = "ski_id")] public string SkiId;
            [DataMember(Name = "definitions")] public string[] Definitions;
        }
    }
}
