using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct RouteSection
    {
        public int LapIndex { get; }
        public string LapId { get; }
        public string LoopId { get; }
        public TrackDistance Start { get; }
        public TrackDistance Length { get; }
        public TrackDistance LoopStart { get; }
        public TrackDistance LoopEnd => new TrackDistance(checked(LoopStart.Millimeters + Length.Millimeters));
        public string EnterConnectionId { get; }
        public bool HasRange { get; }
        internal RouteSection(int lapIndex, string lapId, string loopId, long start, TrackDistance length, bool hasRange,
            TrackDistance loopStart = default, string enterConnectionId = null)
        { LapIndex = lapIndex; LapId = lapId; LoopId = loopId; Start = new TrackDistance(start); Length = length; HasRange = hasRange;
          LoopStart = loopStart; EnterConnectionId = enterConnectionId; }
        public bool Contains(TrackDistance local) => local.Millimeters >= LoopStart.Millimeters && local.Millimeters <= LoopEnd.Millimeters;
        public TrackDistance RouteDistanceOf(TrackDistance local)
        {
            if (!Contains(local)) throw new ArgumentOutOfRangeException(nameof(local));
            return new TrackDistance(checked(Start.Millimeters + local.Millimeters - LoopStart.Millimeters));
        }
    }
    public readonly struct RaceRouteSample
    {
        public int LapIndex { get; }
        public int SectionIndex { get; }
        public TrackDistance RouteDistance { get; }
        public TrackSample Track { get; }
        internal RaceRouteSample(int lap, int section, TrackDistance distance, TrackSample track)
        { LapIndex = lap; SectionIndex = section; RouteDistance = distance; Track = track; }
    }
    public sealed class RaceRoute
    {
        private readonly RouteSection[] sections;
        private readonly TrackSampler[] samplers;
        private readonly long[] lapStarts;
        private readonly long[] lapLengths;
        public string Id { get; }
        public string TrackId { get; }
        public string TrackSnapshotJson { get; }
        public string CompetitionProfileId { get; }
        public string PlanSnapshotJson { get; }
        public string RequirementsSnapshotJson { get; }
        public int SchemaVersion { get; }
        public TrackDistance Length { get; }
        public int SectionCount => sections.Length;
        public int LapCount => lapStarts.Length;
        public RouteSection GetSection(int index) => sections[index];
        public TrackDistance GetLapStart(int index) => new TrackDistance(lapStarts[index]);
        public TrackDistance GetLapLength(int index) => new TrackDistance(lapLengths[index]);
        public RaceRoute(TrackDefinition track, RaceRoutePlan plan, CompetitionRouteRequirements requirements,
            PhysicalJunctionDefinition junctions = null, PhysicalConnectionDefinition connections = null)
        {
            if (track == null || plan == null || requirements == null) throw new ArgumentNullException();
            if (plan.TrackId != track.Id || plan.CompetitionProfileId != requirements.Id || plan.LapCount != requirements.LapCount)
                throw new ArgumentException("Route track/profile identity and lap count must match.");
            if (plan.SchemaVersion == 2) PhysicalRoutePlanValidation.Validate(track,plan,requirements,junctions,connections);
            SchemaVersion = plan.SchemaVersion;
            Id = plan.Id; TrackId = track.Id; TrackSnapshotJson = track.ToJson(); CompetitionProfileId = requirements.Id;
            PlanSnapshotJson = plan.ToJson(); RequirementsSnapshotJson = requirements.ToJson();
            lapStarts = new long[plan.LapCount]; lapLengths = new long[plan.LapCount];
            var list = new List<RouteSection>();
            var samplerList = new List<TrackSampler>();
            var cache = new Dictionary<string, TrackSampler>(StringComparer.Ordinal);
            long distance = 0;
            for (int lapIndex = 0; lapIndex < plan.LapCount; lapIndex++)
            {
                var lap = plan.GetLap(lapIndex);
                lapStarts[lapIndex] = distance;
                bool hasRange = false;
                for (int index = 0; index < lap.LoopCount; index++)
                {
                    var loop = track.GetLoop(lap.GetLoopId(index));
                    if (loop.Kind != TrackLoopKind.Race) throw new ArgumentException("Penalty loops cannot contribute to base competition distance.");
                    var slice = SchemaVersion == 2 ? lap.GetSlice(index) : null;
                    var loopStart = slice == null ? default : slice.Start;
                    var loopEnd = slice == null ? loop.Length : slice.End;
                    if (loopEnd.Millimeters > loop.Length.Millimeters) throw new ArgumentException("Route slice exceeds its physical loop.");
                    bool rangeSection = loop.Id == track.Range.Entry.LoopId && loopStart.Millimeters <= track.Range.Entry.Distance.Millimeters &&
                        loopEnd.Millimeters >= Math.Max(track.Range.Exit.Distance.Millimeters, track.Penalty.Rejoin.Distance.Millimeters);
                    hasRange |= rangeSection;
                    var length = new TrackDistance(loopEnd.Millimeters - loopStart.Millimeters);
                    list.Add(new RouteSection(lapIndex, lap.Id, loop.Id, distance, length, rangeSection, loopStart, slice?.EnterConnectionId));
                    if (!cache.TryGetValue(loop.Id, out var sampler)) { sampler = new TrackSampler(loop); cache.Add(loop.Id, sampler); }
                    samplerList.Add(sampler);
                    distance = checked(distance + length.Millimeters);
                }
                lapLengths[lapIndex] = distance - lapStarts[lapIndex];
                if (requirements.RequiresRange(lapIndex) && !hasRange) throw new ArgumentException("A required shooting lap has no range access.");
            }
            if (distance != requirements.Distance.Millimeters) throw new ArgumentException("Composed distance does not match the competition profile.");
            Length = new TrackDistance(distance); sections = list.ToArray(); samplers = samplerList.ToArray();
        }
        public RaceRouteSample Sample(TrackDistance distance, bool clampAtFinish = false)
        {
            long mm = distance.Millimeters;
            if (mm > Length.Millimeters)
            {
                if (!clampAtFinish) throw new ArgumentOutOfRangeException(nameof(distance));
                mm = Length.Millimeters;
            }
            int low = 0, high = sections.Length - 1;
            while (low < high)
            {
                int middle = low + (high - low) / 2;
                long end = sections[middle].Start.Millimeters + sections[middle].Length.Millimeters;
                if (mm < end) high = middle; else low = middle + 1;
            }
            var section = sections[low];
            var sample = samplers[low].Sample(new TrackDistance(section.LoopStart.Millimeters + mm - section.Start.Millimeters));
            return new RaceRouteSample(section.LapIndex, low, new TrackDistance(mm), sample);
        }
    }
}

