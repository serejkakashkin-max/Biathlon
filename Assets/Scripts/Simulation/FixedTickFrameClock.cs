using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    // Wall-clock scheduling only; every admitted simulation tick keeps its
    // exact fixed duration. Slow frames cannot create an endless catch-up loop.
    public sealed class FixedTickFrameClock
    {
        private readonly FixedTickFrameBudgetDefinition definition;
        private readonly double tickSeconds;
        private double backlog;
        public double DiscardedWallSeconds {get;private set;}
        public double BacklogSeconds=>backlog;
        public FixedTickFrameClock(FixedTickFrameBudgetDefinition definition,int frequency)
        {this.definition=definition??throw new ArgumentNullException(nameof(definition));if(frequency<=0)throw new ArgumentOutOfRangeException(nameof(frequency));tickSeconds=1.0/frequency;}
        public int BeginFrame(double elapsed)
        {
            if(!double.IsFinite(elapsed)||elapsed<0)throw new ArgumentOutOfRangeException(nameof(elapsed));
            backlog+=elapsed;
            if(backlog>definition.MaximumBacklog){DiscardedWallSeconds+=backlog-definition.MaximumBacklog;backlog=definition.MaximumBacklog;}
            return Math.Min(definition.MaximumSteps,(int)Math.Floor((backlog+1e-12)/tickSeconds));
        }
        public void ConsumeTick(){if(backlog+1e-12<tickSeconds)throw new InvalidOperationException("No scheduled tick.");backlog=Math.Max(0,backlog-tickSeconds);}
    }
}
