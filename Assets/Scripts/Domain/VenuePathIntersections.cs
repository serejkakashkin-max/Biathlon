using System;
using System.Globalization;

namespace Biathlon.Domain
{
    /// <summary>Cold-path centreline validation. Width clearance is a separate venue check.</summary>
    public static class VenuePathIntersections
    {
        // Floating-point arithmetic tolerances, not gameplay or authoring limits.
        private const double RelativeTolerance = 1e-12;

        public static void FindSelfIntersections(TrackVisualLoop loop, double verticalClearance,
            Action<string, string, string> add)
        {
            if (loop == null) throw new ArgumentNullException(nameof(loop));
            if (add == null) throw new ArgumentNullException(nameof(add));
            if (double.IsNaN(verticalClearance) || double.IsInfinity(verticalClearance) || verticalClearance <= 0)
                throw new ArgumentOutOfRangeException(nameof(verticalClearance));

            int edges = loop.Count - 1;
            var first = loop.Get(0);
            var last = loop.Get(loop.Count - 1);
            bool closed = first.X == last.X && first.Y == last.Y && first.Z == last.Z;
            for (int i = 0; i < edges; i++)
            {
                for (int j = i + 2; j < edges; j++)
                {
                    if (closed && i == 0 && j == edges - 1) continue;
                    if (!IntersectionGap(loop.Get(i), loop.Get(i + 1), loop.Get(j), loop.Get(j + 1), out double gap)
                        || gap >= verticalClearance) continue;
                    add("loop.self_intersection", "loops/" + loop.LoopId + "/edges/"
                        + i.ToString(CultureInfo.InvariantCulture) + "/" + j.ToString(CultureInfo.InvariantCulture),
                        "Nonadjacent centreline edges intersect with vertical separation "
                        + gap.ToString("R", CultureInfo.InvariantCulture) + " m.");
                }
            }
        }

        private static bool IntersectionGap(TrackVisualKnot a, TrackVisualKnot b,
            TrackVisualKnot c, TrackVisualKnot d, out double gap)
        {
            gap = 0;
            if (Math.Max(a.X, b.X) < Math.Min(c.X, d.X) || Math.Max(c.X, d.X) < Math.Min(a.X, b.X)
                || Math.Max(a.Z, b.Z) < Math.Min(c.Z, d.Z) || Math.Max(c.Z, d.Z) < Math.Min(a.Z, b.Z)) return false;
            double rx = b.X - a.X, rz = b.Z - a.Z, sx = d.X - c.X, sz = d.Z - c.Z;
            double qx = c.X - a.X, qz = c.Z - a.Z;
            double denominator = Cross(rx, rz, sx, sz);
            if (!CrossIsZero(rx, rz, sx, sz))
            {
                double t = Cross(qx, qz, sx, sz) / denominator;
                double u = Cross(qx, qz, rx, rz) / denominator;
                if (t < -RelativeTolerance || t > 1 + RelativeTolerance
                    || u < -RelativeTolerance || u > 1 + RelativeTolerance) return false;
                gap = Math.Abs(Height(a, b, Clamp(t)) - Height(c, d, Clamp(u)));
                return true;
            }

            if (!CrossIsZero(qx, qz, rx, rz)) return false;
            // Both projected segments are collinear. Compare height over the complete
            // overlap: large separation at its ends alone cannot hide a crossing inside.
            bool useX = Math.Abs(rx) >= Math.Abs(rz);
            double start = useX ? a.X : a.Z, delta = useX ? rx : rz;
            double otherStart = useX ? c.X : c.Z, otherDelta = useX ? sx : sz;
            double t0 = (otherStart - start) / delta;
            double t1 = (otherStart + otherDelta - start) / delta;
            double low = Math.Max(0, Math.Min(t0, t1)), high = Math.Min(1, Math.Max(t0, t1));
            if (low > high + RelativeTolerance) return false;
            low = Clamp(low); high = Clamp(high);
            double lowU = Clamp((start + delta * low - otherStart) / otherDelta);
            double highU = Clamp((start + delta * high - otherStart) / otherDelta);
            double lowDifference = Height(a, b, low) - Height(c, d, lowU);
            double highDifference = Height(a, b, high) - Height(c, d, highU);
            gap = lowDifference == 0 || highDifference == 0 || Math.Sign(lowDifference) != Math.Sign(highDifference)
                ? 0 : Math.Min(Math.Abs(lowDifference), Math.Abs(highDifference));
            return true;
        }

        private static double Cross(double ax, double az, double bx, double bz) => ax * bz - az * bx;
        private static bool CrossIsZero(double ax, double az, double bx, double bz) =>
            Math.Abs(Cross(ax, az, bx, bz)) <= RelativeTolerance * (Math.Abs(ax * bz) + Math.Abs(az * bx));
        private static double Height(TrackVisualKnot a, TrackVisualKnot b, double t) => a.Y + (b.Y - a.Y) * t;
        private static double Clamp(double value) => Math.Max(0, Math.Min(1, value));
    }
}
