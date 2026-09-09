using System;
using System.Runtime.Serialization;

namespace Biathlon.Domain
{
    public enum CompetitionFormat { Sprint = 0, Pursuit = 1, MassStart = 2 }
    public enum CompetitionStartType { Interval = 0, PreviousRaceGaps = 1, Simultaneous = 2 }
    public enum CompetitionCategory { Men = 0, Women = 1, Test = 2 }
    [DataContract]
    public sealed class CompetitionShootingBout
    {
        private CompetitionShootingBout() { }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "after_lap", IsRequired = true)] public int AfterLap { get; private set; }
        [DataMember(Name = "stance", IsRequired = true)] public ShootingStance Stance { get; private set; }
    }
    [DataContract]
    public sealed class CompetitionProfile
    {
        private CompetitionProfile() { }
        [DataMember(Name = "schema_version", IsRequired = true)] public int SchemaVersion { get; private set; }
        [DataMember(Name = "id", IsRequired = true)] public string Id { get; private set; }
        [DataMember(Name = "rules_revision", IsRequired = true)] public string RulesRevision { get; private set; }
        [DataMember(Name = "format", IsRequired = true)] public CompetitionFormat Format { get; private set; }
        [DataMember(Name = "category", IsRequired = true)] public CompetitionCategory Category { get; private set; }
        [DataMember(Name = "distance_m", IsRequired = true)] public double DistanceMeters { get; private set; }
        [DataMember(Name = "laps", IsRequired = true)] public int LapCount { get; private set; }
        [DataMember(Name = "start_type", IsRequired = true)] public CompetitionStartType StartType { get; private set; }
        [DataMember(Name = "start_interval_s", IsRequired = true)] public double StartIntervalSeconds { get; private set; }
        [DataMember(Name = "field_capacity", IsRequired = true)] public int FieldCapacity { get; private set; }
        [DataMember(Name = "shots_per_bout", IsRequired = true)] public int ShotsPerBout { get; private set; }
        [DataMember(Name = "penalty_loop_m", IsRequired = true)] public double PenaltyLoopMeters { get; private set; }
        [DataMember(Name = "shooting_bouts", IsRequired = true)] private CompetitionShootingBout[] bouts { get; set; }
        public TrackDistance Distance { get; private set; }
        public TrackDistance PenaltyLoopDistance { get; private set; }
        public int BoutCount => bouts.Length;
        public CompetitionShootingBout GetBout(int index) => bouts[index];
        public static CompetitionProfile FromJson(string json)
        {
            var v = RouteJson.Read<CompetitionProfile>(json);
            if (v == null || v.SchemaVersion != 1 || v.bouts == null) throw new SerializationException("Unsupported competition schema.");
            TrackValidation.Id(v.Id); TrackValidation.Id(v.RulesRevision);
            if (v.Format < CompetitionFormat.Sprint || v.Format > CompetitionFormat.MassStart || v.Category < CompetitionCategory.Men || v.Category > CompetitionCategory.Test ||
                v.StartType < CompetitionStartType.Interval || v.StartType > CompetitionStartType.Simultaneous) throw new ArgumentException("Unknown competition enum.");
            if (v.LapCount < 2 || v.LapCount > 100 || v.FieldCapacity < 1 || v.FieldCapacity > 1000 || v.ShotsPerBout != TargetGeometryDefinition.TargetCount ||
                v.bouts.Length != v.LapCount - 1) throw new ArgumentException("Unsupported lap/field/standard magazine structure.");
            v.Distance = TrackDistance.FromMeters(v.DistanceMeters); v.PenaltyLoopDistance = TrackDistance.FromMeters(v.PenaltyLoopMeters);
            if (v.Distance.Millimeters <= 0 || v.PenaltyLoopDistance.Millimeters <= 0 || v.PenaltyLoopDistance.Millimeters >= v.Distance.Millimeters)
                throw new ArgumentException("Competition distances must be positive and penalty shorter than the race.");
            TrackValidation.Finite(v.StartIntervalSeconds, 0, 600, nameof(StartIntervalSeconds));
            if ((v.StartType == CompetitionStartType.Interval) != (v.StartIntervalSeconds > 0)) throw new ArgumentException("Only interval starts have a positive interval.");
            CompetitionStartType expected = v.Format == CompetitionFormat.Sprint ? CompetitionStartType.Interval : v.Format == CompetitionFormat.Pursuit ? CompetitionStartType.PreviousRaceGaps : CompetitionStartType.Simultaneous;
            if (v.StartType != expected) throw new ArgumentException("Start model conflicts with competition format.");
            for (int i = 0; i < v.BoutCount; i++)
            {
                var b = v.bouts[i];
                if (b == null || b.AfterLap != i + 1 || (b.Stance != ShootingStance.Prone && b.Stance != ShootingStance.Standing))
                    throw new ArgumentException("Every non-final lap must have one ordered, valid shooting bout.");
                TrackValidation.Id(b.Id);
                for (int j = 0; j < i; j++) if (v.bouts[j].Id == b.Id) throw new ArgumentException("Duplicate bout ID.");
            }
            return v;
        }
        public CompetitionRouteRequirements ToRouteRequirements()
        {
            var laps = new int[BoutCount]; for (int i = 0; i < laps.Length; i++) laps[i] = bouts[i].AfterLap;
            return CompetitionRouteRequirements.FromProfile(Id, LapCount, Distance, laps);
        }
        public string ToJson() => RouteJson.Write(this);
    }
}
