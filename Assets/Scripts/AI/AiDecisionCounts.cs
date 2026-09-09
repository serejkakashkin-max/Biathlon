using System;

namespace Biathlon.AI
{
    /// <summary>Bounded observer. Counts decision samples, not time spent in an intent.
    /// Shooting samples do not enter the tactical effort histogram.</summary>
    public sealed class AiDecisionCounts : IAiDecisionSink
    {
        private readonly long[] intents = new long[8];
        private readonly long[] efforts = new long[1001];
        public long Tactical { get; private set; }
        public long Shooting { get; private set; }
        public long IntentCount(int index) => intents[index];
        public long EffortCount(int effort) => efforts[effort];
        public void Record(AiDecisionTrace trace)
        {
            if (trace.Kind == AiTraceKind.Shooting) { Shooting++; return; }
            int intent = (int)trace.Intent.Kind;
            int effort = trace.Pace.Input.RequestedEffort;
            if (trace.Kind != AiTraceKind.Tactical || intent < 0 || intent >= intents.Length || effort < 0 || effort >= efforts.Length)
                throw new ArgumentOutOfRangeException(nameof(trace));
            Tactical++; intents[intent]++; efforts[effort]++;
        }
    }
}
