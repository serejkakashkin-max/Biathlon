using System;
using System.Collections.Generic;
using Biathlon.Domain;
namespace Biathlon.Simulation
{
    public sealed class PhysicalJunctionMap
    {
        private readonly PhysicalTrackMap map;
        private readonly PhysicalJunctionDefinition definition;
        private readonly PhysicalTrackPoint[][] anchors;
        private readonly HashSet<string> activeLoops;
        private readonly bool[] activeRegions;
        public bool UsesRouteBinding=>activeLoops!=null;
        public string UsageContextJson {get;}
        public int ActiveRegionCount {get;}
        public bool IsRegionActive(int index)=>activeRegions[index];
        public PhysicalJunctionMap(PhysicalTrackMap map,PhysicalJunctionDefinition definition,string[] usedLoops=null)
        {
            this.map=map??throw new ArgumentNullException(nameof(map));this.definition=definition??throw new ArgumentNullException(nameof(definition));
            if(map.TrackSnapshotJson!=definition.TrackSnapshotJson) throw new ArgumentException("Junction geometry must bind the exact physical track.");
            if(usedLoops!=null)
            {
                if(usedLoops.Length==0)throw new ArgumentException("An explicit nonempty physical route usage is required.");
                activeLoops=new HashSet<string>(StringComparer.Ordinal);var captured=(string[])usedLoops.Clone();
                foreach(string id in captured){map.MapLoop(id,0);if(!activeLoops.Add(id))throw new ArgumentException("Duplicate physical route usage.");}
                Array.Sort(captured,StringComparer.Ordinal);UsageContextJson=ShootingModelDefinitions.EncodeContext(captured);
            }
            anchors=new PhysicalTrackPoint[definition.Count][];activeRegions=new bool[definition.Count];int regionCount=0;
            for(int i=0;i<anchors.Length;i++)
            {
                var junction=definition.Get(i);anchors[i]=new PhysicalTrackPoint[junction.PortCount];
                int usedPorts=0;
                for(int j=0;j<junction.PortCount;j++)
                { var port=junction.GetPort(j);anchors[i][j]=map.MapLoop(port.Anchor.LoopId,port.Anchor.Distance.Millimeters*SkiState.NanometersPerMillimeter);if(activeLoops==null||activeLoops.Contains(port.Anchor.LoopId))usedPorts++; }
                // Preserve self-crossing regions with two ports on the same used loop. A fork
                // to a physically unreachable format branch has only one used incident port.
                activeRegions[i]=activeLoops==null||usedPorts>=2;if(activeRegions[i])regionCount++;
            }
            ActiveRegionCount=regionCount;
        }
        public bool Contains(int junctionIndex,PhysicalTrackPoint point)
        {
            ValidateUsage(point);var junction=definition.Get(junctionIndex);if(!activeRegions[junctionIndex])return false;
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
            ValidateUsage(start);
            if(travelNanometers<0 || halfBodyNanometers<0 || halfBodyNanometers>start.LoopLengthNanometers ||
                (!start.Closed && travelNanometers>start.LoopLengthNanometers-start.LocalNanometers))
                throw new ArgumentOutOfRangeException();
            var junction=definition.Get(junctionIndex);if(!activeRegions[junctionIndex])return false;
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
            ValidateUsage(a);ValidateUsage(b);
            for(int i=0;i<definition.Count;i++) if(Contains(i,a) && Contains(i,b)) return true;
            return false;
        }
        private void ValidateUsage(PhysicalTrackPoint point)
        {map.Validate(point);if(activeLoops!=null&&!activeLoops.Contains(point.LoopId))throw new ArgumentException("A point on an unbound physical branch cannot bypass junction safety.");}
        public static string[] CaptureUsage(TrackDefinition track,RaceRoute route,PhysicalRangeFieldLayout layout,string startLoop=null)
        {
            if(track==null||route==null||track.ToJson()!=route.TrackSnapshotJson||layout!=null&&!layout.IsBoundTo(track,route))throw new ArgumentException("Exact route, track and layout required.");
            var used=new HashSet<string>(StringComparer.Ordinal);
            for(int i=0;i<route.SectionCount;i++)used.Add(route.GetSection(i).LoopId);
            used.Add(track.Penalty.LoopId);
            if(layout!=null)for(int p=0;p<layout.Count;p++)for(int s=0;s<layout.Definition.GetLane(p).SliceCount;s++)used.Add(layout.Definition.GetLane(p).GetSlice(s).LoopId);
            if(startLoop!=null){track.GetLoop(startLoop);used.Add(startLoop);}
            var result=new string[used.Count];used.CopyTo(result);Array.Sort(result,StringComparer.Ordinal);return result;
        }
    }
}

