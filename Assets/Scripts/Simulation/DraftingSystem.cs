using System;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    public readonly struct DraftOutput
    {
        public AthleteId LeaderId { get; }
        public double Strength { get; }
        public double DragReduction { get; }
        public double DragAreaMultiplier => 1 - DragReduction;
        public double GapMeters { get; }
        public double LateralWakeOffsetMeters { get; }
        internal DraftOutput(AthleteId leader, double strength, double reduction, double gap, double wakeOffset)
        { LeaderId = leader; Strength = strength; DragReduction = reduction; GapMeters = gap; LateralWakeOffsetMeters = wakeOffset; }
    }
    public sealed class DraftingSystem
    {
        private readonly DraftDefinition definition;
        public DraftingSystem(DraftDefinition definition)
        { this.definition = definition ?? throw new ArgumentNullException(nameof(definition)); }
        public DraftOutput Evaluate(SkiPlacement follower, ReadOnlySpan<SkiPlacement> participants, TrackSample track,
            double headwindMetersPerSecond = 0, double crosswindMetersPerSecond = 0)
        {
            if (!follower.IsValid) throw new ArgumentException("Invalid follower.", nameof(follower));
            if (string.IsNullOrEmpty(track.SegmentId)) throw new ArgumentException("A valid track sample is required.", nameof(track));
            PhysiologyNumbers.RequireRange(headwindMetersPerSecond, -definition.MaximumWind, definition.MaximumWind, nameof(headwindMetersPerSecond));
            PhysiologyNumbers.RequireRange(crosswindMetersPerSecond, -definition.MaximumWind, definition.MaximumWind, nameof(crosswindMetersPerSecond));
            var best = default(DraftOutput);
            double head = headwindMetersPerSecond * track.WindExposure;
            double cross = crosswindMetersPerSecond * track.WindExposure;
            double windFactor = Math.Min(definition.MaximumWindFactor, Math.Max(definition.MinimumWindFactor,
                1 + definition.HeadwindSensitivity * head / definition.ReferenceSpeed));
            for (int i = 0; i < participants.Length; i++)
            {
                var leader = participants[i];
                if (!leader.IsValid) throw new ArgumentException("Invalid drafting participant.", nameof(participants));
                if (!track.DraftAllowed || leader.AthleteId.Equals(follower.AthleteId) ||
                    !string.Equals(leader.CorridorId, follower.CorridorId, StringComparison.Ordinal)) continue;
                long delta = leader.Motion.DistanceNanometers - follower.Motion.DistanceNanometers;
                if (delta <= 0) continue;
                double gap = delta / (double)SkiState.NanometersPerMeter;
                if (gap < definition.MinimumGap || gap >= definition.MaximumGap) continue;
                var next=Pair(leader.AthleteId,follower.Motion.SpeedMetersPerSecond,leader.Motion.SpeedMetersPerSecond,
                    gap,follower.LateralMeters-leader.LateralMeters,head,cross,windFactor);
                if(Better(next,best))best=next;
            }
            return best;
        }

        /// <summary>Wake geometry from achieved world positions and velocities.
        /// Every leader's actual travel direction defines its own wake. No
        /// route station, desired lateral line or presentation pose is used.</summary>
        public DraftOutput EvaluateWorld(ManualSkiContactBody follower,ReadOnlySpan<ManualSkiContactBody> participants,
            TrackSample track,double windX=0,double windZ=0)
        {
            if(!follower.IsValid||string.IsNullOrEmpty(track.SegmentId))throw new ArgumentException("Initialized world body and surface required.");
            PhysiologyNumbers.RequireRange(windX,-definition.MaximumWind,definition.MaximumWind,nameof(windX));
            PhysiologyNumbers.RequireRange(windZ,-definition.MaximumWind,definition.MaximumWind,nameof(windZ));
            PhysiologyNumbers.RequireRange(Math.Sqrt(windX*windX+windZ*windZ),0,definition.MaximumWind,"worldWindMagnitude");
            var best=default(DraftOutput);
            for(int i=0;i<participants.Length;i++)
            {
                var leader=participants[i];if(!leader.IsValid)throw new ArgumentException("Initialized world drafting participants required.");
                if(!track.DraftAllowed||leader.Athlete.Equals(follower.Athlete))continue;
                double speed=Math.Sqrt(leader.Velocity.X*leader.Velocity.X+leader.Velocity.Z*leader.Velocity.Z);
                if(speed==0)continue;
                double fx=leader.Velocity.X/speed,fz=leader.Velocity.Z/speed;
                double followerSpeed=follower.Velocity.X*fx+follower.Velocity.Z*fz;
                if(followerSpeed<=0)continue;
                double dx=leader.Pose.X-follower.Pose.X,dz=leader.Pose.Z-follower.Pose.Z;
                double gap=dx*fx+dz*fz;if(gap<definition.MinimumGap||gap>=definition.MaximumGap)continue;
                double lateral=-dx*fz+dz*fx;
                double head=-(windX*fx+windZ*fz)*track.WindExposure;
                double cross=(windX*fz-windZ*fx)*track.WindExposure;
                double factor=Math.Min(definition.MaximumWindFactor,Math.Max(definition.MinimumWindFactor,
                    1+definition.HeadwindSensitivity*head/definition.ReferenceSpeed));
                var next=Pair(leader.Athlete,followerSpeed,speed,gap,lateral,head,cross,factor);
                if(Better(next,best))best=next;
            }
            return best;
        }
        private static bool Better(DraftOutput candidate,DraftOutput prior)
            =>candidate.Strength>0&&(candidate.Strength>prior.Strength||candidate.Strength==prior.Strength&&
                string.CompareOrdinal(candidate.LeaderId.Value,prior.LeaderId.Value)<0);
        private DraftOutput Pair(AthleteId leader,double followerSpeed,double leaderSpeed,
            double gap,double lateral,double head,double cross,double windFactor)
        {
                double width = definition.WakeHalfWidth + gap * definition.WakeExpansion;
                double wakeShift = cross * gap / Math.Max(leaderSpeed, definition.MinimumFlowSpeed) * definition.CrosswindDisplacementFactor;
                double lateralOffset = lateral - wakeShift;
                double alignment = Math.Max(0, 1 - Math.Abs(lateralOffset) / width);
                double distance = (definition.MaximumGap - gap) / (definition.MaximumGap - definition.MinimumGap);
                distance = distance * distance * (3 - 2 * distance);
                double speed = Math.Min(1, Math.Min(leaderSpeed, followerSpeed) / definition.ReferenceSpeed);
                // No aerodynamic saving to refund while net relative air is pushing from behind.
                double strength = followerSpeed + head <= 0 ? 0 : Math.Min(1, distance * alignment * speed * windFactor);
                return strength>0?new DraftOutput(leader,strength,strength*definition.MaximumDragReduction,gap,lateralOffset):default;
        }
    }
}
