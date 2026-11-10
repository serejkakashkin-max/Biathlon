using System;
using Biathlon.Domain;
using Biathlon.Simulation;

namespace Biathlon.Presentation
{
    /// <summary>Running clock to the next timing line, or the recorded result at the line just crossed.</summary>
    public readonly struct LiveSplit
    {
        public bool Visible { get; }
        public bool Crossed { get; }
        public int Ordinal { get; }
        public double RouteMeters { get; }
        public double Seconds { get; }
        public double? LeaderSeconds { get; }
        public string LeaderId { get; }
        public double? DifferenceSeconds => LeaderSeconds.HasValue ? Seconds - LeaderSeconds.Value : (double?)null;
        public int Rank { get; }
        public int Arrivals { get; }

        private LiveSplit(CheckpointLayout layout, int ordinal, CheckpointTimingBoard board,
            bool elapsedClock, double seconds, bool crossed, int rank, int arrivals)
        {
            Visible = true;
            Crossed = crossed;
            Ordinal = ordinal;
            RouteMeters = layout.Get(ordinal).RouteNanometers / (double)SkiState.NanometersPerMeter;
            Seconds = seconds;
            bool hasLeader = board.TryGetLeader(ordinal, out var leader);
            LeaderId = hasLeader ? leader.Athlete.Value : null;
            LeaderSeconds = hasLeader ? Time(leader, elapsedClock) : (double?)null;
            Rank = rank;
            Arrivals = arrivals;
        }

        public static LiveSplit Capture(PackSprintFieldSession field, CheckpointLayout layout, int athleteIndex,
            AthletePresentationState athlete, double approachMeters, double holdSeconds)
        {
            if (!athlete.OnCourse || athlete.Phase != CompetitionPhase.Skiing || athlete.ShootingActive) return default;

            var checkpoints = field.GetAthlete(athleteIndex).Checkpoints;
            bool elapsedClock = field.Profile.Format == CompetitionFormat.Sprint;
            double globalSeconds = field.Tick.Value / (double)field.TickFrequency;
            int previous = athlete.Hud.CheckpointOrdinal;
            if (previous >= 0)
            {
                var split = checkpoints.Get(previous);
                double age = globalSeconds - split.GlobalTime.Seconds;
                if (age >= 0 && age < holdSeconds)
                    return new LiveSplit(layout, previous, field.Timing, elapsedClock, Time(split, elapsedClock),
                        true, athlete.Hud.Rank, athlete.Hud.ComparedAthletes);
            }

            int next = previous + 1;
            if (next >= layout.Count) return default;
            double remaining = layout.Get(next).RouteNanometers / (double)SkiState.NanometersPerMeter - athlete.RouteMeters;
            if (remaining <= 0 || remaining > approachMeters) return default;

            return new LiveSplit(layout, next, field.Timing, elapsedClock,
                elapsedClock ? athlete.ElapsedSeconds : globalSeconds, false, 0, 0);
        }

        private static double Time(CheckpointSplit split, bool elapsedClock) =>
            elapsedClock ? split.ElapsedTime.Seconds : split.GlobalTime.Seconds;
    }
}
