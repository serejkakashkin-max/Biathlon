using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Replay
{
    public sealed class SprintRaceCommandTape
    {
        private struct Run
        {
            public int Athlete;
            public long First, Last;
            public SprintAthleteInput Input;
        }
        private readonly Run[] runs;
        private readonly int[] next, head, tail;
        private readonly AthleteId[] athletes;
        public string Id { get; }
        public string ContextJson { get; }
        public long TickCount { get; private set; }
        public int RunCount { get; private set; }
        public int AthleteCount => athletes.Length;
        public SprintRaceCommandTape(string id, SprintFieldSession field, int capacity)
            : this(id, field?.ContextJson, Roster(field), capacity) { }
        private SprintRaceCommandTape(string id, string context, AthleteId[] roster, int capacity)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(context) || roster.Length == 0 || capacity < roster.Length)
                throw new ArgumentException("Valid race tape identity/context/roster/capacity required.");
            Id = id; ContextJson = context; athletes = roster; runs = new Run[capacity]; next = new int[capacity];
            head = new int[roster.Length]; tail = new int[roster.Length];
            for (int i = 0; i < roster.Length; i++)
            {
                if (!roster[i].IsValid) throw new ArgumentException("Invalid tape athlete.");
                for (int j = 0; j < i; j++) if (roster[i].Equals(roster[j])) throw new ArgumentException("Duplicate tape athlete.");
                head[i] = tail[i] = -1;
            }
        }
        private static AthleteId[] Roster(SprintFieldSession field)
        {
            if (field == null) throw new ArgumentNullException(nameof(field));
            var result = new AthleteId[field.Count]; for (int i = 0; i < result.Length; i++) result[i] = field.GetAthlete(i).AthleteId;
            return result;
        }
        private void ValidateContext(SprintFieldSession field)
        {
            if (field == null || field.ContextJson != ContextJson || field.Count != AthleteCount) throw new ArgumentException("Race replay context mismatch.");
            for (int i = 0; i < AthleteCount; i++) if (!field.GetAthlete(i).AthleteId.Equals(athletes[i])) throw new ArgumentException("Race replay roster mismatch.");
        }
        public void StepAndRecord(SprintFieldSession field, ReadOnlySpan<SprintAthleteInput> inputs)
        {
            ValidateContext(field);
            if (field.Tick.Value != TickCount) throw new ArgumentException("Tape and field recording cursor differ.");
            field.ValidateCommands(inputs);
            int added = 0;
            for (int i = 0; i < AthleteCount; i++) if (tail[i] < 0 || !SameIntent(runs[tail[i]].Input, inputs[i])) added++;
            if (added > runs.Length - RunCount) throw new ArgumentException("Race tape capacity exhausted before command commit.");
            field.Step(inputs); TickCount = field.Tick.Value;
            for (int i = 0; i < AthleteCount; i++)
            {
                if (tail[i] >= 0 && SameIntent(runs[tail[i]].Input, inputs[i])) { runs[tail[i]].Last = TickCount; continue; }
                Append(new Run { Athlete = i, First = TickCount, Last = TickCount, Input = inputs[i] });
            }
        }
        private void Append(Run run)
        {
            int i = RunCount++; runs[i] = run; next[i] = -1;
            if (tail[run.Athlete] < 0) head[run.Athlete] = i; else next[tail[run.Athlete]] = i;
            tail[run.Athlete] = i;
        }
        public ReplayCursor CreateReplay(SprintFieldSession field) => new ReplayCursor(this, field);
        public sealed class ReplayCursor
        {
            private readonly SprintRaceCommandTape tape;
            private readonly SprintFieldSession field;
            private readonly int[] current;
            private readonly SprintAthleteInput[] commands;
            private long tick;
            internal ReplayCursor(SprintRaceCommandTape tape, SprintFieldSession field)
            {
                tape.ValidateContext(field);
                if (field.Tick.Value != 0) throw new ArgumentException("Replay requires a fresh field.");
                this.tape = tape; this.field = field; current = (int[])tape.head.Clone(); commands = new SprintAthleteInput[tape.AthleteCount];
            }
            public bool HasNext => tick < tape.TickCount;
            public void Step()
            {
                if (!HasNext || field.Tick.Value != tick) throw new InvalidOperationException("Replay cursor is exhausted or field was advanced externally.");
                long target = tick + 1;
                for (int i = 0; i < current.Length; i++)
                {
                    int run = current[i];
                    if (run >= 0 && tape.runs[run].Last < target) run = tape.next[run];
                    if (run < 0 || tape.runs[run].First > target || tape.runs[run].Last < target) throw new InvalidOperationException("Incomplete replay coverage.");
                    commands[i] = AtTick(tape.runs[run].Input, target);
                }
                field.Step(commands);
                for (int i = 0; i < current.Length; i++) if (tape.runs[current[i]].Last < target) current[i] = tape.next[current[i]];
                tick = target;
            }
        }
        private static SprintAthleteInput AtTick(SprintAthleteInput v, long tick) => new SprintAthleteInput(
            new TimeTrialInput(new SimulationInput(new SimulationTick(tick), v.Ski.Effort.AthleteId, v.Ski.Effort.RequestedEffort),
                v.Ski.Form, v.Ski.LineQuality, v.Ski.HeadwindMetersPerSecond, v.Ski.TuckRequested, v.Ski.SprintRequested, v.Ski.TechniqueId, v.Ski.MovementEfficiency),
            new WeaponAimCommand(new SimulationTick(tick), v.Aim.AthleteId, v.Aim.Horizontal, v.Aim.Vertical, v.Aim.FineControl),
            v.Trigger, v.HoldBreath, v.Wind, v.Pressure,v.CycleBolt,v.ManualBoltCycle,v.RangeTransit);
        private static bool SameIntent(SprintAthleteInput a, SprintAthleteInput b) =>
            a.Ski.Effort.RequestedEffort == b.Ski.Effort.RequestedEffort && a.Ski.Form == b.Ski.Form && a.Ski.LineQuality == b.Ski.LineQuality &&
            a.Ski.HeadwindMetersPerSecond == b.Ski.HeadwindMetersPerSecond && a.Ski.TuckRequested == b.Ski.TuckRequested &&
            a.Ski.SprintRequested == b.Ski.SprintRequested && a.Ski.TechniqueId == b.Ski.TechniqueId && a.Ski.MovementEfficiency == b.Ski.MovementEfficiency &&
            a.Aim.Horizontal == b.Aim.Horizontal && a.Aim.Vertical == b.Aim.Vertical && a.Aim.FineControl == b.Aim.FineControl &&
            a.RangeTransit==b.RangeTransit && a.CycleBolt==b.CycleBolt && a.ManualBoltCycle==b.ManualBoltCycle && a.Trigger == b.Trigger && a.HoldBreath == b.HoldBreath && a.Wind.EastMetersPerSecond == b.Wind.EastMetersPerSecond &&
            a.Wind.NorthMetersPerSecond == b.Wind.NorthMetersPerSecond && a.Pressure.RaceImportance == b.Pressure.RaceImportance &&
            a.Pressure.PositionImportance == b.Pressure.PositionImportance && a.Pressure.RivalProximity == b.Pressure.RivalProximity &&
            a.Pressure.Confidence == b.Pressure.Confidence && a.Pressure.FinalStanding == b.Pressure.FinalStanding && a.Pressure.FirstPodium == b.Pressure.FirstPodium;
        public string ToJson()
        {
            var dto = new TapeJson { SchemaVersion = 1, Id = Id, Context = ContextJson, Ticks = TickCount, Athletes = new string[AthleteCount], Runs = new RunJson[RunCount] };
            for (int i = 0; i < AthleteCount; i++) dto.Athletes[i] = athletes[i].Value;
            for (int i = 0; i < RunCount; i++) dto.Runs[i] = new RunJson { Athlete = runs[i].Athlete, First = runs[i].First, Last = runs[i].Last, Command = Payload.From(runs[i].Input) };
            using (var stream = new MemoryStream())
            { new DataContractJsonSerializer(typeof(TapeJson)).WriteObject(stream, dto); return Encoding.UTF8.GetString(stream.ToArray()); }
        }
        public static SprintRaceCommandTape FromJson(string json)
        {
            TapeJson dto;
            using (var stream = new MemoryStream(Encoding.UTF8.GetBytes(json ?? throw new ArgumentNullException(nameof(json)))))
                dto = (TapeJson)new DataContractJsonSerializer(typeof(TapeJson)).ReadObject(stream);
            if (dto == null || dto.SchemaVersion != 1 || dto.Ticks < 0 || dto.Athletes == null || dto.Runs == null)
                throw new SerializationException("Invalid race tape schema.");
            var roster = new AthleteId[dto.Athletes.Length]; for (int i = 0; i < roster.Length; i++) roster[i] = new AthleteId(dto.Athletes[i]);
            var result = new SprintRaceCommandTape(dto.Id, dto.Context, roster, Math.Max(roster.Length, dto.Runs.Length));
            long previousFirst = 0; int previousAthlete = -1;
            foreach (var run in dto.Runs)
            {
                if (run == null || run.Athlete < 0 || run.Athlete >= roster.Length || run.Command == null || run.First <= 0 || run.Last < run.First || run.Last > dto.Ticks ||
                    run.First < previousFirst || (run.First == previousFirst && run.Athlete <= previousAthlete)) throw new SerializationException("Invalid ordered race run.");
                int prior = result.tail[run.Athlete];
                if (run.First != (prior < 0 ? 1 : checked(result.runs[prior].Last + 1))) throw new SerializationException("Race run gap/overlap.");
                var input = run.Command.ToInput(roster[run.Athlete], run.First);
                if (prior >= 0 && SameIntent(result.runs[prior].Input, input)) throw new SerializationException("Adjacent identical runs must be merged.");
                result.Append(new Run { Athlete = run.Athlete, First = run.First, Last = run.Last, Input = input });
                previousFirst = run.First; previousAthlete = run.Athlete;
            }
            for (int i = 0; i < roster.Length; i++)
                if (dto.Ticks == 0 ? result.tail[i] >= 0 : result.tail[i] < 0 || result.runs[result.tail[i]].Last != dto.Ticks)
                    throw new SerializationException("Incomplete athlete command coverage.");
            result.TickCount = dto.Ticks; return result;
        }
        [DataContract] private sealed class TapeJson
        {
            [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion;
            [DataMember(Name = "id", IsRequired = true)] public string Id;
            [DataMember(Name = "context_json", IsRequired = true)] public string Context;
            [DataMember(Name = "tick_count", IsRequired = true)] public long Ticks;
            [DataMember(Name = "athletes", IsRequired = true)] public string[] Athletes;
            [DataMember(Name = "runs", IsRequired = true)] public RunJson[] Runs;
        }
        [DataContract] private sealed class RunJson
        {
            [DataMember(Name = "athlete_index", IsRequired = true)] public int Athlete;
            [DataMember(Name = "first_tick", IsRequired = true)] public long First;
            [DataMember(Name = "last_tick", IsRequired = true)] public long Last;
            [DataMember(Name = "command", IsRequired = true)] public Payload Command;
        }
        [DataContract] private sealed class Payload
        {
            [DataMember(Name = "effort", IsRequired = true)] public int Effort;
            [DataMember(Name = "form", IsRequired = true)] public double Form;
            [DataMember(Name = "line_quality", IsRequired = true)] public double Line;
            [DataMember(Name = "headwind_mps", IsRequired = true)] public double Headwind;
            [DataMember(Name = "tuck", IsRequired = true)] public bool Tuck;
            [DataMember(Name = "sprint", IsRequired = true)] public bool Sprint;
            [DataMember(Name = "technique_id", IsRequired = true)] public string Technique;
            [DataMember(Name = "movement_efficiency", IsRequired = true)] public double Efficiency;
            [DataMember(Name = "aim_x", IsRequired = true)] public int X;
            [DataMember(Name = "aim_y", IsRequired = true)] public int Y;
            [DataMember(Name = "fine_aim", IsRequired = true)] public bool Fine;
            [DataMember(Name = "trigger", IsRequired = true)] public bool Trigger;
            [DataMember(Name = "hold_breath", IsRequired = true)] public bool Hold;
            [DataMember(Name = "cycle_bolt", EmitDefaultValue=false)] public bool Cycle;
            [DataMember(Name = "manual_bolt", EmitDefaultValue=false)] public bool Manual;
            [DataMember(Name = "range_transit_soft_contacts", EmitDefaultValue=false)] public bool RangeTransit;
            [DataMember(Name = "wind_east_mps", IsRequired = true)] public double East;
            [DataMember(Name = "wind_north_mps", IsRequired = true)] public double North;
            [DataMember(Name = "race_importance", IsRequired = true)] public double Race;
            [DataMember(Name = "position_importance", IsRequired = true)] public double Position;
            [DataMember(Name = "rival_proximity", IsRequired = true)] public double Rivals;
            [DataMember(Name = "confidence", IsRequired = true)] public double Confidence;
            [DataMember(Name = "final_standing", IsRequired = true)] public bool Final;
            [DataMember(Name = "first_podium", IsRequired = true)] public bool Podium;
            public static Payload From(SprintAthleteInput v) => new Payload { Effort = v.Ski.Effort.RequestedEffort, Form = v.Ski.Form, Line = v.Ski.LineQuality,
                Headwind = v.Ski.HeadwindMetersPerSecond, Tuck = v.Ski.TuckRequested, Sprint = v.Ski.SprintRequested, Technique = v.Ski.TechniqueId, Efficiency = v.Ski.MovementEfficiency,
                X = v.Aim.Horizontal, Y = v.Aim.Vertical, Fine = v.Aim.FineControl, Trigger = v.Trigger, Hold = v.HoldBreath,Cycle=v.CycleBolt,Manual=v.ManualBoltCycle,RangeTransit=v.RangeTransit,
                East = v.Wind.EastMetersPerSecond, North = v.Wind.NorthMetersPerSecond, Race = v.Pressure.RaceImportance, Position = v.Pressure.PositionImportance,
                Rivals = v.Pressure.RivalProximity, Confidence = v.Pressure.Confidence, Final = v.Pressure.FinalStanding, Podium = v.Pressure.FirstPodium };
            public SprintAthleteInput ToInput(AthleteId athlete, long tick) => new SprintAthleteInput(
                new TimeTrialInput(new SimulationInput(new SimulationTick(tick), athlete, Effort), Form, Line, Headwind, Tuck, Sprint, Technique, Efficiency),
                new WeaponAimCommand(new SimulationTick(tick), athlete, X, Y, Fine), Trigger, Hold, new WindVector(East, North),
                new ShootingPressureContext(Race, Position, Rivals, Confidence, Final, Podium),Cycle,Manual,RangeTransit);
        }
    }
}
