using System;
using Biathlon.Domain;

namespace Biathlon.Unity.Presentation
{
    public static class VisualRouteSurface
    {
        /// <summary>Local rendered slope, independent of physical grade metadata. Smooths knot seams.
        /// Geometrically open branches are clamped, closed loops wrap.</summary>
        public static double Grade(TrackVisualLoop loop, double distance, double halfSpan)
        {
            if (loop == null || !double.IsFinite(distance) || !double.IsFinite(halfSpan) || halfSpan <= 0)
                throw new ArgumentException("Invalid visual surface sample.");
            var first = loop.Get(0); var last = loop.Get(loop.Count - 1);
            double length = last.Distance;
            if (distance < 0 || distance > length) throw new ArgumentOutOfRangeException(nameof(distance));
            double span = Math.Min(halfSpan, length * .25);
            bool closed = first.X == last.X && first.Y == last.Y && first.Z == last.Z;
            double a = distance - span, b = distance + span;
            if (closed) { a = (a + length) % length; b %= length; }
            else { a = Math.Max(0, a); b = Math.Min(length, b); }
            var before = loop.Sample(a); var after = loop.Sample(b);
            double x = after.X - before.X, z = after.Z - before.Z;
            double horizontal = Math.Sqrt(x * x + z * z);
            return horizontal < 1e-6 ? 0 : (after.Y - before.Y) / horizontal;
        }
    }
}
