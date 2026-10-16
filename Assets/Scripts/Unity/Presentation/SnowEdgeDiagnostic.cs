using System;

namespace Biathlon.Unity.Presentation
{
    /// <summary>Cold construction evidence. Distances refer to unchanged native route rows.</summary>
    [Serializable] public sealed class SnowEdgeDiagnostic
    {
        public string loop_id, reason;
        public int side, panel;
        public double start_m, end_m;
        public float width_m;
        public bool tapered, vertical_emitted;
    }
}
