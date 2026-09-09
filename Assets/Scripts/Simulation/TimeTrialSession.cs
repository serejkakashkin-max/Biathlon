using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct TimeTrialFrame
    {
        public TimeTrialInput Input { get; }
        public PhysiologyFrame Physiology { get; }
        public RaceRouteSample RouteSample { get; }
        public SkiForceOutput Forces { get; }
        public SkiTechniqueOutput Technique { get; }
        public TuckOutput Tuck { get; }
        public CorneringOutput Cornering { get; }
        public SkiEquipmentOutput Equipment { get; }
        public FinalSprintOutput Sprint { get; }
        internal TimeTrialFrame(TimeTrialInput input, PhysiologyFrame physiology, RaceRouteSample routeSample,
            SkiForceOutput forces, SkiTechniqueOutput technique, TuckOutput tuck, CorneringOutput cornering, SkiEquipmentOutput equipment, FinalSprintOutput sprint)
        { Input = input; Physiology = physiology; RouteSample = routeSample; Forces = forces; Technique = technique; Tuck = tuck; Cornering = cornering; Equipment = equipment; Sprint = sprint; }
    }
    public sealed class TimeTrialSession
    {
        private readonly PhysiologySession physiology;
        private readonly SkiTechniqueSystem technique;
        private readonly SkiTechniqueDefinition techniqueDefinition;
        private readonly TuckSystem tuck;
        private readonly CorneringSystem cornering;
        private readonly SnowConditionSystem snow;
        private readonly SkiEquipmentSystem equipment;
        private readonly FinalSprintSystem sprint;
        private readonly SkiForceSystem forces;
        private readonly FinishTracker finish;
        private readonly TimeTrialAthlete athlete;
        private readonly IRaceEventSink events;
        private readonly FinishTime[] splits;
        private bool priorTuck, priorSprint, priorOverspeed;
        private int priorTechnique = -1;
        public RaceRoute Route { get; }
        public SimulationClock Clock => physiology.Clock;
        public AthleteId AthleteId => physiology.AthleteId;
        public PhysiologyState PhysiologyState => physiology.State;
        public SkiState Motion { get; private set; }
        public TimeTrialFrame LastFrame { get; private set; }
        public int CompletedLaps { get; private set; }
        public bool HasFinished => finish.HasFinished;
        public FinishTime FinishTime => finish.Time;
        public FinishTime GetLapSplit(int index)
        {
            if (index < 0 || index >= CompletedLaps) throw new ArgumentOutOfRangeException(nameof(index));
            return splits[index];
        }
        public TimeTrialSession(SimulationConfig config, SkiModelDefinitions definitions, TimeTrialAthlete athlete,
            TrackDefinition track, RaceRoute route, RaceSnowConditions conditions, string skiId, IRaceEventSink events,
            PhysiologyState? initialPhysiology = null)
        {
            if (config == null || definitions == null || athlete == null || track == null || route == null || events == null)
                throw new ArgumentNullException(nameof(config));
            if (route.TrackId != track.Id) throw new ArgumentException("Time-trial route and track must match.");
            this.athlete = athlete; this.events = events; Route = route;
            var physical = definitions.StatCurves.Apply(definitions.Physiology, athlete.PhysiologyStats);
            physiology = new PhysiologySession(config, physical, new AthleteId(athlete.Id), initialPhysiology ?? PhysiologyState.Rested(physical), events);
            technique = new SkiTechniqueSystem(definitions.Technique); techniqueDefinition = definitions.Technique;
            tuck = new TuckSystem(definitions.Tuck); cornering = new CorneringSystem(definitions.Cornering, definitions.Physics);
            snow = new SnowConditionSystem(definitions.Snow, conditions, track);
            equipment = new SkiEquipmentSystem(definitions.Equipment.GetSki(skiId), definitions.Snow);
            sprint = new FinalSprintSystem(definitions.Sprint, route.Length); forces = new SkiForceSystem(definitions.Physics, config.TickFrequency);
            finish = new FinishTracker(route.Length, physiology.AthleteId, config.TickFrequency, events); splits = new FinishTime[route.LapCount];
            // Validate all segment terrain/cross-definition modifiers before this race becomes runnable.
            for (int i = 0; i < route.SectionCount; i++)
            {
                var loop = track.GetLoop(route.GetSection(i).LoopId); var sampler = new TrackSampler(loop);
                for (int j = 0; j < loop.SegmentCount; j++)
                {
                    var sample = sampler.Sample(loop.GetSegment(j).StartDistance);
                    PhysiologyNumbers.RequireRange(sample.TerrainMuscleFactor, 0, physical.MaximumTerrainMuscleFactor, nameof(track));
                    var gear = equipment.Evaluate(snow.Sample(sample).Id);
                    forces.Step(default, 0, sample, snowResistanceMultiplier: gear.SnowResistanceMultiplier);
                    cornering.Evaluate(default, sample, athlete.Cornering, athlete.Downhill, 1, 0, gear.CorneringGripMultiplier);
                }
            }
        }
        public TimeTrialFrame Step(TimeTrialInput input)
        {
            if (HasFinished) throw new InvalidOperationException("Time trial already finished.");
            if (!input.Effort.Tick.Equals(Clock.Tick.Next()) || !input.Effort.AthleteId.Equals(AthleteId)) throw new ArgumentException("Invalid time-trial tick/athlete.");
            var sampled = Route.Sample(Motion.Distance); var track = sampled.Track; var before = Motion;
            var sprintOutput = sprint.Evaluate(input.SprintRequested, input.Effort.RequestedEffort, before);
            var posture = tuck.Evaluate(input.TuckRequested, sprintOutput.EffectiveEffortControl, athlete.Downhill, before, track);
            var work = new PhysiologyInput(new SimulationInput(input.Effort.Tick, AthleteId, posture.EffectiveEffortControl), input.Form, track.TerrainMuscleFactor);
            var physical = physiology.Preview(work);
            var techniqueOutput = technique.Evaluate(athlete.TechniqueStats, track, physiology.State.MuscularFatigue, physical.Effort.DeliveredPowerWatts, input.TechniqueId);
            var gear = equipment.Evaluate(snow.Sample(track).Id);
            var turn = cornering.Evaluate(before, track, athlete.Cornering, athlete.Downhill, input.LineQuality, physiology.State.MuscularFatigue, gear.CorneringGripMultiplier);
            var movement = forces.Step(before, techniqueOutput.PropulsivePowerWatts * input.MovementEfficiency, track, input.HeadwindMetersPerSecond, posture.DragAreaMultiplier, turn.BrakingNewtons, gear.SnowResistanceMultiplier);
            int techniqueIndex = 0;
            while (techniqueDefinition.GetProfile(techniqueIndex).Id != techniqueOutput.TechniqueId) techniqueIndex++;
            var frame = new TimeTrialFrame(input, physical, sampled, movement, techniqueOutput, posture, turn, gear, sprintOutput);
            // All fallible command/physical evaluation is above this commit boundary.
            physiology.CommitPreview(work, physical); Motion = movement.State; LastFrame = frame;
            Transition(posture.Active, ref priorTuck, RaceEventType.TuckStarted, RaceEventType.TuckEnded);
            Transition(sprintOutput.Active, ref priorSprint, RaceEventType.FinalSprintStarted, RaceEventType.FinalSprintEnded);
            Transition(turn.IsOverspeed, ref priorOverspeed, RaceEventType.CorneringOverspeedStarted, RaceEventType.CorneringOverspeedEnded);
            if (techniqueIndex != priorTechnique) { Record(RaceEventType.TechniqueChanged, techniqueIndex); priorTechnique = techniqueIndex; }
            while (CompletedLaps < Route.LapCount)
            {
                long boundary = checked((Route.GetLapStart(CompletedLaps).Millimeters + Route.GetLapLength(CompletedLaps).Millimeters) * SkiState.NanometersPerMillimeter);
                if (Motion.DistanceNanometers < boundary) break;
                splits[CompletedLaps] = FinishTime.AtCrossing(Clock.Tick, Clock.Frequency, before.DistanceNanometers, Motion.DistanceNanometers, boundary);
                CompletedLaps++; Record(RaceEventType.LapCompleted, CompletedLaps);
            }
            finish.Observe(Clock.Tick, before, Motion);
            return frame;
        }
        private void Transition(bool next, ref bool previous, RaceEventType entered, RaceEventType exited)
        { if (next != previous) Record(next ? entered : exited, 0); previous = next; }
        private void Record(RaceEventType type, int payload) => events.Record(new RaceEvent(Clock.Tick, Clock.Frequency, AthleteId, type, payload));
    }
}
