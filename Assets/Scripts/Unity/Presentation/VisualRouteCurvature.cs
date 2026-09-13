using System;
using Biathlon.Domain;

namespace Biathlon.Unity.Presentation
{
    public static class VisualRouteCurvature
    {
        /// <summary>Signed turn of the rendered route in authored distance units. No physics writes.
        /// Wrap only a geometrically closed loop, never across unrelated route branches.</summary>
        public static double Sample(TrackVisualLoop loop, double distance, double halfSpan)
        {
            if (loop == null || double.IsNaN(distance) || double.IsInfinity(distance) || double.IsNaN(halfSpan) || double.IsInfinity(halfSpan) || halfSpan <= 0)
                throw new ArgumentException("Invalid visual curvature sample.");
            var first = loop.Get(0); var last = loop.Get(loop.Count - 1);
            double length = last.Distance;
            if (distance < 0 || distance > length) throw new ArgumentOutOfRangeException(nameof(distance));
            bool closed = first.X == last.X && first.Y == last.Y && first.Z == last.Z;
            double span = Math.Min(halfSpan, length * .25);
            double a = distance - span, b = distance + span;
            double denominator;
            if (closed) { a = (a + length) % length; b %= length; denominator = span * 2; }
            else { a = Math.Max(0, a); b = Math.Min(length, b); denominator = b - a; }
            var before = loop.Sample(a); var after = loop.Sample(b);
            double cross = before.ForwardZ * after.ForwardX - before.ForwardX * after.ForwardZ;
            double dot = before.ForwardX * after.ForwardX + before.ForwardZ * after.ForwardZ;
            return Math.Atan2(cross, dot) / denominator;
        }
    }
}
