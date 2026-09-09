using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct TargetIntersection
    {
        public bool ReachesPlane { get; }
        public int TargetIndex { get; }
        public double X { get; } public double Y { get; }
        public bool Hit => ReachesPlane && TargetIndex >= 0;
        public TargetIntersection(bool reachesPlane, int targetIndex, double x, double y)
        { ReachesPlane = reachesPlane; TargetIndex = targetIndex; X = x; Y = y; }
    }
    public sealed class TargetGeometrySystem
    {
        private readonly TargetGeometryDefinition definition;
        public TargetGeometrySystem(TargetGeometryDefinition definition) => this.definition = definition ?? throw new ArgumentNullException(nameof(definition));
        public TargetIntersection Intersect(ShotRay ray, ShootingStance stance)
        {
            if (!ray.IsValid) throw new ArgumentException("Invalid ray.");
            double radius = definition.Radius(stance);
            if (ray.DirectionZ == 0) return new TargetIntersection(false, -1, 0, 0);
            double t = (definition.DistanceMeters - ray.OriginZ) / ray.DirectionZ;
            if (!(t > 0) || double.IsInfinity(t)) return new TargetIntersection(false, -1, 0, 0);
            double x = ray.OriginX + t * ray.DirectionX, y = ray.OriginY + t * ray.DirectionY;
            if (double.IsNaN(x) || double.IsNaN(y) || Math.Abs(x) > PhysiologyNumbers.MaximumMagnitude || Math.Abs(y) > PhysiologyNumbers.MaximumMagnitude)
                return new TargetIntersection(false, -1, 0, 0);
            return IntersectPlanePoint(x, y, stance);
        }
        public TargetIntersection IntersectPlanePoint(double x, double y, ShootingStance stance)
        {
            PhysiologyNumbers.RequireRange(x, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(x));
            PhysiologyNumbers.RequireRange(y, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(y));
            double radius = definition.Radius(stance);
            // Canonical nanometre plane coordinates, inclusive exact circle test (ADR-0007).
            long nx = Nanometers(x), ny = Nanometers(y), nr = Nanometers(radius);
            for (int i = 0; i < TargetGeometryDefinition.TargetCount; i++)
            {
                long dx = nx - Nanometers(definition.CenterX(i));
                if (Math.Abs(dx) <= nr && Math.Abs(ny) <= nr && (decimal)dx * dx + (decimal)ny * ny <= (decimal)nr * nr)
                    return new TargetIntersection(true, i, nx / 1e9, ny / 1e9);
            }
            return new TargetIntersection(true, -1, nx / 1e9, ny / 1e9);
        }
        private static long Nanometers(double meters) => (long)Math.Round(meters * 1e9, MidpointRounding.ToEven);
    }
    /// <summary>Independent closure flags. Repeated hits cannot earn a second closure.</summary>
    public struct TargetBankState
    {
        public int ClosedMask { get; private set; }
        public bool IsClosed(int index) { Validate(index); return (ClosedMask & (1 << index)) != 0; }
        public bool Close(int index)
        {
            Validate(index); int bit = 1 << index;
            if ((ClosedMask & bit) != 0) return false;
            ClosedMask |= bit; return true;
        }
        private static void Validate(int index)
        { if (index < 0 || index >= TargetGeometryDefinition.TargetCount) throw new ArgumentOutOfRangeException(nameof(index)); }
    }
}
