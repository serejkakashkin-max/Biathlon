using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Unwired seam-rotation query draft. A retained source/target footprint still
    /// needs both ordinary body checks; this adds the missing continuous rotation at the seam.</summary>
    public sealed class PhysicalApronSeamProjectionDraft
    {
        private const double Epsilon = 2.2204460492503131e-16;
        private readonly PhysicalTrackMap map;
        private readonly PhysicalPassageCorridor opponent;
        private readonly PhysicalApronBodySupportDraft support;
        private readonly PhysicalTrackPoint sourcePoint, targetPoint;
        private readonly double x, z, tx, tz, fx, fz, nx, nz, turn, halfLength, halfWidth, radius, margin;
        private readonly bool ambiguous;
        private readonly bool completePeriodicGeometry;
        public double ProjectionMarginMeters => margin;

        public PhysicalApronSeamProjectionDraft(PhysicalTrackMap map, PhysicalSharedApronDefinition definition,
            OccupancyDefinition body, PhysicalTrackPoint sourcePoint, PhysicalTrackPoint targetPoint, string opponentLoop, bool allBodyHeadings = false)
        {
            if (map == null || definition == null || body == null) throw new ArgumentNullException();
            if (map.TrackSnapshotJson != definition.TrackSnapshotJson || body.ToJson() != definition.OccupancySnapshotJson)
                throw new ArgumentException("Exact seam geometry/body context required.");
            map.Validate(sourcePoint); map.Validate(targetPoint);
            if (map.SameLoop(sourcePoint, targetPoint)) throw new ArgumentException("A seam joins two different physical loops.");
            this.map = map; this.sourcePoint = sourcePoint; this.targetPoint = targetPoint;
            PhysicalPassageCorridor source = Find(sourcePoint.LoopId), target = Find(targetPoint.LoopId); opponent = Find(opponentLoop);
            Point(source, sourcePoint, true, out x, out double sy, out z, out fx, out fz);
            Point(target, targetPoint, false, out tx, out double ty, out tz, out nx, out nz);
            double dx = tx - x, dy = ty - sy, dz = tz - z;
            if (Math.Sqrt(dx * dx + dy * dy + dz * dz) > definition.ValidationPolicy.SeamTolerance)
                throw new ArgumentException("Seam body rotation requires the bound coincident-seam tolerance.");
            double cross = fx * nz - fz * nx, dot = fx * nx + fz * nz;
            turn = Math.Atan2(cross, dot); ambiguous = cross == 0 && dot < 0;
            halfLength = body.LongitudinalSeparation * .5; halfWidth = body.LateralSeparation * .5;
            radius = Math.Sqrt(halfLength * halfLength + halfWidth * halfWidth);
            if (allBodyHeadings) halfLength = halfWidth = radius;
            // New optional apron uses the actual accepted right-normal world center;
            // legacy projectors remain untouched and only supply their numerical reserve.
            support = new PhysicalApronBodySupportDraft(opponent, opponent, body, definition.ValidationPolicy, allBodyHeadings);
            var seamReserve = new PhysicalPassageWorldProjection(source, target, body, definition.ValidationPolicy);
            margin = Math.Max(seamReserve.ProjectionMarginMeters, support.ProjectionMarginMeters);
            PhysicalPassageCorridor Find(string id)
            { for (int i = 0; i < definition.CorridorCount; i++) if (definition.GetCorridor(i).LoopId == id) return definition.GetCorridor(i); throw new ArgumentException("Unbound seam/opponent corridor."); }
        }

        // Explicit complete paid-loop geometry for the optional named penalty merges.
        // The existing bounded-apron constructor and arithmetic remain unchanged.
        internal PhysicalApronSeamProjectionDraft(PhysicalTrackMap map,OccupancyDefinition body,
            PhysicalTrackPoint sourcePoint,PhysicalTrackPoint targetPoint,PhysicalPassageCorridor source,
            PhysicalPassageCorridor target,PhysicalPassageCorridor opponent,VenueValidationPolicy policy,bool periodicSeam=false,bool allBodyHeadings=false)
        {
            if(map==null||body==null||source==null||target==null||opponent==null||policy==null)throw new ArgumentNullException();
            map.Validate(sourcePoint);map.Validate(targetPoint);
            if(map.SameLoop(sourcePoint,targetPoint)&&!periodicSeam)throw new ArgumentException("Explicit seam required.");
            this.map=map;this.sourcePoint=sourcePoint;this.targetPoint=targetPoint;this.opponent=opponent;completePeriodicGeometry=true;
            Point(source,sourcePoint,true,out x,out double sy,out z,out fx,out fz,true);
            Point(target,targetPoint,false,out tx,out double ty,out tz,out nx,out nz,true);
            double dx=tx-x,dy=ty-sy,dz=tz-z;
            if(Math.Sqrt(dx*dx+dy*dy+dz*dz)>policy.SeamTolerance)throw new ArgumentException("Captured complete seam is not coincident.");
            double cross=fx*nz-fz*nx,dot=fx*nx+fz*nz;turn=Math.Atan2(cross,dot);ambiguous=cross==0&&dot<0;
            radius=Math.Sqrt(Math.Pow(body.LongitudinalSeparation*.5,2)+Math.Pow(body.LateralSeparation*.5,2));
            halfLength=allBodyHeadings?radius:body.LongitudinalSeparation*.5;
            halfWidth=allBodyHeadings?radius:body.LateralSeparation*.5;
            support=new PhysicalApronBodySupportDraft(opponent,opponent,body,policy,allBodyHeadings,true);
            margin=Math.Max(new PhysicalPassageWorldProjection(source,target,body,policy).ProjectionMarginMeters,support.ProjectionMarginMeters);
        }

        public bool Conflicts(double lateralBefore, double lateralAfter, PhysicalOccupancySweep other)
        {
            Require(lateralBefore); Require(lateralAfter);
            if (!Contains(other)) return true;
            if (Separated(1, 0) || Separated(0, 1) || Separated(fx, fz) || Separated(-fz, fx) || Separated(nx, nz) || Separated(-nz, nx)) return false;
            for (int i = 1; i < opponent.Count; i++)
            {
                var a = opponent.Get(i - 1); var b = opponent.Get(i); Direction(b.X - a.X, b.Z - a.Z, out double ox, out double oz);
                if (Separated(ox, oz) || Separated(-oz, ox)) return false;
            }
            return true;
            bool Separated(double ax, double az)
            {
                Bounds(lateralBefore, lateralAfter, ax, az, out double low, out double high);
                support.Bounds(other, ax, az, out double otherLow, out double otherHigh);
                double lateral = Math.Max(Math.Max(Math.Abs(lateralBefore), Math.Abs(lateralAfter)), Math.Max(Math.Abs(other.LateralBeforeMeters), Math.Abs(other.LateralAfterMeters)));
                double reserve = margin + 32 * Epsilon * lateral;
                return high + reserve < otherLow - reserve || otherHigh + reserve < low - reserve;
            }
        }

        public bool Conflicts(double lateralBefore, double lateralAfter, PhysicalApronSeamProjectionDraft other,
            double otherLateralBefore, double otherLateralAfter)
        {
            Require(lateralBefore); Require(lateralAfter); Require(otherLateralBefore); Require(otherLateralAfter);
            if (other == null || !ReferenceEquals(map, other.map)) return true;
            return !(Separated(1, 0) || Separated(0, 1) || Separated(fx, fz) || Separated(-fz, fx) || Separated(nx, nz) || Separated(-nz, nx) ||
                Separated(other.fx, other.fz) || Separated(-other.fz, other.fx) || Separated(other.nx, other.nz) || Separated(-other.nz, other.nx));
            bool Separated(double ax, double az)
            {
                Bounds(lateralBefore, lateralAfter, ax, az, out double low, out double high);
                other.Bounds(otherLateralBefore, otherLateralAfter, ax, az, out double otherLow, out double otherHigh);
                double lateral = Math.Max(Math.Max(Math.Abs(lateralBefore), Math.Abs(lateralAfter)), Math.Max(Math.Abs(otherLateralBefore), Math.Abs(otherLateralAfter)));
                double reserve = Math.Max(margin, other.margin) + 32 * Epsilon * lateral;
                return high + reserve < otherLow - reserve || otherHigh + reserve < low - reserve;
            }
        }

        // Public for independent mathematical validation; using these bounds grants no admission.
        public void Bounds(double lateralBefore, double lateralAfter, double ax, double az, out double low, out double high)
        {
            Require(lateralBefore); Require(lateralAfter); Require(ax); Require(az);
            if (ax == 0 && az == 0) throw new ArgumentException("A nonzero projection axis is required.");
            double firstOrigin = x * ax + z * az, secondOrigin = tx * ax + tz * az;
            double originLow = Math.Min(firstOrigin, secondOrigin), originHigh = Math.Max(firstOrigin, secondOrigin), minimum = double.MaxValue, maximum = double.MinValue;
            AddOffset(lateralBefore); AddOffset(lateralAfter); low = minimum; high = maximum;
            void AddOffset(double lateral)
            {
                if (ambiguous)
                {
                    // Both half-turn directions are possible. A full radial support about
                    // the seam encloses every centre-normal and rectangular corner orientation.
                    double reach = (radius + Math.Abs(lateral)) * Math.Sqrt(ax * ax + az * az);
                    AddOffsetValue(-reach); AddOffsetValue(reach); return;
                }
                double forward = fx * ax + fz * az, normal = fz * ax - fx * az;
                double targetForward = nx * ax + nz * az, targetNormal = nz * ax - nx * az;
                for (int along = -1; along <= 1; along += 2) for (int across = -1; across <= 1; across += 2)
                {
                    double longitudinal = along * halfLength, offset = lateral + across * halfWidth;
                    double a = longitudinal * forward + offset * normal, b = offset * forward - longitudinal * normal;
                    AddOffsetValue(a); AddOffsetValue(longitudinal * targetForward + offset * targetNormal);
                    double peak = Math.Atan2(b, a), trough = Math.Atan2(-b, -a), reach = Math.Sqrt(a * a + b * b);
                    if (Inside(peak)) AddOffsetValue(reach); if (Inside(trough)) AddOffsetValue(-reach);
                }
            }
            bool Inside(double angle) => turn >= 0 ? angle >= 0 && angle <= turn : angle <= 0 && angle >= turn;
            void AddOffsetValue(double value) { Add(originLow + value); Add(originHigh + value); }
            void Add(double value) { minimum = Math.Min(minimum, value); maximum = Math.Max(maximum, value); }
        }

        private bool Contains(PhysicalOccupancySweep s)
        {
            try { map.Validate(s.Start); } catch (ArgumentException) { return false; }
            long low = Nm(opponent.Start), high = Nm(opponent.End);
            return s.Start.LoopId == opponent.LoopId && s.Start.LocalNanometers >= low && s.TravelNanometers <= high - s.Start.LocalNanometers &&
                (!s.Start.Closed || s.Start.LocalNanometers + s.TravelNanometers < s.Start.LoopLengthNanometers||
                    completePeriodicGeometry&&s.Start.LocalNanometers+s.TravelNanometers==s.Start.LoopLengthNanometers);
        }
        // A selected path may cross a seam outside this optional apron window. Keep that
        // cache absent and the existing exclusive policy; never extrapolate a seam tangent.
        internal static bool HasCompleteIncidentWindow(PhysicalSharedApronDefinition definition, PhysicalTrackPoint point, bool incoming)
        {
            if (definition == null) return false;
            for (int i = 0; i < definition.CorridorCount; i++)
            {
                var c = definition.GetCorridor(i);
                if (c.LoopId != point.LoopId) continue;
                double at = point.LocalNanometers / (double)SkiState.NanometersPerMeter;
                return at >= c.Start && at <= c.End && !(incoming && at == c.Start) && !(!incoming && at == c.End);
            }
            return false;
        }
        private static void Point(PhysicalPassageCorridor c, PhysicalTrackPoint point, bool incoming, out double x, out double y, out double z, out double fx, out double fz,bool periodicIncoming=false)
        {
            double at = point.LocalNanometers / (double)SkiState.NanometersPerMeter;
            if(periodicIncoming&&incoming&&point.Closed&&at==0&&c.Start==0&&c.End==point.LoopLengthNanometers/(double)SkiState.NanometersPerMeter)at=c.End;
            if (at < c.Start || at > c.End || incoming && at == c.Start || !incoming && at == c.End) throw new ArgumentException("Complete incident seam tangent required.");
            int e = 1;
            while (e < c.Count - 1 && (incoming ? c.Get(e).Distance < at : c.Get(e).Distance <= at)) e++;
            var a = c.Get(e - 1); var b = c.Get(e); double t = (at - a.Distance) / (b.Distance - a.Distance);
            x = a.X + (b.X - a.X) * t; y = a.Y + (b.Y - a.Y) * t; z = a.Z + (b.Z - a.Z) * t; Direction(b.X - a.X, b.Z - a.Z, out fx, out fz);
        }
        private static void Direction(double x, double z, out double fx, out double fz)
        { double scale = Math.Max(Math.Abs(x), Math.Abs(z)); x /= scale; z /= scale; double length = Math.Sqrt(x * x + z * z); fx = x / length; fz = z / length; }
        private static long Nm(double d) => checked(TrackDistance.FromMeters(d).Millimeters * SkiState.NanometersPerMillimeter);
        private static void Require(double value) => PhysiologyNumbers.RequireRange(value, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(value));
    }
}
