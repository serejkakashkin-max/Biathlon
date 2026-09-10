namespace Biathlon.Domain
{
    public enum RaceEventType
    {
        EffortChanged = 1,
        AnaerobicDepleted = 2,
        AnaerobicRefilled = 3,
        MusclesExhausted = 4,
        MusclesFullyRecovered = 5,
        OvertakeStarted = 6,
        OvertakeCompleted = 7,
        OvertakeAborted = 8,
        ContactStarted = 9,
        ContactEnded = 10,
        RaceFinished = 11,
        TuckStarted = 12,
        TuckEnded = 13,
        TechniqueChanged = 14,
        CorneringOverspeedStarted = 15,
        CorneringOverspeedEnded = 16,
        FinalSprintStarted = 17,
        FinalSprintEnded = 18,
        LapCompleted = 19,
        ShootingPhaseChanged = 20,
        ShootingLaneAssigned = 21,
        ShotAccepted = 22,
        BreathHoldStarted = 23,
        BreathHoldEnded = 24,
        ShotResolved = 25,
        CompetitionPhaseChanged = 26,
        CompetitionStarted = 27,
        ShootingBoutCompleted = 28,
        PenaltyLoopCompleted = 29,
        ScheduledStartDue = 30,
        RangeTraversalPhaseChanged = 31,
        CheckpointCrossed = 32,
        CompetitionResultRecorded = 33,
        JunctionReserved = 34,
        JunctionReleased = 35,
        PhysicalSeamCrossed = 36,
        AiIntentChanged = 37,
        AiShootingPolicyChanged = 38,
        AiPursuitTacticsChanged = 39,
        StartApproachCompleted = 40
    }
    public readonly struct RaceEvent
    {
        public SimulationTick Tick { get; }
        public int TickFrequency { get; }
        public double SimulationTimeSeconds => Tick.Value / (double)TickFrequency;
        public AthleteId AthleteId { get; }
        public RaceEventType EventType { get; }
        public int Payload { get; }
        public RaceEvent(SimulationTick tick, int tickFrequency, AthleteId athleteId, RaceEventType eventType, int payload)
        {
            if (tickFrequency <= 0) throw new System.ArgumentOutOfRangeException(nameof(tickFrequency));
            if (!athleteId.IsValid) throw new System.ArgumentException("Invalid athlete ID.", nameof(athleteId));
            Tick = tick;
            TickFrequency = tickFrequency;
            AthleteId = athleteId;
            EventType = eventType;
            Payload = payload;
        }
    }
    /// <summary>Implementations must not throw or re-enter the simulation while recording.</summary>
    public interface IRaceEventSink { void Record(RaceEvent raceEvent); }
}
