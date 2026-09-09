using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Completed-tick race observations shared by independent and physical Sprint fields.
    /// Consumers observe the returned athlete sessions; only the owning field advances them.</summary>
    public interface ISprintFieldView
    {
        SimulationTick Tick { get; }
        int Count { get; }
        TrackDefinition Track { get; }
        RaceRoute Route { get; }
        CompetitionProfile Profile { get; }
        CheckpointTimingBoard Timing { get; }
        SprintAthleteSession GetAthlete(int index);
    }
}
