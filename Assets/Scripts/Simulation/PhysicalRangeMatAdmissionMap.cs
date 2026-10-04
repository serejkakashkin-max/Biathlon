using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Optional physical shooting-stop admission. It neither changes a line nor
    /// controls steering: an off-mat arrival retains an ordinary paid prefix before the stop.</summary>
    public sealed class PhysicalRangeMatAdmissionMap
    {
        private readonly PhysicalRangeFieldLayout layout;
        private readonly VenueRangeMatDefinition mat;
        private readonly OccupancyDefinition body;
        private readonly double[] lateralFromPathLine, forwardFromPathLine;
        private readonly long[,] firing;
        private readonly double tickSeconds;
        private readonly ShootingStance[] stances;
        public string ContextJson { get; }
        public PhysicalRangeMatAdmissionMap(TrackDefinition track, RaceRoute route, CompetitionProfile profile,
            PhysicalRangeFieldLayout layout, TrackVisualDefinition visual, OccupancyDefinition body,
            VenueRangeMatDefinition mat, double firingForwardX, double firingForwardZ, double tickSeconds)
        {
            if (track == null || route == null || profile == null || layout == null || visual == null || body == null || mat == null) throw new ArgumentNullException();
            if (!layout.IsBoundTo(track, route) || visual.TrackId != track.Id || route.RequirementsSnapshotJson != profile.ToRouteRequirements().ToJson())
                throw new ArgumentException("Exact shooting stop layout, route and profile required.");
            mat.Validate(body); FractionalMotionTimeline.Quantize(0, tickSeconds);
            PhysiologyNumbers.RequireRange(firingForwardX, -1, 1, nameof(firingForwardX)); PhysiologyNumbers.RequireRange(firingForwardZ, -1, 1, nameof(firingForwardZ));
            double norm = Math.Sqrt(firingForwardX * firingForwardX + firingForwardZ * firingForwardZ);
            if (Math.Abs(norm - 1) > 1e-9) throw new ArgumentException("An authored unit shooting direction is required.");
            this.layout = layout; this.mat = mat; this.body = body; this.tickSeconds = tickSeconds;
            stances=new ShootingStance[profile.BoutCount];
            for(int bout=0;bout<stances.Length;bout++)stances[bout]=profile.GetBout(bout).Stance;
            lateralFromPathLine = new double[layout.Count]; forwardFromPathLine = new double[layout.Count]; firing = new long[layout.Count, profile.BoutCount];
            for (int p = 0; p < layout.Count; p++)
            {
                var path = layout.GetPath(p); var at = visual.Get(path.MatAnchor.LoopId).Sample(path.MatAnchor.Distance.Meters);
                // The actual committed center is anchor + native path-normal * accepted line.
                double normalX = at.ForwardZ, normalZ = -at.ForwardX;
                lateralFromPathLine[p] = normalX * -firingForwardZ + normalZ * firingForwardX;
                forwardFromPathLine[p] = normalX * firingForwardX + normalZ * firingForwardZ;
                for (int b = 0; b < profile.BoutCount; b++) firing[p, b] = path.FiringRouteNanometers(profile.GetBout(b).AfterLap - 1);
            }
            ContextJson = ShootingModelDefinitions.EncodeContext(new[] { "physical_mat_admission_v1", layout.ContextJson, visual.ToJson(), body.ToJson(), mat.ToJson(),
                profile.ToJson(), firingForwardX.ToString("R", System.Globalization.CultureInfo.InvariantCulture), firingForwardZ.ToString("R", System.Globalization.CultureInfo.InvariantCulture),
                tickSeconds.ToString("R", System.Globalization.CultureInfo.InvariantCulture), "actual_path_right_normal_center_plus_fz_minus_fx;whole_configured_body_final_shooting_heading;fractional_arrival_and_final_line;off_mat_paid_prefix_before_native_mm_stop;no_line_clamp_snap_or_input_automation" });
            if(mat.ProneSupport!=null)ContextJson=ShootingModelDefinitions.EncodeContext(new[]{ContextJson,
                "optional_prone_torso_elbow_boot_support_plus_unchanged_body;profile_bout_stance;no_glove_rifle_extent;paid_prefix_steering_only"});
        }
        public bool ContainsBody(int path, double acceptedLine)
        {
            PhysiologyNumbers.RequireRange(acceptedLine, -PhysiologyNumbers.MaximumMagnitude, PhysiologyNumbers.MaximumMagnitude, nameof(acceptedLine));
            return mat.ContainsBody(lateralFromPathLine[path] * acceptedLine, forwardFromPathLine[path] * acceptedLine, body);
        }
        public bool ContainsBodyAndSupports(int path,int bout,double acceptedLine)
        {
            if(bout<0||bout>=stances.Length)throw new ArgumentOutOfRangeException(nameof(bout));
            PhysiologyNumbers.RequireRange(acceptedLine,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(acceptedLine));
            return mat.ContainsBodyAndSupports(lateralFromPathLine[path]*acceptedLine,forwardFromPathLine[path]*acceptedLine,body,stances[bout]);
        }
        public long FiringRouteNanometers(int path, int bout) => firing[path, bout];
        public long MaximumAcceptedTravel(int path, int bout, RaceMotionPoint initial, ReadOnlySpan<RaceMotionSegment> trajectory,
            double lateralBefore, double lateralAfter, bool reachesFiringLine)
        {
            var terminal = FractionalMotionTimeline.Sample(trajectory, initial, JunctionAdmissionSession.FractionParts, tickSeconds);
            long available = terminal.TravelNanometers - initial.TravelNanometers;
            if (!reachesFiringLine) return available;
            long stop = firing[path, bout];
            if (initial.RouteNanometers >= stop) throw new ArgumentException("An off-mat stop must never have been published before admission.");
            long fraction = -1;
            for (int i = 0; i < trajectory.Length; i++)
            {
                var s = trajectory[i]; if (s.OnPenaltyLoop || s.OnStartApproach) continue;
                if (s.RouteBefore <= stop && s.RouteAfter >= stop)
                {
                    long start = FractionalMotionTimeline.Quantize(s.StartSeconds, tickSeconds), end = FractionalMotionTimeline.Quantize(s.EndSeconds, tickSeconds);
                    long distance = s.RouteAfter - s.RouteBefore;
                    if (distance <= 0) throw new ArgumentException("A firing arrival requires positive actual route travel.");
                    // First native integer fraction reaching the stop, matching accepted arrival arithmetic.
                    decimal numerator = (decimal)(stop - s.RouteBefore) * (end - start);
                    fraction = checked(start + (long)Math.Ceiling(numerator / distance)); break;
                }
            }
            if (fraction < 0 || fraction > JunctionAdmissionSession.FractionParts || terminal.RouteNanometers != stop)
                throw new ArgumentException("A complete exact firing-arrival receipt is required.");
            double arrivalLine = lateralBefore + (lateralAfter - lateralBefore) * fraction / JunctionAdmissionSession.FractionParts;
            if (ContainsBodyAndSupports(path,bout,arrivalLine) && ContainsBodyAndSupports(path,bout,lateralAfter)) return available;
            // Use the already established whole-mm prefix stop quantum; no uncounted setup
            // displacement, tiny loop, hidden centering or narrower collision corridor is added.
            long beforeStop = Math.Max(initial.RouteNanometers, stop - SkiState.NanometersPerMillimeter);
            for (int i = 0; i < trajectory.Length; i++)
            {
                var s = trajectory[i];
                if (s.OnPenaltyLoop || s.OnStartApproach) continue;
                if (s.RouteBefore <= beforeStop && s.RouteAfter >= beforeStop)
                    return Math.Min(available, checked(s.TravelBefore - initial.TravelNanometers + beforeStop - s.RouteBefore));
            }
            if (beforeStop == initial.RouteNanometers) return 0;
            throw new ArgumentException("The complete paid prefix before the physical stop is required.");
        }
        public long MaximumAcceptedTravel(SprintAthleteSession actor, SprintStepPreview preview, int path, double lateralBefore, double lateralAfter,
            Span<RaceMotionSegment> callerScratch)
        {
            if (actor == null || !ReferenceEquals(actor.GetPreparedRangePath(preview), layout.GetPath(path))) throw new ArgumentException("Exact owned prepared shooting path required.");
            if (!preview.MovementActive)
            {
                if (preview.ShootingActive && !ContainsBodyAndSupports(path,actor.Lifecycle.CompletedBouts,lateralAfter)) throw new InvalidOperationException("A committed shooting body is outside its physical mat.");
                return 0;
            }
            if (callerScratch.Length < preview.Movement.SegmentCount) throw new ArgumentException("Complete caller-owned receipt capacity required.");
            for (int i = 0; i < preview.Movement.SegmentCount; i++) callerScratch[i] = actor.GetPreparedSegment(preview, i);
            return MaximumAcceptedTravel(path, actor.Lifecycle.CompletedBouts, new RaceMotionPoint(actor.RouteNanometers, actor.Motion.DistanceNanometers),
                callerScratch.Slice(0, preview.Movement.SegmentCount), lateralBefore, lateralAfter, preview.Movement.AtFiringLine);
        }
    }
}
