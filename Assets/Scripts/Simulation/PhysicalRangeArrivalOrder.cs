using System;
using System.Globalization;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct PhysicalRangeArrivalPreview
    {
        internal PhysicalRangeArrivalOrder Owner { get; }
        internal long Revision { get; }
        public SimulationTick Tick { get; }
        internal PhysicalRangeArrivalPreview(PhysicalRangeArrivalOrder owner, long revision, SimulationTick tick)
        { Owner = owner; Revision = revision; Tick = tick; }
    }

    /// <summary>Prepared Mass Start lane order from final accepted common-line crossings.
    /// Scratch counters are disposable until commit. Body safety and joint athlete/path
    /// publication belong to the field adapter; this coordinator grants no physical admission.</summary>
    public sealed class PhysicalRangeArrivalOrder
    {
        private readonly AthleteId[] roster;
        private readonly int[] startOrdinals, assigned, preparedAssigned, lastBout, preparedLastBout,
            arrivals, preparedArrivals, rowIndices;
        private readonly PhysicalRangeArrival[] accepted, preparedAccepted, rows;
        private readonly bool[] seen;
        private readonly int frequency;
        private long revision;
        private bool previewReady;
        public int Count => roster.Length;
        public int BoutCount => arrivals.Length;
        public SimulationTick Tick { get; private set; }
        public string ContextJson { get; }

        public PhysicalRangeArrivalOrder(CompetitionProfile profile, MassStartGrid grid,
            MassStartRangePolicyDefinition policy, int tickFrequency)
        {
            if (profile == null || grid == null || policy == null) throw new ArgumentNullException();
            if (profile.Format != CompetitionFormat.MassStart || grid.ProfileJson != profile.ToJson() || tickFrequency <= 0)
                throw new ArgumentException("Exact Mass Start profile/grid and a positive clock are required.");
            frequency = tickFrequency;
            roster = new AthleteId[grid.Count]; startOrdinals = new int[grid.Count]; seen = new bool[grid.Count];
            lastBout = new int[grid.Count]; preparedLastBout = new int[grid.Count];
            arrivals = new int[profile.BoutCount]; preparedArrivals = new int[profile.BoutCount];
            int capacity = checked(grid.Count * profile.BoutCount);
            assigned = new int[capacity]; preparedAssigned = new int[capacity];
            accepted = new PhysicalRangeArrival[capacity]; preparedAccepted = new PhysicalRangeArrival[capacity];
            rows = new PhysicalRangeArrival[grid.Count]; rowIndices = new int[grid.Count];
            for (int i = 0; i < Count; i++)
            {
                roster[i] = grid.Get(i).AthleteId; startOrdinals[i] = grid.Get(i).StartOrdinal; lastBout[i] = -1;
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(new[] { "physical_range_arrival_order_v1",
                profile.ToJson(), grid.ContextJson, policy.ToJson(), frequency.ToString(CultureInfo.InvariantCulture),
                "final_accepted_receipts;retimed_fraction_ceiling;first_bout_start_ordinal;later_bout_fraction_then_start_ordinal;sequential_bouts;prepare_commit_only;no_body_admission" });
        }

        public int GetAssignedOrdinal(int athleteIndex, int boutIndex) => assigned[Slot(athleteIndex, boutIndex)];
        public int GetLastAssignedBout(int athleteIndex)
        { ValidateAthleteIndex(athleteIndex); return lastBout[athleteIndex]; }
        public int GetArrivalCount(int boutIndex)
        { ValidateBoutIndex(boutIndex); return arrivals[boutIndex]; }
        public PhysicalRangeArrival GetAcceptedArrival(int athleteIndex, int boutIndex)
        {
            int slot = Slot(athleteIndex, boutIndex);
            if (assigned[slot] == 0) throw new InvalidOperationException("The athlete has not arrived for this bout.");
            return accepted[slot];
        }

        /// <summary>Input rows may have any order. Replacement, including invalid replacement,
        /// invalidates the old handle. An empty accepted batch advances the clock only on commit.</summary>
        public PhysicalRangeArrivalPreview Prepare(SimulationTick tick, ReadOnlySpan<PhysicalRangeArrival> crossings)
        {
            previewReady = false;
            if (!tick.Equals(Tick.Next()) || crossings.Length > Count)
                throw new ArgumentException("Consecutive tick and at most one crossing per athlete are required.");
            Array.Clear(seen, 0, Count);
            for (int r = 0; r < crossings.Length; r++)
            {
                var crossing = crossings[r];
                int i = Index(crossing.AthleteId);
                if (!crossing.IsValid || i < 0 || seen[i] || !crossing.Tick.Equals(tick) || crossing.TickFrequency != frequency ||
                    crossing.BoutIndex < 0 || crossing.BoutIndex >= BoutCount || crossing.BoutIndex != lastBout[i] + 1 ||
                    crossing.Fraction < 0 || crossing.Fraction > JunctionAdmissionSession.FractionParts)
                    throw new ArgumentException("Foreign, duplicate, stale or nonsequential range crossing.");
                seen[i] = true; rows[r] = crossing; rowIndices[r] = i;
            }
            Array.Copy(assigned, preparedAssigned, assigned.Length);
            Array.Copy(accepted, preparedAccepted, accepted.Length);
            Array.Copy(lastBout, preparedLastBout, Count);
            Array.Copy(arrivals, preparedArrivals, BoutCount);
            for (int r = 1; r < crossings.Length; r++)
                for (int j = r; j > 0 && Earlier(j, j - 1); j--)
                {
                    var row = rows[j]; rows[j] = rows[j - 1]; rows[j - 1] = row;
                    int index = rowIndices[j]; rowIndices[j] = rowIndices[j - 1]; rowIndices[j - 1] = index;
                }
            for (int r = 0; r < crossings.Length; r++)
            {
                var crossing = rows[r]; int i = rowIndices[r], b = crossing.BoutIndex;
                if (preparedArrivals[b] >= Count) throw new InvalidOperationException("Range arrival capacity exhausted.");
                int ordinal = b == 0 ? startOrdinals[i] : preparedArrivals[b] + 1;
                preparedArrivals[b]++;
                preparedAssigned[Slot(i, b)] = ordinal; preparedAccepted[Slot(i, b)] = crossing; preparedLastBout[i] = b;
            }
            revision = checked(revision + 1); previewReady = true;
            return new PhysicalRangeArrivalPreview(this, revision, tick);
        }

        private bool Earlier(int a, int b)
        {
            int bout = rows[a].BoutIndex.CompareTo(rows[b].BoutIndex);
            if (bout != 0) return bout < 0;
            int fraction = rows[a].Fraction.CompareTo(rows[b].Fraction);
            return fraction != 0 ? fraction < 0 : startOrdinals[rowIndices[a]] < startOrdinals[rowIndices[b]];
        }
        public void ValidatePreview(PhysicalRangeArrivalPreview preview)
        {
            if (!ReferenceEquals(preview.Owner, this) || !previewReady || preview.Revision != revision || !preview.Tick.Equals(Tick.Next()))
                throw new ArgumentException("Foreign, stale or consumed range arrival preview.");
        }
        public int GetPreparedOrdinal(PhysicalRangeArrivalPreview preview, int athleteIndex, int boutIndex)
        { ValidatePreview(preview); return preparedAssigned[Slot(athleteIndex, boutIndex)]; }
        public int GetPreparedArrivalCount(PhysicalRangeArrivalPreview preview, int boutIndex)
        { ValidatePreview(preview); ValidateBoutIndex(boutIndex); return preparedArrivals[boutIndex]; }
        public void Commit(PhysicalRangeArrivalPreview preview)
        {
            ValidatePreview(preview); previewReady = false;
            Array.Copy(preparedAssigned, assigned, assigned.Length); Array.Copy(preparedAccepted, accepted, accepted.Length);
            Array.Copy(preparedLastBout, lastBout, Count); Array.Copy(preparedArrivals, arrivals, BoutCount);
            Tick = preview.Tick;
        }
        private int Index(AthleteId athlete)
        { for (int i = 0; i < Count; i++) if (roster[i].Equals(athlete)) return i; return -1; }
        private void ValidateAthleteIndex(int index)
        { if (index < 0 || index >= Count) throw new ArgumentOutOfRangeException(nameof(index)); }
        private void ValidateBoutIndex(int index)
        { if (index < 0 || index >= BoutCount) throw new ArgumentOutOfRangeException(nameof(index)); }
        private int Slot(int athleteIndex, int boutIndex)
        { ValidateAthleteIndex(athleteIndex); ValidateBoutIndex(boutIndex); return boutIndex * Count + athleteIndex; }
    }
}
