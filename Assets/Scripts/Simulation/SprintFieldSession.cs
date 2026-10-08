using System;
using System.Globalization;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public sealed class SprintEntrant
    {
        public TimeTrialAthlete SkiAthlete { get; }
        public ShootingAthleteDefinition ShootingAthlete { get; }
        public string SkiId { get; }
        public AthleteId AthleteId => new AthleteId(SkiAthlete.Id);
        public SprintEntrant(TimeTrialAthlete skiAthlete, ShootingAthleteDefinition shootingAthlete, string skiId)
        {
            if (skiAthlete == null || shootingAthlete == null || string.IsNullOrWhiteSpace(skiId) || skiAthlete.Id != shootingAthlete.Id)
                throw new ArgumentException("Matched ski/shooting athlete and equipment ID required.");
            SkiAthlete = skiAthlete; ShootingAthlete = shootingAthlete; SkiId = skiId;
        }
    }
    /// <summary>Independent athlete orchestration with interval Sprint or qualified Pursuit starts. Shared physical geometry is owned by PackSprintFieldSession.</summary>
    public sealed partial class SprintFieldSession : ISprintFieldView
    {
        private readonly SprintAthleteSession[] athletes;
        private readonly SprintResultBook results;
        private readonly IntervalStartScheduler scheduler;
        private readonly ScheduledStart[] due;
        private readonly PursuitStartScheduler pursuitScheduler;
        private readonly PursuitQualificationSnapshot qualification;
        private readonly MassStartGrid massGrid;
        private readonly SimultaneousStartScheduler massScheduler;
        private bool IsInterval => qualification==null && massGrid==null;
        public MassStartResultSnapshot MassStartResult { get; private set; }
        private readonly IRaceEventSink events;
        private readonly SimulationTick firstStart;
        private readonly int frequency;
        private readonly bool requiresPhysicalAdmission;
        private int pursuitFinished;
        public PursuitResultSnapshot PursuitResult { get; private set; }
        private readonly string resultId;
        public SimulationTick Tick { get; private set; }
        public int Count => athletes.Length;
        public int FinishedCount => IsInterval ? results.Count : pursuitFinished;
        public bool IsComplete => FinishedCount == Count;
        public CheckpointTimingBoard Timing { get; }
        public SprintResultSnapshot Result { get; private set; }
        public string ContextJson { get; private set; }
        public TrackDefinition Track { get; }
        public RaceRoute Route { get; }
        public CompetitionProfile Profile { get; }
        public SprintAthleteSession GetAthlete(int index) => athletes[index];
        public ScheduledStart GetStart(int index) => IsInterval ? scheduler.Get(index) : massGrid!=null ? massScheduler.Get(index) : pursuitScheduler.Get(index);
        public SprintFieldSession(SimulationConfig config, CompetitionProfile profile, SkiModelDefinitions skiModel, ShootingModelDefinitions shootingModel,
            TrackDefinition track, RaceRoute route, RaceSnowConditions conditions, ResultTimingDefinition timing,
            ReadOnlySpan<SprintEntrant> entrants, SimulationTick firstStart, string resultId, IRaceEventSink events, PursuitQualificationSnapshot qualification = null,
            MassStartGrid massGrid = null, StartApproachMap startApproach = null, PhysicalRangeFieldLayout rangeLayout = null,
            bool requiresPhysicalAdmission = false)
        {
            if (config == null || profile == null || track == null || route == null || string.IsNullOrWhiteSpace(resultId))
                throw new ArgumentException("Race configuration and result ID required.");
            if(requiresPhysicalAdmission&&(rangeLayout==null||massGrid!=null||profile.Format==CompetitionFormat.MassStart))
                throw new ArgumentException("Reusable physical admission requires Sprint/Pursuit authored paths.");
            this.requiresPhysicalAdmission=requiresPhysicalAdmission;
            if (rangeLayout != null && (!rangeLayout.IsBoundTo(track, route) || !requiresPhysicalAdmission && entrants.Length > rangeLayout.Count))
                throw new ArgumentException("The complete roster needs authored paths bound to the exact field track and route.");
            var rangeChoices = rangeLayout?.CreateChoices();
            var roster = new AthleteId[entrants.Length];
            Track = track; Route = route; Profile = profile;
            for (int i = 0; i < entrants.Length; i++) roster[i] = entrants[i]?.AthleteId ?? throw new ArgumentException("Null entrant.");
            this.massGrid=massGrid;
            if((massGrid==null)!=(startApproach==null) || (massGrid!=null && qualification!=null)) throw new ArgumentException("One explicit start policy required.");
            this.qualification = qualification; this.events = events; this.firstStart = firstStart; frequency = config.TickFrequency;
            due = new ScheduledStart[entrants.Length];
            if (IsInterval)
                scheduler = new IntervalStartScheduler(profile, roster, config.TickFrequency, firstStart, events);
            else if(massGrid!=null)
            {
                if(profile.ToJson()!=massGrid.ProfileJson || entrants.Length!=massGrid.Count)throw new ArgumentException("Exact Mass Start grid/profile required.");
                for(int i=0;i<roster.Length;i++)if(!roster[i].Equals(massGrid.Get(i).AthleteId))throw new ArgumentException("Entrants must follow grid order.");
                massScheduler=new SimultaneousStartScheduler(massGrid,config.TickFrequency,firstStart,events);
            }
            else
            {
                if (profile.ToJson() != qualification.PursuitProfileJson || entrants.Length != qualification.Count)
                    throw new ArgumentException("Pursuit profile and complete qualifying roster required.");
                for (int i = 0; i < roster.Length; i++)
                    if (roster[i].Value != qualification.Get(i).AthleteId) throw new ArgumentException("Entrants must follow qualification order.");
                pursuitScheduler = new PursuitStartScheduler(qualification, config.TickFrequency, firstStart, events);
            }
            if (IsInterval) results = new SprintResultBook(profile, timing, roster, config.TickFrequency, events);
            var checkpoints = new CheckpointLayout(track, route);
            Timing = new CheckpointTimingBoard(checkpoints, roster, config.TickFrequency, IsInterval ? CheckpointClock.Elapsed : CheckpointClock.Global);
            this.resultId = resultId; athletes = new SprintAthleteSession[entrants.Length];
            for (int i = 0; i < entrants.Length; i++)
            {
                var entrant = entrants[i];
                var rangePath = rangeLayout?.GetPath(requiresPhysicalAdmission?0:i);
                var shootingLane = new ShootingLaneAssignment(track.Range.Id,
                    rangePath == null ? "synthetic_lane_" + (i + 1).ToString(CultureInfo.InvariantCulture) : rangePath.LaneId, requiresPhysicalAdmission?1:i + 1);
                // Start order and seed are part of race configuration; all gameplay uses the same physical models.
                var athleteConfig = new SimulationConfig(config.SchemaVersion, config.Id, config.TickFrequency, unchecked(config.Seed + (ulong)i));
                athletes[i] = new SprintAthleteSession(athleteConfig, profile, skiModel, shootingModel, entrant.SkiAthlete, entrant.ShootingAthlete,
                    track, route, checkpoints, conditions, entrant.SkiId, GetStart(i).Tick,
                    shootingLane, events,
                    startApproach,massGrid==null?0:-massGrid.Get(i).LongitudinalNanometers,rangePath,rangeChoices);
            }
            var context = new List<string> { "sprint_independent_paths_dedicated_lanes_v1", config.Id,
                config.TickFrequency.ToString(CultureInfo.InvariantCulture), config.Seed.ToString(CultureInfo.InvariantCulture),
                firstStart.Value.ToString(CultureInfo.InvariantCulture), resultId, profile.ToJson(), track.ToJson(), route.PlanSnapshotJson,
                route.RequirementsSnapshotJson, conditions.ToJson(), timing.ToJson(), skiModel.Physiology.ToJson(), skiModel.StatCurves.ToJson(),
                skiModel.Physics.ToJson(), skiModel.Technique.ToJson(), skiModel.Tuck.ToJson(), skiModel.Cornering.ToJson(), skiModel.Snow.ToJson(),
                skiModel.Equipment.ToJson(), skiModel.Sprint.ToJson(), shootingModel.ContextJson };
            for (int i = 0; i < entrants.Length; i++)
            { context.Add(entrants[i].SkiAthlete.ToJson()); context.Add(entrants[i].ShootingAthlete.ToJson()); context.Add(entrants[i].SkiId); }
            if (qualification != null) { context[0] = "pursuit_gap_field_v1"; context.Add(qualification.ToJson()); context.Add("exact_global_finish_order_v1"); }
            if(massGrid!=null){context[0]="mass_start_shared_clock_v1";context.Add(massGrid.ContextJson);context.Add(startApproach.ContextJson);context.Add("exact_global_finish_order_v1");}
            if (rangeLayout != null)
            {
                context.Add(rangeLayout.ContextJson);
                context.Add(requiresPhysicalAdmission?"reusable_authored_range_paths_v1;unassigned_shared_prefix_placeholder;external_joint_physical_admission":
                    "independent_authored_range_paths_v1;initial_roster_slot;prepared_choices;no_body_admission");
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(context.ToArray());
        }
        public void Step(ReadOnlySpan<SprintAthleteInput> inputs)
        {
            if(manualScopedWorld!=null)throw new InvalidOperationException("A scoped world field cannot resume legacy movement.");
            if(requiresPhysicalAdmission)throw new InvalidOperationException("Reusable mats require the Pack field's joint physical admission transaction.");
            ValidateCommands(inputs);
            var tick = Tick.Next();
            if (IsInterval) scheduler.Step(tick, due); else if(massGrid!=null)massScheduler.Step(tick,due); else pursuitScheduler.Step(tick, due);
            for (int i = 0; i < Count; i++)
            {
                var athlete = athletes[i]; if (athlete.HasFinished) continue;
                int oldSplits = athlete.Checkpoints.Count;
                athlete.Step(inputs[i]);
                for (int checkpoint = oldSplits; checkpoint < athlete.Checkpoints.Count; checkpoint++) Timing.Record(athlete.Checkpoints.Get(checkpoint));
                if (athlete.HasFinished) RecordFinish(athlete.Lifecycle);
            }
            Tick = tick;
            if (IsComplete) CompleteResults();
        }
        private void RecordFinish(CompetitionLifecycle lifecycle)
        {
            if (IsInterval) results.Record(lifecycle);
            else
            {
                pursuitFinished++;
                events.Record(new RaceEvent(lifecycle.Tick, frequency, lifecycle.AthleteId, RaceEventType.CompetitionResultRecorded, (int)RaceResultStatus.Finished));
            }
        }
        private void CompleteResults()
        {
            if (IsInterval) Result = results.Complete(resultId);
            else
            {
                var races = new CompetitionLifecycle[Count];
                for (int i = 0; i < Count; i++) races[i] = athletes[i].Lifecycle;
                if(massGrid!=null)MassStartResult=MassStartResultSnapshot.Create(resultId,massGrid,frequency,firstStart,races);
                else PursuitResult = PursuitResultSnapshot.Create(resultId, qualification, frequency, firstStart, races);
            }
        }
        public void ValidateCommands(ReadOnlySpan<SprintAthleteInput> inputs)
        {
            if (IsComplete || inputs.Length != Count) throw new ArgumentException("Complete field command batch required for an active race.");
            var tick = Tick.Next();
            for (int i = 0; i < Count; i++)
            {
                var input = inputs[i]; var athlete = athletes[i];
                if (input.Ski.Effort.Tick.Value != tick.Value || input.Aim.Tick.Value != tick.Value ||
                    !input.Ski.Effort.AthleteId.Equals(athlete.AthleteId) || !input.Aim.AthleteId.Equals(athlete.AthleteId))
                    throw new ArgumentException("Field rows must match stable roster order and global tick.");
                if (!athlete.HasFinished) athlete.ValidateCommand(input);
            }
        }
        internal void CommitPrepared(ReadOnlySpan<SprintAthleteInput> inputs, ReadOnlySpan<SprintStepPreview> previews)
        {
            ValidateCommands(inputs);
            if (previews.Length != Count) throw new ArgumentException("Complete prepared field required.");
            for (int i = 0; i < Count; i++) if (!athletes[i].HasFinished) athletes[i].ValidatePreparedCommit(previews[i]);
            var tick = Tick.Next(); if (IsInterval) scheduler.Step(tick, due); else if(massGrid!=null)massScheduler.Step(tick,due); else pursuitScheduler.Step(tick, due);
            for (int i = 0; i < Count; i++)
            {
                var athlete = athletes[i]; if (athlete.HasFinished) continue;
                int oldSplits = athlete.Checkpoints.Count;
                athlete.CommitStep(previews[i]);
                for (int checkpoint = oldSplits; checkpoint < athlete.Checkpoints.Count; checkpoint++) Timing.Record(athlete.Checkpoints.Get(checkpoint));
                if (athlete.HasFinished) RecordFinish(athlete.Lifecycle);
            }
            Tick = tick; if (IsComplete) CompleteResults();
        }
    }
}
