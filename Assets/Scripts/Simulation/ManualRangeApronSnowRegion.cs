using System;
using System.Collections.Generic;
using Biathlon.Domain;

namespace Biathlon.Simulation
{
    /// <summary>Guaranteed interior of the venue's existing continuous, flat range apron.
    /// This proves snow under a complete body; it cannot select a mat or advance a phase.</summary>
    public sealed class ManualRangeApronSnowRegion
    {
        public double MinX {get;} public double MaxX {get;}
        public double MinZ {get;} public double MaxZ {get;}
        public string ContextJson {get;}
        internal string TrackJson {get;} internal string VisualJson {get;} internal string PathsJson {get;}
        internal string BodyJson {get;}
        private readonly double radius,halfLength,halfWidth,edge;
        public ManualRangeApronSnowRegion(VenueAuthoringPlan venue)
        {
            if(venue==null||!venue.UsesPhysicalPaths||venue.SharedApron==null||venue.Manifest.Range.Forward.X!=0||venue.Manifest.Range.Forward.Z!=1)
                throw new ArgumentException("The bound cardinal physical shared range apron is required.");
            // The existing draw rectangle includes the widths of every private
            // mat ribbon, not just their centres. Reproduce its cold float
            // vertex arithmetic so admission never invents extra visible snow.
            float minX=float.PositiveInfinity,maxX=float.NegativeInfinity;
            float minZ=(float)(venue.Manifest.Range.Origin.Z-venue.Manifest.Range.ApronDepth),maxZ=(float)venue.Manifest.Range.Origin.Z;
            for(int lane=0;lane<venue.Paths.Count;lane++)
            {
                string id=venue.Paths.GetLane(lane).MatAnchor.LoopId;var loop=venue.Track.GetLoop(id);var path=venue.Visual.Get(id);
                var sampler=new TrackSampler(loop);var distances=new SortedSet<double>{0,loop.Length.Meters};
                for(int i=0;i<path.Count;i++)distances.Add(path.Get(i).Distance);
                for(int i=0;i<loop.SegmentCount;i++)distances.Add(loop.GetSegment(i).StartDistance.Meters);
                int steps=(int)Math.Ceiling(loop.Length.Meters/venue.Visual.SampleSpacing);
                if(steps>100000)throw new ArgumentException("Bounded apron sampling required.");
                for(int i=1;i<steps;i++)distances.Add(loop.Length.Meters*i/steps);
                foreach(double d in distances)
                {
                    var p=path.Sample(d);float half=(float)(sampler.Sample(TrackDistance.FromMeters(Math.Round(d,3,MidpointRounding.ToEven))).WidthMeters/2);
                    if((float)p.Y!=(float)venue.Manifest.Range.Origin.Y)throw new ArgumentException("The shared apron must be flat.");
                    float x=(float)p.X,z=(float)p.Z,sx=(float)p.ForwardZ*half,sz=(float)-p.ForwardX*half;
                    minX=Math.Min(minX,Math.Min(x-sx,x+sx));maxX=Math.Max(maxX,Math.Max(x-sx,x+sx));
                    minZ=Math.Min(minZ,Math.Min(z-sz,z+sz));maxZ=Math.Max(maxZ,Math.Max(z-sz,z+sz));
                }
            }
            MinX=minX;MaxX=maxX;MinZ=minZ;MaxZ=maxZ;
            var body=venue.Occupancy;
            halfLength=body.LongitudinalSeparation*.5;halfWidth=body.LateralSeparation*.5;edge=body.EdgeClearance;
            radius=.5*Math.Sqrt(body.LongitudinalSeparation*body.LongitudinalSeparation+body.LateralSeparation*body.LateralSeparation)+body.EdgeClearance;
            TrackJson=venue.Track.ToJson();VisualJson=venue.Visual.ToJson();PathsJson=venue.Paths.ToJson();BodyJson=body.ToJson();
            ContextJson=ShootingModelDefinitions.EncodeContext(new[]{"manual_range_apron_snow_interior_v2",TrackJson,venue.Manifest.ToJson(),VisualJson,PathsJson,venue.SharedApron.ToJson(),BodyJson,
                "same_original_flat_draw_rectangle_and_native_vertex_arithmetic;continuous_full_body_margin;range_moving_occurrences_only;unchanged_phase_gates_and_projection"});
        }
        internal double Clearance(ManualSkiHeadingState pose)
        {
            double fx=Math.Sin(pose.HeadingRadians),fz=Math.Cos(pose.HeadingRadians);
            double ex=Math.Abs(fx)*halfLength+Math.Abs(fz)*halfWidth+edge;
            double ez=Math.Abs(fz)*halfLength+Math.Abs(fx)*halfWidth+edge;
            return Math.Min(Math.Min(pose.X-ex-MinX,MaxX-pose.X-ex),Math.Min(pose.Z-ez-MinZ,MaxZ-pose.Z-ez));
        }
        public bool Clears(ManualSkiWorldTrajectory trajectory,ManualRacePathKind kind)
        {
            if(!trajectory.IsValid||(kind!=ManualRacePathKind.RangeApproach&&kind!=ManualRacePathKind.RangeExit))return false;
            double h=trajectory.DurationSeconds*.5;var mid=trajectory.Sample(.5);var v=trajectory.VelocityAt(.5);
            double movement=Math.Sqrt(v.X*v.X+v.Z*v.Z)*h+.5*trajectory.PlanarAccelerationBound*h*h+2*trajectory.PositionErrorBoundMeters;
            double reach=radius+movement;
            return mid.X-reach>MinX&&mid.X+reach<MaxX&&mid.Z-reach>MinZ&&mid.Z+reach<MaxZ;
        }
    }
}
