using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public enum TrackBoundaryMode { Reject, Clamp, Wrap }
    public readonly struct TrackSample
    {
        public string LoopId { get; }
        public string SegmentId { get; }
        public int SegmentIndex { get; }
        public TrackDistance Distance { get; }
        public double SegmentFraction { get; }
        public double Grade { get; }
        public double CurvaturePerMeter { get; }
        public double WidthMeters { get; }
        public string SnowProfileId { get; }
        public double WindExposure { get; }
        public double TerrainMuscleFactor { get; }
        public bool DraftAllowed { get; }
        public bool OvertakeAllowed { get; }
        public double RiskLevel { get; }
        public TrackSemanticZone SemanticZone { get; }
        internal TrackSample WithTravelGeometry(double grade,double curvature)
        {
            if(string.IsNullOrEmpty(SegmentId))throw new ArgumentException("Initialized surface metadata required.");
            PhysiologyNumbers.RequireRange(grade,-1,1,nameof(grade));
            PhysiologyNumbers.RequireRange(curvature,-PhysiologyNumbers.MaximumMagnitude,PhysiologyNumbers.MaximumMagnitude,nameof(curvature));
            return new TrackSample(this,grade,curvature);
        }
        private TrackSample(TrackSample source,double grade,double curvature)
        { this=source;Grade=grade;CurvaturePerMeter=curvature; }
        internal TrackSample(TrackLoopDefinition loop, TrackSegmentDefinition segment, int index, long millimeters)
        {
            LoopId = loop.Id; SegmentId = segment.Id; SegmentIndex = index;
            Distance = new TrackDistance(millimeters);
            SegmentFraction = (millimeters - segment.StartDistance.Millimeters) / (double)segment.Length.Millimeters;
            Grade = segment.StartGrade + (segment.EndGrade - segment.StartGrade) * SegmentFraction;
            CurvaturePerMeter = segment.CurvaturePerMeter; WidthMeters = segment.WidthMeters;
            SnowProfileId = segment.SnowProfileId; WindExposure = segment.WindExposure;
            TerrainMuscleFactor = segment.TerrainMuscleFactor; DraftAllowed = segment.DraftAllowed;
            OvertakeAllowed = segment.OvertakeAllowed; RiskLevel = segment.RiskLevel; SemanticZone = segment.SemanticZone;
        }
    }
    public sealed class TrackSampler
    {
        private readonly TrackLoopDefinition loop;
        private readonly long[] endMillimeters;
        public TrackSampler(TrackLoopDefinition loop)
        {
            this.loop = loop ?? throw new ArgumentNullException(nameof(loop));
            endMillimeters = new long[loop.SegmentCount];
            for (int i = 0; i < endMillimeters.Length; i++)
            {
                var segment = loop.GetSegment(i);
                endMillimeters[i] = checked(segment.StartDistance.Millimeters + segment.Length.Millimeters);
            }
        }
        public TrackSample Sample(TrackDistance distance, TrackBoundaryMode boundaryMode = TrackBoundaryMode.Reject)
        {
            long mm = distance.Millimeters;
            switch (boundaryMode)
            {
                case TrackBoundaryMode.Reject:
                    if (mm > loop.Length.Millimeters) throw new ArgumentOutOfRangeException(nameof(distance));
                    break;
                case TrackBoundaryMode.Clamp:
                    mm = Math.Min(mm, loop.Length.Millimeters);
                    break;
                case TrackBoundaryMode.Wrap:
                    if (!loop.Closed) throw new ArgumentException("Cannot wrap an open loop.", nameof(boundaryMode));
                    mm %= loop.Length.Millimeters;
                    break;
                default: throw new ArgumentOutOfRangeException(nameof(boundaryMode));
            }
            int low = 0, high = endMillimeters.Length - 1;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                if (mm < endMillimeters[middle]) high = middle;
                else low = middle + 1;
            }
            return new TrackSample(loop, loop.GetSegment(low), low, mm);
        }
    }
}
