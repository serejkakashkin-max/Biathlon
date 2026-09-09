using System;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public sealed class PhysicalJunctionMap
    {
        private readonly PhysicalTrackMap map;
        private readonly PhysicalJunctionDefinition definition;
        private readonly PhysicalTrackPoint[][] anchors;
        public PhysicalJunctionMap(PhysicalTrackMap map,PhysicalJunctionDefinition definition)
        {
            this.map=map??throw new ArgumentNullException(nameof(map));this.definition=definition??throw new ArgumentNullException(nameof(definition));
            if(map.TrackSnapshotJson!=definition.TrackSnapshotJson) throw new ArgumentException("Junction geometry must bind the exact physical track.");
            anchors=new PhysicalTrackPoint[definition.Count][];
            for(int i=0;i<anchors.Length;i++)
            {
                var junction=definition.Get(i);anchors[i]=new PhysicalTrackPoint[junction.PortCount];
                for(int j=0;j<junction.PortCount;j++)
                { var port=junction.GetPort(j);anchors[i][j]=map.MapLoop(port.Anchor.LoopId,port.Anchor.Distance.Millimeters*SkiState.NanometersPerMillimeter); }
            }
        }
        public bool Contains(int junctionIndex,PhysicalTrackPoint point)
        {
            map.Validate(point);var junction=definition.Get(junctionIndex);
            for(int i=0;i<junction.PortCount;i++)
            {
                var anchor=anchors[junctionIndex][i];if(!map.SameLoop(anchor,point)) continue;
                var port=junction.GetPort(i);long delta=point.LocalNanometers-anchor.LocalNanometers;
                long before=port.GuardBefore.Millimeters*SkiState.NanometersPerMillimeter,after=port.GuardAfter.Millimeters*SkiState.NanometersPerMillimeter;
                if(delta>=-before && delta<=after) return true;
                if(point.Closed && ((delta+point.LoopLengthNanometers>=-before && delta+point.LoopLengthNanometers<=after) ||
                    (delta-point.LoopLengthNanometers>=-before && delta-point.LoopLengthNanometers<=after))) return true;
            }
            return false;
        }
        /// <summary>Conservative whole-body occupancy over a forward sweep on one loop. Travel is
        /// explicit so complete circuits cannot disappear when endpoints have equal coordinates.
        /// A branch-changing trajectory must be split and queried on every incident loop.</summary>
        public bool TouchesForwardSweep(int junctionIndex,PhysicalTrackPoint start,long travelNanometers,long halfBodyNanometers)
        {
            map.Validate(start);
            if(travelNanometers<0 || halfBodyNanometers<0 || halfBodyNanometers>start.LoopLengthNanometers ||
                (!start.Closed && travelNanometers>start.LoopLengthNanometers-start.LocalNanometers))
                throw new ArgumentOutOfRangeException();
            var junction=definition.Get(junctionIndex);
            for(int i=0;i<junction.PortCount;i++)
            {
                var anchor=anchors[junctionIndex][i];if(!map.SameLoop(anchor,start)) continue;
                var port=junction.GetPort(i);
                long before=port.GuardBefore.Millimeters*SkiState.NanometersPerMillimeter+halfBodyNanometers;
                long after=port.GuardAfter.Millimeters*SkiState.NanometersPerMillimeter+halfBodyNanometers;
                if(start.Closed && (travelNanometers>=start.LoopLengthNanometers || before+after>=start.LoopLengthNanometers)) return true;
                long end=start.LocalNanometers+travelNanometers;
                long low=anchor.LocalNanometers-before,high=anchor.LocalNanometers+after;
                if(start.LocalNanometers<=high && end>=low) return true;
                if(start.Closed && ((start.LocalNanometers<=high-start.LoopLengthNanometers && end>=low-start.LoopLengthNanometers) ||
                    (start.LocalNanometers<=high+start.LoopLengthNanometers && end>=low+start.LoopLengthNanometers))) return true;
            }
            return false;
        }
        public bool SharesJunction(PhysicalTrackPoint a,PhysicalTrackPoint b)
        {
            map.Validate(a);map.Validate(b);
            for(int i=0;i<definition.Count;i++) if(Contains(i,a) && Contains(i,b)) return true;
            return false;
        }
    }
}

